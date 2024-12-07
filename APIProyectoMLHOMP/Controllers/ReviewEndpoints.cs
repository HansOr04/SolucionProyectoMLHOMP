using Microsoft.EntityFrameworkCore;
using APIProyectoMLHOMP.Data;
using APIProyectoMLHOMP.Data.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
namespace APIProyectoMLHOMP.Controllers;

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Review").WithTags(nameof(Review));

        group.MapGet("/", async (ProyectoContextPloContext db) =>
        {
            return await db.Reviews.ToListAsync();
        })
        .WithName("GetAllReviews")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Review>, NotFound>> (int reviewid, ProyectoContextPloContext db) =>
        {
            return await db.Reviews.AsNoTracking()
                .FirstOrDefaultAsync(model => model.ReviewId == reviewid)
                is Review model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetReviewById")
        .WithOpenApi();

        group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (int reviewid, Review review, ProyectoContextPloContext db) =>
        {
            var affected = await db.Reviews
                .Where(model => model.ReviewId == reviewid)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.ReviewId, review.ReviewId)
                    .SetProperty(m => m.ApartmentId, review.ApartmentId)
                    .SetProperty(m => m.UserId, review.UserId)
                    .SetProperty(m => m.Title, review.Title)
                    .SetProperty(m => m.Content, review.Content)
                    .SetProperty(m => m.OverallRating, review.OverallRating)
                    .SetProperty(m => m.CleanlinessRating, review.CleanlinessRating)
                    .SetProperty(m => m.CommunicationRating, review.CommunicationRating)
                    .SetProperty(m => m.CheckInRating, review.CheckInRating)
                    .SetProperty(m => m.AccuracyRating, review.AccuracyRating)
                    .SetProperty(m => m.LocationRating, review.LocationRating)
                    .SetProperty(m => m.ValueRating, review.ValueRating)
                    .SetProperty(m => m.CreatedDate, review.CreatedDate)
                    .SetProperty(m => m.UpdatedDate, review.UpdatedDate)
                    .SetProperty(m => m.IsApproved, review.IsApproved)
                    );
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("UpdateReview")
        .WithOpenApi();

        group.MapPost("/", async (Review review, ProyectoContextPloContext db) =>
        {
            db.Reviews.Add(review);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/api/Review/{review.ReviewId}",review);
        })
        .WithName("CreateReview")
        .WithOpenApi();

        group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (int reviewid, ProyectoContextPloContext db) =>
        {
            var affected = await db.Reviews
                .Where(model => model.ReviewId == reviewid)
                .ExecuteDeleteAsync();
            return affected == 1 ? TypedResults.Ok() : TypedResults.NotFound();
        })
        .WithName("DeleteReview")
        .WithOpenApi();
    }
}
