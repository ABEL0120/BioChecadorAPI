namespace BioChecadorAPI.Models
{
    public class AMN
    {
        public int M103 { get; set; }
        public int Compañia { get; set; }
        public string M104 { get; set; } = string.Empty; //Nombre completo del usuario
        public string M105 { get; set; } = string.Empty; //RFC
    }
}
