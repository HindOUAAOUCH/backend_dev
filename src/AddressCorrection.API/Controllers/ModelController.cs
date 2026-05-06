using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddressCorrection.src.AddressCorrection.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelController : ControllerBase
{
    private readonly IActiveLlmModelProvider _modelProvider;
    public ModelController(IActiveLlmModelProvider modelProvider)
    {
        _modelProvider = modelProvider;
    }
    [HttpPost]
    public IActionResult SelectModel([FromBody] SelectModelRequest request)
    {
        if(request.ModelName is null)
        {
            _modelProvider.ResetSelection();
            return Ok(new { message = "reset selection" });
        }
        _modelProvider.SelectModel(request.ModelName);
        return Ok(new { message = $"Modèle sélectionné : {request.ModelName}" });
    }
}
