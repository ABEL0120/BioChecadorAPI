using System.ComponentModel.DataAnnotations;

namespace BioChecadorAPI.DTOs
{
    public class VacacionesDto
    {
        public class SolicitudVacacionDto
        {
            public int Compania { get; set; }
            public int M103 { get; set; }
            public int Numero { get; set; }
            public string Periodo { get; set; } = string.Empty;
            public string CorreoEmpleado { get; set; } = string.Empty;
            public string FechaRegistro { get; set; } = string.Empty;
            public string FechaInicio { get; set; } = string.Empty;
            public string FechaFin { get; set; } = string.Empty;
            public string FechaRetorno { get; set; } = string.Empty;
            public int DiasSolicitados { get; set; }
            public string ComentariosEmpleado { get; set; } = string.Empty;
            public string Estatus { get; set; } = "P";
            public string Cancelada { get; set; } = string.Empty;
            public int JefeInmediato { get; set; }
        }

        public class CrearSolicitudDto
        {
            [Required(ErrorMessage = "La compañía es requerida.")]
            [Range(1, int.MaxValue, ErrorMessage = "El ID de la compañía debe ser mayor a 0.")]
            public int Compania { get; set; }

            [Required(ErrorMessage = "El número de empleado es requerido.")]
            [Range(1, int.MaxValue, ErrorMessage = "El número de empleado debe ser mayor a 0.")]
            public int M103 { get; set; }

            [Required(ErrorMessage = "El periodo es requerido.")]
            [StringLength(20, ErrorMessage = "El periodo no puede exceder los 20 caracteres.")]
            [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El periodo debe tener el formato YYYY-YYYY.")]
            public string Periodo { get; set; } = string.Empty;

            [Required(ErrorMessage = "El correo del empleado es requerido.")]
            [StringLength(120, ErrorMessage = "El correo no puede exceder los 120 caracteres.")]
            [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
            public string CorreoEmpleado { get; set; } = string.Empty;

            [Required(ErrorMessage = "La fecha de inicio es requerida.")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "La fecha de inicio debe tener 10 caracteres.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "La fecha de inicio debe tener el formato YYYY/MM/DD.")]
            public string FechaInicio { get; set; } = string.Empty;

            [Required(ErrorMessage = "La fecha de fin es requerida.")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "La fecha de fin debe tener 10 caracteres.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "La fecha de fin debe tener el formato YYYY/MM/DD.")]
            public string FechaFin { get; set; } = string.Empty;

            [Required(ErrorMessage = "La fecha de retorno es requerida.")]
            [StringLength(10, MinimumLength = 10, ErrorMessage = "La fecha de retorno debe tener 10 caracteres.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "La fecha de retorno debe tener el formato YYYY/MM/DD.")]
            public string FechaRetorno { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los días solicitados son requeridos.")]
            [Range(1, 365, ErrorMessage = "Los días solicitados deben ser al menos 1.")]
            public int DiasSolicitados { get; set; }

            [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder los 500 caracteres.")]
            public string ComentariosEmpleado { get; set; } = string.Empty;

            [Required(ErrorMessage = "El jefe inmediato es requerido.")]
            [Range(1, int.MaxValue, ErrorMessage = "El ID del jefe inmediato debe ser mayor a 0.")]
            public int JefeInmediato { get; set; }

            public string TieneRestriccion { get; set; } = string.Empty;
            public string FechasRestringidas { get; set; } = string.Empty;
            public string TieneDiasAnticipados { get; set; } = string.Empty;
            public int DiasAnticipados { get; set; }
            public string PeriodoAnticipado { get; set; } = string.Empty;
            public string FechasAnticipadas { get; set; } = string.Empty;
            public string NombreEmpleado { get; set; } = string.Empty;
        }

        public class CancelarSolicitudDto
        {
            [Required(ErrorMessage = "La compañía es requerida.")]
            public int Compania { get; set; }

            [Required(ErrorMessage = "El número de empleado es requerido.")]
            public int M103 { get; set; }

            [Required(ErrorMessage = "El número de folio es requerido.")]
            public int Numero { get; set; }

            [StringLength(500, ErrorMessage = "Los comentarios no pueden exceder los 500 caracteres.")]
            public string Comentarios { get; set; } = string.Empty;
        }

        public class PeriodoVacacionDto
        {
            public string Periodo { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public int DiasDisponibles { get; set; }
        }

        public class DatosInicialesVacacionesDto
        {
            public int NumeroEmpleado { get; set; }
            public string NombreEmpleado { get; set; } = string.Empty;
            public string CorreoEmpleado { get; set; } = string.Empty;
            public int JefeInmediato { get; set; }
            public string JefeInmediatoNombre { get; set; } = string.Empty;
            public string JefeInmediatoCorreo { get; set; } = string.Empty;
            public PeriodoVacacionDto PeriodoActual { get; set; } = new();
            public List<PeriodoVacacionDto> PeriodosHistorial { get; set; } = new();
            public List<string> FechasRestringidas { get; set; } = new();
        }

        public class CalcularFechasRequestDto
        {
            [Required]
            public int Compania { get; set; }
            [Required]
            public int M103 { get; set; }
            [Required]
            public string FechaInicio { get; set; } = string.Empty;
            [Required]
            public int DiasSolicitados { get; set; }
            [Required]
            public string Periodo { get; set; } = string.Empty;
        }

        public class CalcularFechasResponseDto
        {
            public string FechaFin { get; set; } = string.Empty;
            public string FechaRetorno { get; set; } = string.Empty;
            public string TieneDiasAnticipados { get; set; } = string.Empty;
            public int DiasAnticipados { get; set; }
            public string PeriodoAnticipado { get; set; } = string.Empty;
            public string FechasAnticipadas { get; set; } = string.Empty;
            public bool CruzaRestringidas { get; set; }
            public string FechasRestringidasCruzadas { get; set; } = string.Empty;
            public List<string> DiasEfectivos { get; set; } = new();
            public List<string> DiasInhabiles { get; set; } = new();
        }

        public class ActualizarSolicitudDto
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

            [Required(ErrorMessage = "La fecha de retorno es requerida.")]
            [RegularExpression(@"^\d{4}/\d{2}/\d{2}$", ErrorMessage = "Formato YYYY/MM/DD.")]
            public string FechaRetorno { get; set; } = string.Empty;

            [Required(ErrorMessage = "Los días solicitados son requeridos.")]
            [Range(1, 365, ErrorMessage = "Los días deben ser al menos 1.")]
            public int DiasSolicitados { get; set; }

            [StringLength(500, ErrorMessage = "Máximo 500 caracteres.")]
            public string ComentariosEmpleado { get; set; } = string.Empty;

            public string TieneRestriccion { get; set; } = string.Empty;
            public string FechasRestringidas { get; set; } = string.Empty;
            public string TieneDiasAnticipados { get; set; } = string.Empty;
            public int DiasAnticipados { get; set; }
            public string PeriodoAnticipado { get; set; } = string.Empty;
            public string FechasAnticipadas { get; set; } = string.Empty;
        }

    }
}