// Importación de espacios de nombres necesarios para el funcionamiento del controlador
using Microsoft.AspNetCore.Mvc; // Proporciona tipos y funcionalidades para construir aplicaciones web con ASP.NET Core MVC.
using Microsoft.EntityFrameworkCore; // Ofrece herramientas para interactuar con bases de datos utilizando Entity Framework Core.
using ProyectoMLHOMP.Models; // Incluye los modelos definidos en el proyecto para interactuar con la base de datos.
using System.Diagnostics; // Proporciona clases para interactuar con procesos del sistema y herramientas de diagnóstico.

namespace ProyectoMLHOMP.Controllers // Define el espacio de nombres para el controlador.
{
    // Definición de la clase HomeController, que hereda de la clase base Controller de ASP.NET Core MVC.
    public class HomeController : Controller
    {
        // Campos privados para almacenar las instancias de ILogger y ProyectoContext.
        private readonly ILogger<HomeController> _logger; // Permite registrar mensajes de error e información para la depuración y el diagnóstico.
        private readonly ProyectoContext _context; // Contexto de la base de datos que maneja la interacción con la tabla de apartamentos.

        // Constructor de la clase HomeController. Se llama cuando se crea una instancia del controlador.
        // Recibe un ILogger y un ProyectoContext como parámetros, que son inyectados automáticamente por el contenedor de dependencias de ASP.NET Core.
        public HomeController(ILogger<HomeController> logger, ProyectoContext context)
        {
            _logger = logger; // Asigna el logger pasado al campo privado _logger.
            _context = context; // Asigna el contexto de la base de datos pasado al campo privado _context.
        }

        // Método asincrónico que maneja las solicitudes GET para la acción Index.
        // Se utiliza para obtener una lista de apartamentos disponibles y renderizar la vista principal.
        public async Task<IActionResult> Index()
        {
            try // Bloque try-catch para manejar posibles errores durante la consulta de la base de datos.
            {
                // Consulta a la base de datos para obtener apartamentos disponibles.
                // Usa Include para cargar información relacionada del propietario de cada apartamento.
                // Filtra por aquellos apartamentos que están disponibles (IsAvailable).
                // Ordena los resultados de manera descendente por la fecha de creación (CreatedAt).
                var apartments = await _context.Apartment
                    .Include(a => a.Owner) // Incluye los datos del propietario en la consulta para evitar múltiples consultas.
                    .Where(a => a.IsAvailable) // Filtra los apartamentos que están marcados como disponibles.
                    .OrderByDescending(a => a.CreatedAt) // Ordena los apartamentos por la fecha de creación, del más reciente al más antiguo.
                    .ToListAsync(); // Ejecuta la consulta de forma asincrónica y convierte los resultados en una lista.

                // Devuelve la vista junto con la lista de apartamentos obtenida.
                return View(apartments);
            }
            catch (Exception ex) // Captura cualquier excepción que ocurra durante la consulta.
            {
                // Registra el error en el sistema de logs, incluyendo el mensaje de excepción.
                _logger.LogError(ex, "Error al cargar los apartamentos");
                // Devuelve la vista, pero con una lista vacía de apartamentos en caso de error.
                return View(new List<Apartment>());
            }
        }

        // Método que maneja las solicitudes GET para la acción Privacy.
        // Muestra la vista de privacidad.
        public IActionResult Privacy()
        {
            return View(); // Devuelve la vista asociada a la política de privacidad.
        }

        // Método para manejar errores en la aplicación.
        // La anotación ResponseCache especifica que la respuesta no se almacena en caché.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Devuelve la vista de error con un modelo que contiene el ID de la solicitud.
            // Si no hay un ID de actividad disponible, utiliza el identificador de rastreo de HttpContext.
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
