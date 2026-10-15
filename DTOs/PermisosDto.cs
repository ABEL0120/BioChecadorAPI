using System.ComponentModel.DataAnnotations;

namespace BioChecadorAPI.DTOs
{
    public class PermisosDto
    {
        public class PermisoEmpleadoDto
        {
            public int Compania { get; set; }
            public int Numero { get; set; }
            public int M103 { get; set; }
            public int SecuenciaM103 { get; set; }
            public string NombreEmpleado { get; set; } = string.Empty;
            public string FechaInicioPermiso { get; set; } = string.Empty;
            public string FechaFinalizaPermiso { get; set; } = string.Empty;
            public string HoraIniciaPermiso { get; set; } = string.Empty;
            public string HoraFinalizaPermiso { get; set; } = string.Empty;
            public int MinutosTotalPermiso { get; set; }
            public string TiempoTexto { get; set; } = string.Empty;
            public string MotivoPermiso { get; set; } = string.Empty;
            public string FechaRegistro { get; set; } = string.Empty;
            public string HoraRegistro { get; set; } = string.Empty;
            public string VoBo { get; set; } = "PENDIENTE";
            public int PersonalVoBo { get; set; }
            public string PersonalNombreVoBo { get; set; } = string.Empty;
            public string AplicaVacaciones { get; set; } = "N";
            public int SolicitudVacaciones { get; set; }
        }

        public class DatosInicialesPermisosDto
        {
            public int NumeroEmpleado { get; set; }
            public string NombreEmpleado { get; set; } = string.Empty;
            public string CorreoEmpleado { get; set; } = string.Empty;
            public int JefeInmediato { get; set; }
            public string JefeInmediatoNombre { get; set; } = string.Empty;
            public string JefeInmediatoCorreo { get; set; } = string.Empty;
            public int DiasVacacionesDisponibles { get; set; }
            public string PeriodoVacacionesActual { get; set; } = string.Empty;
            public string TurnoDescripcion { get; set; } = string.Empty;
            public string HorarioHoy { get; set; } = string.Empty;
            public string ComidaHoy { get; set; } = string.Empty;
        }

        public class CalcularTiempoPermisoRequestDto
        {
            [Required]
            [Range(1, int.MaxValue)]
            public int Compania { get; set; }

            [Required]
            [Range(1, int.MaxValue)]
            public int M103 { get; set; }

            [Required]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
            public string FechaInicio { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$")]
            public string FechaFin { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{2}:\d{2}$")]
            public string HoraInicio { get; set; } = string.Empty;

            [Required]
            [RegularExpression(@"^\d{2}:\d{2}$")]
            public string HoraFin { get; set; } = string.Empty;
        }

        public class CalcularTiempoPermisoResponseDto
        {
            public int MinutosBrutos { get; set; }
            public int MinutosComidaDescontados { get; set; }
            public int MinutosNetos { get; set; }
            public string TiempoTexto { get; set; } = string.Empty;
            public bool PermiteVacaciones { get; set; }
            public int DiasLaborablesVacaciones { get; set; }
            public bool CruzaComida { get; set; }
            public string MensajeDetalle { get; set; } = string.Empty;
            public bool EsValido { get; set; } = true;
            public string ErrorValidacion { get; set; } = string.Empty;
        }

        public class CrearPermisoDto
        {
            [Required(ErrorMessage = "La compañía es requerida.")]
            [Range(1, int.MaxValue, ErrorMessage = "El ID de la compañía debe ser mayor a 0.")]
            public int Compania { get; set; }

            [Required(ErrorMessage = "El número de empleado es requerido.")]
            [Range(1, int.MaxValue, ErrorMessage = "El número de empleado debe ser mayor a 0.")]
            public int M103 { get; set; }

            [Required(ErrorMessage = "La fecha de inicio es requerida.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "Formato YYYY/MM/DD.")]
            public string FechaInicio { get; set; } = string.Empty;

            [Required(ErrorMessage = "La fecha de fin es requerida.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "Formato YYYY/MM/DD.")]
            public string FechaFin { get; set; } = string.Empty;

            [Required(ErrorMessage = "La hora de inicio es requerida.")]
            [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Formato HH:mm.")]
            public string HoraInicio { get; set; } = string.Empty;

            [Required(ErrorMessage = "La hora de fin es requerida.")]
            [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Formato HH:mm.")]
            public string HoraFin { get; set; } = string.Empty;

            [Required(ErrorMessage = "El motivo es requerido.")]
            [StringLength(500, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre 3 y 500 caracteres.")]
            public string Motivo { get; set; } = string.Empty;

            public string AplicaVacaciones { get; set; } = "N";
        }

        public class ActualizarPermisoDto
        {
            [Required(ErrorMessage = "La compañía es requerida.")]
            [Range(1, int.MaxValue, ErrorMessage = "El ID de la compañía debe ser mayor a 0.")]
            public int Compania { get; set; }

            [Required(ErrorMessage = "El número de empleado es requerido.")]
            [Range(1, int.MaxValue, ErrorMessage = "El número de empleado debe ser mayor a 0.")]
            public int M103 { get; set; }

            [Required(ErrorMessage = "El número de folio es requerido.")]
            [Range(1, int.MaxValue, ErrorMessage = "El número de folio debe ser mayor a 0.")]
            public int Numero { get; set; }

            [Required(ErrorMessage = "La fecha de inicio es requerida.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "Formato YYYY/MM/DD.")]
            public string FechaInicio { get; set; } = string.Empty;

            [Required(ErrorMessage = "La fecha de fin es requerida.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "Formato YYYY/MM/DD.")]
            public string FechaFin { get; set; } = string.Empty;

            [Required(ErrorMessage = "La hora de inicio es requerida.")]
            [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Formato HH:mm.")]
            public string HoraInicio { get; set; } = string.Empty;

            [Required(ErrorMessage = "La hora de fin es requerida.")]
            [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Formato HH:mm.")]
            public string HoraFin { get; set; } = string.Empty;

            [Required(ErrorMessage = "El motivo es requerido.")]
            [StringLength(500, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre 3 y 500 caracteres.")]
            public string Motivo { get; set; } = string.Empty;

            public string AplicaVacaciones { get; set; } = "N";
        }

        public class CancelarPermisoDto
        {
            [Required(ErrorMessage = "La compañía es requerida.")]
            [Range(1, int.MaxValue)]
            public int Compania { get; set; }

            [Required(ErrorMessage = "El número de empleado es requerido.")]
            [Range(1, int.MaxValue)]
            public int M103 { get; set; }

            [Required(ErrorMessage = "El folio es requerido.")]
            [Range(1, int.MaxValue)]
            public int Numero { get; set; }
        }
    }
}
