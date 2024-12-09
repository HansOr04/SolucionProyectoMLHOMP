using System;
using System.Collections.Generic;
using APIProyectoMLHOMP.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace APIProyectoMLHOMP.Data;

public partial class ProyectoContextPloContext : DbContext
{
    public ProyectoContextPloContext()
    {
    }

    public ProyectoContextPloContext(DbContextOptions<ProyectoContextPloContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Apartment> Apartments { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=ProyectoContextPLO");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Apartment>(entity =>
        {
            entity.ToTable("Apartment");

            entity.HasIndex(e => e.UserId, "IX_Apartment_UserId");

            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.ApartmentsOwned)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Booking");

            entity.HasIndex(e => e.ApartmentId, "IX_Booking_ApartmentId");

            entity.HasIndex(e => e.UserId, "IX_Booking_UserId");

            entity.HasOne(d => d.Apartment).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ApartmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.BookingsAsGuest)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Review");

            entity.HasIndex(e => e.ApartmentId, "IX_Review_ApartmentId");

            entity.HasIndex(e => e.UserId, "IX_Review_UserId");

            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Apartment).WithMany(p => p.Reviews).HasForeignKey(d => d.ApartmentId);

            entity.HasOne(d => d.User).WithMany(p => p.ReviewsWritten)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "IX_User_Email").IsUnique();

            entity.HasIndex(e => e.Username, "IX_User_Username").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(100);
            entity.Property(e => e.Biography).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(50);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.Languages).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
