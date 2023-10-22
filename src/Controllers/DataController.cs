using Microsoft.AspNetCore.Mvc;
using Services;

namespace Controllers;

[Route("[controller]")]
public class DataController : ControllerBase
{
    private readonly CheckupdatesService checkupdatesService = null!;
    private readonly ILogger<DataController> logger = null!;

    public DataController(CheckupdatesService checkupdatesService, ILogger<DataController> logger)
    {
        this.checkupdatesService = checkupdatesService;
        this.logger = logger;
    }

    public async Task<IActionResult> Get()
    {
        var data = await checkupdatesService.GetCurrentUpdates();

        return Ok(data);
    }
}
