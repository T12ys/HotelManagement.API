// ═══════════════════════════════════════════════════════
// RoomsController.cs
// ═══════════════════════════════════════════════════════
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages individual room units within the hotel (physical rooms).
/// Each room belongs to a room type and has a room number, floor and availability flag.
/// Read endpoints are public. Write endpoints require Admin or Moderator role.
/// </summary>
[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns a paginated list of all rooms.
    /// Supports search by room number and custom sorting.
    /// </summary>
    /// <param name="request">Pagination, search and sort parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of rooms.</returns>
    /// <response code="200">Returns the room list.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    /// <summary>
    /// Returns a single room by its Id.
    /// </summary>
    /// <param name="id">Room Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Room details.</returns>
    /// <response code="200">Room found.</response>
    /// <response code="404">Room not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoomResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new room unit and assigns it to a room type.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="dto">Room data: room number, room type Id and floor.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 201 Created with the new room Id.</returns>
    /// <response code="201">Room created successfully.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPost]
    [Authorize(Policy = "RoomWrite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateRoomDto dto, CancellationToken ct)
    {
        var id = await ((RoomService)_service).CreateAsync(
            dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>
    /// Updates room details (number, floor, room type).
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="id">Room Id.</param>
    /// <param name="dto">Updated room data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Room updated successfully.</response>
    /// <response code="404">Room not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "RoomWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomDto dto, CancellationToken ct)
    {
        await ((RoomService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Toggles room availability without deleting it.
    /// Marking a room as unavailable prevents new reservations for it.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="id">Room Id.</param>
    /// <param name="dto">Availability flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Availability updated.</response>
    /// <response code="404">Room not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPatch("{id:int}/availability")]
    [Authorize(Policy = "RoomWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeAvailability(
        int id, [FromBody] ChangeRoomAvailabilityDto dto, CancellationToken ct)
    {
        await ((RoomService)_service).ChangeAvailabilityAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a room.
    /// Admin role required.
    /// </summary>
    /// <param name="id">Room Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Room deleted.</response>
    /// <response code="404">Room not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RoomDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((RoomService)_service).DeleteAsync(
            id, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}