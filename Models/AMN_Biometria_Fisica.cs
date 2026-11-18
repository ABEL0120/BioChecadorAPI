namespace BioChecadorAPI.Models
{
    public class AMN_Biometria_Fisica
    {
        public int Numero { get; set; }
        public string RFC { get; set; } = string.Empty;
        public byte[] Template { get; set; } = Array.Empty<byte>();
        public string Fecha_Alta { get; set; } = string.Empty;
        public string Baja { get; set; } = string.Empty;
    }
}
