using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RoomWrite")]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto, CancellationToken ct)
    {
        var id = await ((RoomService)_service).CreateAsync(
            dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RoomWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto, CancellationToken ct)
    {
        await ((RoomService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    [HttpPatch("{id:int}/availability")]
    [Authorize(Policy = "RoomWrite")]
    public async Task<IActionResult> ChangeAvailability(
        int id, [FromBody] ChangeRoomAvailabilityDto dto, CancellationToken ct)
    {
        await ((RoomService)_service).ChangeAvailabilityAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RoomDelete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((RoomService)_service).DeleteAsync(
            id, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}