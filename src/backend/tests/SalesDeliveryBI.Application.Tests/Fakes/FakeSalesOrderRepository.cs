using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Tests.Fakes;

/// <summary>Records the scope SalesOrderAppService resolved and passed down, so tests can assert on it.</summary>
internal sealed class FakeSalesOrderRepository : ISalesOrderRepository
{
    public UnitScope? LastScope { get; private set; }
    public int CallCount { get; private set; }

    /// <summary>Defaults to empty — set before calling the AppService to test grid paging/sorting on non-trivial data.</summary>
    public IReadOnlyList<SalesOrderRowDto> Orders { get; set; } = [];

    public Task<DashboardResponse<SalesOrderDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        LastScope = scope;
        CallCount++;
        var dto = new SalesOrderDto(new SalesOrderKpisDto(0m, 0, 0d), [], Orders);
        return Task.FromResult(new DashboardResponse<SalesOrderDto>(dto, DateTime.UtcNow));
    }
}
