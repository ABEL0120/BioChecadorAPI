using System.ComponentModel.DataAnnotations;

namespace BioChecadorAPI.DTOs
{
    public class HuellaFisicaDto
    {
        public int Numero { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public string TemplateBase64 { get; set; } = string.Empty;
    }

    public class UltimoMovimientoSensorHoyDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string UltimoMovimientoHoy { get; set; } = string.Empty;
        public bool RequiereSeleccion { get; set; }
        public string? SiguienteMovimientoSugerido { get; set; }
        public string[]? Opciones { get; set; }
    }

    public class MarcarSensorFisicoRequestDto
    {
        [Required(ErrorMessage = "El RFC es obligatorio.")]
        [StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener 12 o 13 caracteres.")]
        [RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", ErrorMessage = "El RFC no tiene un formato válido.")]
        public string Rfc { get; set; } = string.Empty;

        public string? TipoMovimientoOpcional { get; set; }
        public string DispositivoNombre { get; set; } = "Windows Device (Sensor Fisico)";
        public string UserAgent { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
    }

    public class SensorFisicoResponseDto
    {
        public bool RequiereSeleccion { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public string UltimoMovimientoHoy { get; set; } = string.Empty;
        public string[]? Opciones { get; set; }
        public int? Numero { get; set; }
        public int? NumeroCompania { get; set; }
        public string? TipoMovimiento { get; set; }
        public string? EtiquetaMovimiento { get; set; }
        public string? FechaHora { get; set; }
        public string? LineaHistorial { get; set; }
    }

    public class UltimaChecadaSensorDto
    {
        public int Numero { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public string FechaHora { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public string EtiquetaMovimiento { get; set; } = string.Empty;
        public string DispositivoNombre { get; set; } = string.Empty;
        public string LineaHistorial { get; set; } = string.Empty;
    }
}
