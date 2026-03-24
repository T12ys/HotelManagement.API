using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Simulates payment processing for reservations.
/// This is a mock implementation — no real payment gateway is involved.
/// A successful payment transitions a reservation from <c>Pending</c> to <c>Confirmed</c>.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public PaymentsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    /// <summary>
    /// Processes a mock payment for a reservation.
    /// When <c>SimulateSuccess</c> is <c>true</c>, the reservation transitions from
    /// <c>Pending</c> → <c>Confirmed</c> and <c>PaidAt</c> is set to the current UTC time.
    /// When <c>SimulateSuccess</c> is <c>false</c>, a payment failure is simulated
    /// and the reservation remains in <c>Pending</c> status.
    /// Returns 422 if the 15-minute hold has expired — the user must create a new reservation.
    /// </summary>
    /// <param name="dto">Reservation Id and success/failure flag.</param>
    /// <returns>Updated reservation with new status and PaidAt timestamp.</returns>
    /// <response code="200">Payment processed. Reservation is now Confirmed.</response>
    /// <response code="404">Reservation not found.</response>
    /// <response code="422">
    /// Payment failed (PAYMENT_FAILED) or hold expired (HOLD_EXPIRED).
    /// The response body contains an error <c>code</c> field for client-side handling.
    /// </response>
    [HttpPost("mock")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> MockPayment([FromBody] MockPaymentDto dto)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _reservations.ProcessMockPaymentAsync(dto.ReservationId, dto.SimulateSuccess, ip);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "HOLD_EXPIRED")
        {
            return UnprocessableEntity(new
            {
                message = "The reservation hold has expired. Please create a new reservation.",
                code = "HOLD_EXPIRED"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "PAYMENT_FAILED")
        {
            return UnprocessableEntity(new
            {
                message = "Payment was declined. Please try again.",
                code = "PAYMENT_FAILED"
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}