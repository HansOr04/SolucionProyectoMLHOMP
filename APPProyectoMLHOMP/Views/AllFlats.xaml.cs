using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Input;
using Newtonsoft.Json;
using APPProyectoMLHOMP.Models;

namespace APPProyectoMLHOMP.Views
{
    public partial class AllFlats : ContentPage
    {
        public ObservableCollection<Apartment> Apartments { get; set; } = new ObservableCollection<Apartment>();
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://localhost:7020";
        private bool _isLoading;
        public ICommand FlatTappedCommand { get; private set; }

        public AllFlats()
        {
            InitializeComponent();

            // Inicializar el comando de tap
            FlatTappedCommand = new Command<Apartment>(OnFlatTapped);

            ApartmentCollectionView.ItemsSource = Apartments;

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            LoadApartments();
        }

        public async Task LoadApartments()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;
                LoadingIndicator.IsVisible = true;
                ErrorLabel.IsVisible = false;

                var response = await _httpClient.GetAsync("api/Apartment");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apartments = JsonConvert.DeserializeObject<List<Apartment>>(content);
                    Apartments.Clear();
                    foreach (var apartment in apartments)
                    {
                        apartment.ImageUrl = "https://images.pexels.com/photos/1571460/pexels-photo-1571460.jpeg";
                        Apartments.Add(apartment);
                    }
                }
                else
                {
                    ErrorLabel.Text = $"Error al cargar apartamentos. Status: {response.StatusCode}";
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = "Error de conexión. Por favor, intente nuevamente.";
                ErrorLabel.IsVisible = true;
                Console.WriteLine($"Error detallado: {ex}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                _isLoading = false;
            }
        }

        private async void OnCreateFlatClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreateFlat());
        }

        private async void OnFlatTapped(Apartment apartment)
        {
            if (apartment == null)
                return;

            await Navigation.PushAsync(new FlatDetailsPage(apartment));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadApartments();
        }
    }
}