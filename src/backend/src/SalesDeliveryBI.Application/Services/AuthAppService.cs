using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>
/// Plain AppService, no MediatR — same style as QuotationAppService (docs/plans/backend/architecture.md).
/// Deliberately never distinguishes *why* a login failed (unknown email, wrong password, inactive user) —
/// a null result always maps to one generic "Invalid email or password" response, so a caller can't
/// enumerate which emails exist (same anti-enumeration instinct as the 404-vs-403 quotation-detail case).
/// </summary>
public class AuthAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthAppService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        Domain.Entities.User? user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        Guid[] unitIds = user.UserUnits.Select(uu => uu.UnitId).ToArray();
        (string token, DateTime expiresAtUtc) = _jwtTokenGenerator.Generate(user, unitIds);

        return new LoginResponseDto(token, expiresAtUtc, user.DisplayName);
    }
}
