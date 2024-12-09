using Microsoft.EntityFrameworkCore;
using APIProyectoMLHOMP.Data;
using APIProyectoMLHOMP.Data.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
namespace APIProyectoMLHOMP.Controllers;

public static class ApartmentEndpoints
{
    public static void MapApartmentEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Apartment").WithTags(nameof(Apartment));

        group.MapGet("/", async (ProyectoContextPloContext db) =>
        {
            return await db.Apartments.ToListAsync();
        })
        .WithName("GetAllApartments")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Apartment>, NotFound>> (int apartmentid, ProyectoContextPloContext db) =>
        {
            return await db.Apartments.AsNoTracking()
                .FirstOrDefaultAsync(model => model.ApartmentId == apartmentid)
                is Apartment model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetApartmentById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int apartmentid, Apartment apartment, ProyectoContextPloContext db) =>
        {
            var affected = await db.Apartments
                .Where(model => model.ApartmentId == apartmentid)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.ApartmentId, apartment.ApartmentId)
                    .SetProperty(m => m.Title, apartment.Title)
                    .SetProperty(m => m.Description, apartment.Description)
                    .SetProperty(m => m.PricePerNight, apartment.PricePerNight)
                    .SetProperty(m => m.Address, apartment.Address)
                    .SetProperty(m => m.City, apartment.City)
                    .SetProperty(m => m.Country, apartment.Country)
                    .SetProperty(m => m.Bedrooms, apartment.Bedrooms)
                    .SetProperty(m => m.Bathrooms, apartment.Bathrooms)
                    .SetProperty(m => m.MaxOccupancy, apartment.MaxOccupancy)
                    .SetProperty(m => m.ImageUrl, apartment.ImageUrl)
                    .SetProperty(m => m.IsAvailable, apartment.IsAvailable)
                    .SetProperty(m => m.CreatedAt, apartment.CreatedAt)
                    .SetProperty(m => m.UpdatedAt, apartment.UpdatedAt)
                    .SetProperty(m => m.UserId, apartment.UserId)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateApartment")
        .WithOpenApi();

        group.MapPost("/", async (Apartment apartment, ProyectoContextPloContext db) =>
        {
            db.Apartments.Add(apartment);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Apartment/{apartment.ApartmentId}",apartment);
        })
        .WithName("CreateApartment")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int apartmentid, ProyectoContextPloContext db) =>
        {
            var affected = await db.Apartments
                .Where(model => model.ApartmentId == apartmentid)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteApartment")
        .WithOpenApi();
    }
}
