namespace HotelWebApplication.DTOs.ReservationDTOs;

public class MockPaymentDto
{
    public Guid ReservationId { get; set; }

    /// <summary>
    /// Симулировать успех (true) или неудачу (false)
    /// </summary>
    public bool SimulateSuccess { get; set; } = true;
}