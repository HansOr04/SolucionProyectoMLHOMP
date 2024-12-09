using Microsoft.EntityFrameworkCore;
using APIProyectoMLHOMP.Data;
using APIProyectoMLHOMP.Data.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
namespace APIProyectoMLHOMP.Controllers;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Booking").WithTags(nameof(Booking));

        group.MapGet("/", async (ProyectoContextPloContext db) =>
        {
            return await db.Bookings.ToListAsync();
        })
        .WithName("GetAllBookings")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Booking>, NotFound>> (int bookingid, ProyectoContextPloContext db) =>
        {
            return await db.Bookings.AsNoTracking()
                .FirstOrDefaultAsync(model => model.BookingId == bookingid)
                is Booking model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetBookingById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int bookingid, Booking booking, ProyectoContextPloContext db) =>
        {
            var affected = await db.Bookings
                .Where(model => model.BookingId == bookingid)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.BookingId, booking.BookingId)
                    .SetProperty(m => m.StartDate, booking.StartDate)
                    .SetProperty(m => m.EndDate, booking.EndDate)
                    .SetProperty(m => m.NumberOfGuests, booking.NumberOfGuests)
                    .SetProperty(m => m.TotalPrice, booking.TotalPrice)
                    .SetProperty(m => m.ApartmentId, booking.ApartmentId)
                    .SetProperty(m => m.UserId, booking.UserId)
                    .SetProperty(m => m.CreatedAt, booking.CreatedAt)
                    .SetProperty(m => m.UpdatedAt, booking.UpdatedAt)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateBooking")
        .WithOpenApi();

        group.MapPost("/", async (Booking booking, ProyectoContextPloContext db) =>
        {
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Booking/{booking.BookingId}",booking);
        })
        .WithName("CreateBooking")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int bookingid, ProyectoContextPloContext db) =>
        {
            var affected = await db.Bookings
                .Where(model => model.BookingId == bookingid)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteBooking")
        .WithOpenApi();
    }
}
