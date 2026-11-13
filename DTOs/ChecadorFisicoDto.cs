using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BioChecadorAPI.DTOs
{
    public class WebAuthnAssertionResponseDto
    {
        [Required(ErrorMessage = "authenticatorData es obligatorio.")]
        public string AuthenticatorData { get; set; } = string.Empty;

        [Required(ErrorMessage = "clientDataJSON es obligatorio.")]
        [JsonPropertyName("clientDataJSON")]
        public string ClientDataJson { get; set; } = string.Empty;

        [Required(ErrorMessage = "signature es obligatorio.")]
        public string Signature { get; set; } = string.Empty;

        public string? UserHandle { get; set; }
    }

    public class WebAuthnAssertionDto
    {
        [Required(ErrorMessage = "El id de la credencial es obligatorio.")]
        public string Id { get; set; } = string.Empty;

        public string? RawId { get; set; }

        public string Type { get; set; } = "public-key";

        [Required(ErrorMessage = "La respuesta de aserción WebAuthn es obligatoria.")]
        public WebAuthnAssertionResponseDto Response { get; set; } = new();
    }

    public class ChecadorFisicoRequestDto
    {
        [Required(ErrorMessage = "La respuesta de aserción WebAuthn es obligatoria.")]
        public WebAuthnAssertionDto Passkey { get; set; } = new();

        [Required(ErrorMessage = "El token de checador físico es obligatorio.")]
        public string TokenChecadorFisico { get; set; } = string.Empty;

        public string? TipoMovimientoOpcional { get; set; }

        public decimal Latitud { get; set; }

        public decimal Longitud { get; set; }

        public string DispositivoNombre { get; set; } = string.Empty;
    }

    public class ChecadorFisicoResponseDto
    {
        public bool RequiereSeleccion { get; set; }

        public string Rfc { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Opciones { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TipoMovimiento { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FechaHora { get; set; }
    }

    public class UltimoMovimientoMovilDto
    {
        public int Numero { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public string FechaHora { get; set; } = string.Empty;
        public string DispositivoNombre { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
    }

    public class PasskeyCredencialDto
    {
        public int Numero { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public string CredentialId { get; set; } = string.Empty;
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public long SignCount { get; set; }
        public string DispositivoNombre { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public int NumeroEmpleado { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}

