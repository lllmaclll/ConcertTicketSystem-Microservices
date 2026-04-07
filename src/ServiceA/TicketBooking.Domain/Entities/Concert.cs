namespace TicketBooking.Domain.Entities
{
    public class Concert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string PosterImageUrl { get; set; } = string.Empty; // URL รูปภาพ
        public int TotalSeats { get; set; }
    }
}