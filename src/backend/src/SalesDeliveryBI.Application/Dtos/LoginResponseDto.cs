namespace SalesDeliveryBI.Application.Dtos;

public sealed record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string DisplayName);
