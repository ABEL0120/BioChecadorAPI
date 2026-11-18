using BioChecadorAPI.DTOs;
using BioChecadorAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Controllers
{
    [Route("api/sensor-fisico/")]
    [ApiController]
    public class SensorFisicoController : ControllerBase
    {
        private readonly ISensorFisicoService _sensorFisicoService;

        public SensorFisicoController(ISensorFisicoService sensorFisicoService)
        {
            _sensorFisicoService = sensorFisicoService;
        }

        [HttpGet("huellas")]
        [ProducesResponseType(typeof(ApiResponse<List<HuellaFisicaDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerHuellasActivas()
        {
            var respuesta = await _sensorFisicoService.ObtenerHuellasActivasAsync();
            return Ok(respuesta);
        }

        [HttpGet("ultimo-movimiento/{rfc}")]
        [ProducesResponseType(typeof(ApiResponse<UltimoMovimientoSensorHoyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UltimoMovimientoSensorHoyDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerUltimoMovimientoHoy([FromRoute] string rfc)
        {
            if (string.IsNullOrWhiteSpace(rfc))
            {
                return BadRequest(new ApiResponse<UltimoMovimientoSensorHoyDto>
                {
                    Success = false,
                    Message = "El RFC es obligatorio."
                });
            }

            var respuesta = await _sensorFisicoService.ObtenerUltimoMovimientoHoyAsync(rfc);
            return Ok(respuesta);
        }

        [HttpPost("marcar")]
        [ProducesResponseType(typeof(ApiResponse<SensorFisicoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SensorFisicoResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Marcar([FromBody] MarcarSensorFisicoRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message));

                return BadRequest(new ApiResponse<SensorFisicoResponseDto>
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(errores) ? "Parámetros de solicitud inválidos." : errores
                });
            }

            var (statusCode, respuesta) = await _sensorFisicoService.RegistrarChecadaAsync(dto);
            return StatusCode(statusCode, respuesta);
        }

        [HttpGet("ultimas-checadas")]
        [ProducesResponseType(typeof(ApiResponse<List<UltimaChecadaSensorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerUltimasChecadas(
            [FromQuery] int limite = 5,
            [FromQuery] string? dispositivoNombre = null)
        {
            var respuesta = await _sensorFisicoService.ObtenerUltimasChecadasAsync(limite, dispositivoNombre);
            return Ok(respuesta);
        }
    }
}
