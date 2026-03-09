namespace HotelWebApplication.DTOs.PriceDTOs;

public class PriceCalculationRequestDto
{
    public int RoomTypeId { get; set; }

    public DateTime StartDate { get; set; }

    // Дата выезда не включается в оплату
    // Пример: заезд 1 янв, выезд 3 янв = 2 ночи (1 янв и 2 янв)
    public DateTime EndDate { get; set; }
}