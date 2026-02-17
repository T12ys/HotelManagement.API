using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        return Ok("Доступно всем, даже без авторизации");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public IActionResult AdminEndpoint()
    {
        return Ok("Доступно только Admin");
    }

    [Authorize(Roles = "Moderator")]
    [HttpGet("moderator")]
    public IActionResult ModeratorEndpoint()
    {
        return Ok("Доступно только Moderator");
    }

    [Authorize]
    [HttpGet("anyuser")]
    public IActionResult AnyAuthenticated()
    {
        return Ok("Доступно любому авторизованному пользователю");
    }
}
