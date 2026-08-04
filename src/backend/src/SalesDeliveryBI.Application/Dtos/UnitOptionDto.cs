namespace SalesDeliveryBI.Application.Dtos;

/// <summary>A unit the caller is allowed to filter dashboards by — scoped by IUnitAccessGuard, never the full catalog.</summary>
public sealed record UnitOptionDto(Guid Id, string Name);
