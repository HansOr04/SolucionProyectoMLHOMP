using System;
using System.Collections.Generic;

namespace APIProyectoMLHOMP.Data.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int NumberOfGuests { get; set; }

    public double TotalPrice { get; set; }

    public int ApartmentId { get; set; }

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
