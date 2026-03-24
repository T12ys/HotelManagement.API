namespace HotelWebApplication.DTOs.ReservationDTOs;

public class MockPaymentDto
{
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Simulate success (true) or failure (false)
    /// </summary>
    public bool SimulateSuccess { get; set; } = true;
}