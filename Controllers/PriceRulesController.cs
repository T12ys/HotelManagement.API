using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages price rules for room types.
/// A price rule defines a surcharge or discount (fixed amount or percentage)
/// applied to a date range (seasonal) or a specific date (special event).
/// Read and calculation endpoints are public. Write endpoints require Admin or Moderator role.
/// </summary>
[ApiController]
[Route("api/price-rules")]
public class PriceRulesController : ControllerBase
{
    private readonly IPriceRuleService _service;

    public PriceRulesController(IPriceRuleService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns a paginated list of price rules for a specific room type.
    /// </summary>
    /// <param name="roomTypeId">Room type Id to filter by.</param>
    /// <param name="request">Pagination and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of price rules.</returns>
    /// <response code="200">Returns the price rule list.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PriceRuleResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PriceRuleResponseDto>>> GetByRoomType(
        [FromQuery] int roomTypeId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetByRoomTypeAsync(roomTypeId, request, ct));
    }

    /// <summary>
    /// Returns all active price rules that overlap a given date range for a room type.
    /// Includes both room-type-specific rules and global rules (RoomTypeId = null).
    /// Used by the admin price calendar to visualise active pricing.
    /// </summary>
    /// <param name="dto">Room type Id, start date and end date of the period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of price rules active within the given period.</returns>
    /// <response code="200">Returns overlapping price rules.</response>
    [HttpGet("period")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PriceRuleResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PriceRuleResponseDto>>> GetRulesForPeriod(
        [FromQuery] PeriodRulesRequestDto dto, CancellationToken ct)
    {
        if (dto.To == default)
            dto.To = DateTime.UtcNow.Date.AddYears(1);

        return Ok(await _service.GetRulesForPeriodAsync(dto, ct));
    }

    /// <summary>
    /// Returns a single price rule by its Id.
    /// </summary>
    /// <param name="id">Price rule Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Price rule details.</returns>
    /// <response code="200">Price rule found.</response>
    /// <response code="404">Price rule not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PriceRuleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceRuleResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns all price rules, optionally filtered by room type.
    /// Includes both specific and global rules.
    /// </summary>
    /// <param name="roomTypeId">Optional room type Id filter.</param>
    /// <param name="request">Pagination parameters.</param>
    /// <returns>Paginated list of all price rules.</returns>
    /// <response code="200">Returns the full price rule list.</response>
    [HttpGet("all")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<PriceRuleResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? roomTypeId, [FromQuery] PagedRequest request)
    {
        var result = await _service.GetAllAsync(roomTypeId, request);
        return Ok(result);
    }

    /// <summary>
    /// Calculates the total price for a stay, broken down by day.
    /// Applies all active price rules (seasonal, special date, global and room-type-specific).
    /// The checkout date is not charged — only nights between check-in and check-out are counted.
    /// </summary>
    /// <param name="dto">Room type Id, check-in date and check-out date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Price breakdown: total base price, total final price, number of nights,
    /// and per-day details with applied rules and price deltas.
    /// </returns>
    /// <response code="200">Returns the price calculation result.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="422">Validation failed (invalid dates, less than 1 night).</response>
    [HttpGet("calculate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PriceCalculationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PriceCalculationResponseDto>> Calculate(
        [FromQuery] PriceCalculationRequestDto dto, CancellationToken ct)
    {
        return Ok(await _service.CalculatePriceAsync(dto, ct));
    }

    /// <summary>
    /// Creates a new price rule.
    /// For <c>SpecialDate</c> rules, <c>StartDate</c> and <c>EndDate</c> must be equal.
    /// For percentage rules, <c>Value</c> must be between 1 and 100.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="dto">Price rule data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP 201 Created with the new rule Id.</returns>
    /// <response code="201">Price rule created.</response>
    /// <response code="404">Room type not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost]
    [Authorize(Policy = "PriceRuleWrite")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreatePriceRuleDto dto, CancellationToken ct)
    {
        var id = await ((PriceRuleService)_service).CreateAsync(
            dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    /// <summary>
    /// Updates an existing price rule.
    /// Room type cannot be changed after creation.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="id">Price rule Id.</param>
    /// <param name="dto">Updated price rule data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Price rule updated.</response>
    /// <response code="404">Price rule not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "PriceRuleWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePriceRuleDto dto, CancellationToken ct)
    {
        await ((PriceRuleService)_service).UpdateAsync(
            id, dto, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a price rule.
    /// Admin role required.
    /// </summary>
    /// <param name="id">Price rule Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Price rule deleted.</response>
    /// <response code="404">Price rule not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "PriceRuleDelete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ((PriceRuleService)_service).DeleteAsync(
            id, ct,
            actorUserId: GetCurrentUserId(),
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Returns a paginated list of room types that currently have an active discount of 15% or more.
    /// Intended for highlighting promotional offers on the public-facing homepage.
    /// </summary>
    /// <param name="request">Pagination parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of discounted room types.</returns>
    /// <response code="200">Returns the discounted room types.</response>
    [HttpGet("discounted")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RoomTypeResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RoomTypeResponseDto>>> GetDiscounted(
        [FromQuery] PagedRequest request, CancellationToken ct)
    {
        return Ok(await _service.GetDiscountedRoomTypesAsync(request, ct));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}