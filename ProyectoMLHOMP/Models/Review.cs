using ProyectoMLHOMP.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ProyectoMLHOMP.Models
{
    /// <summary>
    /// Modelo que representa una reseña de apartamento en el sistema
    /// Gestiona las valoraciones y comentarios de los huéspedes sobre su estancia
    /// Incluye múltiples criterios de evaluación para una valoración detallada
    /// </summary>
    public class Review
    {
        /// <summary>
        /// Identificador único de la reseña
        /// Clave primaria generada automáticamente
        /// </summary>
        [Key]
        public int ReviewId { get; set; }

        /// <summary>
        /// ID del apartamento al que pertenece la reseña
        /// Clave foránea que relaciona con la tabla de apartamentos
        /// </summary>
        [Required(ErrorMessage = "El ID del apartamento es requerido")]
        public int ApartmentId { get; set; }

        /// <summary>
        /// ID del usuario que escribe la reseña
        /// Clave foránea que relaciona con la tabla de usuarios
        /// </summary>
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        public int UserId { get; set; }

        /// <summary>
        /// Título descriptivo de la reseña
        /// Resumen breve de la experiencia del huésped
        /// </summary>
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder los 100 caracteres")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Contenido detallado de la reseña
        /// Descripción completa de la experiencia del huésped
        /// </summary>
        [Required(ErrorMessage = "El contenido es requerido")]
        [StringLength(1000, ErrorMessage = "El contenido no puede exceder los 1000 caracteres")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Calificación general de la estancia
        /// Valoración global de la experiencia
        /// </summary>
        [Required(ErrorMessage = "La calificación general es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int OverallRating { get; set; }

        /// <summary>
        /// Calificación específica para la limpieza del apartamento
        /// Evalúa el nivel de limpieza y mantenimiento
        /// </summary>
        [Required(ErrorMessage = "La calificación de limpieza es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int CleanlinessRating { get; set; }

        /// <summary>
        /// Calificación de la comunicación con el anfitrión
        /// Evalúa la calidad y eficiencia de la comunicación
        /// </summary>
        [Required(ErrorMessage = "La calificación de comunicación es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int CommunicationRating { get; set; }

        /// <summary>
        /// Calificación del proceso de check-in
        /// Evalúa la facilidad y eficiencia del proceso de llegada
        /// </summary>
        [Required(ErrorMessage = "La calificación de check-in es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int CheckInRating { get; set; }

        /// <summary>
        /// Calificación de la precisión de la información
        /// Evalúa si el apartamento coincide con su descripción
        /// </summary>
        [Required(ErrorMessage = "La calificación de precisión es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int AccuracyRating { get; set; }

        /// <summary>
        /// Calificación de la ubicación del apartamento
        /// Evalúa la conveniencia y características del área
        /// </summary>
        [Required(ErrorMessage = "La calificación de ubicación es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int LocationRating { get; set; }

        /// <summary>
        /// Calificación de la relación calidad-precio
        /// Evalúa si el precio es justo para lo ofrecido
        /// </summary>
        [Required(ErrorMessage = "La calificación de valor es requerida")]
        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5")]
        public int ValueRating { get; set; }

        /// <summary>
        /// Fecha y hora en que se creó la reseña
        /// Se establece automáticamente al crear la reseña
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Fecha y hora de la última modificación de la reseña
        /// Nullable para identificar reseñas nunca modificadas
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Estado de aprobación de la reseña
        /// Permite implementar un sistema de moderación si es necesario
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Referencia al apartamento reseñado
        /// Permite acceder a los detalles completos del apartamento
        /// </summary>
        [ForeignKey("ApartmentId")]
        public virtual Apartment? Apartment { get; set; }

        /// <summary>
        /// Referencia al usuario que escribió la reseña
        /// Permite acceder a los detalles del autor
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? Reviewer { get; set; }
    }
}