using BioChecadorAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static BioChecadorAPI.DTOs.NominaDto;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Controllers
{
    [ApiController]
    [Route("api/nomina")]
    public class NominaController : ControllerBase
    {
        private readonly INominaService _nominaService;

        public NominaController(INominaService nominaService)
        {
            _nominaService = nominaService;
        }

        [HttpGet("recibos/{compania}/{m103}")]
        public async Task<IActionResult> ConsultarRecibos(int compania, int m103, [FromQuery] string? fechaInicial, [FromQuery] string? fechaFinal, [FromQuery] string? rfcCompania)
        {
            var res = await _nominaService.ConsultarRecibosEmpleadoAsync(compania, m103, fechaInicial, fechaFinal, rfcCompania);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpGet("descargar/{anio}/{proceso}/{tipo}/{archivo}")]
        public async Task<IActionResult> Descargar(int anio, int proceso, string tipo, string archivo, [FromQuery] string? rfcCompania)
        {
            var (bytes, contentType, nombreDescarga) = await _nominaService.DescargarArchivoAsync(anio, proceso, tipo, archivo, rfcCompania);
            if (bytes == null)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "No se pudo recuperar el archivo de nómina.",
                    Data = false
                });
            }

            return File(bytes, contentType, nombreDescarga);
        }
    }
}
