namespace APPProyectoMLHOMP
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrar todas las rutas de navegación
            Routing.RegisterRoute(nameof(Views.VistaUno), typeof(Views.VistaUno));
            Routing.RegisterRoute(nameof(Views.VistaLoginRegister), typeof(Views.VistaLoginRegister));
        }
    }
}