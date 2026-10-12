using BioChecadorAPI.DTOs;
using BioChecadorAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BioChecadorAPI.DTOs.VacacionesDto;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Controllers
{
    [Route("api/vacaciones/")]
    [ApiController]
    public class VacacionesController : ControllerBase
    {
        private readonly IVacacionesService _vacacionesService;

        public VacacionesController(IVacacionesService vacacionesService)
        {
            _vacacionesService = vacacionesService;
        }

        [HttpGet("datos-iniciales/{compania}/{m103}")]
        [ProducesResponseType(typeof(ApiResponse<DatosInicialesVacacionesDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DatosInicialesVacacionesDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerDatosIniciales(int compania, int m103)
        {
            var respuesta = await _vacacionesService.ConsultarDatosInicialesAsync(compania, m103);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpPost("calcular-fechas")]
        [ProducesResponseType(typeof(ApiResponse<CalcularFechasResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CalcularFechasResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CalcularFechas([FromBody] CalcularFechasRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<CalcularFechasResponseDto>
                {
                    Success = false,
                    Message = errores,
                    Data = null
                });
            }

            var respuesta = await _vacacionesService.CalcularFechasAsync(dto);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpGet("historial/{compania}/{m103}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<SolicitudVacacionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<SolicitudVacacionDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerHistorial(int compania, int m103)
        {
            var respuesta = await _vacacionesService.ConsultarHistorialAsync(compania, m103);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpPost("solicitar")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SolicitarVacaciones([FromBody] CrearSolicitudDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<int>
                {
                    Success = false,
                    Message = errores,
                    Data = 0
                });
            }

            var respuesta = await _vacacionesService.EnviarSolicitudAsync(dto);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpDelete("cancelar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelarSolicitud([FromBody] CancelarSolicitudDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = errores,
                    Data = false
                });
            }

            var respuesta = await _vacacionesService.CancelarSolicitudAsync(dto);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpPut("actualizar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarSolicitud([FromBody] ActualizarSolicitudDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = errores,
                    Data = false
                });
            }

            var respuesta = await _vacacionesService.ActualizarSolicitudAsync(dto);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

    }
}