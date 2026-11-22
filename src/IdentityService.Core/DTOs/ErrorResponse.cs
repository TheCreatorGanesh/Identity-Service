namespace IdentityService.Core.DTOs;

/// <summary>
/// Standard error response matching existing API format.
/// </summary>
public sealed class ErrorResponse
{
    public required string Error { get; set; }
    public required string Detail { get; set; }
    public string? TraceId { get; set; }
}
