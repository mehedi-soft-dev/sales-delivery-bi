using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Application.Tests.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _usersByEmail;

    public FakeUserRepository(params User[] users) => _usersByEmail = users.ToDictionary(u => u.Email);

    public string? LastEmail { get; private set; }
    public int CallCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        LastEmail = email;
        CallCount++;
        return Task.FromResult(_usersByEmail.GetValueOrDefault(email));
    }
}
