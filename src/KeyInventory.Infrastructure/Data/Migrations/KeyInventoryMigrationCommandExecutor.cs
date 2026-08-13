using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace KeyInventory.Infrastructure.Data.Migrations;

/// <summary>
/// Runs the one-time Key-issued justification provenance extract inside the migration transaction.
/// </summary>
public sealed class KeyInventoryMigrationCommandExecutor : IMigrationCommandExecutor
{
    public void ExecuteNonQuery(
        IEnumerable<MigrationCommand> migrationCommands,
        IRelationalConnection connection)
    {
        ArgumentNullException.ThrowIfNull(migrationCommands);
        ArgumentNullException.ThrowIfNull(connection);

        IReadOnlyList<MigrationCommand> commands = migrationCommands as IReadOnlyList<MigrationCommand>
            ?? migrationCommands.ToList();

        IDbContextTransaction? userTransaction = connection.CurrentTransaction;
        if (userTransaction is not null && commands.Any(command => command.TransactionSuppressed))
        {
            throw new NotSupportedException(
                "Transaction-suppressed migration commands cannot run inside a user transaction.");
        }

        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        {
            connection.Open();
            try
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    foreach (MigrationCommand command in commands)
                    {
                        if (transaction is null
                            && !command.TransactionSuppressed
                            && userTransaction is null)
                        {
                            transaction = connection.BeginTransaction();
                        }

                        if (transaction is not null && command.TransactionSuppressed)
                        {
                            transaction.Commit();
                            transaction.Dispose();
                            transaction = null;
                        }

                        if (KeyIssuedJustificationProvenanceExtract.IsMarkerCommand(command.CommandText))
                        {
                            DbTransaction dbTransaction =
                                (transaction ?? userTransaction)?.GetDbTransaction()
                                ?? throw new InvalidOperationException(
                                    "Provenance extract requires an active database transaction.");
                            KeyIssuedJustificationProvenanceExtract.Execute(
                                connection.DbConnection,
                                dbTransaction);
                            continue;
                        }

                        command.ExecuteNonQuery(connection);
                    }

                    transaction?.Commit();
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public async Task ExecuteNonQueryAsync(
        IEnumerable<MigrationCommand> migrationCommands,
        IRelationalConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migrationCommands);
        ArgumentNullException.ThrowIfNull(connection);

        IReadOnlyList<MigrationCommand> commands = migrationCommands as IReadOnlyList<MigrationCommand>
            ?? migrationCommands.ToList();

        IDbContextTransaction? userTransaction = connection.CurrentTransaction;
        if (userTransaction is not null && commands.Any(command => command.TransactionSuppressed))
        {
            throw new NotSupportedException(
                "Transaction-suppressed migration commands cannot run inside a user transaction.");
        }

        using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IDbContextTransaction? transaction = null;
                try
                {
                    foreach (MigrationCommand command in commands)
                    {
                        if (transaction is null
                            && !command.TransactionSuppressed
                            && userTransaction is null)
                        {
                            transaction = await connection.BeginTransactionAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }

                        if (transaction is not null && command.TransactionSuppressed)
                        {
                            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                            await transaction.DisposeAsync().ConfigureAwait(false);
                            transaction = null;
                        }

                        if (KeyIssuedJustificationProvenanceExtract.IsMarkerCommand(command.CommandText))
                        {
                            DbTransaction dbTransaction =
                                (transaction ?? userTransaction)?.GetDbTransaction()
                                ?? throw new InvalidOperationException(
                                    "Provenance extract requires an active database transaction.");
                            KeyIssuedJustificationProvenanceExtract.Execute(
                                connection.DbConnection,
                                dbTransaction);
                            continue;
                        }

                        await command.ExecuteNonQueryAsync(
                                connection,
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (transaction is not null)
                    {
                        await transaction.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }
}
