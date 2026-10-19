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

        [HttpGet("estado")]
        public IActionResult ObtenerEstado([FromQuery] string rfc, [FromQuery] string? dispositivo)
        {
            if (string.IsNullOrWhiteSpace(rfc))
            {
                return BadRequest(new { success = false, message = "RFC es obligatorio." });
            }

            var rfcNorm = rfc.Trim().ToUpperInvariant();
            var dispNorm = (dispositivo ?? string.Empty).Trim();
            var todas = _subStore.ObtenerTodas();
            bool tieneSuscripcion = todas.Any(e => 
                e.Rfc.Trim().ToUpperInvariant() == rfcNorm &&
                e.Dispositivos != null &&
                e.Dispositivos.Any(d => (string.IsNullOrWhiteSpace(dispNorm) || string.Equals(d.Dispositivo.Trim(), dispNorm, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(d.PushSubscription))
            );

            return Ok(new { success = true, tieneSuscripcion });
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

        [HttpPost("probar")]
        public async Task<IActionResult> ProbarPush([FromBody] ProbarPushDto dto, [FromServices] IPushNotificationService pushService)
        {
            string? subJson = dto.SubscriptionJson;
            if (string.IsNullOrWhiteSpace(subJson) && !string.IsNullOrWhiteSpace(dto.Rfc))
            {
                var rfcNorm = dto.Rfc.Trim().ToUpperInvariant();
                var emp = _subStore.ObtenerTodas().FirstOrDefault(e => e.Rfc == rfcNorm);
                var disp = emp?.Dispositivos?.FirstOrDefault(d => string.IsNullOrWhiteSpace(dto.Dispositivo) || string.Equals(d.Dispositivo, dto.Dispositivo, StringComparison.OrdinalIgnoreCase));
                subJson = disp?.PushSubscription;
            }

            if (string.IsNullOrWhiteSpace(subJson))
            {
                return BadRequest(new { success = false, message = "No se encontró suscripción push para el usuario o dispositivo especificado." });
            }

            var resultado = await pushService.EnviarNotificacionAsync(
                subJson,
                "Prueba de Recordatorio",
                "¡Excelente! Las notificaciones de alta prioridad están funcionando en tu dispositivo.",
                "/home",
                $"test-{DateTime.Now.Ticks}"
            );

            if (resultado == PushEnvioResultado.Exitoso)
            {
                return Ok(new { success = true, message = "Notificación de prueba enviada exitosamente." });
            }
            else
            {
                return StatusCode(500, new { success = false, message = $"Error al enviar notificación ({resultado})." });
            }
        }
    }
}
