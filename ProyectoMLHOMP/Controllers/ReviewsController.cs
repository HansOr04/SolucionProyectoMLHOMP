using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ProyectoMLHOMP.Models;

namespace ProyectoMLHOMP.Controllers
{
    /// <summary>
    /// Controlador que maneja todas las operaciones relacionadas con las reseñas de apartamentos
    /// </summary>
    public class ReviewsController : Controller
    {
        // Contexto de base de datos para acceder a las entidades
        private readonly ProyectoContext _context;
        // Logger para registrar eventos y errores
        private readonly ILogger<ReviewsController> _logger;

        /// <summary>
        /// Constructor que inicializa el contexto de base de datos y el logger
        /// </summary>
        public ReviewsController(ProyectoContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Método GET que muestra el formulario para crear una nueva reseña
        /// Requiere que el usuario esté autenticado
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Create(int? apartmentId)
        {
            // Validar que se proporcione un ID de apartamento
            if (!apartmentId.HasValue)
            {
                _logger.LogWarning("Intento de crear reseña sin ID de apartamento");
                return BadRequest("Se requiere un ID de apartamento válido");
            }

            // Registrar el inicio del proceso de creación
            _logger.LogInformation($"Iniciando creación de reseña para apartamento {apartmentId}");

            try
            {
                // Buscar el apartamento en la base de datos, incluyendo información del propietario
                var apartment = await _context.Apartment
                    .Include(a => a.Owner)
                    .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId);

                // Verificar si el apartamento existe
                if (apartment == null)
                {
                    _logger.LogWarning($"Apartamento {apartmentId} no encontrado");
                    TempData["Error"] = "El apartamento especificado no existe";
                    return RedirectToAction("Index", "Home");
                }

                // Verificar si el usuario está autenticado
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    // Redirigir al login si no está autenticado
                    return RedirectToAction("Login", "Users",
                        new { returnUrl = Url.Action("Create", "Reviews", new { apartmentId = apartmentId }) });
                }

                // Obtener el ID del usuario actual desde los claims
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    _logger.LogError("Error al obtener el ID del usuario");
                    TempData["Error"] = "Error al verificar la identidad del usuario";
                    return RedirectToAction("Details", "Apartments", new { id = apartmentId });
                }

                // Verificar si el usuario ya ha escrito una reseña para este apartamento
                var existingReview = await _context.Review
                    .AnyAsync(r => r.ApartmentId == apartmentId && r.UserId == userId);

                if (existingReview)
                {
                    TempData["Error"] = "Ya has escrito una reseña para este apartamento";
                    return RedirectToAction("Details", "Apartments", new { id = apartmentId });
                }

                // Crear un nuevo objeto Review con los datos iniciales
                var review = new Review
                {
                    ApartmentId = apartmentId.Value,
                    CreatedDate = DateTime.UtcNow
                };

                // Preparar datos para la vista
                ViewData["ApartmentTitle"] = apartment.Title;
                ViewData["ApartmentId"] = apartmentId;
                ViewData["IsOwner"] = (apartment.UserId == userId);

