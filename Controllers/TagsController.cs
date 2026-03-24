using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages room amenity tags (e.g. Wi-Fi, Sea View, Balcony).
/// Tags support multilingual translations and are associated with room types.
/// Read endpoints are public. Write endpoints require Admin or Moderator role.
/// </summary>
[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly ITagService _service;

    public TagsController(ITagService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns a paginated list of all tags.
    /// Supports search by slug and custom sorting.
    /// </summary>
    /// <param name="request">Pagination, search and sort parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of tags with translations.</returns>
    /// <response code="200">Returns the tag list.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<TagResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TagResponseDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    /// <summary>
    /// Returns a single tag by its Id, including all language translations.
    /// </summary>
    /// <param name="id">Tag Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tag details with slug and translations dictionary.</returns>
    /// <response code="200">Tag found.</response>
    /// <response code="404">Tag not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TagResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new tag. The slug is auto-generated from the English translation.
    /// The <c>Translations</c> object must contain at least an <c>"en"</c> key.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="dto">Tag translations (keyed by language code, e.g. "en", "ru", "az").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 201 Created with the new tag Id.</returns>
    /// <response code="201">Tag created successfully.</response>
    /// <response code="409">A tag with this name (slug) already exists.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPost]
    [Authorize(Policy = "TagWrite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTagDto dto, CancellationToken ct)
    {
        var id = await ((TagService)_service).CreateAsync(
            dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>
    /// Updates the translations of an existing tag.
    /// The slug is not changed on update.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="id">Tag Id.</param>
    /// <param name="dto">Updated translations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Tag updated successfully.</response>
    /// <response code="404">Tag not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "TagWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTagDto dto, CancellationToken ct)
    {
        await ((TagService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a tag.
    /// The tag is automatically disassociated from all room types.
    /// Admin role required.
    /// </summary>
    /// <param name="id">Tag Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Tag deleted.</response>
    /// <response code="404">Tag not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "TagDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((TagService)_service).DeleteAsync(
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