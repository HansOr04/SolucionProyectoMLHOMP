using System;
using System.Collections.Generic;

namespace APPProyectoMLHOMP.Models
{
    public class Apartment
    {
        /// <summary>
        /// Identificador único del apartamento
        /// </summary>
        public int ApartmentId { get; set; }

        /// <summary>
        /// Título o nombre del apartamento
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del apartamento
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Precio por noche
        /// </summary>
        public double PricePerNight { get; set; }

        /// <summary>
        /// Dirección física del apartamento
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Ciudad donde se encuentra el apartamento
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// País donde se encuentra el apartamento
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Número de habitaciones del apartamento
        /// </summary>
        public int Bedrooms { get; set; }

        /// <summary>
        /// Número de baños en el apartamento
        /// </summary>
        public int Bathrooms { get; set; }

        /// <summary>
        /// Número máximo de personas que pueden ocupar el apartamento
        /// </summary>
        public int MaxOccupancy { get; set; }

        /// <summary>
        /// URL de la imagen principal del apartamento
        /// </summary>
        public string ImageUrl { get; set; } = "/images/apartments/default-apartment.jpg";

        /// <summary>
        /// Indica si el apartamento está disponible para reservas
        /// </summary>
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Fecha y hora de creación del registro
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de la última actualización
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// ID del usuario propietario del apartamento
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Referencia al usuario propietario del apartamento
        /// </summary>
        public User? Owner { get; set; }

        /// <summary>
        /// Colección de reservas asociadas al apartamento
        /// </summary>
        public ICollection<Booking>? Bookings { get; set; } = new List<Booking>();

        /// <summary>
        /// Colección de reseñas asociadas al apartamento
        /// </summary>
        public ICollection<Review>? Reviews { get; set; } = new List<Review>();

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
