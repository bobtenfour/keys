using System.Text;
using KeyInventory.Application.Reports;

namespace KeyInventory.Application.OperatorAudit;

public interface IOperatorAuditTrailUseCase
{
    Task<IReadOnlyList<OperatorAuditTrailItem>> QueryAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken);

    Task<ReportExportTable> BuildExportTableAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken);

    Task<string> ExportCsvAsync(OperatorAuditTrailQuery query, CancellationToken cancellationToken);
}

public sealed class OperatorAuditTrailUseCase : IOperatorAuditTrailUseCase
{
    private readonly IOperatorAuditPersistencePort _persistence;

    public OperatorAuditTrailUseCase(IOperatorAuditPersistencePort persistence)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
    }

    public Task<IReadOnlyList<OperatorAuditTrailItem>> QueryAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return _persistence.QueryAsync(query, cancellationToken);
    }

    public async Task<ReportExportTable> BuildExportTableAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OperatorAuditTrailItem> rows = await QueryAsync(query, cancellationToken).ConfigureAwait(false);
        string filterContext = BuildFilterContext(query);
        IReadOnlyList<IReadOnlyList<ReportExportCell>> exportRows = rows
            .Select(static item => (IReadOnlyList<ReportExportCell>)
            [
                ReportExportCell.DateTimeUtcValue(item.OccurredAtUtc),
                ReportExportCell.FromText(item.OperatorReference),
                ReportExportCell.FromText(item.ActionType),
                ReportExportCell.FromText($"{item.SubjectType}: {item.SubjectReference}"),
                ReportExportCell.FromText(item.Details)
            ])
            .ToArray();

        return new ReportExportTable(
            "Audit Trail",
            "AuditTrail",
            filterContext,
            ["Date/Time (UTC)", "Operator", "Action", "Subject", "Details"],
            exportRows);
    }

    public async Task<string> ExportCsvAsync(OperatorAuditTrailQuery query, CancellationToken cancellationToken)
    {
        ReportExportTable table = await BuildExportTableAsync(query, cancellationToken).ConfigureAwait(false);
        return FormatCsv(table);
    }

    private static string BuildFilterContext(OperatorAuditTrailQuery query)
    {
        List<string> parts = [];
        if (query.FromUtc is not null)
        {
            parts.Add($"From={query.FromUtc:u}");
        }

        if (query.ToUtc is not null)
        {
            parts.Add($"To={query.ToUtc:u}");
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorReference))
        {
            parts.Add($"Operator={query.OperatorReference.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            parts.Add($"Action={query.ActionType.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectReference))
        {
            parts.Add($"Subject={query.SubjectReference.Trim()}");
        }

        return parts.Count == 0 ? "All audit records" : string.Join("; ", parts);
    }

    private static string FormatCsv(ReportExportTable table)
    {
        StringBuilder builder = new();
        builder.AppendLine(string.Join(',', table.Headers.Select(EscapeCsv)));
        foreach (IReadOnlyList<ReportExportCell> row in table.Rows)
        {
            builder.AppendLine(string.Join(',', row.Select(cell => EscapeCsv(cell.Text))));
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string value)
    {
        string text = value ?? string.Empty;
        if (text.Contains('"', StringComparison.Ordinal)
            || text.Contains(',', StringComparison.Ordinal)
            || text.Contains('\n', StringComparison.Ordinal)
            || text.Contains('\r', StringComparison.Ordinal))
        {
            return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return text;
    }
}
