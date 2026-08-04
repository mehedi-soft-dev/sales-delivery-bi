using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Application.Tests.Fakes;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Application.Tests;

/// <summary>
/// Never distinguishes *why* a login fails (unknown email, wrong password, inactive user) — every
/// failure path returns null, mapped by the controller to one generic 401 (anti-enumeration).
/// </summary>
public class AuthAppServiceTests
{
    private static User BuildUser(string email, string password, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        DisplayName = "Test User",
        PasswordHash = new FakePasswordHasher().Hash(password),
        RoleId = Guid.NewGuid(),
        IsActive = isActive,
    };

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenFromGenerator()
    {
        User user = BuildUser("user@example.com", "correct-password");
        var userRepository = new FakeUserRepository(user);
        var passwordHasher = new FakePasswordHasher();
        var jwtTokenGenerator = new FakeJwtTokenGenerator();
        var appService = new AuthAppService(userRepository, passwordHasher, jwtTokenGenerator);

        LoginResponseDto? result = await appService.LoginAsync(new LoginRequestDto("user@example.com", "correct-password"));

        Assert.NotNull(result);
        Assert.Equal("fake-token", result!.Token);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal(1, jwtTokenGenerator.CallCount);
        Assert.Same(user, jwtTokenGenerator.LastUser);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull_AndNeverCallsTokenGenerator()
    {
        User user = BuildUser("user@example.com", "correct-password");
        var appService = new AuthAppService(new FakeUserRepository(user), new FakePasswordHasher(), new FakeJwtTokenGenerator());

        LoginResponseDto? result = await appService.LoginAsync(new LoginRequestDto("user@example.com", "wrong-password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsNull()
    {
        var appService = new AuthAppService(new FakeUserRepository(), new FakePasswordHasher(), new FakeJwtTokenGenerator());

        LoginResponseDto? result = await appService.LoginAsync(new LoginRequestDto("nobody@example.com", "anything"));

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull_EvenWithCorrectPassword()
    {
        User user = BuildUser("user@example.com", "correct-password", isActive: false);
        var appService = new AuthAppService(new FakeUserRepository(user), new FakePasswordHasher(), new FakeJwtTokenGenerator());

        LoginResponseDto? result = await appService.LoginAsync(new LoginRequestDto("user@example.com", "correct-password"));

        Assert.Null(result);
    }
}
