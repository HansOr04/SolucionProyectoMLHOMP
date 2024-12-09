using System.Net.Http.Json;
using APPProyectoMLHOMP.Models;
using System.Text.Json;

namespace APPProyectoMLHOMP.Views;

public class LoginApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public JsonElement User { get; set; }
}

public partial class VistaLoginRegister : ContentPage
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://localhost:7020";

    public VistaLoginRegister()
    {
        InitializeComponent();
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryEmail.Text) || string.IsNullOrWhiteSpace(EntryPassword.Text))
        {
            await DisplayAlert("Error", "Por favor complete todos los campos", "OK");
            return;
        }

        try
        {
            LoadingIndicator.IsVisible = true;
            LoginButton.IsEnabled = false;

            var loginData = new
            {
                Username = EntryEmail.Text.Trim(),
                Password = EntryPassword.Text
            };

            var response = await _httpClient.PostAsJsonAsync("api/User/login", loginData);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Respuesta del servidor: {content}");

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginApiResponse>();

                if (loginResponse.Success)
                {
                    var user = JsonSerializer.Deserialize<User>(loginResponse.User.GetRawText());

                    Preferences.Default.Set("UserId", user.UserId.ToString());
                    Preferences.Default.Set("UserName", user.Username);

                    await DisplayAlert("Éxito", loginResponse.Message, "OK");
                    await Navigation.PushAsync(new AllFlats());
                }
                else
                {
                    await DisplayAlert("Error", loginResponse.Message, "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", $"Error al iniciar sesión. Status: {response.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
            Console.WriteLine($"Error detallado: {ex}");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnRegisterTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new Register());
    }
}
