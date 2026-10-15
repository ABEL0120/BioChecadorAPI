using BioChecadorAPI.DTOs;
using BioChecadorAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BioChecadorAPI.DTOs.PermisosDto;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Controllers
{
    [Route("api/permisos/")]
    [ApiController]
    public class PermisosController : ControllerBase
    {
        private readonly IPermisosService _permisosService;

        public PermisosController(IPermisosService permisosService)
        {
            _permisosService = permisosService;
        }

        [HttpGet("datos-iniciales/{compania}/{m103}")]
        [ProducesResponseType(typeof(ApiResponse<DatosInicialesPermisosDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DatosInicialesPermisosDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerDatosIniciales(int compania, int m103)
        {
            var respuesta = await _permisosService.ConsultarDatosInicialesAsync(compania, m103);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpPost("calcular-tiempo")]
        [ProducesResponseType(typeof(ApiResponse<CalcularTiempoPermisoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CalcularTiempoPermisoResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CalcularTiempo([FromBody] CalcularTiempoPermisoRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<CalcularTiempoPermisoResponseDto>
                {
                    Success = false,
                    Message = errores
                });
            }

            var respuesta = await _permisosService.CalcularTiempoAsync(dto);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpGet("historial/{compania}/{m103}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PermisoEmpleadoDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PermisoEmpleadoDto>>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerHistorial(int compania, int m103)
        {
            var respuesta = await _permisosService.ConsultarHistorialAsync(compania, m103);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpPost("solicitar")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SolicitarPermiso([FromBody] CrearPermisoDto dto)
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

            var respuesta = await _permisosService.SolicitarPermisoAsync(dto);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpPut("actualizar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarPermiso([FromBody] ActualizarPermisoDto dto)
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

            var respuesta = await _permisosService.ActualizarPermisoAsync(dto);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        [HttpDelete("cancelar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CancelarPermiso([FromBody] CancelarPermisoDto dto)
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

            var respuesta = await _permisosService.CancelarPermisoAsync(dto);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }
    }
}
