using KeyInventory.Application.Readiness;
using KeyInventory.Web.Presentation;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KeyInventory.Web.Pages.Administration;

public sealed class IndexModel : PageModel
{
    private readonly IOperationalReadinessUseCase _readiness;

    public IndexModel(IOperationalReadinessUseCase readiness)
    {
        _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    public OperationalReadinessViewModel Readiness { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        OperationalReadinessSnapshot snapshot = await _readiness.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        Readiness = new OperationalReadinessViewModel(snapshot);
    }
}
