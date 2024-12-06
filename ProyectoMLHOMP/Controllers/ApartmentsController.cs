using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoMLHOMP.Models;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace ProyectoMLHOMP.Controllers
{
    [Authorize]
    public class ApartmentsController : Controller
    {
        private readonly ProyectoContext _context;
        private readonly ILogger<ApartmentsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ApartmentsController(ProyectoContext context,
                                  ILogger<ApartmentsController> logger,
                                  IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Usuario no autenticado o ID de usuario inválido");
            }
            return userId;
        }

        private bool IsValidImageFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return false;

                // Verificar la extensión
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return false;

                // Verificar el tipo MIME
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    return false;

                // Verificar el contenido real del archivo
                using (var reader = new BinaryReader(file.OpenReadStream()))
                {
                    var signatures = new Dictionary<string, List<byte[]>>
                    {
                        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
                        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
                        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } }
                    };

                    var headerBytes = reader.ReadBytes(8);
                    return signatures[extension].Any(signature =>
                        headerBytes.Take(signature.Length).SequenceEqual(signature));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el archivo de imagen");
                return false;
            }
        }

        private async Task<(bool success, string imagePath, string errorMessage)> ProcessApartmentImage(
            IFormFile imageFile,
            string? currentImagePath = null)
        {
            try
            {
                // Validar el archivo
                if (!IsValidImageFile(imageFile))
                {
                    return (false, string.Empty, "El archivo no es una imagen válida");
                }

                // Crear directorio si no existe
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "apartments");
                Directory.CreateDirectory(uploadsFolder);

                // Generar nombre único para la imagen
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Guardar nueva imagen
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                // Eliminar imagen anterior si existe y no es la imagen por defecto
                if (!string.IsNullOrEmpty(currentImagePath) &&
                    currentImagePath != "/images/apartments/default-apartment.jpg" &&
                    currentImagePath != "/images/default-apartment.jpg")
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, currentImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                return (true, $"/images/apartments/{uniqueFileName}", string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la imagen");
                return (false, string.Empty, $"Error al procesar la imagen: {ex.Message}");
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userIsHost = User.IsInRole("Host");

                ViewData["CurrentUserId"] = userId;
                ViewData["IsHost"] = userIsHost;

                var apartments = userIsHost
                    ? await _context.Apartment
                        .Include(a => a.Owner)
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.CreatedAt)
                        .ToListAsync()
                    : await _context.Apartment
                        .Include(a => a.Owner)
                        .Where(a => a.IsAvailable)
                        .OrderByDescending(a => a.CreatedAt)
                        .ToListAsync();

                if (userIsHost && !apartments.Any())
                {
                    TempData["Info"] = "No tienes departamentos registrados. ¡Comienza a publicar ahora!";
                }

                return View(apartments);
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Acceso no autorizado al intentar ver los departamentos");
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los departamentos");
                TempData["Error"] = "Ocurrió un error al cargar los departamentos";
                return View(new List<Apartment>());
            }
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Intento de ver detalles con ID nulo");
                TempData["Error"] = "ID de departamento no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var apartment = await _context.Apartment
                    .Include(a => a.Owner)
                    .Include(a => a.Bookings)
                    .Include(a => a.Reviews)
                        .ThenInclude(r => r.Reviewer)
                    .FirstOrDefaultAsync(m => m.ApartmentId == id);

                if (apartment == null)
                {
                    _logger.LogWarning($"Departamento no encontrado: {id}");
                    TempData["Error"] = "Departamento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();
                ViewBag.IsOwner = apartment.UserId == userId;
                ViewBag.CurrentUserId = userId;

                return View(apartment);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar los detalles del departamento {id}");
                TempData["Error"] = "Error al cargar los detalles del departamento";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Host")]
        public IActionResult Create()
        {
            return View(new Apartment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Create([Bind("Title,Description,PricePerNight,Address,City,Country,Bedrooms,Bathrooms,MaxOccupancy,ImageFile")] Apartment apartment)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)
                                        .ToList();
                    foreach (var error in errors)
                    {
                        _logger.LogWarning($"Error de validación: {error}");
                    }
                    return View(apartment);
                }

                // Validaciones de negocio
                if (apartment.PricePerNight <= 0)
                {
                    ModelState.AddModelError("PricePerNight", "El precio debe ser mayor a 0");
                    return View(apartment);
                }

                if (apartment.MaxOccupancy <= 0)
                {
                    ModelState.AddModelError("MaxOccupancy", "La ocupación máxima debe ser mayor a 0");
                    return View(apartment);
                }

                // Validación de imagen
                if (apartment.ImageFile == null)
                {
                    ModelState.AddModelError("ImageFile", "Debe seleccionar una imagen");
                    return View(apartment);
                }

                if (apartment.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("ImageFile", "El archivo de imagen está vacío");
                    return View(apartment);
                }

                if (apartment.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "La imagen no puede superar los 5MB");
                    return View(apartment);
                }

                var (success, imagePath, errorMessage) = await ProcessApartmentImage(apartment.ImageFile);
                if (!success)
                {
                    ModelState.AddModelError("ImageFile", errorMessage);
                    return View(apartment);
                }

                try
                {
                    apartment.ImageUrl = imagePath;
                    apartment.UserId = GetCurrentUserId();
                    apartment.CreatedAt = DateTime.UtcNow;
                    apartment.IsAvailable = true;

                    _context.Add(apartment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Departamento creado exitosamente: {apartment.Title}");
                    TempData["Success"] = "Departamento creado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    // Limpiar la imagen si falló el guardado
                    if (!string.IsNullOrEmpty(imagePath) &&
                        imagePath != "/images/apartments/default-apartment.jpg")
                    {
                        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }

                    _logger.LogError(ex, "Error de base de datos al crear departamento");
                    ModelState.AddModelError("", "Error al guardar en la base de datos. Por favor, intente nuevamente.");
                    return View(apartment);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear departamento");
                ModelState.AddModelError("", "Ocurrió un error inesperado. Por favor, intente nuevamente.");
                return View(apartment);
            }
        }

        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Intento de editar con ID nulo");
                TempData["Error"] = "ID de departamento no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var apartment = await _context.Apartment.FindAsync(id);
                if (apartment == null)
                {
                    _logger.LogWarning($"Departamento no encontrado para edición: {id}");
                    TempData["Error"] = "Departamento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();
                if (apartment.UserId != userId)
                {
                    _logger.LogWarning($"Usuario {userId} intentó editar un departamento que no le pertenece: {id}");
                    TempData["Error"] = "No tienes permiso para editar este departamento";
                    return RedirectToAction(nameof(Index));
                }

                return View(apartment);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar el formulario de edición para el departamento {id}");
                TempData["Error"] = "Error al cargar el formulario de edición";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Edit(int id, [Bind("ApartmentId,Title,Description,PricePerNight,Address,City,Country,Bedrooms,Bathrooms,MaxOccupancy,IsAvailable,ImageFile,ImageUrl")] Apartment apartment)
        {
            if (id != apartment.ApartmentId)
            {
                _logger.LogWarning($"ID no coincidente en edición: {id} vs {apartment.ApartmentId}");
                TempData["Error"] = "ID de departamento no coincide";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(apartment);
                }

                var userId = GetCurrentUserId();
                var originalApartment = await _context.Apartment.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.ApartmentId == id);

                if (originalApartment == null)
                {
                    TempData["Error"] = "Departamento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (originalApartment.UserId != userId)
                {
                    _logger.LogWarning($"Usuario {userId} intentó editar un departamento que no le pertenece: {id}");
                    TempData["Error"] = "No tienes permiso para editar este departamento";
                    return RedirectToAction(nameof(Index));
                }

                // Validaciones de negocio
                if (apartment.PricePerNight <= 0)
                {
                    ModelState.AddModelError("PricePerNight", "El precio debe ser mayor a 0");
                    return View(apartment);
                }

                if (apartment.MaxOccupancy <= 0)
                {
                    ModelState.AddModelError("MaxOccupancy", "La ocupación máxima debe ser mayor a 0");
                    return View(apartment);
                }

                // Procesar nueva imagen si se proporciona
                if (apartment.ImageFile != null && apartment.ImageFile.Length > 0)
                {
                    if (apartment.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "La imagen no puede superar los 5MB");
                        return View(apartment);
                    }

                    var (success, imagePath, errorMessage) = await ProcessApartmentImage(apartment.ImageFile, originalApartment.ImageUrl);
                    if (!success)
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        return View(apartment);
                    }

                    apartment.ImageUrl = imagePath;
                }
                else
                {
                    apartment.ImageUrl = originalApartment.ImageUrl;
                }

                apartment.UserId = userId;
                apartment.UpdatedAt = DateTime.UtcNow;
                apartment.CreatedAt = originalApartment.CreatedAt;

                try
                {
                    _context.Update(apartment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Departamento actualizado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ApartmentExists(apartment.ApartmentId))
                    {
                        TempData["Error"] = "El departamento ya no existe";
                        return RedirectToAction(nameof(Index));
                    }
                    _logger.LogError(ex, $"Error de concurrencia al actualizar departamento {id}");
                    ModelState.AddModelError("", "Error al actualizar. Otro usuario puede haber modificado este departamento.");
                    return View(apartment);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el departamento {id}");
                ModelState.AddModelError("", "Ocurrió un error al actualizar el departamento");
                return View(apartment);
            }
        }

        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Intento de eliminar con ID nulo");
                TempData["Error"] = "ID de departamento no especificado";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var apartment = await _context.Apartment
                    .Include(a => a.Owner)
                    .Include(a => a.Bookings)
                    .FirstOrDefaultAsync(m => m.ApartmentId == id);

                if (apartment == null)
                {
                    _logger.LogWarning($"Departamento no encontrado para eliminación: {id}");
                    TempData["Error"] = "Departamento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();
                if (apartment.UserId != userId)
                {
                    _logger.LogWarning($"Usuario {userId} intentó eliminar un departamento que no le pertenece: {id}");
                    TempData["Error"] = "No tienes permiso para eliminar este departamento";
                    return RedirectToAction(nameof(Index));
                }

                if (apartment.Bookings?.Any(b => b.StartDate > DateTime.Today) ?? false)
                {
                    TempData["Error"] = "No se puede eliminar el departamento porque tiene reservas futuras";
                    return RedirectToAction(nameof(Index));
                }

                return View(apartment);
            }
            catch (UnauthorizedAccessException)
            {
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cargar la vista de eliminación para el departamento {id}");
                TempData["Error"] = "Error al cargar la vista de eliminación";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var apartment = await _context.Apartment
                    .Include(a => a.Bookings)
                    .FirstOrDefaultAsync(a => a.ApartmentId == id);

                if (apartment == null)
                {
                    _logger.LogWarning($"Departamento no encontrado para eliminación confirmada: {id}");
                    TempData["Error"] = "Departamento no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var userId = GetCurrentUserId();
                if (apartment.UserId != userId)
                {
                    _logger.LogWarning($"Usuario {userId} intentó eliminar un departamento que no le pertenece: {id}");
                    TempData["Error"] = "No tienes permiso para eliminar este departamento";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si hay reservas futuras
                if (apartment.Bookings?.Any(b => b.StartDate > DateTime.Today) ?? false)
                {
                    _logger.LogWarning($"Intento de eliminar departamento {id} con reservas futuras");
                    TempData["Error"] = "No se puede eliminar el departamento porque tiene reservas futuras";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    // Eliminar imagen si no es la por defecto
                    if (!string.IsNullOrEmpty(apartment.ImageUrl) &&
                        apartment.ImageUrl != "/images/apartments/default-apartment.jpg" &&
                        apartment.ImageUrl != "/images/default-apartment.jpg")
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, apartment.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                            _logger.LogInformation($"Imagen eliminada: {apartment.ImageUrl}");
                        }
                    }

                    _context.Apartment.Remove(apartment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Departamento {id} eliminado exitosamente por el usuario {userId}");
                    TempData["Success"] = "Departamento eliminado exitosamente";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, $"Error de base de datos al eliminar el departamento {id}");
                    TempData["Error"] = "Error al eliminar el departamento. Puede tener registros relacionados.";
                    return RedirectToAction(nameof(Index));
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, $"Error al eliminar la imagen del departamento {id}");
                    // Continuar con la eliminación del registro aunque falle la eliminación de la imagen
                    _context.Apartment.Remove(apartment);
                    await _context.SaveChangesAsync();

                    TempData["Warning"] = "Departamento eliminado, pero hubo un error al eliminar la imagen.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Intento de acceso no autorizado en eliminación de departamento");
                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado al eliminar el departamento {id}");
                TempData["Error"] = "Ocurrió un error inesperado al eliminar el departamento";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Verifica si existe un departamento con el ID especificado
        /// </summary>
        /// <param name="id">ID del departamento a verificar</param>
        /// <returns>true si existe, false en caso contrario</returns>
        private bool ApartmentExists(int id)
        {
            try
            {
                return _context.Apartment.Any(e => e.ApartmentId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar la existencia del departamento {id}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si un usuario es propietario de un departamento
        /// </summary>
        /// <param name="apartmentId">ID del departamento</param>
        /// <param name="userId">ID del usuario</param>
        /// <returns>true si es propietario, false en caso contrario</returns>
        private async Task<bool> IsUserApartmentOwner(int apartmentId, int userId)
        {
            try
            {
                return await _context.Apartment
                    .AnyAsync(a => a.ApartmentId == apartmentId && a.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar la propiedad del departamento {apartmentId} para el usuario {userId}");
                return false;
            }
        }

        /// <summary>
        /// Verifica si un departamento tiene reservas futuras
        /// </summary>
        /// <param name="apartmentId">ID del departamento</param>
        /// <returns>true si tiene reservas futuras, false en caso contrario</returns>
        private async Task<bool> HasFutureBookings(int apartmentId)
        {
            try
            {
                return await _context.Booking
                    .AnyAsync(b => b.ApartmentId == apartmentId && b.StartDate > DateTime.Today);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al verificar reservas futuras del departamento {apartmentId}");
                return false;
            }
        }
    }
}