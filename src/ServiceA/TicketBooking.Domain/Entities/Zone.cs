namespace TicketBooking.Domain.Entities;

public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConcertId { get; set; }
    public string Name { get; set; } = string.Empty; // เช่น VIP, Zone A
    public decimal Price { get; set; }
    public int TotalSeats { get; set; }
}