using SalesDeliveryBI.Application.Abstractions;

namespace SalesDeliveryBI.Application.Tests.Fakes;

/// <summary>No real hashing needed for these tests — "hash of X" is just the literal string "hash:X".</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string hash) => hash == Hash(password);
}
