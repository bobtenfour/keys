using KeyInventory.Application.OperatorAudit;
using KeyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KeyInventory.Infrastructure.OperatorAudit;

public sealed class OperatorAuditPersistenceAdapter : IOperatorAuditPersistencePort
{
    private readonly KeyInventoryDbContext _dbContext;

    public OperatorAuditPersistenceAdapter(KeyInventoryDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public void Stage(OperatorAuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _dbContext.OperatorAuditRecords.Add(new OperatorAuditRecordEntity
        {
            AuditRecordId = record.AuditRecordId,
            OccurredAtUtc = record.OccurredAtUtc,
            OperatorReference = record.OperatorReference,
            ActionType = record.ActionType,
            SubjectType = record.SubjectType,
            SubjectReference = record.SubjectReference,
            Details = record.Details
        });
    }

    public async Task<IReadOnlyList<OperatorAuditTrailItem>> QueryAsync(
        OperatorAuditTrailQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<OperatorAuditRecordEntity> source = _dbContext.OperatorAuditRecords.AsNoTracking();
        if (query.FromUtc is not null)
        {
            source = source.Where(entity => entity.OccurredAtUtc >= query.FromUtc);
        }

        if (query.ToUtc is not null)
        {
            source = source.Where(entity => entity.OccurredAtUtc <= query.ToUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.OperatorReference))
        {
            string operatorReference = query.OperatorReference.Trim();
            source = source.Where(entity => entity.OperatorReference == operatorReference);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            string actionType = query.ActionType.Trim();
            source = source.Where(entity => entity.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectReference))
        {
            string subject = query.SubjectReference.Trim();
            source = source.Where(entity =>
                entity.SubjectReference.Contains(subject)
                || entity.SubjectType.Contains(subject));
        }

        List<OperatorAuditRecordEntity> rows = await source
            .OrderByDescending(entity => entity.OccurredAtUtc)
            .ThenByDescending(entity => entity.AuditRecordId)
            .Take(500)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(entity => new OperatorAuditTrailItem(
                entity.AuditRecordId,
                entity.OccurredAtUtc,
                entity.OperatorReference,
                entity.ActionType,
                entity.SubjectType,
                entity.SubjectReference,
                entity.Details))
            .ToArray();
    }
}
