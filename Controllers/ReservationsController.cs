using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Публичные эндпоинты для бронирования (без авторизации)
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
    /// Создать бронь. Атомарная проверка доступности, статус Pending + hold 15 мин.
    /// </summary>
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
            return Conflict(new { message = "Выбранный период уже занят. Пожалуйста, выберите другие даты." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Получить бронь по Id (для отображения страницы подтверждения)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _reservations.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Список всех броней с фильтрацией (статус, комната, даты, email клиента)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "ReservationRead")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ReservationFilterRequest filter)
    {
        var result = await _reservations.GetAllAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Изменить даты / статус / заметки брони. Проверяет доступность при смене дат.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ReservationWrite")]
    [ProducesResponseType(typeof(ReservationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
            return Conflict(new { message = "Новые даты пересекаются с существующей бронью." });
        }
    }

    /// <summary>
    /// Отменить бронь (Admin или Moderator)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ReservationCancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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