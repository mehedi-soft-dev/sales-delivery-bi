namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Every dashboard endpoint's response shape: the payload plus `lastRefresh` (api-contract.md, "Common Rules").</summary>
public sealed record DashboardResponse<T>(T Data, DateTime LastRefresh);
