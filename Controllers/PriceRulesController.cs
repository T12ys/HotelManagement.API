using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/price-rules")]
public class PriceRulesController : ControllerBase
{
    private readonly IPriceRuleService _service;

    public PriceRulesController(IPriceRuleService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PriceRuleResponseDto>>> GetByRoomType(
        [FromQuery] int roomTypeId,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        return Ok(await _service.GetByRoomTypeAsync(roomTypeId, request, ct));
    }

    [HttpGet("period")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PriceRuleResponseDto>>> GetRulesForPeriod(
        [FromQuery] PeriodRulesRequestDto dto,
        CancellationToken ct)
    {
        if (dto.To == default)
            dto.To = DateTime.UtcNow.Date.AddYears(1);

        return Ok(await _service.GetRulesForPeriodAsync(dto, ct));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<PriceRuleResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int? roomTypeId, [FromQuery] PagedRequest request)
    {
        var result = await _service.GetAllAsync(roomTypeId, request);
        return Ok(result);
    }

    [HttpGet("calculate")]
    [AllowAnonymous]
    public async Task<ActionResult<PriceCalculationResponseDto>> Calculate(
        [FromQuery] PriceCalculationRequestDto dto,
        CancellationToken ct)
    {
        return Ok(await _service.CalculatePriceAsync(dto, ct));
    }

    [HttpPost]
    [Authorize(Policy = "PriceRuleWrite")]
    public async Task<IActionResult> Create([FromBody] CreatePriceRuleDto dto, CancellationToken ct)
    {
        var id = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "PriceRuleWrite")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePriceRuleDto dto, CancellationToken ct)
    {
        await _service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "PriceRuleDelete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}