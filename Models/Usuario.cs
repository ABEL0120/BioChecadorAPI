namespace BioChecadorAPI.Models
{
    public class Usuario
    {
        public int Numero {get; set; }
        public string Nombre {get; set; } = string.Empty;
        public string Clave_Seguridad {get; set; } = string.Empty;
        public string Correo {get; set; } = string.Empty;
        public string User {get; set; } = string.Empty;
        public string Baja {get; set; } = string.Empty;
    }
}
