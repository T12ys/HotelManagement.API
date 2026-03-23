using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages room types (e.g. Standard, Deluxe, Suite).
/// Read endpoints are public. Create/update/delete require Admin role.
/// Photo management requires Admin or Moderator role.
/// </summary>
[ApiController]
[Route("api/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _service;

    public RoomTypesController(IRoomTypeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns a paginated list of room types with optional filters.
    /// Supports filtering by code, active status, capacity, occupancy, price range, tags and availability dates.
    /// </summary>
    /// <param name="request">Filter and pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of room types including photos and tags.</returns>
    /// <response code="200">Returns the filtered list of room types.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomTypeResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoomTypeResponseDto>>> GetPaged(
        [FromQuery] RoomTypeFilterRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    /// <summary>
    /// Returns a single room type by its Id, including photos and associated tags.
    /// </summary>
    /// <param name="id">Room type Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Room type details.</returns>
    /// <response code="200">Room type found.</response>
    /// <response code="404">Room type not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RoomTypeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomTypeResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of individual rooms (physical units) belonging to the specified room type.
    /// </summary>
    /// <param name="id">Room type Id.</param>
    /// <param name="request">Pagination and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of rooms for this room type.</returns>
    /// <response code="200">Returns the room list.</response>
    /// <response code="404">Room type not found.</response>
    [HttpGet("{id:int}/rooms")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> GetRoomsByTypeId(
        int id, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _service.GetRoomsByTypeIdAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new room type with optional photos.
    /// Accepts multipart/form-data — include <c>photos</c> files alongside the DTO fields.
    /// Admin role required.
    /// </summary>
    /// <param name="dto">Room type data (code, name, description, capacity, price, tags).</param>
    /// <param name="photos">Optional room photos to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 201 Created with the new room type Id in the Location header.</returns>
    /// <response code="201">Room type created successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost]
    [Authorize(Policy = "RoomTypeWrite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

    /// <summary>
    /// Updates an existing room type (name, description, capacity, price, tags, active status).
    /// Photos are managed separately via the <c>/photos</c> endpoints.
    /// Admin role required.
    /// </summary>
    /// <param name="id">Room type Id.</param>
    /// <param name="dto">Updated room type data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Room type updated successfully.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "RoomTypeWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomTypeDto dto, CancellationToken ct)
    {
        await ((RoomTypeService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Deletes a room type and all associated photos (from cloud storage).
    /// Admin role required.
    /// </summary>
    /// <param name="id">Room type Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Room type deleted successfully.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RoomTypeDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((RoomTypeService)_service).DeleteAsync(
            id, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Uploads additional photos for an existing room type.
    /// Accepts multipart/form-data with one or more image files.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="id">Room type Id.</param>
    /// <param name="photos">One or more image files to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Photos uploaded successfully.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPost("{id:int}/photos")]
    [Authorize(Policy = "PhotoManagement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddPhotos(int id, [FromForm] List<IFormFile> photos, CancellationToken ct)
    {
        await ((RoomTypeService)_service).AddPhotosAsync(
            id, photos, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return Ok();
    }

    /// <summary>
    /// Deletes a single photo by its Id.
    /// The file is removed from cloud storage and the database record is deleted.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="photoId">Photo Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Photo deleted successfully.</response>
    /// <response code="404">Photo not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpDelete("photos/{photoId:int}")]
    [Authorize(Policy = "PhotoManagement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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