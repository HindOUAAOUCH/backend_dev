namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

public class AddressResponse
{
    public string? HouseNumber { get; set; }
    public string? Street { get; set; }
    public string? Complement { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Status { get; set; }
    public string? CorrectionNote { get; set; }
}
