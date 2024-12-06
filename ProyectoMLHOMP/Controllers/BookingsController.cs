using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ProyectoMLHOMP.Models;

namespace ProyectoMLHOMP.Controllers
{
    /// <summary>
    /// Controlador que maneja todas las operaciones relacionadas con reservas de apartamentos
    /// Requiere autenticación para todas las acciones
    /// </summary>
    [Authorize]
    public class BookingsController : Controller
    {
        // Contexto de base de datos para acceder a las entidades
        private readonly ProyectoContext _context;
        // Logger para registro de eventos y errores
        private readonly ILogger<BookingsController> _logger;

        /// <summary>
        /// Constructor que inicializa el contexto de base de datos y el logger
        /// </summary>
        public BookingsController(ProyectoContext context, ILogger<BookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: Muestra todas las reservas del usuario
        /// Para hosts muestra las reservas de sus apartamentos
        /// Para guests muestra sus propias reservas
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Obtener ID del usuario y verificar si es host
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var isHost = User.IsInRole("Host");

                if (isHost)
                {
                    // Obtener reservas de los apartamentos del host
                    var hostBookings = await _context.Booking
                        .Include(b => b.Apartment)
                        .Include(b => b.Guest)
                        .Where(b => b.Apartment.UserId == userId)
                        .OrderByDescending(b => b.CreatedAt)
                        .ToListAsync();
                    return View(hostBookings);
                }
                else
                {
                    // Obtener reservas del usuario guest
                    var userBookings = await _context.Booking
                        .Include(b => b.Apartment)
                        .Include(b => b.Guest)
                        .Where(b => b.UserId == userId)
                        .OrderByDescending(b => b.CreatedAt)
                        .ToListAsync();
                    return View(userBookings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar las reservas");
                TempData["Error"] = "Ocurrió un error al cargar las reservas";
                return View(new List<Booking>());
            }
        }

        /// <summary>
        /// GET: Muestra los detalles de una reserva específica
        /// Verifica que el usuario sea el propietario de la reserva o del apartamento
        /// </summary>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Buscar la reserva con sus relaciones
                var booking = await _context.Booking
                    .Include(b => b.Apartment)
                    .Include(b => b.Guest)
                    .FirstOrDefaultAsync(m => m.BookingId == id);

                if (booking == null)
                {
                    _logger.LogWarning("Reserva {BookingId} no encontrada", id);
                    return NotFound();
                }

                // Verificar autorización
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (booking.UserId != userId && booking.Apartment.UserId != userId)
                {
                    _logger.LogWarning("Usuario {UserId} intentó acceder a la reserva {BookingId} sin autorización", userId, id);
                    return Forbid();
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los detalles de la reserva {BookingId}", id);
                TempData["Error"] = "Error al cargar los detalles de la reserva";
                return RedirectToAction("Index", "Apartments");
            }
        }

