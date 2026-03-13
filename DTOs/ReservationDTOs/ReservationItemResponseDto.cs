public class ReservationItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }  // Price * Quantity
}