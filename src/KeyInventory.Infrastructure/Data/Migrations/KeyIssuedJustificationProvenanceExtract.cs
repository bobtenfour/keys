using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <summary>
/// ONE-TIME migration-only extract of Loan justification from OperatorAudit "Key issued" Details.
/// Not registered in DI as an application service. Invoked only from migration apply.
/// </summary>
internal static class KeyIssuedJustificationProvenanceExtract
{
    internal const string MarkerSql = "SELECT 1; -- KEYINVENTORY:KeyIssuedJustificationProvenanceExtract";

    private static readonly Regex JustificationTailPattern = new(
        "^(Department|Room)/(.+)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static void Apply(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        migrationBuilder.Sql(MarkerSql);
    }

    internal static bool IsMarkerCommand(string commandText)
        => !string.IsNullOrEmpty(commandText)
            && commandText.Contains("KEYINVENTORY:KeyIssuedJustificationProvenanceExtract", StringComparison.Ordinal);

    internal static void Execute(DbConnection connection, DbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        List<AuditIssueRow> audits = LoadKeyIssuedAudits(connection, transaction);
        Dictionary<string, ParsedJustification> byLoan = new(StringComparer.Ordinal);
        foreach (AuditIssueRow audit in audits)
        {
            ParsedJustification parsed = ParseJustificationOrThrow(audit);
            if (byLoan.TryGetValue(audit.LoanCode, out ParsedJustification? existing)
                && existing is not null)
            {
                if (!existing.Equals(parsed))
                {
                    throw new InvalidOperationException(
                        $"Migration stopped: conflicting Key issued justification audits for LoanCode '{audit.LoanCode}'.");
                }

                continue;
            }

            byLoan[audit.LoanCode] = parsed;
        }

        foreach ((string loanCode, ParsedJustification justification) in byLoan)
        {
            ApplyToLoanOrThrow(connection, transaction, loanCode, justification);
        }
    }

    private static List<AuditIssueRow> LoadKeyIssuedAudits(DbConnection connection, DbTransaction transaction)
    {
        using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT SubjectReference, Details
            FROM OperatorAuditRecords
            WHERE ActionType = N'Key issued'
              AND SubjectType = N'Loan'
            ORDER BY OccurredAtUtc, AuditRecordId;
            """;

        List<AuditIssueRow> rows = [];
        using DbDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            rows.Add(new AuditIssueRow(
                reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
        }

        return rows;
    }

    /// <summary>
    /// Test/migration hook: parse the trailing Justification=kind/code segment from Key issued Details.
    /// Returns false when the segment is missing, malformed, or the code contains ';' or '/' .
    /// </summary>
    internal static bool TryParseJustificationSegment(string details, out string kind, out string code)
    {
        kind = string.Empty;
        code = string.Empty;
        if (string.IsNullOrEmpty(details))
        {
            return false;
        }

        int index = details.LastIndexOf("Justification=", StringComparison.Ordinal);
        if (index < 0)
        {
            return false;
        }

        string rest = details[(index + "Justification=".Length)..];
        Match match = JustificationTailPattern.Match(rest);
        if (!match.Success)
        {
            return false;
        }

        string parsedKind = match.Groups[1].Value;
        string parsedCode = match.Groups[2].Value;
        if (string.IsNullOrWhiteSpace(parsedCode)
            || parsedCode.Contains(';', StringComparison.Ordinal)
            || parsedCode.Contains('/', StringComparison.Ordinal))
        {
            return false;
        }

        kind = parsedKind;
        code = parsedCode;
        return true;
    }

    private static ParsedJustification ParseJustificationOrThrow(AuditIssueRow audit)
    {
        string details = audit.Details ?? string.Empty;
        if (!TryParseJustificationSegment(details, out string kind, out string code))
        {
            if (details.LastIndexOf("Justification=", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    $"Migration stopped: Key issued audit for LoanCode '{audit.LoanCode}' has no Justification= segment.");
            }

            int index = details.LastIndexOf("Justification=", StringComparison.Ordinal);
            string rest = details[(index + "Justification=".Length)..];
            Match match = JustificationTailPattern.Match(rest);
            if (!match.Success)
            {
                throw new InvalidOperationException(
                    $"Migration stopped: Key issued audit for LoanCode '{audit.LoanCode}' has invalid Justification segment.");
            }

            string failedCode = match.Groups[2].Value;
            if (failedCode.Contains(';', StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Migration stopped: Justification code for LoanCode '{audit.LoanCode}' contains ';'.");
            }

            if (failedCode.Contains('/', StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Migration stopped: Justification code for LoanCode '{audit.LoanCode}' contains '/' (ambiguous).");
            }

            throw new InvalidOperationException(
                $"Migration stopped: Justification code for LoanCode '{audit.LoanCode}' is empty.");
        }

        return new ParsedJustification(kind, code);
    }

    private static void ApplyToLoanOrThrow(
        DbConnection connection,
        DbTransaction transaction,
        string loanCode,
        ParsedJustification justification)
    {
        using (DbCommand existsCommand = connection.CreateCommand())
        {
            existsCommand.Transaction = transaction;
            existsCommand.CommandText =
                """
                SELECT JustificationKind, JustificationDepartmentId, JustificationDepartmentCodeSnapshot, JustificationRoomCode
                FROM Loans
                WHERE LoanCode = @loanCode;
                """;
            AddParameter(existsCommand, "@loanCode", loanCode);

            using DbDataReader reader = existsCommand.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException(
                    $"Migration stopped: LoanCode '{loanCode}' from Key issued audit was not found.");
            }

            string? existingKind = reader.IsDBNull(0) ? null : reader.GetString(0);
            Guid? existingDepartmentId = reader.IsDBNull(1) ? null : reader.GetGuid(1);
            string? existingDepartmentSnapshot = reader.IsDBNull(2) ? null : reader.GetString(2);
            string? existingRoomCode = reader.IsDBNull(3) ? null : reader.GetString(3);
            bool alreadySet = existingKind is not null
                || existingDepartmentId is not null
                || !string.IsNullOrWhiteSpace(existingDepartmentSnapshot)
                || !string.IsNullOrWhiteSpace(existingRoomCode);
            if (alreadySet)
            {
                throw new InvalidOperationException(
                    $"Migration stopped: LoanCode '{loanCode}' already has justification set from a conflicting source.");
            }
        }

        if (string.Equals(justification.Kind, "Department", StringComparison.Ordinal))
        {
            ApplyDepartmentJustification(connection, transaction, loanCode, justification.Code);
            return;
        }

        if (string.Equals(justification.Kind, "Room", StringComparison.Ordinal))
        {
            ApplyRoomJustification(connection, transaction, loanCode, justification.Code);
            return;
        }

        throw new InvalidOperationException(
            $"Migration stopped: unsupported justification kind '{justification.Kind}' for LoanCode '{loanCode}'.");
    }

    private static void ApplyDepartmentJustification(
        DbConnection connection,
        DbTransaction transaction,
        string loanCode,
        string departmentCode)
    {
        List<(Guid Id, string Code)> matches = ResolveDepartments(connection, transaction, departmentCode);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"Migration stopped: Department code '{departmentCode}' for LoanCode '{loanCode}' was not found.");
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Migration stopped: Department code '{departmentCode}' for LoanCode '{loanCode}' matches multiple departments under collation rules.");
        }

        using DbCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE Loans
            SET JustificationKind = N'Department',
                JustificationDepartmentId = @departmentId,
                JustificationDepartmentCodeSnapshot = @snapshot,
                JustificationRoomCode = NULL
            WHERE LoanCode = @loanCode;
            """;
        AddParameter(update, "@departmentId", matches[0].Id);
        AddParameter(update, "@snapshot", matches[0].Code);
        AddParameter(update, "@loanCode", loanCode);
        update.ExecuteNonQuery();
    }

