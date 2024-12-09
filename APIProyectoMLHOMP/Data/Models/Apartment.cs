using System;
using System.Collections.Generic;

namespace APIProyectoMLHOMP.Data.Models;

public partial class Apartment
{
    public int ApartmentId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public double PricePerNight { get; set; }

    public string Address { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Country { get; set; } = null!;

    public int Bedrooms { get; set; }

    public int Bathrooms { get; set; }

    public int MaxOccupancy { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int UserId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual User User { get; set; } = null!;
}
