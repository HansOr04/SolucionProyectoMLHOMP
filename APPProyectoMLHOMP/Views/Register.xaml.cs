using System.Net.Http.Json;
using APPProyectoMLHOMP.Models;

namespace APPProyectoMLHOMP.Views;

public class RegisterResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public User User { get; set; }
}

public partial class Register : ContentPage
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://localhost:7020";

    public Register()
    {
        InitializeComponent();
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        _httpClient = new HttpClient(handler);
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        // Establecer la fecha máxima como hoy
        DatePickerBirth.MaximumDate = DateTime.Today;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (!ValidateForm())
        {
            return;
        }

        try
        {
            LoadingIndicator.IsVisible = true;
            RegisterButton.IsEnabled = false;
            ErrorLabel.IsVisible = false;

            var registerData = new
            {
                FirstName = EntryFirstName.Text?.Trim() ?? string.Empty,
                LastName = EntryLastName.Text?.Trim() ?? string.Empty,
                DateOfBirth = DatePickerBirth.Date,
                Address = EntryAddress.Text?.Trim() ?? string.Empty,
                City = EntryCity.Text?.Trim() ?? string.Empty,
                Country = EntryCountry.Text?.Trim() ?? string.Empty,
                Email = EntryEmail.Text?.Trim() ?? string.Empty,
                PhoneNumber = EntryPhone.Text?.Trim() ?? string.Empty,
                Username = EntryUsername.Text?.Trim() ?? string.Empty,
                Password = EntryPassword.Text ?? string.Empty,
                Biography = EditorBiography.Text?.Trim(),
                Languages = EntryLanguages.Text?.Trim()
            };

            var response = await _httpClient.PostAsJsonAsync("api/User/register", registerData);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Respuesta del servidor: {content}");

            if (response.IsSuccessStatusCode)
            {
                var registerResponse = await response.Content.ReadFromJsonAsync<RegisterResponse>();
                if (registerResponse?.Success == true)
                {
                    await DisplayAlert("Éxito", "Registro completado exitosamente", "OK");
                    await Navigation.PushAsync(new AllFlats());
                }
                else
                {
                    ErrorLabel.Text = registerResponse?.Message ?? "Error desconocido";
                    ErrorLabel.IsVisible = true;
                }
            }
            else
            {
                ErrorLabel.Text = $"Error al registrar usuario. Status: {response.StatusCode}";
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
            RegisterButton.IsEnabled = true;
        }
    }

    private bool ValidateForm()
    {
        // Validar campos requeridos
        if (string.IsNullOrWhiteSpace(EntryFirstName.Text) ||
            string.IsNullOrWhiteSpace(EntryLastName.Text) ||
            string.IsNullOrWhiteSpace(EntryEmail.Text) ||
            string.IsNullOrWhiteSpace(EntryPhone.Text) ||
            string.IsNullOrWhiteSpace(EntryAddress.Text) ||
            string.IsNullOrWhiteSpace(EntryCity.Text) ||
            string.IsNullOrWhiteSpace(EntryCountry.Text) ||
            string.IsNullOrWhiteSpace(EntryUsername.Text) ||
            string.IsNullOrWhiteSpace(EntryPassword.Text) ||
            string.IsNullOrWhiteSpace(EntryConfirmPassword.Text))
        {
            ErrorLabel.Text = "Por favor, complete todos los campos obligatorios";
            ErrorLabel.IsVisible = true;
            return false;
        }

        // Validar contraseñas coincidentes
        if (EntryPassword.Text != EntryConfirmPassword.Text)
        {
            ErrorLabel.Text = "Las contraseñas no coinciden";
            ErrorLabel.IsVisible = true;
            return false;
        }

        // Validar formato de correo electrónico
        if (!IsValidEmail(EntryEmail.Text))
        {
            ErrorLabel.Text = "Por favor, ingrese un correo electrónico válido";
            ErrorLabel.IsVisible = true;
            return false;
        }

        ErrorLabel.IsVisible = false;
        return true;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
