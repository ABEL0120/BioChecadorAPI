using System.Buffers.Binary;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;

namespace BioChecadorAPI.Services
{
    public interface IChecadorFisicoPasskeyService
    {
        Task<(bool Valido, string Mensaje, PasskeyCredencialDto? Credencial, uint SignCount)> ValidarAssertionAsync(WebAuthnAssertionDto passkey);
    }

    public class ChecadorFisicoPasskeyService : IChecadorFisicoPasskeyService
    {
        private readonly IAmnRepository _amnRepository;
        private readonly string _serverDomain;
        private readonly HashSet<string> _allowedOrigins;

        public ChecadorFisicoPasskeyService(IAmnRepository amnRepository, IConfiguration configuration)
        {
            _amnRepository = amnRepository;
            _serverDomain = (configuration["Fido2:ServerDomain"] ?? string.Empty).Trim();
            var origins = configuration.GetSection("Fido2:Origins").Get<string[]>() ?? Array.Empty<string>();
            _allowedOrigins = new HashSet<string>(
                origins.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim().TrimEnd('/')),
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<(bool Valido, string Mensaje, PasskeyCredencialDto? Credencial, uint SignCount)> ValidarAssertionAsync(WebAuthnAssertionDto passkey)
        {
            var candidatos = GenerarCandidatosCredentialId(passkey.Id, passkey.RawId);
            if (candidatos.Count == 0)
            {
                return (false, "Identificador de credencial WebAuthn inválido.", null, 0);
            }

            var credencial = await _amnRepository.ObtenerCredencialPasskeyPorIdAsync(candidatos);
            if (credencial == null)
            {
                return (false, "Passkey no registrada o empleado inactivo.", null, 0);
            }

            if (!IntentarDecodificarBase64Flexible(passkey.Response.AuthenticatorData, out var authData) ||
                !IntentarDecodificarBase64Flexible(passkey.Response.ClientDataJson, out var clientDataBytes) ||
                !IntentarDecodificarBase64Flexible(passkey.Response.Signature, out var signatureBytes))
            {
                return (false, "Formato inválido en los datos de aserción WebAuthn.", null, 0);
            }

            if (!ValidarClientData(clientDataBytes, out var mensajeClientData))
            {
                return (false, mensajeClientData, null, 0);
            }

            if (authData.Length < 37)
            {
                return (false, "authenticatorData WebAuthn inválido.", null, 0);
            }

            if (!string.IsNullOrWhiteSpace(_serverDomain))
            {
                byte[] rpIdHashEsperado = SHA256.HashData(Encoding.UTF8.GetBytes(_serverDomain));
                if (!CryptographicOperations.FixedTimeEquals(authData.AsSpan(0, 32), rpIdHashEsperado))
                {
                    return (false, "El dominio RP de la Passkey no coincide con la configuración del servidor.", null, 0);
                }
            }

            byte flags = authData[32];
            if ((flags & 0x01) == 0)
            {
                return (false, "Presencia de usuario no verificada en la Passkey.", null, 0);
            }

            uint signCount = BinaryPrimitives.ReadUInt32BigEndian(authData.AsSpan(33, 4));
            if (credencial.SignCount > 0 && signCount > 0 && signCount < credencial.SignCount)
            {
                return (false, "Contador de firma WebAuthn inválido.", null, 0);
            }

            byte[] clientDataHash = SHA256.HashData(clientDataBytes);
            byte[] signedData = new byte[authData.Length + clientDataHash.Length];
            Buffer.BlockCopy(authData, 0, signedData, 0, authData.Length);
            Buffer.BlockCopy(clientDataHash, 0, signedData, authData.Length, clientDataHash.Length);

            if (!VerificarFirmaPublicKey(credencial.PublicKey, signedData, signatureBytes))
            {
                return (false, "Firma criptográfica de la Passkey inválida.", null, 0);
            }

            return (true, string.Empty, credencial, signCount);
        }

        private bool ValidarClientData(byte[] clientDataBytes, out string mensaje)
        {
            try
            {
                using var doc = JsonDocument.Parse(clientDataBytes);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp) ||
                    !string.Equals(typeProp.GetString(), "webauthn.get", StringComparison.Ordinal))
                {
                    mensaje = "Tipo de operación WebAuthn inválido.";
                    return false;
                }

                if (!root.TryGetProperty("challenge", out var challengeProp) ||
                    string.IsNullOrWhiteSpace(challengeProp.GetString()))
                {
                    mensaje = "El desafío WebAuthn está vacío.";
                    return false;
                }

                if (_allowedOrigins.Count > 0)
                {
                    if (!root.TryGetProperty("origin", out var originProp))
                    {
                        mensaje = "Origen WebAuthn no especificado.";
                        return false;
                    }

                    string origin = (originProp.GetString() ?? string.Empty).Trim().TrimEnd('/');
                    if (!_allowedOrigins.Contains(origin))
                    {
                        mensaje = "Origen WebAuthn no permitido.";
                        return false;
                    }
                }

                mensaje = string.Empty;
                return true;
            }
            catch
            {
                mensaje = "clientDataJSON WebAuthn inválido.";
                return false;
            }
        }

