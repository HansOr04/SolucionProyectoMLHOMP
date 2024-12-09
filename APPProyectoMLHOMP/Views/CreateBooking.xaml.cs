using System.Text;
using Newtonsoft.Json;
using APPProyectoMLHOMP.Models;

namespace APPProyectoMLHOMP.Views
{
    public partial class CreateBooking : ContentPage
    {
        private readonly int apartmentId;
        private readonly double pricePerNight;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7020";

        public CreateBooking(int apartmentId, double pricePerNight)
        {
            InitializeComponent();
            this.apartmentId = apartmentId;
            this.pricePerNight = pricePerNight;

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Configurar valores iniciales
            StartDatePicker.Date = DateTime.Today;
            EndDatePicker.Date = DateTime.Today.AddDays(1);

            StartDatePicker.DateSelected += OnDateSelected;
            EndDatePicker.DateSelected += OnDateSelected;
            GuestsEntry.TextChanged += OnGuestsChanged;

            UpdateTotalPrice();
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            UpdateTotalPrice();
        }

        private void OnGuestsChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            var nights = (EndDatePicker.Date - StartDatePicker.Date).Days;
            if (nights > 0)
            {
                var total = nights * pricePerNight;
                TotalPriceLabel.Text = $"${total:F2}";
            }
        }

        private async void OnConfirmBookingClicked(object sender, EventArgs e)
        {
            if (!ValidateBooking(out string errorMessage))
            {
                await DisplayAlert("Error", errorMessage, "OK");
                return;
            }

            try
            {
                var booking = new Booking
                {
                    StartDate = StartDatePicker.Date,
                    EndDate = EndDatePicker.Date,
                    NumberOfGuests = int.Parse(GuestsEntry.Text),
                    TotalPrice = double.Parse(TotalPriceLabel.Text.Replace("$", "")),
                    ApartmentId = apartmentId,
                    UserId = 1, // Asumiendo un usuario de prueba
                    CreatedAt = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(booking);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Booking", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Reserva creada correctamente", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", $"Error al crear la reserva: {response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear la reserva: {ex.Message}", "OK");
            }
        }

        private bool ValidateBooking(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (StartDatePicker.Date < DateTime.Today)
            {
                errorMessage = "La fecha de inicio no puede ser anterior a hoy";
                return false;
            }

            if (EndDatePicker.Date <= StartDatePicker.Date)
            {
                errorMessage = "La fecha de fin debe ser posterior a la fecha de inicio";
                return false;
            }

            if (string.IsNullOrWhiteSpace(GuestsEntry.Text))
            {
                errorMessage = "Debe especificar el número de huéspedes";
                return false;
            }

            if (!int.TryParse(GuestsEntry.Text, out int guests) || guests < 1)
            {
                errorMessage = "El número de huéspedes debe ser mayor a 0";
                return false;
            }

            return true;
        }
    }
}