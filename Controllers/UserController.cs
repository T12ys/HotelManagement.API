using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.DTOs.UserDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // все эндпоинты требуют авторизацию
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IReservationService _reservationService;

    public UserController(IUserService userService, IReservationService reservationService)
    {
        _userService = userService;
        _reservationService = reservationService;
    }

    // Вспомогательный метод — достаём userId из JWT
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

 
    // GET api/user/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _userService.GetProfileAsync(GetUserId());
        return Ok(profile);
    }

    // PUT api/user/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var updated = await _userService.UpdateProfileAsync(GetUserId(), dto);
        return Ok(updated);
    }

    // POST api/user/change-password
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _userService.ChangePasswordAsync(GetUserId(), dto);
        return NoContent();
    }

    [HttpGet("all")]
    [Authorize(Policy = "UserRead")]
    public async Task<IActionResult> GetAllUsers([FromQuery] PagedRequest request)
    {
        var result = await _userService.GetAllUsersAsync(request);
        return Ok(result);
    }

    // PUT api/user/{userId}/role — только Admin
    [HttpPut("{userId:guid}/role")]
    [Authorize(Policy = "UserRoleWrite")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleDto dto)
    {
        var updated = await _userService.UpdateUserRoleAsync(userId, dto);
        return Ok(updated);
    }

    [HttpGet("reservations")]
    public async Task<IActionResult> GetMyReservations([FromQuery] PagedRequest request)
    {
        var result = await _reservationService.GetMyReservationsAsync(GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("reservations/{reservationId:guid}/cancel")]
    public async Task<IActionResult> CancelMyReservation(Guid reservationId)
    {
        var reservation = await _reservationService.GetByIdAsync(reservationId)
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.UserId != GetUserId())
            return Forbid();

        var daysUntilCheckIn = (reservation.StartDate - DateTime.UtcNow).TotalDays;
        if (daysUntilCheckIn < 7)
            return BadRequest(new { message = "Отменить бронирование можно не позднее чем за 7 дней до заезда." });

        await _reservationService.CancelAsync(reservationId, GetUserId());
        return NoContent();
    }
}