        private static bool VerificarFirmaPublicKey(byte[] publicKey, byte[] signedData, byte[] signature)
        {
            if (publicKey == null || publicKey.Length == 0 || signature.Length == 0)
            {
                return false;
            }

            if (VerificarConSpki(publicKey, signedData, signature))
            {
                return true;
            }

            return VerificarConCose(publicKey, signedData, signature);
        }

        private static bool VerificarConSpki(byte[] publicKey, byte[] signedData, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                var hash = ecdsa.KeySize switch
                {
                    384 => HashAlgorithmName.SHA384,
                    521 => HashAlgorithmName.SHA512,
                    _ => HashAlgorithmName.SHA256
                };
                return ecdsa.VerifyData(signedData, signature, hash, DSASignatureFormat.Rfc3279DerSequence);
            }
            catch
            {
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(publicKey, out _);
                return rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1) ||
                       rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            catch
            {
            }

            return false;
        }

        private static bool VerificarConCose(byte[] publicKey, byte[] signedData, byte[] signature)
        {
            try
            {
                var reader = new CborReader(publicKey, CborConformanceMode.Lax);
                int? mapLen = reader.ReadStartMap();

                int kty = 0;
                int alg = 0;
                int crv = 0;
                byte[]? x = null;
                byte[]? y = null;
                byte[]? n = null;
                byte[]? e = null;

                int count = 0;
                while (reader.PeekState() != CborReaderState.EndMap && (!mapLen.HasValue || count < mapLen.Value))
                {
                    int key = reader.ReadInt32();
                    switch (key)
                    {
                        case 1:
                            kty = reader.ReadInt32();
                            break;
                        case 3:
                            alg = reader.ReadInt32();
                            break;
                        case -1:
                            if (reader.PeekState() == CborReaderState.ByteString)
                            {
                                n = reader.ReadByteString();
                            }
                            else
                            {
                                crv = reader.ReadInt32();
                            }
                            break;
                        case -2:
                            if (reader.PeekState() == CborReaderState.ByteString)
                            {
                                var bytes = reader.ReadByteString();
                                x = bytes;
                                e = bytes;
                            }
                            else
                            {
                                reader.SkipValue();
                            }
                            break;
                        case -3:
                            if (reader.PeekState() == CborReaderState.ByteString)
                            {
                                y = reader.ReadByteString();
                            }
                            else
                            {
                                reader.SkipValue();
                            }
                            break;
                        default:
                            reader.SkipValue();
                            break;
                    }

                    count++;
                }

                if (kty == 2 && x != null && y != null)
                {
                    var curve = crv switch
                    {
                        2 => ECCurve.NamedCurves.nistP384,
                        3 => ECCurve.NamedCurves.nistP521,
                        _ => ECCurve.NamedCurves.nistP256
                    };

                    var hash = alg switch
                    {
                        -35 => HashAlgorithmName.SHA384,
                        -36 => HashAlgorithmName.SHA512,
                        _ => HashAlgorithmName.SHA256
                    };

                    var ecParams = new ECParameters
                    {
                        Curve = curve,
                        Q = new ECPoint { X = x, Y = y }
                    };

                    using var ecdsa = ECDsa.Create(ecParams);
                    return ecdsa.VerifyData(signedData, signature, hash, DSASignatureFormat.Rfc3279DerSequence);
                }

                if (kty == 3 && n != null && e != null)
                {
                    var hash = alg switch
                    {
                        -258 or -38 => HashAlgorithmName.SHA384,
                        -259 or -39 => HashAlgorithmName.SHA512,
                        _ => HashAlgorithmName.SHA256
                    };

                    var padding = alg is -37 or -38 or -39
                        ? RSASignaturePadding.Pss
                        : RSASignaturePadding.Pkcs1;

                    var rsaParams = new RSAParameters
                    {
                        Modulus = n,
                        Exponent = e
                    };

                    using var rsa = RSA.Create(rsaParams);
                    return rsa.VerifyData(signedData, signature, hash, padding);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GenerarCandidatosCredentialId(string? id, string? rawId)
        {
            var candidatos = new List<string>();

            void AgregarVariantes(string? valor)
            {
                if (string.IsNullOrWhiteSpace(valor))
                {
                    return;
                }

                string limpio = valor.Trim();
                candidatos.Add(limpio);

                if (IntentarDecodificarBase64Flexible(limpio, out var bytes))
                {
                    string base64 = Convert.ToBase64String(bytes);
                    string base64Url = base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    candidatos.Add(base64Url);
                    candidatos.Add(base64);
                }
            }

            AgregarVariantes(id);
            AgregarVariantes(rawId);

            return candidatos.Distinct(StringComparer.Ordinal).ToList();
        }

        private static bool IntentarDecodificarBase64Flexible(string? valor, out byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            try
            {
                string normalizado = valor.Trim().Replace('-', '+').Replace('_', '/');
                switch (normalizado.Length % 4)
                {
                    case 2:
                        normalizado += "==";
                        break;
                    case 3:
                        normalizado += "=";
                        break;
                }

                bytes = Convert.FromBase64String(normalizado);
                return true;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }
    }
}

