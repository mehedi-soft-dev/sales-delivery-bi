using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) Generate(User user, IReadOnlyCollection<Guid> unitIds);
}
