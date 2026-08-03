namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>
/// Resolves the caller's identity from the current request's JWT claims (sub, permissions, user_units).
/// Implemented in Infrastructure/Security once the Identity service's token contract is consumed (Phase 7).
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    IReadOnlyCollection<string> Permissions { get; }
    IReadOnlyCollection<Guid> UnitIds { get; }
}
