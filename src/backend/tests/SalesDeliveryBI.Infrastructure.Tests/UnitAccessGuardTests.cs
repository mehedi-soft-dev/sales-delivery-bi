using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Infrastructure.Tests;

public class UnitAccessGuardTests
{
    private static readonly Guid UnitA = Guid.NewGuid();
    private static readonly Guid UnitB = Guid.NewGuid();
    private static readonly Guid UnitOutsideAssignment = Guid.NewGuid();

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid UserId => Guid.NewGuid();
        public required IReadOnlyCollection<string> Permissions { get; init; }
        public required IReadOnlyCollection<Guid> UnitIds { get; init; }
    }

    [Fact]
    public void Validate_ViewAllUnitsPermission_NullRequestedUnit_ReturnsUnrestricted()
    {
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationViewAllUnits],
            UnitIds = [],
        });

        UnitScope scope = guard.Validate(null);

        Assert.True(scope.IsUnrestricted);
    }

    [Fact]
    public void Validate_ViewAllUnitsPermission_SpecificRequestedUnit_ReturnsRestrictedToThatUnit()
    {
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationViewAllUnits],
            UnitIds = [],
        });

        UnitScope scope = guard.Validate(UnitOutsideAssignment);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal([UnitOutsideAssignment], scope.UnitIds);
    }

    [Fact]
    public void Validate_NoViewAllUnitsPermission_NullRequestedUnit_ReturnsRestrictedToAssignedUnits()
    {
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationView],
            UnitIds = [UnitA, UnitB],
        });

        UnitScope scope = guard.Validate(null);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal(new[] { UnitA, UnitB }, scope.UnitIds);
    }

    [Fact]
    public void Validate_NoViewAllUnitsPermission_RequestedUnitWithinAssignment_ReturnsRestrictedToThatUnit()
    {
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationView],
            UnitIds = [UnitA, UnitB],
        });

        UnitScope scope = guard.Validate(UnitA);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal([UnitA], scope.UnitIds);
    }

    [Fact]
    public void Validate_NoViewAllUnitsPermission_RequestedUnitOutsideAssignment_ThrowsForbidden()
    {
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationView],
            UnitIds = [UnitA, UnitB],
        });

        Assert.Throws<ForbiddenAccessException>(() => guard.Validate(UnitOutsideAssignment));
    }

    [Fact]
    public void Validate_MissingQuotationViewPermissionEntirely_StillEnforcesUnitScoping()
    {
        // IUnitAccessGuard only decides unit scope — whether the caller has bi.quotation.view at all
        // is enforced by the QuotationRead authorization policy (Phase 10), not here.
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [],
            UnitIds = [UnitA],
        });

        UnitScope scope = guard.Validate(UnitA);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal([UnitA], scope.UnitIds);
    }
}