                return View(review);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error inesperado
                _logger.LogError(ex, $"Error al preparar el formulario de reseña para el apartamento {apartmentId}");
                TempData["Error"] = "Ocurrió un error al cargar el formulario. Por favor, intenta nuevamente.";
                return RedirectToAction("Details", "Apartments", new { id = apartmentId });
            }
        }

        /// <summary>
        /// Método POST para procesar la creación de una nueva reseña
        /// Requiere autenticación y token antifalsificación
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("ApartmentId,Title,Content,OverallRating,CleanlinessRating,CommunicationRating,CheckInRating,AccuracyRating,LocationRating,ValueRating")] Review review)
        {
            // Validar el ID del apartamento
            if (review.ApartmentId <= 0)
            {
                _logger.LogError("Intento de crear reseña con ID de apartamento inválido");
                return BadRequest("ID de apartamento inválido");
            }

            try
            {
                // Validar el modelo recibido
                if (!ModelState.IsValid)
                {
                    // Si el modelo no es válido, obtener detalles del apartamento para la vista
                    var apartmentDetails = await _context.Apartment.FindAsync(review.ApartmentId);
                    if (apartmentDetails == null)
                    {
                        return NotFound("Apartamento no encontrado");
                    }

                    // Preparar datos para la vista
                    ViewData["ApartmentTitle"] = apartmentDetails.Title;
                    ViewData["ApartmentId"] = review.ApartmentId;

                    // Registrar los errores de validación
                    var modelErrors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning($"Modelo inválido: {modelErrors}");

                    return View(review);
                }

                // Obtener y validar el ID del usuario actual
                if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                {
                    _logger.LogError("Error al obtener el ID del usuario");
                    return RedirectToAction("Login", "Users");
                }

                // Verificar si el apartamento existe
                var targetApartment = await _context.Apartment
                    .FirstOrDefaultAsync(a => a.ApartmentId == review.ApartmentId);

                if (targetApartment == null)
                {
                    _logger.LogError($"Apartamento {review.ApartmentId} no encontrado al intentar crear reseña");
                    TempData["Error"] = "El apartamento especificado no existe";
                    return RedirectToAction("Index", "Home");
                }

                // Verificar si ya existe una reseña del usuario para este apartamento
                var existingReview = await _context.Review
                    .AnyAsync(r => r.ApartmentId == review.ApartmentId && r.UserId == userId);

                if (existingReview)
                {
                    TempData["Error"] = "Ya has escrito una reseña para este apartamento";
                    return RedirectToAction("Details", "Apartments", new { id = review.ApartmentId });
                }

                // Determinar si el usuario es el propietario del apartamento
                var isOwner = targetApartment.UserId == userId;

                // Completar los datos de la reseña
                review.UserId = userId;
                review.CreatedDate = DateTime.UtcNow;
                review.IsApproved = isOwner; // Las reseñas del propietario se aprueban automáticamente

                // Guardar la reseña en la base de datos
                await _context.Review.AddAsync(review);
                var saveResult = await _context.SaveChangesAsync();

                if (saveResult > 0)
                {
                    // Éxito al guardar
                    TempData["Success"] = "Tu reseña ha sido publicada correctamente";
                    _logger.LogInformation($"Reseña creada exitosamente para el apartamento {review.ApartmentId}");
                    return RedirectToAction("Details", "Apartments", new { id = review.ApartmentId });
                }
                else
                {
                    throw new Exception("No se pudo guardar la reseña en la base de datos");
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier error durante el proceso
                _logger.LogError(ex, $"Error al crear reseña para apartamento {review.ApartmentId}");
                ModelState.AddModelError("", "Ocurrió un error al guardar la reseña. Por favor, intenta nuevamente.");

                // Recuperar datos del apartamento para la vista
                var currentApartment = await _context.Apartment.FindAsync(review.ApartmentId);
                ViewData["ApartmentTitle"] = currentApartment?.Title;
                ViewData["ApartmentId"] = review.ApartmentId;

                return View(review);
            }
        }

        /// <summary>
        /// Método GET para mostrar el formulario de edición de una reseña
        /// Requiere autenticación y verifica que el usuario sea el autor de la reseña
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            // Validar que se proporcione un ID
            if (id == null)
            {
                return NotFound();
            }

            // Buscar la reseña incluyendo datos del apartamento
            var review = await _context.Review
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }

            // Verificar que el usuario actual sea el autor de la reseña
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || review.UserId != int.Parse(userId))
            {
                return Forbid();
            }

            // Preparar datos para la vista
            ViewData["ApartmentTitle"] = review.Apartment?.Title;
            return View(review);
        }

        /// <summary>
        /// Método POST para procesar la actualización de una reseña
        /// Requiere autenticación, token antifalsificación y verifica la autoría
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("ReviewId,ApartmentId,Title,Content,OverallRating,CleanlinessRating,CommunicationRating,CheckInRating,AccuracyRating,LocationRating,ValueRating")] Review review)
        {
            // Verificar que el ID coincida con el de la reseña
            if (id != review.ReviewId)
            {
                return NotFound();
            }

            try
            {
                // Obtener la reseña existente sin tracking para comparación
                var existingReview = await _context.Review
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.ReviewId == id);

                if (existingReview == null)
                {
                    return NotFound();
                }

                // Verificar que el usuario actual sea el autor
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || existingReview.UserId != int.Parse(userId))
                {
                    return Forbid();
                }

                // Mantener los datos que no deben cambiar
                review.UserId = existingReview.UserId;
                review.CreatedDate = existingReview.CreatedDate;
                review.UpdatedDate = DateTime.UtcNow;
                review.IsApproved = existingReview.IsApproved;

                // Actualizar la reseña
                _context.Update(review);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Apartments", new { id = review.ApartmentId });
            }
            catch (DbUpdateConcurrencyException)
            {
                // Manejar errores de concurrencia
                if (!await _context.Review.AnyAsync(r => r.ReviewId == review.ReviewId))
                {
                    return NotFound();
                }
                throw;
            }
        }

        /// <summary>
        /// Método GET para mostrar la confirmación de eliminación de una reseña
        /// Requiere autenticación y verifica la autoría
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            // Validar que se proporcione un ID
            if (id == null)
            {
                return NotFound();
            }

            // Buscar la reseña incluyendo datos del apartamento
            var review = await _context.Review
                .Include(r => r.Apartment)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null)
            {
                return NotFound();
            }

            // Verificar que el usuario actual sea el autor
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || review.UserId != int.Parse(userId))
            {
                return Forbid();
            }

            return View(review);
        }

        /// <summary>
        /// Método POST para procesar la eliminación de una reseña
        /// Requiere autenticación, token antifalsificación y verifica la autoría
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Buscar la reseña a eliminar
            var review = await _context.Review.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            // Verificar que el usuario actual sea el autor
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || review.UserId != int.Parse(userId))
            {
                return Forbid();
            }

            // Guardar el ID del apartamento para la redirección
            var apartmentId = review.ApartmentId;

            // Eliminar la reseña
            _context.Review.Remove(review);
            await _context.SaveChangesAsync();

            // Redirigir a los detalles del apartamento
            return RedirectToAction("Details", "Apartments", new { id = apartmentId });
        }
    }
}