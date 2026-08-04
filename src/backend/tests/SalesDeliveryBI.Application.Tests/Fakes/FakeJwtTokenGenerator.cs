using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Application.Tests.Fakes;

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public User? LastUser { get; private set; }
    public IReadOnlyCollection<Guid>? LastUnitIds { get; private set; }
    public int CallCount { get; private set; }

    public (string Token, DateTime ExpiresAtUtc) Generate(User user, IReadOnlyCollection<Guid> unitIds)
    {
        LastUser = user;
        LastUnitIds = unitIds;
        CallCount++;
        return ("fake-token", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
