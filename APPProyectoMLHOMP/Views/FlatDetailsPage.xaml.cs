using APPProyectoMLHOMP.Models;

namespace APPProyectoMLHOMP.Views
{
    public partial class FlatDetailsPage : ContentPage
    {
        private readonly Apartment _apartment;

        public FlatDetailsPage(Apartment apartment)
        {
            InitializeComponent();
            _apartment = apartment;
            LoadApartmentDetails();
        }

        private void LoadApartmentDetails()
        {
            Title = _apartment.Title;
            TitleLabel.Text = _apartment.Title;
            PriceLabel.Text = $"${_apartment.PricePerNight:N2} por noche";

            // Configurar estado de disponibilidad
            AvailabilityLabel.Text = _apartment.IsAvailable ? "Disponible" : "No Disponible";
            AvailabilityLabel.TextColor = _apartment.IsAvailable ? Color.FromArgb("#28a745") : Color.FromArgb("#dc3545");

            // Información de ubicación
            AddressLabel.Text = _apartment.Address;
            CityCountryLabel.Text = $"{_apartment.City}, {_apartment.Country}";

            // Características
            BedroomsLabel.Text = _apartment.Bedrooms.ToString();
            BathroomsLabel.Text = _apartment.Bathrooms.ToString();
            OccupancyLabel.Text = _apartment.MaxOccupancy.ToString();

            // Descripción
            DescriptionLabel.Text = _apartment.Description;

            // Fechas
            CreatedAtLabel.Text = $"Creado: {_apartment.CreatedAt:dd/MM/yyyy HH:mm}";
            if (_apartment.UpdatedAt.HasValue)
            {
                UpdatedAtLabel.Text = $"Actualizado: {_apartment.UpdatedAt:dd/MM/yyyy HH:mm}";
                UpdatedAtLabel.IsVisible = true;
            }
            else
            {
                UpdatedAtLabel.IsVisible = false;
            }
        }
    }
}