        /// <summary>
        /// GET: Muestra el formulario para crear una nueva reserva
        /// Acepta tanto id como apartmentId como parámetros
        /// </summary>
        public async Task<IActionResult> Create(int? id, int? apartmentId)
        {
            // Usar el ID que no sea null, dando preferencia a id
            int? targetApartmentId = id ?? apartmentId;

            _logger.LogInformation("Iniciando proceso de reserva para apartamento {ApartmentId}", targetApartmentId);

            if (targetApartmentId == null)
            {
                _logger.LogWarning("Intento de crear reserva sin ID de apartamento");
                return BadRequest("Se requiere un ID de apartamento válido");
            }

            try
            {
                // Obtener apartamento con sus relaciones
                var apartment = await _context.Apartment
                    .Include(a => a.Owner)
                    .Include(a => a.Bookings)
                    .FirstOrDefaultAsync(a => a.ApartmentId == targetApartmentId);

                if (apartment == null)
                {
                    _logger.LogWarning("Apartamento {ApartmentId} no encontrado", targetApartmentId);
                    return NotFound("El apartamento especificado no existe");
                }

                // Verificar disponibilidad
                if (!apartment.IsAvailable)
                {
                    _logger.LogWarning("Intento de reservar apartamento no disponible {ApartmentId}", targetApartmentId);
                    TempData["Error"] = "Este apartamento no está disponible para reservas";
                    return RedirectToAction("Details", "Apartments", new { id = targetApartmentId });
                }

                // Verificar que no sea el propietario
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (apartment.UserId == userId)
                {
                    _logger.LogWarning("Usuario {UserId} intentó reservar su propio apartamento {ApartmentId}", userId, targetApartmentId);
                    TempData["Error"] = "No puedes reservar tu propio apartamento";
                    return RedirectToAction("Details", "Apartments", new { id = targetApartmentId });
                }

                // Crear objeto reserva con valores iniciales
                var booking = new Booking
                {
                    ApartmentId = apartment.ApartmentId,
                    UserId = userId,
                    StartDate = DateTime.Today.AddDays(1),
                    EndDate = DateTime.Today.AddDays(2),
                    NumberOfGuests = 1,
                    TotalPrice = apartment.PricePerNight,
                    CreatedAt = DateTime.UtcNow
                };

                // Preparar datos para la vista
                ViewData["Apartment"] = apartment;
                ViewData["MaxGuests"] = apartment.MaxOccupancy;
                ViewData["PricePerNight"] = apartment.PricePerNight;
                ViewData["MinStartDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                ViewData["MaxStartDate"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al preparar la creación de reserva para apartamento {ApartmentId}", targetApartmentId);
                TempData["Error"] = "Ocurrió un error al procesar tu solicitud";
                return RedirectToAction("Index", "Apartments");
            }
        }/// <summary>
         /// POST: Procesa la creación de una nueva reserva
         /// Realiza validaciones de fechas, disponibilidad y precios
         /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ApartmentId,StartDate,EndDate,NumberOfGuests,TotalPrice")] Booking booking)
        {
            _logger.LogInformation("Procesando creación de reserva para apartamento {ApartmentId}", booking.ApartmentId);

            try
            {
                // Obtener información del apartamento
                var apartment = await _context.Apartment
                    .FirstOrDefaultAsync(a => a.ApartmentId == booking.ApartmentId);

                if (apartment == null)
                {
                    _logger.LogWarning("Apartamento {ApartmentId} no encontrado", booking.ApartmentId);
                    return NotFound();
                }

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                // Validación de fechas y huéspedes
                if (booking.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "La fecha de inicio no puede ser en el pasado");
                }

                if (booking.EndDate <= booking.StartDate)
                {
                    ModelState.AddModelError("EndDate", "La fecha de fin debe ser posterior a la fecha de inicio");
                }

                if (booking.NumberOfGuests > apartment.MaxOccupancy)
                {
                    ModelState.AddModelError("NumberOfGuests", $"El número de huéspedes no puede superar {apartment.MaxOccupancy}");
                }

                // Verificación del precio total
                var nights = (booking.EndDate - booking.StartDate).Days;
                var expectedTotalPrice = nights * apartment.PricePerNight;

                if (Math.Abs(booking.TotalPrice - expectedTotalPrice) > 0.01) // Tolerancia para errores de redondeo
                {
                    booking.TotalPrice = expectedTotalPrice; // Corregir el precio si no coincide
                }

                // Verificar disponibilidad en las fechas seleccionadas
                var existingBooking = await _context.Booking
                    .AnyAsync(b => b.ApartmentId == booking.ApartmentId &&
                                  ((b.StartDate <= booking.StartDate && b.EndDate > booking.StartDate) ||
                                   (b.StartDate < booking.EndDate && b.EndDate >= booking.EndDate) ||
                                   (b.StartDate >= booking.StartDate && b.EndDate <= booking.EndDate)));

                if (existingBooking)
                {
                    ModelState.AddModelError("", "El apartamento no está disponible en las fechas seleccionadas");
                }

                if (ModelState.IsValid)
                {
                    // Completar datos de la reserva
                    booking.UserId = userId;
                    booking.CreatedAt = DateTime.UtcNow;

                    // Guardar la reserva
                    await _context.Booking.AddAsync(booking);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Reserva creada exitosamente para apartamento {ApartmentId}", booking.ApartmentId);
                    TempData["Success"] = "Reserva creada exitosamente";
                    return RedirectToAction("Index", "Apartments");
                }

                // Si hay errores, recargar la vista con los datos necesarios
                ViewData["Apartment"] = apartment;
                ViewData["MaxGuests"] = apartment.MaxOccupancy;
                ViewData["PricePerNight"] = apartment.PricePerNight;
                ViewData["MinStartDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                ViewData["MaxStartDate"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear reserva para apartamento {ApartmentId}", booking.ApartmentId);
                ModelState.AddModelError("", "Ocurrió un error al procesar la reserva");

                var apartment = await _context.Apartment.FindAsync(booking.ApartmentId);
                ViewData["Apartment"] = apartment;
                ViewData["MaxGuests"] = apartment?.MaxOccupancy;
                ViewData["PricePerNight"] = apartment?.PricePerNight;
                ViewData["MinStartDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                ViewData["MaxStartDate"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");

                return View(booking);
            }
        }

