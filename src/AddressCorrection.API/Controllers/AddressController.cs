using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddressCorrection.src.AddressCorrection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly ILogger<AddressController> _logger;

    public AddressController(IAddressService addressService, ILogger<AddressController> logger)
    {
        _addressService = addressService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> Correct([FromBody] AddressRequest request)
    {
        try
        {
            var result = await _addressService.CorrectAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Entrée invalide → 400 Bad Request
            return BadRequest(new { error = ex.Message });
        }
        catch (AllModelsFailedException)
        {
            // Tous les LLM ont échoué → 503 Service Unavailable
            _logger.LogError("All LLM models failed for address: {Address}", request.RawAddress);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Address correction service is temporarily unavailable. Please retry later." });
        }
        catch (Exception ex)
        {
            // Erreur inattendue → 500, sans exposer les détails internes en production
            _logger.LogError(ex, "Unexpected error while correcting address: {Address}", request.RawAddress);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred." });
        }
    }
}
