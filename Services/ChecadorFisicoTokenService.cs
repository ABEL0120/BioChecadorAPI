using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BioChecadorAPI.Services
{
    public interface IChecadorFisicoTokenService
    {
        bool ValidarToken(string? tokenChecadorFisico);
        string GenerarToken(long? bloque = null);
    }

    public class ChecadorFisicoTokenService : IChecadorFisicoTokenService
    {
        private readonly byte[] _secretKeyBytes;

        public ChecadorFisicoTokenService(IConfiguration configuration)
        {
            var secretKey = configuration["ChecadorFisicoSecretKey"] ?? string.Empty;
            _secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        }

        public bool ValidarToken(string? tokenChecadorFisico)
        {
            if (_secretKeyBytes.Length == 0 || string.IsNullOrWhiteSpace(tokenChecadorFisico))
            {
                return false;
            }

            if (!IntentarDecodificarToken(tokenChecadorFisico.Trim(), out var tokenBytes))
            {
                return false;
            }

            long bloqueActual = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;

            return ValidarParaBloque(tokenBytes, bloqueActual) ||
                   ValidarParaBloque(tokenBytes, bloqueActual - 1) ||
                   ValidarParaBloque(tokenBytes, bloqueActual + 1);
        }

        public string GenerarToken(long? bloque = null)
        {
            if (_secretKeyBytes.Length == 0)
            {
                return string.Empty;
            }

            long bloqueObjetivo = bloque ?? (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60);
            byte[] hash = CalcularHmacBloque(bloqueObjetivo);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private bool ValidarParaBloque(byte[] tokenBytes, long bloque)
        {
            byte[] esperado = CalcularHmacBloque(bloque);
            return tokenBytes.Length == esperado.Length &&
                   CryptographicOperations.FixedTimeEquals(tokenBytes, esperado);
        }

        private byte[] CalcularHmacBloque(long bloque)
        {
            byte[] payload = Encoding.UTF8.GetBytes(bloque.ToString(CultureInfo.InvariantCulture));
            using var hmac = new HMACSHA256(_secretKeyBytes);
            return hmac.ComputeHash(payload);
        }

        private static bool IntentarDecodificarToken(string token, out byte[] bytes)
        {
            if (token.Length == 64)
            {
                try
                {
                    bytes = Convert.FromHexString(token);
                    return bytes.Length == 32;
                }
                catch
                {
                }
            }

            try
            {
                string base64 = token.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2:
                        base64 += "==";
                        break;
                    case 3:
                        base64 += "=";
                        break;
                }

                bytes = Convert.FromBase64String(base64);
                return bytes.Length == 32;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }
    }
}

