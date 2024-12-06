using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ProyectoMLHOMP.Models
{
    /// <summary>
    /// Modelo que representa un usuario en el sistema
    /// Gestiona la información personal y credenciales de acceso
    /// Incluye índices únicos para email y username para evitar duplicados
    /// </summary>
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(Username), IsUnique = true)]
    public class User
    {
        /// <summary>
        /// Identificador único del usuario
        /// Se genera automáticamente en la base de datos
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        /// <summary>
        /// Nombre del usuario
        /// Limitado a 50 caracteres
        /// </summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Apellido del usuario
        /// Limitado a 50 caracteres
        /// </summary>
        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de nacimiento del usuario
        /// Utilizada para verificar edad y generar estadísticas
        /// </summary>
        [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// Dirección física del usuario
        /// Limitada a 100 caracteres
        /// </summary>
        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Ciudad de residencia del usuario
        /// Limitada a 50 caracteres
        /// </summary>
        [Required(ErrorMessage = "La ciudad es requerida")]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// País de residencia del usuario
        /// Limitado a 50 caracteres
        /// </summary>
        [Required(ErrorMessage = "El país es requerido")]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Correo electrónico del usuario
        /// Debe ser único en el sistema y tener formato válido
        /// </summary>
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Número de teléfono del usuario
        /// Utilizado para contacto y verificación
        /// </summary>
        [Required(ErrorMessage = "El teléfono es requerido")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de usuario único en el sistema
        /// Utilizado para inicio de sesión
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Hash de la contraseña almacenado en la base de datos
        /// No se almacena la contraseña en texto plano
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña temporal para proceso de registro/cambio
        /// No se almacena en la base de datos
        /// </summary>
        [NotMapped]
        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Fecha en que el usuario se registró en el sistema
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Indica si el usuario es un anfitrión
        /// Determina permisos y funcionalidades disponibles
        /// </summary>
        public bool IsHost { get; set; }

        /// <summary>
        /// Indica si el usuario ha sido verificado
        /// Puede usarse para verificación de email o identidad
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Biografía o descripción del usuario
        /// Limitada a 500 caracteres
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Biography { get; set; } = string.Empty;

        /// <summary>
        /// Idiomas que habla el usuario
        /// Limitado a 100 caracteres
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Languages { get; set; } = string.Empty;

        /// <summary>
        /// URL de la imagen de perfil del usuario
        /// Usa una imagen por defecto si no se especifica otra
        /// </summary>
        public string ProfileImageUrl { get; set; } = "/images/default-profile.jpg";

        /// <summary>
        /// Colección de apartamentos que posee el usuario
        /// Disponible solo para usuarios que son anfitriones
        /// </summary>
        public virtual ICollection<Apartment>? ApartmentsOwned { get; set; }

        /// <summary>
        /// Colección de reservas realizadas por el usuario
        /// Historial de reservas como huésped
        /// </summary>
        public virtual ICollection<Booking>? BookingsAsGuest { get; set; }

        /// <summary>
        /// Colección de reseñas escritas por el usuario
        /// Historial de evaluaciones realizadas
        /// </summary>
        public virtual ICollection<Review>? ReviewsWritten { get; set; }

        /// <summary>
        /// Obtiene el nombre completo del usuario
        /// Concatena nombre y apellido
        /// </summary>
        /// <returns>String con el nombre completo</returns>
        public string GetFullName() => $"{FirstName} {LastName}";

        /// <summary>
        /// Verifica si una contraseña coincide con el hash almacenado
        /// </summary>
        /// <param name="password">Contraseña a verificar</param>
        /// <returns>True si la contraseña es correcta, False en caso contrario</returns>
        public bool VerifyPassword(string password)
        {
            return HashPassword(password) == this.PasswordHash;
        }

        /// <summary>
        /// Genera un hash SHA256 de la contraseña proporcionada
        /// Método estático utilizado tanto para crear como para verificar contraseñas
        /// </summary>
        /// <param name="password">Contraseña a hashear</param>
        /// <returns>String con el hash en formato hexadecimal</returns>
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}