namespace BioChecadorAPI.DTOs
{
    public class NominaDto
    {
        public class CalendarioPeriodoDto
        {
            public int Anio { get; set; }
            public int Proceso { get; set; }
            public string FechaInicio { get; set; } = string.Empty;
            public string FechaTermino { get; set; } = string.Empty;
            public string FechaPago { get; set; } = string.Empty;
        }

        public class ReciboNominaDto
        {
            public int Anio { get; set; }
            public int Proceso { get; set; }
            public string FechaInicio { get; set; } = string.Empty;
            public string FechaTermino { get; set; } = string.Empty;
            public string FechaPago { get; set; } = string.Empty;
            public string NombreArchivo { get; set; } = string.Empty;
            public bool TienePdf { get; set; }
            public bool TieneXml { get; set; }
        }
    }
}
