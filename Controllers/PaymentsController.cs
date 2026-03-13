using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Фиктивная платёжная система (симуляция успеха/неудачи)
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
    /// Обработать mock-оплату. SimulateSuccess=true → Pending→Confirmed.
    /// SimulateSuccess=false → возвращает ошибку оплаты (422).
    /// </summary>
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
                message = "Время удержания брони истекло. Пожалуйста, создайте новую бронь.",
                code = "HOLD_EXPIRED"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "PAYMENT_FAILED")
        {
            return UnprocessableEntity(new
            {
                message = "Оплата не прошла. Попробуйте ещё раз.",
                code = "PAYMENT_FAILED"
            });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}