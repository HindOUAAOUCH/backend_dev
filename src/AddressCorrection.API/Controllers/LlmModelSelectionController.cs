using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddressCorrection.src.AddressCorrection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LlmModelSelectionController : ControllerBase
{
    private readonly IModelSelectionService _modelSelectionService;
    public LlmModelSelectionController(IModelSelectionService modelSelectionService)
    {
        _modelSelectionService = modelSelectionService;
    }
    [HttpPost]
    public IActionResult SelectModel([FromBody] SelectModelRequest request)
    {
        if(request.ModelName is null)
        {
            _modelSelectionService.ResetSelection();
            return Ok(new { message = "reset selection" });
        }
        _modelSelectionService.SelectModel(request.ModelName);
        return Ok(new { message = $"Modèle sélectionné : {request.ModelName}" });
    }
}
