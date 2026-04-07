using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Entities;

namespace TicketBooking.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Concert> Concerts { get; }
    DbSet<Zone> Zones { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}