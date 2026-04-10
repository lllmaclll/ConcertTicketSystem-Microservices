using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TicketBooking.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // เก็บตัวที่เข้ารหัสแล้วเท่านั้น
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // 🔥 เพิ่ม: ค่าเริ่มต้นเป็น User
    }
}