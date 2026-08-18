namespace BioChecadorAPI.Models
{
    public class Compañia
    {
        public int Numero_Compañia { get; set; }
        public string Razon_Social { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
        public int Radio_Tolerancia_Metros { get; set; } = 150;
    }
}
