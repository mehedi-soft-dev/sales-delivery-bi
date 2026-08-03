namespace SalesDeliveryBI.Application.Common;

/// <summary>Thrown by IUnitAccessGuard when a caller requests a unit outside their assignment. Maps to HTTP 403.</summary>
public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}
