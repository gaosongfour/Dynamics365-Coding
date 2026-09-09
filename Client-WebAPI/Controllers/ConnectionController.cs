using Microsoft.AspNetCore.Mvc;
using Crm.ClientWebAPI.Services;

namespace Crm.ClientWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly CrmServiceManager _serviceManager;

    public ConnectionController(CrmServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    /// <summary>
    /// Initialize CRM connection
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        var result = await _serviceManager.InitializeAsync();
        if (!string.IsNullOrEmpty(result.Error))
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Check CRM connection status
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            IsReady = _serviceManager.IsReady,
            OrganizationName = _serviceManager.OrgFriendlyName
        });
    }
}
