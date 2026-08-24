using System.ComponentModel.DataAnnotations;

namespace BioChecadorAPI.DTOs
{

    public class VerificarRfcRequestDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string DispositivoNombre { get; set; } = string.Empty;
    }

    public class HorarioDiaDto
    {
        public int DiaIndice { get; set; }
        public string DiaNombre { get; set; } = string.Empty;
        public bool EsLaborable { get; set; }
        public string? Entrada { get; set; }
        public string? Salida { get; set; }
        public int ToleranciaEntradaMinutos { get; set; }
        public string? SalidaComida { get; set; }
        public string? RegresoComida { get; set; }
        public int ToleranciaComidaMinutos { get; set; }
    }

    public class TurnoDetalleDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string DiasPatron { get; set; } = string.Empty;
        public List<HorarioDiaDto> Dias { get; set; } = new();
    }

    public class EnrolarBiometriaDto
    {
        [Required(ErrorMessage = "El RFC es obligatorio.")]
        [StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener 12 o 13 caracteres.")]
        [RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", ErrorMessage = "El RFC no tiene un formato válido.")]
        public string Rfc { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de la credencial biométrica es obligatorio.")]
        public string CredentialId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La llave pública biométrica es obligatoria.")]
        public string PublicKey { get; set; } = string.Empty;

        public string Dispositivo { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    public class EstadoEmpleadoDto
    {
        public bool Existe { get; set; }
        public bool TieneBiometria { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public decimal LatitudEmpresa { get; set; }
        public decimal LongitudEmpresa { get; set; }
        public int RadioToleranciaMetros { get; set; }
    }

    public class MarcarAsistenciaDto
    {
        [Required(ErrorMessage = "El RFC es obligatorio.")]
        [StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener 12 o 13 caracteres.")]
        [RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", ErrorMessage = "El RFC no tiene un formato válido.")]
        public string Rfc { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de credencial es obligatorio.")]
        public string CredentialId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La latitud es obligatoria.")]
        public decimal Latitud { get; set; }

        [Required(ErrorMessage = "La longitud es obligatoria.")]
        public decimal Longitud { get; set; }

        public string Dispositivo { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = "Entrada";
    }

    public class RegistroChecadaResponseDto
    {
        public string Rfc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public double DistanciaMetros { get; set; }
        public bool DentroDeRango { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class EstadoEmpleadoResponseDto
    {
        public bool Existe { get; set; }
        public string UltimoMovimientoHoy { get; set; } = String.Empty;
        public bool TieneBiometria { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public decimal LatitudEmpresa { get; set; }
        public decimal LongitudEmpresa { get; set; }
        public int RadioToleranciaMetros { get; set; } = 150;
        public TurnoDetalleDto? Horario { get; set; }
    }
}
