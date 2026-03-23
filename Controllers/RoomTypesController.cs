using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _service;

    public RoomTypesController(IRoomTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<RoomTypeResponseDto>>> GetPaged(
        [FromQuery] RoomTypeFilterRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomTypeResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:int}/rooms")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> GetRoomsByTypeId(
        int id, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _service.GetRoomsByTypeIdAsync(id, request, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "RoomTypeWrite")]
    public async Task<IActionResult> Create(
        [FromForm] CreateRoomTypeDto dto,
        [FromForm] List<IFormFile>? photos,
        CancellationToken ct)
    {
        var id = await ((RoomTypeService)_service).CreateAsync(
            dto, photos, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RoomTypeWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomTypeDto dto, CancellationToken ct)
    {
        await ((RoomTypeService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RoomTypeDelete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((RoomTypeService)_service).DeleteAsync(
            id, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    [HttpPost("{id:int}/photos")]
    [Authorize(Policy = "PhotoManagement")]
    public async Task<IActionResult> AddPhotos(int id, [FromForm] List<IFormFile> photos, CancellationToken ct)
    {
        await ((RoomTypeService)_service).AddPhotosAsync(
            id, photos, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return Ok();
    }

    [HttpDelete("photos/{photoId:int}")]
    [Authorize(Policy = "PhotoManagement")]
    public async Task<IActionResult> DeletePhoto(int photoId, CancellationToken ct)
    {
        await ((RoomTypeService)_service).DeletePhotoAsync(
            photoId, ct,
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