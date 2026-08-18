using System.Text;
using System.Security.Cryptography;

namespace BioChecadorAPI.Helpers
{
    public static class AuthHelper
    {
        public static string HashearClave(string clave)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(clave));
            return Convert.ToHexString(bytes);
        }

        //public static bool VerificarClave(string claveIngresada, string claveHasheada)
        //{
        //    var claveIngresadaHasheada = HashearClave(claveIngresada);
        //    return claveIngresadaHasheada.Equals(claveHasheada, StringComparison.OrdinalIgnoreCase);
        //}
    }
}
