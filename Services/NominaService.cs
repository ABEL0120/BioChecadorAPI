using System.Net.Http.Json;
using BioChecadorAPI.Repositories;
using static BioChecadorAPI.DTOs.NominaDto;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Services
{
    public interface INominaService
    {
        Task<ApiResponse<List<ReciboNominaDto>>> ConsultarRecibosEmpleadoAsync(int compania, int m103, string? fechaInicial, string? fechaFinal);
        Task<(byte[]? ArchivoBytes, string ContentType, string NombreDescarga)> DescargarArchivoAsync(int anio, int proceso, string tipo, string archivo);
    }

    public class NominaService : INominaService
    {
        private readonly INominaRepository _nominaRepo;
        private readonly HttpClient _httpClient;
        private readonly string _microservicioUrl;

        public NominaService(INominaRepository nominaRepo, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _nominaRepo = nominaRepo;
            _httpClient = httpClientFactory.CreateClient("NominaDocApiClient");
            _microservicioUrl = configuration["Microservicios:NominaDocApiUrl"] ?? "http://localhost:5000";
        }

        public async Task<ApiResponse<List<ReciboNominaDto>>> ConsultarRecibosEmpleadoAsync(int compania, int m103, string? fechaInicial, string? fechaFinal)
        {
            var (nombre, _) = await _nominaRepo.ObtenerDatosEmpleadoAsync(compania, m103);
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return new ApiResponse<List<ReciboNominaDto>>
                {
                    Success = false,
                    Message = "Empleado no encontrado.",
                    Data = new List<ReciboNominaDto>()
                };
            }

            var periodos = await _nominaRepo.ObtenerPeriodosTimbradosAsync(fechaInicial, fechaFinal);
            if (!periodos.Any())
            {
                return new ApiResponse<List<ReciboNominaDto>>
                {
                    Success = true,
                    Message = "No hay periodos timbrados en las fechas seleccionadas.",
                    Data = new List<ReciboNominaDto>()
                };
            }

            var requestMicroservicio = new
            {
                NumeroEmpleado = m103,
                NombreEmpleado = nombre,
                Periodos = periodos
            };

            var response = await _httpClient.PostAsJsonAsync($"{_microservicioUrl}/api/archivos/verificar-recibos", requestMicroservicio);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<List<ReciboNominaDto>>
                {
                    Success = false,
                    Message = "Error al conectar con el servidor de archivos de nómina.",
                    Data = new List<ReciboNominaDto>()
                };
            }

            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<List<ReciboNominaDto>>>();
            return apiRes ?? new ApiResponse<List<ReciboNominaDto>> { Success = false, Message = "Respuesta vacía" };
        }

        public async Task<(byte[]? ArchivoBytes, string ContentType, string NombreDescarga)> DescargarArchivoAsync(int anio, int proceso, string tipo, string archivo)
        {
            var response = await _httpClient.GetAsync($"{_microservicioUrl}/api/archivos/descargar/{anio}/{proceso}/{tipo}/{archivo}");

            if (!response.IsSuccessStatusCode)
            {
                return (null, string.Empty, string.Empty);
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            string contentType = response.Content.Headers.ContentType?.ToString() ?? (tipo.ToLower() == "xml" ? "application/xml" : "application/pdf");
            string ext = tipo.ToLower() == "xml" ? ".xml" : ".pdf";

            return (bytes, contentType, $"{archivo}{ext}");
        }
    }
}
