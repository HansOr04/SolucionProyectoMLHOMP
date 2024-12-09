using System;
using System.Collections.Generic;

namespace APIProyectoMLHOMP.Data.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ApartmentId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int OverallRating { get; set; }

    public int CleanlinessRating { get; set; }

    public int CommunicationRating { get; set; }

    public int CheckInRating { get; set; }

    public int AccuracyRating { get; set; }

    public int LocationRating { get; set; }

    public int ValueRating { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public bool IsApproved { get; set; }

    public virtual Apartment Apartment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