    private static List<(Guid Id, string Code)> ResolveDepartments(
        DbConnection connection,
        DbTransaction transaction,
        string departmentCode)
    {
        using DbCommand resolve = connection.CreateCommand();
        resolve.Transaction = transaction;
        resolve.CommandText =
            """
            SELECT DepartmentId, DepartmentCode
            FROM Departments
            WHERE DepartmentCode COLLATE SQL_Latin1_General_CP1_CI_AS = @code COLLATE SQL_Latin1_General_CP1_CI_AS;
            """;
        AddParameter(resolve, "@code", departmentCode);

        List<(Guid Id, string Code)> matches = [];
        using DbDataReader reader = resolve.ExecuteReader();
        while (reader.Read())
        {
            matches.Add((reader.GetGuid(0), reader.GetString(1)));
        }

        return matches;
    }

    private static void ApplyRoomJustification(
        DbConnection connection,
        DbTransaction transaction,
        string loanCode,
        string roomCode)
    {
        using DbCommand resolve = connection.CreateCommand();
        resolve.Transaction = transaction;
        resolve.CommandText =
            """
            SELECT RoomCode
            FROM Rooms
            WHERE RoomCode = @code;
            """;
        AddParameter(resolve, "@code", roomCode);

        List<string> matches = [];
        using (DbDataReader reader = resolve.ExecuteReader())
        {
            while (reader.Read())
            {
                matches.Add(reader.GetString(0));
            }
        }

        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"Migration stopped: Room code '{roomCode}' for LoanCode '{loanCode}' was not found.");
        }

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Migration stopped: Room code '{roomCode}' for LoanCode '{loanCode}' matched multiple rooms.");
        }

        using DbCommand update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText =
            """
            UPDATE Loans
            SET JustificationKind = N'Room',
                JustificationDepartmentId = NULL,
                JustificationDepartmentCodeSnapshot = NULL,
                JustificationRoomCode = @roomCode
            WHERE LoanCode = @loanCode;
            """;
        AddParameter(update, "@roomCode", matches[0]);
        AddParameter(update, "@loanCode", loanCode);
        update.ExecuteNonQuery();
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record AuditIssueRow(string LoanCode, string Details);

    private sealed record ParsedJustification(string Kind, string Code);
}
