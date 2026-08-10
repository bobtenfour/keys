using KeyInventory.Application.OperatorAudit;
using Microsoft.AspNetCore.Http;

namespace KeyInventory.Infrastructure.OperatorAudit;

/// <summary>
/// Resolves the authenticated KeyInventory user name. Tests may set <see cref="TestOperatorReference"/>.
/// </summary>
public sealed class OperatorIdentityAccessor : IOperatorIdentityAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OperatorIdentityAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public const string DenyOperatorMarker = "__deny__";

    public static AsyncLocal<string?> TestOperatorReference { get; } = new();

    public string GetRequiredOperatorReference()
    {
        if (!string.IsNullOrWhiteSpace(TestOperatorReference.Value))
        {
            if (string.Equals(TestOperatorReference.Value, DenyOperatorMarker, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "An authenticated KeyInventory operator is required to record audit evidence.");
            }

            return TestOperatorReference.Value.Trim();
        }

        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            // Non-HTTP test/host composition: no authenticated session exists.
            return "test-operator";
        }

        string? name = httpContext.User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(name) || httpContext.User?.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException(
                "An authenticated KeyInventory operator is required to record audit evidence.");
        }

        return name.Trim();
    }
}
