using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoMLHOMP.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoMLHOMP.Controllers
{
    /// <summary>
    /// Controlador que maneja todas las operaciones relacionadas con usuarios
    /// Incluye registro, autenticación, perfil y gestión de usuarios
    /// </summary>
    public class UsersController : Controller
    {
        // Contexto de base de datos para acceder a las entidades
        private readonly ProyectoContext _context;
        // Proporciona información sobre el entorno de hosting web
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Logger para registro de eventos y errores
        private readonly ILogger<UsersController> _logger;

        /// <summary>
        /// Constructor que inicializa las dependencias necesarias
        /// </summary>
        public UsersController(ProyectoContext context, IWebHostEnvironment webHostEnvironment, ILogger<UsersController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        /// <summary>
        /// GET: Muestra el formulario de registro
        /// Redirige a Apartments si el usuario ya está autenticado
        /// </summary>
        public IActionResult Register()
        {
            // Verificar si el usuario ya está autenticado
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Apartments");
            }
            return View(new User());
        }

        /// <summary>
        /// GET: Muestra el perfil del usuario actual
        /// Requiere autenticación
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Obtener ID del usuario actual desde los claims
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

                // Buscar usuario incluyendo sus apartamentos
                var user = await _context.User
                    .Include(u => u.ApartmentsOwned)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario {userId} no encontrado al intentar ver el perfil");
                    return RedirectToAction("Index", "Home");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                // Registrar y manejar cualquier error
                _logger.LogError(ex, "Error al cargar el perfil del usuario");
                TempData["Error"] = "Error al cargar el perfil";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// GET: Muestra el formulario de edición del perfil
        /// Requiere autenticación
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            try
            {
                // Obtener ID del usuario actual
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _context.User.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario {userId} no encontrado al intentar editar");
                    return RedirectToAction("Index", "Home");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                // Manejar errores durante la carga del formulario
                _logger.LogError(ex, "Error al cargar el formulario de edición del perfil");
                TempData["Error"] = "Error al cargar el formulario de edición";
                return RedirectToAction("Profile");
            }
        }

        /// <summary>
        /// POST: Procesa el registro de un nuevo usuario
        /// Incluye carga de imagen de perfil y validaciones
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, IFormFile? profileImage)
        {
            try
            {
                // Validar el modelo recibido
                if (!ModelState.IsValid)
                {
                    // Registrar errores de validación
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Error de validación: {error.ErrorMessage}");
                    }
                    return View(user);
                }

                // Asegurar que la base de datos está disponible
                if (!await _context.Database.CanConnectAsync())
                {
                    _logger.LogInformation("Inicializando base de datos...");
                    await _context.Database.EnsureCreatedAsync();
                }

                // Verificar si ya existe un usuario con el mismo email o username
                var existingUser = await _context.User
                    .Where(u => u.Email == user.Email || u.Username == user.Username)
                    .FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    // Agregar errores específicos para email y username duplicados
                    if (existingUser.Email == user.Email)
                    {
                        ModelState.AddModelError("Email", "Este email ya está registrado");
                    }
                    if (existingUser.Username == user.Username)
                    {
                        ModelState.AddModelError("Username", "Este nombre de usuario ya está en uso");
                    }
                    return View(user);
                }

                // Procesar la imagen de perfil
                string profileImagePath = "/images/default-profile.jpg";
                if (profileImage != null && profileImage.Length > 0)
                {
                    try
                    {
                        // Crear directorio si no existe
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                        Directory.CreateDirectory(uploadsFolder);

                        // Generar nombre único para la imagen
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(profileImage.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Guardar la imagen
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }
                        profileImagePath = $"/images/profiles/{uniqueFileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al procesar la imagen: {ex.Message}");
                    }
                }

                // Crear nuevo usuario con los datos proporcionados
                var newUser = new User
                {
                    FirstName = user.FirstName.Trim(),
                    LastName = user.LastName.Trim(),
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address.Trim(),
                    City = user.City.Trim(),
                    Country = user.Country.Trim(),
                    Email = user.Email.Trim().ToLower(),
                    PhoneNumber = user.PhoneNumber.Trim(),
                    Username = user.Username.Trim(),
                    PasswordHash = Models.User.HashPassword(user.Password),
                    Biography = user.Biography?.Trim() ?? "",
                    Languages = user.Languages?.Trim() ?? "",
                    ProfileImageUrl = profileImagePath,
                    RegistrationDate = DateTime.UtcNow,
                    IsHost = false,
                    IsVerified = false
                };

                try
                {
                    // Guardar el nuevo usuario en la base de datos
                    _context.User.Add(newUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Usuario registrado exitosamente: {newUser.Email}");

                    // Iniciar sesión automáticamente
                    await LoginUser(newUser);
                    TempData["Success"] = "¡Registro exitoso! Bienvenido a nuestra plataforma.";
                    return RedirectToAction("Index", "Apartments");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError($"Error al guardar en la base de datos: {ex.Message}");
                    ModelState.AddModelError("", "Error al guardar el usuario. Por favor, intente nuevamente.");
                    return View(user);
                }
            }
            catch (Exception ex)
            {
                // Manejar cualquier otro error durante el proceso
                _logger.LogError($"Error general en el registro: {ex.Message}");
                ModelState.AddModelError("", "Ocurrió un error durante el registro. Por favor, intente nuevamente.");
                return View(user);
            }
        }/// <summary>
         /// POST: Actualiza el perfil de un usuario existente
         /// Requiere autenticación y token antifalsificación
         /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit([Bind("FirstName,LastName,Address,City,Country,PhoneNumber,Biography,Languages,IsHost,ProfileImageUrl")] User updatedUser, IFormFile? profileImage)
        {
            try
            {
                // Obtener usuario actual
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                var user = await _context.User.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario {userId} no encontrado al intentar actualizar");
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index", "Home");
                }

                // Remover validaciones para campos que no se actualizan
                ModelState.Remove("Email");
                ModelState.Remove("Username");
                ModelState.Remove("Password");
                ModelState.Remove("PasswordHash");
                ModelState.Remove("DateOfBirth");

                if (!ModelState.IsValid)
                {
                    // Mantener datos originales para campos no editables
                    updatedUser.Email = user.Email;
                    updatedUser.Username = user.Username;
                    updatedUser.DateOfBirth = user.DateOfBirth;
                    return View(updatedUser);
                }

                // Procesar nueva imagen de perfil si se proporciona
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Validar tamaño de la imagen
                    if (profileImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ProfileImage", "La imagen no puede superar los 5MB");
                        return View(updatedUser);
                    }

                    // Validar tipo de archivo
                    if (!IsValidImageFile(profileImage))
                    {
                        ModelState.AddModelError("ProfileImage", "El archivo debe ser una imagen válida (jpg, jpeg, png)");
                        return View(updatedUser);
                    }

                    // Procesar y guardar la imagen
                    var (success, imagePath, errorMessage) = await ProcessProfileImage(profileImage, user.ProfileImageUrl);
                    if (!success)
                    {
                        ModelState.AddModelError("ProfileImage", errorMessage);
                        return View(updatedUser);
                    }
                    user.ProfileImageUrl = imagePath;
                }

                // Actualizar campos permitidos
                user.FirstName = updatedUser.FirstName?.Trim() ?? user.FirstName;
                user.LastName = updatedUser.LastName?.Trim() ?? user.LastName;
                user.Address = updatedUser.Address?.Trim() ?? user.Address;
                user.City = updatedUser.City?.Trim() ?? user.City;
                user.Country = updatedUser.Country?.Trim() ?? user.Country;
                user.PhoneNumber = updatedUser.PhoneNumber?.Trim() ?? user.PhoneNumber;
                user.Biography = updatedUser.Biography?.Trim() ?? user.Biography;
                user.Languages = updatedUser.Languages?.Trim() ?? user.Languages;

                // Verificar cambio en estado de host
                bool isHostChanged = user.IsHost != updatedUser.IsHost;
                user.IsHost = updatedUser.IsHost;

                try
                {
                    // Guardar cambios en la base de datos
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // Actualizar claims si cambió el estado de host
                    if (isHostChanged)
                    {
                        await RefreshUserClaims(user);
                        TempData["Success"] = user.IsHost
                            ? "¡Perfil actualizado exitosamente! Bienvenido como anfitrión."
                            : "Perfil actualizado exitosamente. Modo anfitrión desactivado.";
                    }
                    else
                    {
                        TempData["Success"] = "Perfil actualizado exitosamente.";
                    }

                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, $"Error de concurrencia al actualizar el usuario {userId}");
                    TempData["Error"] = "Error al guardar los cambios. Por favor, intente nuevamente.";
                    return View(updatedUser);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el perfil");
                TempData["Error"] = "Error al actualizar el perfil. Por favor, intente nuevamente.";
                return View(updatedUser);
            }
        }

        /// <summary>
        /// GET: Muestra el formulario de inicio de sesión
        /// </summary>
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirigir si ya está autenticado
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// POST: Procesa el inicio de sesión
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;

                // Validar campos requeridos
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError(string.Empty, "Usuario y contraseña son requeridos");
                    return View();
                }

                // Buscar usuario por username
                var user = await _context.User.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Usuario no encontrado");
                    return View();
                }

                // Verificar contraseña
                if (!user.VerifyPassword(password))
                {
                    ModelState.AddModelError(string.Empty, "Contraseña incorrecta");
                    return View();
                }

                // Iniciar sesión
                await LoginUser(user);
                TempData["Success"] = $"¡Bienvenido de nuevo, {user.FirstName}!";

                // Redirigir a la URL de retorno si es válida
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el login");
                ModelState.AddModelError(string.Empty, "Error al iniciar sesión. Por favor, intente nuevamente.");
                return View();
            }
        }

        /// <summary>
        /// POST: Cierra la sesión del usuario actual
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Has cerrado sesión exitosamente.";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Método privado para gestionar el proceso de login
        /// Crea y establece los claims del usuario
        /// </summary>
        private async Task LoginUser(User user)
        {
            // Crear claims para la identidad del usuario
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.GetFullName()),
                new Claim(ClaimTypes.Role, user.IsHost ? "Host" : "Guest")
            };

            // Crear identidad de claims
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Configurar propiedades de autenticación
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                IsPersistent = true,
            };

            // Realizar el signin
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        /// <summary>
        /// Actualiza los claims del usuario, útil cuando cambia el rol
        /// </summary>
        private async Task RefreshUserClaims(User user)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await LoginUser(user);
        }

        /// <summary>
        /// Verifica si existe un usuario con el ID especificado
        /// </summary>
        private bool UserExists(int id)
        {
            return _context.User.Any(e => e.UserId == id);
        }

        /// <summary>
        /// Valida que el archivo sea una imagen con formato permitido
        /// </summary>
        private bool IsValidImageFile(IFormFile file)
        {
            // Lista de tipos MIME permitidos para imágenes
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
            return allowedTypes.Contains(file.ContentType.ToLower());
        }

        /// <summary>
        /// Procesa y guarda la imagen de perfil, eliminando la anterior si existe
        /// Retorna una tupla con el resultado de la operación
        /// </summary>
        private async Task<(bool success, string imagePath, string errorMessage)> ProcessProfileImage(IFormFile profileImage, string currentImagePath)
        {
            try
            {
                // Crear directorio para imágenes si no existe
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                // Generar nombre único para la imagen
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(profileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Guardar nueva imagen
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                // Eliminar imagen anterior si existe y no es la default
                if (currentImagePath != "/images/default-profile.jpg")
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, currentImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                return (true, $"/images/profiles/{uniqueFileName}", string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la imagen del perfil");
                return (false, string.Empty, "Error al procesar la imagen del perfil.");
            }
        }
    }
}