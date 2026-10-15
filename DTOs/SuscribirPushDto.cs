namespace BioChecadorAPI.DTOs
{
    public enum PushEnvioResultado
    {
        Exitoso,
        SuscripcionExpirada, // 404 / 410 Gone -> requiere eliminación
        ErrorTemporal       // timeout, 5xx, socket -> NO eliminar
    }

    public class SuscribirPushDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;
        public string SubscriptionJson { get; set; } = string.Empty;
    }

    public class DesuscribirPushDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
    }

    public class ProbarPushDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string? Dispositivo { get; set; }
        public string? SubscriptionJson { get; set; }
    }

    public class PushSubscriptionPayload
    {
        public string Endpoint { get; set; } = string.Empty;
        public PushSubscriptionKeys Keys { get; set; } = new();
    }

    public class PushSubscriptionKeys
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }

    public class NotificacionPushModel
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Icon { get; set; } = "/pwa-192x192-v2.png";
        public string Badge { get; set; } = "/pwa-192x192-v2.png";
        public string Tag { get; set; } = string.Empty;
        public bool Renotify { get; set; } = true;
        public bool RequireInteraction { get; set; } = false;
        public int[] Vibrate { get; set; } = new[] { 250, 100, 250 };
        public object? Data { get; set; }
    }

    public class SuscripcionPersistenteDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string Dispositivo { get; set; } = string.Empty;
        public string PushSubscription { get; set; } = string.Empty;
        public int MinutosAnticipacion { get; set; } = 15;
        public string UltimoMovimiento { get; set; } = string.Empty;
        public string FechaUltimoMovimiento { get; set; } = string.Empty;
        public List<HorarioDiaDto> Dias { get; set; } = new();
    }

    public class DispositivoPushDto
    {
        public string Dispositivo { get; set; } = string.Empty;
        public string PushSubscription { get; set; } = string.Empty;
    }

    public class EmpleadoSuscripcionPersistenteDto
    {
        public string Rfc { get; set; } = string.Empty;
        public int MinutosAnticipacion { get; set; } = 15;
        public string? UltimoMovimiento { get; set; }
        public string? FechaUltimoMovimiento { get; set; }
        public List<HorarioDiaDto> Dias { get; set; } = new();
        public List<DispositivoPushDto> Dispositivos { get; set; } = new();
    }
}