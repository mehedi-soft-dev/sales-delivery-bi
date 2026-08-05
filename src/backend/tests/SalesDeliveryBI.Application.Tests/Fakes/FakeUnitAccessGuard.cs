using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Tests.Fakes;

internal sealed class FakeUnitAccessGuard : IUnitAccessGuard
{
    private readonly Func<Guid?, UnitScope> _validate;

    public FakeUnitAccessGuard(Func<Guid?, UnitScope> validate) => _validate = validate;

    public Guid? LastRequestedUnitId { get; private set; }
    public int CallCount { get; private set; }

    public UnitScope Validate(Guid? requestedUnitId)
    {
        LastRequestedUnitId = requestedUnitId;
        CallCount++;
        return _validate(requestedUnitId);
    }

    public UnitScope Validate(Guid? requestedUnitId, string allUnitsPermissionCode) => Validate(requestedUnitId);
}
