using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System;

namespace BioChecadorAPI.Helpers
{
    public static class AuthHelper
    {
        public static string HashearClave(string clave)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(clave));
            return Convert.ToHexString(bytes);
        }

        public static string DesEncripta(string pass, string clave = "NOM_2020")
        {
            if (string.IsNullOrEmpty(pass))
                return string.Empty;
            var sb = new StringBuilder();
            int j = 0;
            for (int i = 0; i < pass.Length; i += 2)
            {
                string hexPair = pass.Substring(i, 2);
                int byteVal = Convert.ToInt32(hexPair, 16);
                char keyChar = clave[j % clave.Length];
                sb.Append((char)(keyChar ^ byteVal));
                j++;
            }
            return sb.ToString();
        }

        //public static bool VerificarClave(string claveIngresada, string claveHasheada)
        //{
        //    var claveIngresadaHasheada = HashearClave(claveIngresada);
        //    return claveIngresadaHasheada.Equals(claveHasheada, StringComparison.OrdinalIgnoreCase);
        //}
    }
}
