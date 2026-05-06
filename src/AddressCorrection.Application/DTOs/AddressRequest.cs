using System.ComponentModel.DataAnnotations;

namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

public class AddressRequest
{
    [Required]
    public string RawAddress { get; set; } = string.Empty;
}
