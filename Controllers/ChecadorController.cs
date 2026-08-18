using BioChecadorAPI.DTOs;
using BioChecadorAPI.Services;
using Microsoft.AspNetCore.Mvc;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Controllers
{
    [Route("api/checador/")]
    [ApiController]
    public class ChecadorController : ControllerBase
    {
        private readonly IChecadorService _checadorService;

        public ChecadorController(IChecadorService checadorService)
        {
            _checadorService = checadorService;
        }

        [HttpPost("verificar-rfc")]
        [ProducesResponseType(typeof(ApiResponse<EstadoEmpleadoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<EstadoEmpleadoResponseDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<EstadoEmpleadoResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerificarRfc([FromBody] ConsultaRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<EstadoEmpleadoResponseDto>
                {
                    Success = false,
                    Message = errores
                });
            }

            var resultado = await _checadorService.VerificarRfcAsync(dto);

            if (!resultado.Success)
            {
                return NotFound(resultado);
            }

            return Ok(resultado);
        }

        [HttpPost("enrolar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Enrolar([FromBody] EnrolarBiometriaDto dto)
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

            var respuesta = await _checadorService.EnrolarBiometriaAsync(dto);

            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }

            return Ok(respuesta);
        }

        [HttpPost("marcar")]
        [ProducesResponseType(typeof(ApiResponse<RegistroChecadaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<RegistroChecadaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Marcar([FromBody] MarcarAsistenciaDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errores = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return BadRequest(new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = errores
                });
            }
            var respuesta = await _checadorService.MarcarAsistenciaAsync(dto);
            if (!respuesta.Success)
            {
                return BadRequest(respuesta);
            }
            return Ok(respuesta);
        }

        //----------------
    }
}
