namespace BioChecadorAPI.Models
{
    public class Seguridad
    {
        public int Numero { get; set; }
        public string Clave_Seguridad { get; set; } = string.Empty;
        public string Nombre_Usuario { get; set; } = string.Empty;
        public int Compañia { get; set; } 
    }
}
