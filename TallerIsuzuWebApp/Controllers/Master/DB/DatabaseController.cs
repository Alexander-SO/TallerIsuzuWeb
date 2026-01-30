using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseService _dbService;

    public DatabaseController(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost("EjecutarSP")]
    public async Task<IActionResult> EjecutarSP([FromBody] SPDynamicRequest request)
    {
        var result = await _dbService.EjecutarSP_Dinamico(request);
        return Ok(result);
    }
}
