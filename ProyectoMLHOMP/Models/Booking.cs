using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProyectoMLHOMP.Models
{
    /// <summary>
    /// Modelo que representa una reserva de apartamento en el sistema
    /// Gestiona la información relacionada con las reservas realizadas por los usuarios
    /// </summary>
    public class Booking
    {
        /// <summary>
        /// Identificador único de la reserva
        /// Clave primaria generada automáticamente
        /// </summary>
        [Key]
        public int BookingId { get; set; }

        /// <summary>
        /// Fecha de inicio de la estancia
        /// Debe ser una fecha válida y futura
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Fecha de finalización de la estancia
        /// Debe ser posterior a la fecha de inicio
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Número de personas que se hospedarán
        /// Debe ser mayor a 0 y no exceder la capacidad máxima del apartamento
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "El número de huéspedes debe ser mayor a 0")]
        public int NumberOfGuests { get; set; }

        /// <summary>
        /// Precio total de la reserva
        /// Calculado multiplicando el precio por noche por el número de noches
        /// Almacenado como float en la base de datos
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio total debe ser mayor a 0")]
        [Column(TypeName = "float")]
        public double TotalPrice { get; set; }

        /// <summary>
        /// ID del apartamento reservado
        /// Clave foránea que relaciona con la tabla de apartamentos
        /// </summary>
        [Required]
        public int ApartmentId { get; set; }

        /// <summary>
        /// ID del usuario que realiza la reserva
        /// Clave foránea que relaciona con la tabla de usuarios
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Fecha y hora en que se creó la reserva
        /// Se establece automáticamente al crear la reserva
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Fecha y hora de la última modificación de la reserva
        /// Nullable para identificar reservas nunca modificadas
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Referencia al apartamento reservado
        /// Permite acceder a los detalles completos del apartamento
        /// Se configura con DeleteBehavior.Restrict para prevenir eliminación en cascada
        /// </summary>
        [ForeignKey("ApartmentId")]
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public virtual Apartment? Apartment { get; set; }

        /// <summary>
        /// Referencia al usuario que realizó la reserva
        /// Permite acceder a los detalles del huésped
        /// Se configura con DeleteBehavior.Restrict para prevenir eliminación en cascada
        /// </summary>
        [ForeignKey("UserId")]
        [DeleteBehavior(DeleteBehavior.Restrict)]
        public virtual User? Guest { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// Inicializa la fecha de creación con la hora UTC actual
        /// </summary>
        public Booking()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
}