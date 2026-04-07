using TicketBooking.Domain.Enums;

namespace TicketBooking.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConcertId { get; set; }
        public Guid ZoneId { get; set; } // ตั๋วนี้อยู่โซนไหน
        public decimal Price { get; set; } // ราคาตั๋วใบนี้
        public string SeatNumber { get; set; } = string.Empty; // เช่น A1, B12
        public TicketStatus Status { get; set; } = TicketStatus.Available;
        public Guid? UserId { get; set; } // ใครเป็นคนจอง
         public DateTime? CreatedAt { get; set; } // เก็บเวลาที่เริ่มจอง (Locked)
    }
}