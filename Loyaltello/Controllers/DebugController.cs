using Microsoft.AspNetCore.Mvc;

namespace Loyaltello.Controllers;

public class DebugController : Controller
{
    [HttpGet("/debug")]
    public IActionResult Index()
    {
        return Ok("Hello, world!");
    }
}