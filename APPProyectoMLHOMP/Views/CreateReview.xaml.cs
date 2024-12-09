using System.Text;
using Newtonsoft.Json;
using APPProyectoMLHOMP.Models;

namespace APPProyectoMLHOMP.Views
{
    public partial class CreateReview : ContentPage
    {
        private readonly int apartmentId;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7020";

        public CreateReview(int apartmentId)
        {
            InitializeComponent();
            this.apartmentId = apartmentId;

            // Configurar HttpClient
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // Configurar valores iniciales de los sliders
            OverallRatingSlider.ValueChanged += OnRatingChanged;
            CleanlinessSlider.ValueChanged += OnRatingChanged;
            CommunicationSlider.ValueChanged += OnRatingChanged;
            CheckInSlider.ValueChanged += OnRatingChanged;
            AccuracySlider.ValueChanged += OnRatingChanged;
            LocationSlider.ValueChanged += OnRatingChanged;
            ValueSlider.ValueChanged += OnRatingChanged;
        }

        private void OnRatingChanged(object sender, ValueChangedEventArgs e)
        {
            var slider = sender as Slider;
            if (slider != null)
            {
                slider.Value = Math.Round(e.NewValue); // Redondear al número entero más cercano
            }
        }

        private async void OnPublishReviewClicked(object sender, EventArgs e)
        {
            if (!ValidateReview(out string errorMessage))
            {
                await DisplayAlert("Error", errorMessage, "OK");
                return;
            }

            try
            {
                var review = new Review
                {
                    ApartmentId = apartmentId,
                    UserId = 1, // Usuario de prueba
                    Title = TitleEntry.Text.Trim(),
                    Content = ContentEditor.Text.Trim(),
                    OverallRating = (int)OverallRatingSlider.Value,
                    CleanlinessRating = (int)CleanlinessSlider.Value,
                    CommunicationRating = (int)CommunicationSlider.Value,
                    CheckInRating = (int)CheckInSlider.Value,
                    AccuracyRating = (int)AccuracySlider.Value,
                    LocationRating = (int)LocationSlider.Value,
                    ValueRating = (int)ValueSlider.Value,
                    CreatedDate = DateTime.UtcNow,
                    IsApproved = false // Pendiente de aprobación
                };

                var json = JsonConvert.SerializeObject(review);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("api/Review", content);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Reseña publicada correctamente", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error al publicar la reseña: {response.StatusCode}\n{errorContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo publicar la reseña: {ex.Message}", "OK");
            }
        }

        private bool ValidateReview(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                errorMessage = "El título es requerido";
                return false;
            }

            if (TitleEntry.Text.Length > 100)
            {
                errorMessage = "El título no puede exceder los 100 caracteres";
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContentEditor.Text))
            {
                errorMessage = "El contenido es requerido";
                return false;
            }

            if (ContentEditor.Text.Length > 1000)
            {
                errorMessage = "El contenido no puede exceder los 1000 caracteres";
                return false;
            }

            // Validar que todas las calificaciones estén dentro del rango permitido
            if (OverallRatingSlider.Value < 1 || OverallRatingSlider.Value > 5 ||
                CleanlinessSlider.Value < 1 || CleanlinessSlider.Value > 5 ||
                CommunicationSlider.Value < 1 || CommunicationSlider.Value > 5 ||
                CheckInSlider.Value < 1 || CheckInSlider.Value > 5 ||
                AccuracySlider.Value < 1 || AccuracySlider.Value > 5 ||
                LocationSlider.Value < 1 || LocationSlider.Value > 5 ||
                ValueSlider.Value < 1 || ValueSlider.Value > 5)
            {
                errorMessage = "Todas las calificaciones deben estar entre 1 y 5";
                return false;
            }

            return true;
        }
    }
}