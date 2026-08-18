using System.ComponentModel.DataAnnotations;

namespace BioChecadorAPI.DTOs
{
    public class AuthUsuarioDto
    {
        //
    }

    public class RegistroUsuarioDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(250, ErrorMessage = "El correo no puede superar los 250 caracteres")]
        public string Correo { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "El usaurio no puede superar los 10 caracteres.")]
        public string User { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 255 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "La contraseña debe contener al menos una letra mayúscula, una letra minúscula, un número y un carácter especial.")]
        public string Clave_Seguridad { get; set; } = string.Empty;
    }

    public class  LoginUsuarioDto
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Clave_Seguridad { get; set; } = string.Empty;
    }

    public class UsuarioResponseDto
    {
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
    }
}
