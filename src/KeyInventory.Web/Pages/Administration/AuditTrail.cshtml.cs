using KeyInventory.Application.OperatorAudit;
using KeyInventory.Application.Reports;
using KeyInventory.Web.Presentation;
using KeyInventory.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KeyInventory.Web.Pages.Administration;

public sealed class AuditTrailModel : PageModel
{
    private readonly IOperatorAuditTrailUseCase _trail;
    private readonly IReportExcelExporter _excel;
    private readonly IReportPdfExporter _pdf;

    public AuditTrailModel(
        IOperatorAuditTrailUseCase trail,
        IReportExcelExporter excel,
        IReportPdfExporter pdf)
    {
        _trail = trail ?? throw new ArgumentNullException(nameof(trail));
        _excel = excel ?? throw new ArgumentNullException(nameof(excel));
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));
    }

    [BindProperty(SupportsGet = true)]
    public string? FromLocal { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ToLocal { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Operator { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Subject { get; set; }

    public IReadOnlyList<OperatorAuditTrailItem> Rows { get; private set; } = [];

    public IReadOnlyList<SelectListItem> ActionOptions { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetExportAsync(string? format, CancellationToken cancellationToken)
    {
        if (!TryBuildQuery(out OperatorAuditTrailQuery query, out string? error))
        {
            ErrorMessage = error;
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            return Page();
        }

        string csv = await _trail.ExportCsvAsync(query, cancellationToken).ConfigureAwait(false);
        ReportExportTable table = await _trail.BuildExportTableAsync(query, cancellationToken).ConfigureAwait(false);
        return ReportExportResultFactory.Create(
            format,
            "audit-trail",
            () => csv,
            () => _excel.Export(table),
            () => _pdf.Export(table));
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        ActionOptions = BuildActionOptions();
        if (!TryBuildQuery(out OperatorAuditTrailQuery query, out string? error))
        {
            ErrorMessage = error;
            Rows = [];
            return;
        }

        Rows = await _trail.QueryAsync(query, cancellationToken).ConfigureAwait(false);
    }

    private bool TryBuildQuery(out OperatorAuditTrailQuery query, out string? error)
    {
        error = null;
        DateTimeOffset? fromUtc = null;
        DateTimeOffset? toUtc = null;

        if (!string.IsNullOrWhiteSpace(FromLocal))
        {
            if (!OperatorLocalTimestamp.TryParseToUtc(FromLocal, out DateTimeOffset parsedFrom, out string? fromError))
            {
                query = new OperatorAuditTrailQuery(null, null, null, null, null);
                error = fromError ?? "From date/time is invalid.";
                return false;
            }

            fromUtc = parsedFrom;
        }

        if (!string.IsNullOrWhiteSpace(ToLocal))
        {
            if (!OperatorLocalTimestamp.TryParseToUtc(ToLocal, out DateTimeOffset parsedTo, out string? toError))
            {
                query = new OperatorAuditTrailQuery(null, null, null, null, null);
                error = toError ?? "To date/time is invalid.";
                return false;
            }

            toUtc = parsedTo;
        }

        query = new OperatorAuditTrailQuery(
            fromUtc,
            toUtc,
            string.IsNullOrWhiteSpace(Operator) ? null : Operator.Trim(),
            string.IsNullOrWhiteSpace(Action) ? null : Action.Trim(),
            string.IsNullOrWhiteSpace(Subject) ? null : Subject.Trim());
        return true;
    }

    private SelectListItem[] BuildActionOptions()
    {
        string[] actions =
        [
            OperatorAuditActions.KeyRegistered,
            OperatorAuditActions.KeyAccessPatternCreated,
            OperatorAuditActions.PhysicalKeyCopyRegistered,
            OperatorAuditActions.KeyMarkedLost,
            OperatorAuditActions.KeyDestroyed,
            OperatorAuditActions.CustodyClosedLost,
            OperatorAuditActions.CustodyClosedDestroyed,
            OperatorAuditActions.LostKeyReplaced,
            OperatorAuditActions.KeyRoomAssignmentAdded,
            OperatorAuditActions.KeyRoomAssignmentRemoved,
            OperatorAuditActions.KeyAccessPatternRoomAssignmentAdded,
            OperatorAuditActions.KeyAccessPatternRoomAssignmentRemoved,
            OperatorAuditActions.KeyIssued,
            OperatorAuditActions.KeyReturned,
            OperatorAuditActions.WorkforceMemberCreated,
            OperatorAuditActions.WorkforceMemberMaintained,
            OperatorAuditActions.WorkforceMemberTerminated,
            OperatorAuditActions.WorkAssignmentCreated,
            OperatorAuditActions.WorkAssignmentEnded,
            OperatorAuditActions.OrganizationCreated,
            OperatorAuditActions.OrganizationActivated,
            OperatorAuditActions.OrganizationRetired,
            OperatorAuditActions.DepartmentCreated,
            OperatorAuditActions.DepartmentActivated,
            OperatorAuditActions.DepartmentRetired,
            OperatorAuditActions.BuildingCreated,
            OperatorAuditActions.BuildingActivated,
            OperatorAuditActions.BuildingRetired,
            OperatorAuditActions.RoomCreated,
            OperatorAuditActions.RoomUpdated,
            OperatorAuditActions.RoomActivated,
            OperatorAuditActions.RoomRetired
        ];

        return actions
            .Select(action => new SelectListItem(action, action, string.Equals(action, Action, StringComparison.Ordinal)))
            .ToArray();
    }
}
