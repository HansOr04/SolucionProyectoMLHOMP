using Microsoft.EntityFrameworkCore;
using ProyectoMLHOMP.Models;

public class ProyectoContext : DbContext
{
    public ProyectoContext(DbContextOptions<ProyectoContext> options)
        : base(options)
    {
    }

    public DbSet<User> User { get; set; } = default!;
    public DbSet<Apartment> Apartment { get; set; } = default!;
    public DbSet<Booking> Booking { get; set; } = default!;
    public DbSet<Review> Review { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de User
        modelBuilder.Entity<User>()
            .HasMany(u => u.ApartmentsOwned)
            .WithOne(a => a.Owner)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.BookingsAsGuest)
            .WithOne(b => b.Guest)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ReviewsWritten)
            .WithOne(r => r.Reviewer)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración de índices únicos
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
    }
}