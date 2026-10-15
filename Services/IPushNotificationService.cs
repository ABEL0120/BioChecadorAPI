using System.Net;
using System.Text.Json;
using BioChecadorAPI.DTOs;
using WebPush;

namespace BioChecadorAPI.Services
{
    public interface IPushNotificationService
    {
        Task<PushEnvioResultado> EnviarNotificacionAsync(
            string subscriptionJson,
            string titulo,
            string mensaje,
            string url = "/home",
            string tag = "",
            CancellationToken ct = default);
    }

    public class PushNotificationService : IPushNotificationService
    {
        private readonly VapidDetails _vapidDetails;
        private readonly WebPushClient _client;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(IConfiguration config, ILogger<PushNotificationService> logger)
        {
            _logger = logger;
            var subject = config["VapidKeys:Subject"];
            var publicKey = config["VapidKeys:PublicKey"];
            var privateKey = config["VapidKeys:PrivateKey"];

            _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            _client = new WebPushClient();
        }

        public async Task<PushEnvioResultado> EnviarNotificacionAsync(
            string subscriptionJson,
            string titulo,
            string mensaje,
            string url = "/home",
            string tag = "",
            CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subscriptionJson))
                {
                    _logger.LogWarning("Intento de envío con subscriptionJson vacío.");
                    return PushEnvioResultado.SuscripcionExpirada;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var subData = JsonSerializer.Deserialize<PushSubscriptionPayload>(subscriptionJson, options);

                if (subData == null || string.IsNullOrWhiteSpace(subData.Endpoint) || subData.Keys == null ||
                    string.IsNullOrWhiteSpace(subData.Keys.P256dh) || string.IsNullOrWhiteSpace(subData.Keys.Auth))
                {
                    _logger.LogWarning("Estructura de suscripción push inválida.");
                    return PushEnvioResultado.SuscripcionExpirada;
                }

                var subscription = new PushSubscription(
                    subData.Endpoint,
                    subData.Keys.P256dh,
                    subData.Keys.Auth
                );

                var resolvedTag = !string.IsNullOrWhiteSpace(tag) ? tag : $"reloj-{Guid.NewGuid():N}";

                var payloadObj = new NotificacionPushModel
                {
                    Title = titulo,
                    Body = mensaje,
                    Icon = "/pwa-192x192-v2.png",
                    Badge = "/pwa-192x192-v2.png",
                    Tag = resolvedTag,
                    Renotify = true,
                    RequireInteraction = false,
                    Vibrate = new[] { 250, 100, 250 },
                    Data = new
                    {
                        url,
                        tag = resolvedTag
                    }
                };

                var payloadJson = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Cabeceras RFC 8030: Urgency 'high' despierta el radio del dispositivo en Android de inmediato
                var headers = new Dictionary<string, object>
                {
                    { "Urgency", "high" },
                    { "Topic", "asistencia" }
                };

                var pushOptions = new Dictionary<string, object>
                {
                    { "vapidDetails", _vapidDetails },
                    { "TTL", 3600 },
                    { "headers", headers }
                };

                await _client.SendNotificationAsync(subscription, payloadJson, pushOptions, ct);
                _logger.LogInformation("Push enviado exitosamente a {Endpoint} con Tag '{Tag}'", subData.Endpoint, resolvedTag);
                return PushEnvioResultado.Exitoso;
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Suscripción WebPush expirada/revocada ({StatusCode}): {Message}", ex.StatusCode, ex.Message);
                return PushEnvioResultado.SuscripcionExpirada;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitorio enviando notificación push: {Message}", ex.Message);
                return PushEnvioResultado.ErrorTemporal;
            }
        }
    }
}