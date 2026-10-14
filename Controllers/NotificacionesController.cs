using Microsoft.AspNetCore.Mvc;
using BioChecadorAPI.DTOs;
using BioChecadorAPI.Services;
using BioChecadorAPI.Repositories;

namespace BioChecadorAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacionesController : ControllerBase
    {
        private readonly IPushSubscriptionStore _subStore;
        private readonly IChecadorService _checadorService;
        private readonly IAmnRepository _amnRepo;

        public NotificacionesController(
            IPushSubscriptionStore subStore,
            IChecadorService checadorService,
            IAmnRepository amnRepo)
        {
            _subStore = subStore;
            _checadorService = checadorService;
            _amnRepo = amnRepo;
        }

        [HttpPost("suscribir")]
        public async Task<IActionResult> Suscribir([FromBody] SuscribirPushDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Rfc) || string.IsNullOrWhiteSpace(dto.SubscriptionJson))
            {
                return BadRequest(new { success = false, message = "RFC y suscripción son obligatorios." });
            }

            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            var dispNombre = dto.Dispositivo ?? "Dispositivo";

            var request = new VerificarRfcRequestDto
            {
                Rfc = rfcLimpio,
                DispositivoNombre = dispNombre
            };

            var estadoEmpleado = await _checadorService.VerificarRfcAsync(request);
            var dias = estadoEmpleado?.Data?.Horario?.Dias ?? new List<HorarioDiaDto>();

            var ultimoMov = await _amnRepo.ObtenerUltimoMovimientoHoyAsync(rfcLimpio);

            await _subStore.AgregarOSuscribirDispositivoAsync(
                rfcLimpio,
                15,
                dias,
                dispNombre,
                dto.SubscriptionJson,
                ultimoMov
            );

            await _amnRepo.GuardarSuscripcionPushAsync(
                rfcLimpio,
                dispNombre,
                dto.SubscriptionJson
            );

            return Ok(new { success = true, message = "Recordatorios activados correctamente." });
        }

        [HttpPost("desuscribir")]
        public async Task<IActionResult> Desuscribir([FromBody] DesuscribirPushDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Rfc))
            {
                return BadRequest(new { success = false, message = "RFC es obligatorio." });
            }

            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            var dispNombre = dto.Dispositivo ?? "Dispositivo";

            await _subStore.EliminarDispositivoAsync(rfcLimpio, dispNombre);

            await _amnRepo.EliminarSuscripcionPushAsync(
                rfcLimpio,
                dispNombre
            );

            return Ok(new { success = true, message = "Dispositivo desuscrito correctamente." });
        }
    }
}
