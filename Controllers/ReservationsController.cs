using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages hotel reservations.
/// Creating a reservation is public (no authentication required).
/// Viewing the list, editing and cancelling reservations require Admin or Moderator role.
/// </summary>
[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    /// <summary>
    /// Creates a new reservation with an atomic availability check.
    /// The reservation is placed in <c>Pending</c> status with a 15-minute hold (HeldUntil).
    /// If the requested period is already booked, returns 409 Conflict.
    /// Call <c>POST /api/payments/mock</c> to confirm the reservation before the hold expires.
    /// </summary>
    /// <param name="dto">Reservation details: room type, guest info, dates, optional add-on items.</param>
    /// <returns>Created reservation with Id, status, HeldUntil and total price.</returns>
    /// <response code="201">Reservation created. Status is Pending.</response>
    /// <response code="409">The selected dates are already booked for this room type.</response>
    /// <response code="422">Validation failed (invalid dates, missing required fields).</response>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
            dto.UserId = Guid.Parse(userIdClaim);

        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _reservations.CreateAsync(dto, ip);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "CONFLICT")
        {
            return Conflict(new { message = "The selected period is already booked. Please choose different dates." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns a single reservation by its GUID.
    /// Used to display the booking confirmation page after creation or payment.
    /// No authentication required — the client uses the reservation Id obtained from the create response.
    /// </summary>
    /// <param name="id">Reservation GUID.</param>
    /// <returns>Full reservation details including status, dates, total price and add-on items.</returns>
    /// <response code="200">Reservation found.</response>
    /// <response code="404">Reservation not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _reservations.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns a paginated list of all reservations with optional filters.
    /// Accessible by Admin and Moderator roles only.
    /// Supports filtering by room type, room Id, status, date range and customer email.
    /// </summary>
    /// <param name="filter">Filter and pagination parameters.</param>
    /// <returns>Paginated list of reservations.</returns>
    /// <response code="200">Returns the filtered and paginated reservation list.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Insufficient permissions (Admin or Moderator required).</response>
    [HttpGet]
    [Authorize(Policy = "ReservationRead")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] ReservationFilterRequest filter)
    {
        var result = await _reservations.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Updates reservation dates, status or notes.
    /// When dates are changed, an availability conflict check is performed.
    /// Cannot be used to manually set status to <c>Confirmed</c> — use <c>POST /api/payments/mock</c> for that.
    /// Accessible by Admin and Moderator roles only.
    /// </summary>
    /// <param name="id">Reservation GUID.</param>
    /// <param name="dto">Fields to update (all optional — only provided fields are applied).</param>
    /// <returns>Updated reservation.</returns>
    /// <response code="200">Reservation updated successfully.</response>
    /// <response code="404">Reservation not found.</response>
    /// <response code="409">New dates conflict with an existing reservation.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ReservationWrite")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReservationDto dto)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _reservations.UpdateAsync(id, dto, actorId, ip);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "CONFLICT")
        {
            return Conflict(new { message = "New dates overlap with an existing reservation." });
        }
    }

    /// <summary>
    /// Cancels a reservation, changing its status to <c>Cancelled</c>.
    /// The action is recorded in the audit log.
    /// Accessible by Admin and Moderator roles only.
    /// </summary>
    /// <param name="id">Reservation GUID.</param>
    /// <response code="204">Reservation cancelled successfully.</response>
    /// <response code="404">Reservation not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ReservationCancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var actorId = GetCurrentUserId();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _reservations.CancelAsync(id, actorId, ip);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException("User ID claim not found.");
        return Guid.Parse(claim);
    }
}