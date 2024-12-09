namespace APPProyectoMLHOMP.Views;

public partial class VistaUno : ContentPage
{
    public VistaUno()
    {
        InitializeComponent();
    }

    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("VistaLoginRegister");
    }
}