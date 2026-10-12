using Microsoft.Extensions.Configuration;
using System;

namespace BioChecadorAPI.Data
{
    public interface IEmailData
    {
        string Email { get; }
        string PasswordApp { get; }
    }

    public class EmailData : IEmailData
    {
        public string Email { get; }
        public string PasswordApp { get; }

        public EmailData(IConfiguration configuration)
        {
            Email = configuration["Mail:Email"]
                ?? throw new InvalidOperationException("El correo 'Mail:Email' no está configurado en el appsettings.json.");

            PasswordApp = configuration["Mail:PasswordApp"]
                ?? throw new InvalidOperationException("La contraseña 'Mail:PasswordApp' no está configurada en el appsettings.json.");
        }
    }
}