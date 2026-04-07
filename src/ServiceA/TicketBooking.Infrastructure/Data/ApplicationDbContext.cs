using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Entities;
using TicketBooking.Application.Interfaces;

namespace TicketBooking.Infrastructure.Data
{
    // 2. เพิ่ม IApplicationDbContext ตรงนี้
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // สร้างตาราง 4 ตาราง
        public DbSet<Concert> Concerts { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Zone> Zones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // สามารถเขียน Setting เพิ่มเติมได้ที่นี่ เช่น Index หรือความยาวตัวอักษร

            // ---------------------------------------------------
            // 1. กฎของตาราง Ticket (ตั๋ว)
            // ---------------------------------------------------
            modelBuilder.Entity<Ticket>(entity =>
            {
                // กฎข้อ 1: บังคับความยาวของเลขที่นั่ง (SeatNumber) ไม่ให้เกิน 10 ตัวอักษร (เช่น "VIP-A01")
                // ช่วยประหยัดพื้นที่ Database และทำให้ค้นหาเร็วขึ้น
                entity.Property(t => t.SeatNumber)
                    .HasMaxLength(10)
                    .IsRequired();

                // กฎข้อ 2: "1 คอนเสิร์ต ห้ามมีเลขที่นั่งซ้ำกันเด็ดขาด!" (Unique Index)
                // ถ้ามีใครแฮ็กระบบหรือเกิดบั๊ก พยายามจองที่นั่ง A1 ซ้ำในคอนเสิร์ตเดียวกัน Database จะเตะออกทันที (กันตั๋วผี 100%)
                entity.HasIndex(t => new { t.ConcertId, t.SeatNumber })
                    .IsUnique();
            });

            // ---------------------------------------------------
            // 2. กฎของตาราง Concert (คอนเสิร์ต)
            // ---------------------------------------------------
            modelBuilder.Entity<Concert>(entity =>
            {
                // กฎข้อ 3: ชื่อคอนเสิร์ตห้ามว่าง และจำกัดความยาวไม่เกิน 200 ตัวอักษร
                entity.Property(c => c.Name)
                    .HasMaxLength(200)
                    .IsRequired();
            });

            // ---------------------------------------------------
            // 3. กฎของตาราง Zone (โซนที่นั่ง)
            // ---------------------------------------------------
            modelBuilder.Entity<Zone>(entity =>
            {
                // กฎข้อ 4: ชื่อโซนห้ามว่าง และจำกัดความยาวไม่เกิน 50 ตัวอักษร
                entity.Property(z => z.Name).HasMaxLength(50).IsRequired();
            });
        }
    }
}