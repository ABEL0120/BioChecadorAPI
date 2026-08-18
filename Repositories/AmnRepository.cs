using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BioChecadorAPI.Repositories
{
    public interface IAmnRepository
    {
        Task<EstadoEmpleadoResponseDto?> ConsultarEstadoPorRfcAsync(string rfc);
        Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, string publicKey, string dispositivo);
    }

    public class AmnRepository : IAmnRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public AmnRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<EstadoEmpleadoResponseDto?> ConsultarEstadoPorRfcAsync(string rfc)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT ISNULL(a.M105, '') AS RFC, ISNULL(a.M104, '') AS Nombre, ISNULL(a.Compañia, 0) AS NumeroCompania, ISNULL(c.Razon_Social, '') AS RazonSocial, ISNULL(c.Latitud, 0) AS LatitudEmpresa,
            ISNULL(c.Longitud, 0) AS LongitudEmpresa, ISNULL(c.Radio_Tolerancia_Metros, 150) AS RadioToleranciaMetros,
            CASE WHEN EXISTS (SELECT 1 FROM AMN_Biometria b WHERE b.RFC = a.M105 AND b.Baja = '') THEN 1
            ELSE 0 END AS TieneBiometria
            FROM AMN a LEFT JOIN Compañias c ON a.Compañia = c.Numero_Compañia
            WHERE a.M105 = @rfc";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }



            return new EstadoEmpleadoResponseDto
            {
                Existe = true,
                TieneBiometria = reader.GetInt32(7) == 1,
                Rfc = reader.GetString(0).Trim(),
                Nombre = reader.GetString(1).Trim(),
                NumeroCompania = reader.GetInt32(2),
                RazonSocial = reader.GetString(3).Trim(),
                LatitudEmpresa = reader.GetDecimal(4),
                LongitudEmpresa = reader.GetDecimal(5),
                RadioToleranciaMetros = reader.GetInt32(6)
            };
        }

        public async Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, string publicKey, string dispositivo)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"INSERT INTO AMN_Biometria (RFC, Credential_Id, Public_Key, Dispositivo, Fecha_Registro, Baja)
            VALUES (@rfc, @credentialId, @publicKey, @dispositivo, GETDATE(), '')";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@credentialId", SqlDbType.VarChar, 500).Value = credentialId;
            cmd.Parameters.Add("@publicKey", SqlDbType.VarChar, -1).Value = publicKey;
            cmd.Parameters.Add("@dispositivo", SqlDbType.VarChar, 255).Value = string.IsNullOrWhiteSpace(dispositivo) ? (object)DBNull.Value : dispositivo.Trim();
            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
