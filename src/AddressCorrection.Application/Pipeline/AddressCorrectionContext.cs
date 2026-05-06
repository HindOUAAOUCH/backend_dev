using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline;

public class AddressCorrectionContext
{
    public AddressRequest Request { get; set; } = null!;
    public string? NormalizedAddress { get; set; }
    public AddressResponse? Result { get; set; }
    public string? ModelUsed { get; set; }
    public bool FromCache { get; set; }
    public string? Status { get; set; }
    public long DurationMs { get; set; }
    public Exception? Error { get; set; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public bool IsFailed => Error != null;
}
