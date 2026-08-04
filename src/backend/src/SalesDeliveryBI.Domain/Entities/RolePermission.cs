using System.Diagnostics.CodeAnalysis;
using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "CA1711 flags 'Permission' to avoid confusion with legacy System.Security.Permissions " +
        "code-access-security sets — not applicable here; RolePermission is the standard name for this join entity.")]
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public required string PermissionCode { get; set; }
}
