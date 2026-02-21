using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly ITagService _service;

    public TagsController(ITagService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<TagResponseDto>>> GetPaged([FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TagResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "TagWrite")]
    public async Task<IActionResult> Create([FromBody] CreateTagDto dto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "TagWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateTagDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "TagDelete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
