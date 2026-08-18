namespace BioChecadorAPI.Models
{
    public class AMN_Desafio_WebAuthn
    {
        public int Id { get; set; }
        public string RFC { get; set; } = string.Empty;
        public string Challenge { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Fecha_Expiracion { get; set; } = string.Empty;
        public string Consumido { get; set; } = "N";
    }
}
