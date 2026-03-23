using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.DTOs.UserDTOs;
using HotelWebApplication.Services;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Manages user profiles, passwords and roles.
/// All endpoints require authentication.
/// Role management requires Admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IReservationService _reservationService;

    public UserController(IUserService userService, IReservationService reservationService)
    {
        _userService = userService;
        _reservationService = reservationService;
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user.
    /// </summary>
    /// <returns>User profile: email, display name, phone and role.</returns>
    /// <response code="200">Returns the user profile.</response>
    /// <response code="401">Authentication required.</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _userService.GetProfileAsync(GetUserId());
        return Ok(profile);
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user (email, display name, phone).
    /// Email uniqueness is validated before saving.
    /// </summary>
    /// <param name="dto">Fields to update (all optional).</param>
    /// <returns>Updated user profile.</returns>
    /// <response code="200">Profile updated successfully.</response>
    /// <response code="409">The new email is already taken by another account.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        var updated = await ((UserService)_userService).UpdateProfileAsync(
            userId, dto,
            actorUserId: userId,
            ip: GetIp());
        return Ok(updated);
    }

    /// <summary>
    /// Changes the password of the currently authenticated user.
    /// The current password must be provided for verification.
    /// After a successful change, all active refresh tokens remain valid
    /// (force re-login by calling logout separately if needed).
    /// </summary>
    /// <param name="dto">Current password and new password.</param>
    /// <response code="204">Password changed successfully.</response>
    /// <response code="401">Current password is incorrect or user is not authenticated.</response>
    /// <response code="422">Validation failed (new password too weak).</response>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetUserId();
        await ((UserService)_userService).ChangePasswordAsync(
            userId, dto,
            actorUserId: userId,
            ip: GetIp());
        return NoContent();
    }

    /// <summary>
    /// Returns a paginated list of all registered users.
    /// Supports search by email or display name.
    /// Admin or Moderator role required.
    /// </summary>
    /// <param name="request">Pagination and search parameters.</param>
    /// <returns>Paginated list of user profiles.</returns>
    /// <response code="200">Returns the user list.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin or Moderator role required.</response>
    [HttpGet("all")]
    [Authorize(Policy = "UserRead")]
    [ProducesResponseType(typeof(PagedResult<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers([FromQuery] PagedRequest request)
    {
        var result = await _userService.GetAllUsersAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Changes the role of a user (e.g. Customer → Moderator).
    /// Assigning the Admin role is not allowed via this endpoint.
    /// Admin role required.
    /// </summary>
    /// <param name="userId">Target user Id.</param>
    /// <param name="dto">New role.</param>
    /// <returns>Updated user profile with the new role.</returns>
    /// <response code="200">Role updated successfully.</response>
    /// <response code="400">Cannot assign Admin role.</response>
    /// <response code="404">User not found.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpPut("{userId:guid}/role")]
    [Authorize(Policy = "UserRoleWrite")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleDto dto)
    {
        var updated = await ((UserService)_userService).UpdateUserRoleAsync(
            userId, dto,
            actorUserId: GetUserId(),
            ip: GetIp());
        return Ok(updated);
    }

    /// <summary>
    /// Returns a paginated list of reservations belonging to the currently authenticated user.
    /// Ordered by creation date descending.
    /// </summary>
    /// <param name="request">Pagination parameters.</param>
    /// <returns>Paginated list of the user's reservations.</returns>
    /// <response code="200">Returns the reservation list.</response>
    /// <response code="401">Authentication required.</response>
    [HttpGet("reservations")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyReservations([FromQuery] PagedRequest request)
    {
        var result = await _reservationService.GetMyReservationsAsync(GetUserId(), request);
        return Ok(result);
    }

    /// <summary>
    /// Cancels one of the authenticated user's own reservations.
    /// Cancellation is only allowed at least 7 days before the check-in date.
    /// </summary>
    /// <param name="reservationId">Reservation GUID.</param>
    /// <response code="204">Reservation cancelled successfully.</response>
    /// <response code="400">Cancellation not allowed (less than 7 days before check-in).</response>
    /// <response code="403">The reservation does not belong to the current user.</response>
    /// <response code="404">Reservation not found.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPost("reservations/{reservationId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelMyReservation(Guid reservationId)
    {
        var reservation = await _reservationService.GetByIdAsync(reservationId)
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.UserId != GetUserId())
            return Forbid();

        var daysUntilCheckIn = (reservation.StartDate - DateTime.UtcNow).TotalDays;
        if (daysUntilCheckIn < 7)
            return BadRequest(new { message = "Reservations can only be cancelled at least 7 days before check-in." });

        await _reservationService.CancelAsync(reservationId, GetUserId(), GetIp());
        return NoContent();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}