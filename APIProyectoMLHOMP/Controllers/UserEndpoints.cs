using Microsoft.EntityFrameworkCore;
using APIProyectoMLHOMP.Data;
using APIProyectoMLHOMP.Data.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;

namespace APIProyectoMLHOMP.Controllers;

public record LoginRequest(string Username, string Password);
public record LoginResponse(bool Success, string Message, User? User);
public record RegisterRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string Address,
    string City,
    string Country,
    string Email,
    string PhoneNumber,
    string Username,
    string Password,
    string? Biography,
    string? Languages
);

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/User").WithTags(nameof(User));

        // Login
        group.MapPost("/login", async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>>> (
            LoginRequest request,
            ProyectoContextPloContext db) =>
        {
            try
            {
                // Validar campos requeridos
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Usuario y contraseña son requeridos", null));
                }

                // Buscar usuario por username
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Usuario no encontrado", null));
                }

                // Verificar contraseña usando el método de User
                if (!user.VerifyPassword(request.Password))
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Contraseña incorrecta", null));
                }

                // Login exitoso
                return TypedResults.Ok(new LoginResponse(
                    Success: true,
                    Message: $"¡Bienvenido de nuevo, {user.FirstName}!",
                    User: user
                ));
            }
            catch (Exception)
            {
                return TypedResults.BadRequest(new LoginResponse(
                    Success: false,
                    Message: "Error al iniciar sesión. Por favor, intente nuevamente.",
                    User: null
                ));
            }
        })
        .WithName("Login")
        .WithOpenApi();

        // Register
        group.MapPost("/register", async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>>> (
            RegisterRequest request,
            ProyectoContextPloContext db) =>
        {
            try
            {
                // Validar campos requeridos
                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Todos los campos son requeridos", null));
                }

                // Verificar usuario existente
                var existingUser = await db.Users
                    .AnyAsync(u => u.Email == request.Email || u.Username == request.Username);

                if (existingUser)
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "El email o nombre de usuario ya está en uso", null));
                }

                var newUser = new User
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    DateOfBirth = request.DateOfBirth,
                    Address = request.Address.Trim(),
                    City = request.City.Trim(),
                    Country = request.Country.Trim(),
                    Email = request.Email.Trim().ToLower(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    Username = request.Username.Trim(),
                    PasswordHash = User.HashPassword(request.Password),
                    Biography = request.Biography?.Trim() ?? "",
                    Languages = request.Languages?.Trim() ?? "",
                    ProfileImageUrl = "/images/default-profile.jpg",
                    RegistrationDate = DateTime.UtcNow,
                    IsHost = false,
                    IsVerified = false
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();

                return TypedResults.Ok(new LoginResponse(
                    Success: true,
                    Message: "Usuario registrado exitosamente",
                    User: newUser
                ));
            }
            catch (Exception)
            {
                return TypedResults.BadRequest(new LoginResponse(
                    Success: false,
                    Message: "Error al registrar el usuario",
                    User: null
                ));
            }
        })
        .WithName("Register")
        .WithOpenApi();

        // Get All Users
        group.MapGet("/", async (ProyectoContextPloContext db) =>
        {
            return await db.Users
                .AsNoTracking()
                .ToListAsync();
        })
        .WithName("GetAllUsers")
        .WithOpenApi();

        // Get User by ID
        group.MapGet("/{id}", async Task<Results<Ok<User>, NotFound>> (int userid, ProyectoContextPloContext db) =>
        {
            return await db.Users
                .AsNoTracking()
                .Include(u => u.ApartmentsOwned)
                .Include(u => u.BookingsAsGuest)
                .Include(u => u.ReviewsWritten)
                .FirstOrDefaultAsync(model => model.UserId == userid)
                is User model
                    ? TypedResults.Ok(model)
                    : TypedResults.NotFound();
        })
        .WithName("GetUserById")
        .WithOpenApi();

        // Update User
        group.MapPut("/{id}", async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>>> (
            int userid,
            User updatedUser,
            ProyectoContextPloContext db) =>
        {
            try
            {
                var user = await db.Users.FindAsync(userid);

                if (user == null)
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Usuario no encontrado", null));
                }

                // Actualizar propiedades
                user.FirstName = updatedUser.FirstName;
                user.LastName = updatedUser.LastName;
                user.Address = updatedUser.Address;
                user.City = updatedUser.City;
                user.Country = updatedUser.Country;
                user.PhoneNumber = updatedUser.PhoneNumber;
                user.Biography = updatedUser.Biography;
                user.Languages = updatedUser.Languages;
                user.IsHost = updatedUser.IsHost;
                user.ProfileImageUrl = updatedUser.ProfileImageUrl;

                await db.SaveChangesAsync();

                return TypedResults.Ok(new LoginResponse(
                    Success: true,
                    Message: "Perfil actualizado exitosamente",
                    User: user
                ));
            }
            catch (Exception)
            {
                return TypedResults.BadRequest(new LoginResponse(
                    Success: false,
                    Message: "Error al actualizar el perfil",
                    User: null
                ));
            }
        })
        .WithName("UpdateUser")
        .WithOpenApi();

        // Delete User
        group.MapDelete("/{id}", async Task<Results<Ok<LoginResponse>, BadRequest<LoginResponse>>> (
            int userid,
            ProyectoContextPloContext db) =>
        {
            try
            {
                var user = await db.Users.FindAsync(userid);

                if (user == null)
                {
                    return TypedResults.BadRequest(new LoginResponse(false, "Usuario no encontrado", null));
                }

                db.Users.Remove(user);
                await db.SaveChangesAsync();

                return TypedResults.Ok(new LoginResponse(
                    Success: true,
                    Message: "Usuario eliminado exitosamente",
                    User: null
                ));
            }
            catch (Exception)
            {
                return TypedResults.BadRequest(new LoginResponse(
                    Success: false,
                    Message: "Error al eliminar el usuario",
                    User: null
                ));
            }
        })
        .WithName("DeleteUser")
        .WithOpenApi();

        // Get User Bookings
        group.MapGet("/{id}/bookings", async Task<Results<Ok<List<Booking>>, NotFound>> (
            int userid,
            ProyectoContextPloContext db) =>
        {
            var bookings = await db.Users
                .Where(u => u.UserId == userid)
                .SelectMany(u => u.BookingsAsGuest)
                .Include(b => b.Apartment)
                .ToListAsync();

            return bookings.Any()
                ? TypedResults.Ok(bookings)
                : TypedResults.NotFound();
        })
        .WithName("GetUserBookings")
        .WithOpenApi();

        // Get User Reviews
        group.MapGet("/{id}/reviews", async Task<Results<Ok<List<Review>>, NotFound>> (
            int userid,
            ProyectoContextPloContext db) =>
        {
            var reviews = await db.Users
                .Where(u => u.UserId == userid)
                .SelectMany(u => u.ReviewsWritten)
                .Include(r => r.Apartment)
                .ToListAsync();

            return reviews.Any()
                ? TypedResults.Ok(reviews)
                : TypedResults.NotFound();
        })
        .WithName("GetUserReviews")
        .WithOpenApi();

        // Get User Apartments
        group.MapGet("/{id}/apartments", async Task<Results<Ok<List<Apartment>>, NotFound>> (
            int userid,
            ProyectoContextPloContext db) =>
        {
            var apartments = await db.Users
                .Where(u => u.UserId == userid)
                .SelectMany(u => u.ApartmentsOwned)
                .Include(a => a.Reviews)
                .ToListAsync();

            return apartments.Any()
                ? TypedResults.Ok(apartments)
                : TypedResults.NotFound();
        })
        .WithName("GetUserApartments")
        .WithOpenApi();
    }
}