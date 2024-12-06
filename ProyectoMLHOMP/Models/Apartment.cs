using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoMLHOMP.Models
{
    /// <summary>
    /// Modelo que representa un apartamento en el sistema
    /// Contiene toda la información relevante para la gestión y reserva de apartamentos
    /// </summary>
    public class Apartment
    {
        /// <summary>
        /// Identificador único del apartamento
        /// Se genera automáticamente en la base de datos
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ApartmentId { get; set; }

        /// <summary>
        /// Título o nombre del apartamento
        /// Usado para mostrar en listados y búsquedas
        /// </summary>
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder los 100 caracteres")]
        [Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del apartamento
        /// Incluye características, amenidades y otra información relevante
        /// </summary>
        [Required(ErrorMessage = "La descripción es requerida")]
        [StringLength(2000, ErrorMessage = "La descripción no puede exceder los 2000 caracteres")]
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Precio por noche en la moneda base del sistema
        /// Se valida para asegurar un precio válido y razonable
        /// </summary>
        [Required(ErrorMessage = "El precio por noche es requerido")]
        [Range(0.01, 99999.99, ErrorMessage = "El precio debe estar entre $0.01 y $99,999.99")]
        [Display(Name = "Precio por Noche")]
        [DataType(DataType.Currency)]
        public double PricePerNight { get; set; }

        /// <summary>
        /// Dirección física del apartamento
        /// Usada para ubicación y mapas
        /// </summary>
        [Required(ErrorMessage = "La dirección es requerida")]
        [Display(Name = "Dirección")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Ciudad donde se encuentra el apartamento
        /// Utilizada para búsquedas y filtros
        /// </summary>
        [Required(ErrorMessage = "La ciudad es requerida")]
        [Display(Name = "Ciudad")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// País donde se encuentra el apartamento
        /// Utilizado para búsquedas y filtros internacionales
        /// </summary>
        [Required(ErrorMessage = "El país es requerido")]
        [Display(Name = "País")]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Número de habitaciones del apartamento
        /// Limitado a un rango razonable para validación
        /// </summary>
        [Required(ErrorMessage = "El número de habitaciones es requerido")]
        [Range(1, 20, ErrorMessage = "El número de habitaciones debe estar entre 1 y 20")]
        [Display(Name = "Habitaciones")]
        public int Bedrooms { get; set; }

        /// <summary>
        /// Número de baños en el apartamento
        /// Limitado a un rango razonable para validación
        /// </summary>
        [Required(ErrorMessage = "El número de baños es requerido")]
        [Range(1, 20, ErrorMessage = "El número de baños debe estar entre 1 y 20")]
        [Display(Name = "Baños")]
        public int Bathrooms { get; set; }

        /// <summary>
        /// Número máximo de personas que pueden ocupar el apartamento
        /// Usado para validar reservas y mostrar capacidad
        /// </summary>
        [Required(ErrorMessage = "La ocupación máxima es requerida")]
        [Range(1, 50, ErrorMessage = "La ocupación máxima debe estar entre 1 y 50 personas")]
        [Display(Name = "Ocupación Máxima")]
        public int MaxOccupancy { get; set; }

        /// <summary>
        /// URL de la imagen principal del apartamento
        /// Si no se proporciona una imagen, se usa una imagen por defecto
        /// </summary>
        [Required(ErrorMessage = "La imagen principal es requerida")]
        [Display(Name = "Imagen Principal")]
        public string ImageUrl { get; set; } = "/images/apartments/default-apartment.jpg";

        /// <summary>
        /// Archivo de imagen temporal para procesar la carga de imágenes
        /// No se almacena en la base de datos
        /// </summary>
        [NotMapped]
        [Display(Name = "Imagen del Departamento")]
        [Required(ErrorMessage = "Debe seleccionar una imagen")]
        public IFormFile? ImageFile { get; set; }

        /// <summary>
        /// Indica si el apartamento está disponible para reservas
        /// Permite al propietario desactivar temporalmente las reservas
        /// </summary>
        [Display(Name = "Disponible")]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Fecha y hora de creación del registro
        /// Se establece automáticamente al crear el apartamento
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de la última actualización
        /// Nullable para distinguir registros nunca actualizados
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID del usuario propietario del apartamento
        /// Clave foránea que relaciona con la tabla de usuarios
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Referencia al usuario propietario del apartamento
        /// Permite acceder a los datos del propietario a través de la relación
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? Owner { get; set; }

        /// <summary>
        /// Colección de reservas asociadas al apartamento
        /// Permite acceder al historial de reservas
        /// </summary>
        public virtual ICollection<Booking>? Bookings { get; set; } = new List<Booking>();

        /// <summary>
        /// Colección de reseñas asociadas al apartamento
        /// Permite acceder a las valoraciones y comentarios de los huéspedes
        /// </summary>
        public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();

        /// <summary>
        /// Constructor por defecto
        /// Inicializa las colecciones y establece valores por defecto
        /// </summary>
        public Apartment()
        {
            Bookings = new List<Booking>();
            Reviews = new List<Review>();
            CreatedAt = DateTime.UtcNow;
            IsAvailable = true;
            ImageUrl = "/images/apartments/default-apartment.jpg";
        }
    }
}