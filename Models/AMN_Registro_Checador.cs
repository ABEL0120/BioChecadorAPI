namespace BioChecadorAPI.Models
{
    public class AMN_Registro_Checador
    {
        public long Id { get; set; }
        public string RFC { get; set; } = string.Empty;
        public int Numero_Compañia { get; set; }
        public string Fecha_Hora { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public string Dispositivo_User_Agent { get; set; } = string.Empty;
        public string Dispositivo_Nombre { get; set; } = string.Empty;
        public string Tipo_Movimiento { get; set; } = "ENTRADA";
        public string Firma_Valida { get; set; } = "S";
    }
}
