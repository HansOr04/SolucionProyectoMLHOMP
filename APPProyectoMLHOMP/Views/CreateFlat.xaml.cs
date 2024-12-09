using APPProyectoMLHOMP.Models;
using System.Net.Http.Json;

namespace APPProyectoMLHOMP.Views
{
    public partial class CreateFlat : ContentPage
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7020";

        public CreateFlat()
        {
            InitializeComponent();

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserIdEntry.Text))
            {
                await DisplayAlert("Error", "El ID de usuario es requerido", "OK");
                return;
            }

            try
            {
                var apartment = new Apartment
                {
                    Title = TitleEntry.Text,
                    Description = DescriptionEditor.Text,
                    PricePerNight = double.Parse(PriceEntry.Text),
                    Address = AddressEntry.Text,
                    City = CityEntry.Text,
                    Country = CountryEntry.Text,
                    Bedrooms = int.Parse(BedroomsEntry.Text),
                    Bathrooms = int.Parse(BathroomsEntry.Text),
                    MaxOccupancy = int.Parse(MaxOccupancyEntry.Text),
                    UserId = int.Parse(UserIdEntry.Text),
                    IsAvailable = AvailabilitySwitch.IsToggled,
                    ImageUrl = "/images/apartments/default-apartment.jpg",
                    CreatedAt = DateTime.UtcNow
                };

                var response = await _httpClient.PostAsJsonAsync("api/Apartment", apartment);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Apartamento creado correctamente", "OK");
                    // Modificar la navegación
                    await Navigation.PopAsync();
                    // Opcionalmente, actualizar la lista de apartamentos en la página anterior
                    if (Navigation.NavigationStack.Count > 0 &&
                        Navigation.NavigationStack[Navigation.NavigationStack.Count - 1] is AllFlats allFlatsPage)
                    {
                        await allFlatsPage.LoadApartments();
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo crear el apartamento: {errorContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Error al crear el apartamento: " + ex.Message, "OK");
            }
        }

        // Agregar método para manejar el botón de cancelar
        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}