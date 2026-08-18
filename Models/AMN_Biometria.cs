namespace BioChecadorAPI.Models
{
    public class AMN_Biometria
    {
        public int Id { get; set; }
        public string RFC { get; set; } = string.Empty;
        public string Credential_Id { get; set; } = string.Empty;
        public byte[] Public_Key { get; set; } = Array.Empty<byte>();
        public long Sign_Count { get; set; }
        public string Dispositivo_Nombre { get; set; } = string.Empty;
        public string Dispositivo_User_Agent { get; set; } = string.Empty;
        public string Fecha_Alta { get; set; } = string.Empty;
        public string Baja { get; set; } = string.Empty;
    }
}
