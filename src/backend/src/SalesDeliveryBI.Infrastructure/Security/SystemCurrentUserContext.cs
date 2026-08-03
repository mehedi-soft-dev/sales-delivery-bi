using SalesDeliveryBI.Application.Abstractions;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>
/// Fixed "system" identity for code paths with no HTTP request — startup seeding, Quartz jobs.
/// Registered as the default ICurrentUserContext until Phase 7 adds a request-scoped,
/// JWT-claims-based implementation for actual API calls.
/// </summary>
public class SystemCurrentUserContext : ICurrentUserContext
{
    public static readonly Guid SystemUserId = Guid.Parse("44444444-4444-4444-4444-444444444401");

    public Guid UserId => SystemUserId;
    public IReadOnlyCollection<string> Permissions => Array.Empty<string>();
    public IReadOnlyCollection<Guid> UnitIds => Array.Empty<Guid>();
}
