using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [FromQuery] RoomTypeFilterRequest request,
    CancellationToken ct)
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

    [HttpPost]
    [Authorize(Policy = "RoomTypeWrite")]
    public async Task<IActionResult> Create([FromForm] CreateRoomTypeDto dto,[FromForm] List<IFormFile>? photos, CancellationToken ct)
    {
        var id = await _service.CreateAsync(dto, photos, ct);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "RoomTypeWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoomTypeDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RoomTypeDelete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/photos")]
    [Authorize(Policy = "PhotoManagement")]
    public async Task<IActionResult> AddPhotos(int id, [FromForm] List<IFormFile> photos, CancellationToken ct)
    {
        await _service.AddPhotosAsync(id, photos, ct);
        return Ok();
    }

    [HttpDelete("photos/{photoId:int}")]
    [Authorize(Policy = "PhotoManagement")]
    public async Task<IActionResult> DeletePhoto(int photoId, CancellationToken ct)
    {
        await _service.DeletePhotoAsync(photoId, ct);
        return NoContent();
    }
}