        /// <summary>
        /// GET: Muestra el formulario de edición de una reserva
        /// Verifica autorización y que la reserva no sea en el pasado
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Obtener la reserva con información del apartamento
                var booking = await _context.Booking
                    .Include(b => b.Apartment)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    return NotFound();
                }

                // Verificar que el usuario sea el propietario de la reserva
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (booking.UserId != userId)
                {
                    return Forbid();
                }

                // Verificar que la reserva no sea en el pasado
                if (booking.StartDate < DateTime.Today)
                {
                    TempData["Error"] = "No se pueden editar reservas pasadas";
                    return RedirectToAction("Index", "Apartments");
                }

                // Preparar datos para la vista
                ViewData["Apartment"] = booking.Apartment;
                ViewData["MaxGuests"] = booking.Apartment.MaxOccupancy;
                ViewData["PricePerNight"] = booking.Apartment.PricePerNight;
                ViewData["MinStartDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                ViewData["MaxStartDate"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la edición de la reserva {BookingId}", id);
                TempData["Error"] = "Error al cargar la reserva";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: Procesa la actualización de una reserva existente
        /// Realiza validaciones de fechas, capacidad y autorización
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            try
            {
                // Obtener la reserva existente con datos del apartamento
                var existingBooking = await _context.Booking
                    .Include(b => b.Apartment)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (existingBooking == null)
                {
                    return NotFound();
                }

                // Verificar autorización
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (existingBooking.UserId != userId)
                {
                    return Forbid();
                }

                // Validaciones de fechas y capacidad
                if (booking.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "La fecha de inicio no puede ser en el pasado");
                }

                if (booking.EndDate <= booking.StartDate)
                {
                    ModelState.AddModelError("EndDate", "La fecha de fin debe ser posterior a la fecha de inicio");
                }

                if (booking.NumberOfGuests > existingBooking.Apartment.MaxOccupancy)
                {
                    ModelState.AddModelError("NumberOfGuests", "El número de huéspedes excede la capacidad máxima");
                }

                if (ModelState.IsValid)
                {
                    // Mantener datos originales que no deben cambiar
                    booking.UserId = existingBooking.UserId;
                    booking.CreatedAt = existingBooking.CreatedAt;
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Recalcular precio total
                    var days = (booking.EndDate - booking.StartDate).Days;
                    booking.TotalPrice = days * existingBooking.Apartment.PricePerNight;

                    // Actualizar la reserva
                    _context.Update(booking);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Reserva {BookingId} actualizada exitosamente", id);
                    TempData["Success"] = "Reserva actualizada exitosamente";
                    return RedirectToAction("Index", "Apartments");
                }

                // Si hay errores, recargar la vista con los datos necesarios
                ViewData["Apartment"] = existingBooking.Apartment;
                ViewData["MaxGuests"] = existingBooking.Apartment.MaxOccupancy;
                ViewData["PricePerNight"] = existingBooking.Apartment.PricePerNight;
                ViewData["MinStartDate"] = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");
                ViewData["MaxStartDate"] = DateTime.Today.AddMonths(6).ToString("yyyy-MM-dd");

                return View(booking);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await BookingExists(booking.BookingId))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Error de concurrencia al actualizar la reserva {BookingId}", id);
                    throw;
                }
            }
        }/// <summary>
         /// GET: Muestra la confirmación para eliminar una reserva
         /// Verifica autorización y política de cancelación (48 horas)
         /// </summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Obtener la reserva con sus relaciones
                var booking = await _context.Booking
                    .Include(b => b.Apartment)
                    .Include(b => b.Guest)
                    .FirstOrDefaultAsync(m => m.BookingId == id);

                if (booking == null)
                {
                    return NotFound();
                }

                // Verificar que el usuario sea el propietario de la reserva
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (booking.UserId != userId)
                {
                    return Forbid();
                }

                // Verificar política de cancelación de 48 horas
                if (booking.StartDate <= DateTime.Today.AddDays(2))
                {
                    TempData["Error"] = "No se pueden cancelar reservas que comienzan en menos de 48 horas";
                    return RedirectToAction("Index", "Apartments");
                }

                return View(booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la vista de eliminación para la reserva {BookingId}", id);
                TempData["Error"] = "Error al procesar la solicitud de cancelación";
                return RedirectToAction("Index", "Apartments");
            }
        }

        /// <summary>
        /// POST: Procesa la eliminación de una reserva
        /// Verifica autorización y política de cancelación antes de eliminar
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Obtener la reserva con información del apartamento
                var booking = await _context.Booking
                    .Include(b => b.Apartment)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    return NotFound();
                }

                // Verificar autorización
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                if (booking.UserId != userId)
                {
                    return Forbid();
                }

                // Verificar política de cancelación de 48 horas
                if (booking.StartDate <= DateTime.Today.AddDays(2))
                {
                    TempData["Error"] = "No se pueden cancelar reservas que comienzan en menos de 48 horas";
                    return RedirectToAction("Index", "Apartments");
                }

                // Eliminar la reserva
                _context.Booking.Remove(booking);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Reserva {BookingId} cancelada exitosamente", id);
                TempData["Success"] = "Reserva cancelada exitosamente";
                return RedirectToAction("Index", "Apartments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la reserva {BookingId}", id);
                TempData["Error"] = "Error al cancelar la reserva";
                return RedirectToAction("Index", "Apartments");
            }
        }

        /// <summary>
        /// Verifica si existe una reserva con el ID especificado
        /// </summary>
        /// <param name="id">ID de la reserva a verificar</param>
        /// <returns>True si la reserva existe, False en caso contrario</returns>
        private async Task<bool> BookingExists(int id)
        {
            return await _context.Booking.AnyAsync(e => e.BookingId == id);
        }

        /// <summary>
        /// Verifica la disponibilidad de un apartamento en un rango de fechas
        /// Opcionalmente excluye una reserva específica (útil para ediciones)
        /// </summary>
        /// <param name="apartmentId">ID del apartamento a verificar</param>
        /// <param name="startDate">Fecha de inicio del período</param>
        /// <param name="endDate">Fecha de fin del período</param>
        /// <param name="excludeBookingId">ID de reserva a excluir de la verificación (opcional)</param>
        /// <returns>True si el apartamento está disponible, False si está ocupado</returns>
        private async Task<bool> IsApartmentAvailable(int apartmentId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
        {
            // Verificar que no existan reservas que se solapen con el período especificado
            return !await _context.Booking
                .AnyAsync(b => b.ApartmentId == apartmentId
                    && b.BookingId != excludeBookingId
                    && ((b.StartDate <= startDate && b.EndDate > startDate)    // Reserva existente cubre la fecha de inicio
                        || (b.StartDate < endDate && b.EndDate >= endDate)     // Reserva existente cubre la fecha de fin
                        || (b.StartDate >= startDate && b.EndDate <= endDate)  // Reserva existente está contenida en el período
                        ));
        }
    }
}