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
            Permissions = [PermissionCodes.QuotationViewPipeline],
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
            Permissions = [PermissionCodes.QuotationViewPipeline],
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
            Permissions = [PermissionCodes.QuotationViewPipeline],
            UnitIds = [UnitA, UnitB],
        });

        Assert.Throws<ForbiddenAccessException>(() => guard.Validate(UnitOutsideAssignment));
    }

    [Fact]
    public void Validate_MissingQuotationViewPermissionEntirely_StillEnforcesUnitScoping()
    {
        // IUnitAccessGuard only decides unit scope — whether the caller has any bi.quotation.view.* permission
        // at all is enforced by the per-dashboard authorization policies (AuthorizationPolicies), not here.
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [],
            UnitIds = [UnitA],
        });

        UnitScope scope = guard.Validate(UnitA);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal([UnitA], scope.UnitIds);
    }

    [Fact]
    public void Validate_WithExplicitAllUnitsPermissionCode_UsesThatCodeNotQuotationViewAllUnits()
    {
        // A caller with a DIFFERENT module's all-units code (not bi.quotation.viewAllUnits) must still be
        // treated as unrestricted when that code is the one explicitly passed in.
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.SalesOrderViewAllUnits],
            UnitIds = [UnitA],
        });

        UnitScope scope = guard.Validate(null, PermissionCodes.SalesOrderViewAllUnits);

        Assert.True(scope.IsUnrestricted);
    }

    [Fact]
    public void Validate_WithExplicitAllUnitsPermissionCode_QuotationViewAllUnitsAloneDoesNotGrantIt()
    {
        // Holding bi.quotation.viewAllUnits must NOT satisfy a different module's all-units check.
        var guard = new UnitAccessGuard(new FakeCurrentUserContext
        {
            Permissions = [PermissionCodes.QuotationViewAllUnits],
            UnitIds = [UnitA],
        });

        UnitScope scope = guard.Validate(UnitA, PermissionCodes.SalesOrderViewAllUnits);

        Assert.False(scope.IsUnrestricted);
        Assert.Equal([UnitA], scope.UnitIds);
    }
}
