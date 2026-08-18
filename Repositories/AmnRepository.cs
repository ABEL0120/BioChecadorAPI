using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BioChecadorAPI.Repositories
{
    public interface IAmnRepository
    {
        Task<EstadoEmpleadoResponseDto?> ConsultarEstadoPorRfcAsync(string rfc);
        Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, byte[] publicKey, string dispositivoNombre, string userAgent);
        Task<bool> InsertarChecadaAsync(string rfc, int numeroCompania, decimal latitud, decimal longitud, string userAgent, string dispositivoNombre, string tipoMovimiento);
        Task<bool> ValidarCredencialBiometricaAsync(string rfc, string credentialId);
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

        public async Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, byte[] publicKey, string dispositivoNombre, string userAgent)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"INSERT INTO AMN_Biometria  (RFC, Credential_Id, Public_Key, Sign_Count, Dispositivo_Nombre, Dispositivo_User_Agent, Fecha_Alta, Baja)
            VALUES (@rfc, @credentialId, @publicKey, 0, @dispositivoNombre, @userAgent, CONVERT(VARCHAR(30), GETDATE(), 120), '')";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@credentialId", SqlDbType.VarChar, 255).Value = credentialId;
            cmd.Parameters.Add("@publicKey", SqlDbType.VarBinary, -1).Value = publicKey;
            cmd.Parameters.Add("@dispositivoNombre", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            cmd.Parameters.Add("@userAgent", SqlDbType.VarChar, 500).Value = userAgent ?? string.Empty;

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> ValidarCredencialBiometricaAsync(string rfc, string credentialId)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT COUNT(1) FROM AMN_Biometria 
            WHERE RFC = @rfc AND Credential_Id = @credentialId AND Baja = ''";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@credentialId", SqlDbType.VarChar, 500).Value = credentialId;
            await conn.OpenAsync();
            var count = (int?)await cmd.ExecuteScalarAsync() ?? 0;
            return count > 0;
        }

        public async Task<bool> InsertarChecadaAsync(string rfc, int numeroCompania, decimal latitud, decimal longitud, string userAgent, string dispositivoNombre, string tipoMovimiento)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"INSERT INTO AMN_Registros_Checador  (RFC, Numero_Compañia, Fecha_Hora, Latitud, Longitud, Dispositivo_User_Agent, Dispositivo_Nombre, Tipo_Movimiento, Firma_Valida) 
            VALUES (@rfc, @compania, CONVERT(VARCHAR(30), GETDATE(), 120), @latitud, @longitud, @userAgent, @dispositivoNombre, @tipo, 'S')";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = numeroCompania;
            cmd.Parameters.Add("@latitud", SqlDbType.Decimal).Value = latitud;
            cmd.Parameters.Add("@longitud", SqlDbType.Decimal).Value = longitud;
            cmd.Parameters.Add("@userAgent", SqlDbType.VarChar, 500).Value = userAgent ?? string.Empty;
            cmd.Parameters.Add("@dispositivoNombre", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 20).Value = tipoMovimiento.ToUpperInvariant();
            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}
