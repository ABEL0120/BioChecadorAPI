using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using BioChecadorAPI.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using static BioChecadorAPI.Helpers.AuthHelper;
using static BioChecadorAPI.Helpers.BDHelper;
using static BioChecadorAPI.Helpers.ChecadorHelper;

namespace BioChecadorAPI.Repositories
{
    public interface IAmnRepository
    {
        Task<EstadoEmpleadoResponseDto?> ConsultarEstadoPorRfcAsync(string rfc, string dispositivoNombre);
        Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, byte[] publicKey, string dispositivoNombre, string userAgent);
        Task<bool> InsertarChecadaAsync(string rfc, int numeroCompania, decimal latitud, decimal longitud, string userAgent, string dispositivoNombre, string tipoMovimiento);
        Task<bool> ValidarCredencialBiometricaAsync(string rfc, string credentialId);
        Task<string> ObtenerUltimoMovimientoHoyAsync(string rfc);
        Task<HistoricoAMNResponse[]> ObtenerHistoricoAmnAsync(string rfc, int numeroCompania);
    }

    public class AmnRepository : IAmnRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public AmnRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<EstadoEmpleadoResponseDto?> ConsultarEstadoPorRfcAsync(string rfc, string dispositivoNombre)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT ISNULL(a.M105, '') AS RFC, ISNULL(a.M104, '') AS Nombre, ISNULL(a.Compañia, 0) AS NumeroCompania, ISNULL(c.Razon_Social, '') AS RazonSocial, ISNULL(c.Adicional, '') AS Adicional, ISNULL(c.Latitud, 0) AS LatitudEmpresa, ISNULL(c.Longitud, 0) AS LongitudEmpresa, ISNULL(c.Radio_Tolerancia_Metros, 150) AS RadioToleranciaMetros, ISNULL(c.Inicio_Nomina, '') AS InicioNomina,
            CASE WHEN EXISTS (SELECT 1 FROM AMN_Biometria b WHERE b.RFC = a.M105 AND b.Dispositivo_Nombre = @dispositivo AND b.Baja = '') THEN 1 ELSE 0 END AS TieneBiometria,
            t.D147 AS TurnoDescripcion, t.T102 AS TurnoPatron, t.*
            FROM AMN a LEFT JOIN Compañias c ON a.Compañia = c.Numero_Compañia LEFT JOIN Turnos t ON a.Compañia = t.Compañia AND a.M147 = t.M147
            WHERE a.M105 = @rfc";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@dispositivo", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            await conn.OpenAsync();
            string rfcEncontrado;
            string nombre;
            int numeroCompania;
            string razonSocial;
            string adicional;
            decimal latitudEmpresa;
            decimal longitudEmpresa;
            int radioToleranciaMetros;
            string inicioNomina;
            bool tieneBiometria;
            TurnoDetalleDto? horario = null;

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                rfcEncontrado = reader.GetString(0).Trim();
                nombre = reader.GetString(1).Trim();
                numeroCompania = reader.GetInt32(2);
                razonSocial = DesEncripta(reader.GetString(3).Trim());
                adicional = DesEncripta(reader.GetString(4).Trim());
                latitudEmpresa = reader.GetDecimal(5);
                longitudEmpresa = reader.GetDecimal(6);
                radioToleranciaMetros = reader.GetInt32(7);
                inicioNomina = reader.GetString(8).Trim();
                tieneBiometria = reader.GetInt32(9) == 1;

                if (!reader.IsDBNull(10))
                {
                    horario = MapearTurno(reader, inicioNomina);
                }
            }

            const string queryUltimo = @"SELECT TOP 1 Tipo_Movimiento FROM AMN_Registros_Checador WHERE RFC = @rfc AND CONVERT(date, Fecha_Hora) = CONVERT(date, GETDATE()) ORDER BY Fecha_Hora DESC";
            using var cmdUltimo = new SqlCommand(queryUltimo, conn);
            cmdUltimo.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            var resultUltimo = await cmdUltimo.ExecuteScalarAsync();
            string ultimoMov = resultUltimo?.ToString()?.ToUpperInvariant() ?? string.Empty;
            return new EstadoEmpleadoResponseDto
            {
                Existe = true,
                UltimoMovimientoHoy = ultimoMov,
                TieneBiometria = tieneBiometria,
                Rfc = rfcEncontrado,
                Nombre = nombre,
                NumeroCompania = numeroCompania,
                RazonSocial = $"{razonSocial} - {adicional}",
                LatitudEmpresa = latitudEmpresa,
                LongitudEmpresa = longitudEmpresa,
                RadioToleranciaMetros = radioToleranciaMetros,
                Horario = horario
            };
        }

        public async Task<bool> GuardarBiometriaAsync(string rfc, string credentialId, byte[] publicKey, string dispositivoNombre, string userAgent)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            const string queryUpdate = @"UPDATE AMN_Biometria  SET Baja = '*' WHERE RFC = @rfc AND Dispositivo_Nombre = @dispositivo AND Baja = ''";
            using var cmdU = new SqlCommand(queryUpdate, conn);
            cmdU.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmdU.Parameters.Add("@dispositivo", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            await cmdU.ExecuteNonQueryAsync();
            int ultimoId = await ObtenerIdMax(conn, "AMN_Biometria");
            const string queryInsert = @"INSERT INTO AMN_Biometria (Numero, RFC, Credential_Id, Public_Key, Sign_Count, Dispositivo_Nombre, Dispositivo_User_Agent, Fecha_Alta, Baja)
            VALUES (@numero, @rfc, @credentialId, @publicKey, 0, @dispositivoNombre, @userAgent, @fecha, '')";
            using var cmd = new SqlCommand(queryInsert, conn);
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = ultimoId + 1;
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@credentialId", SqlDbType.VarChar, 255).Value = credentialId;
            cmd.Parameters.Add("@publicKey", SqlDbType.VarBinary, -1).Value = publicKey;
            cmd.Parameters.Add("@dispositivoNombre", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            cmd.Parameters.Add("@userAgent", SqlDbType.VarChar, 500).Value = userAgent ?? string.Empty;
            cmd.Parameters.Add("@fecha", SqlDbType.VarChar, 30).Value = FormatearFecha(DateTime.Now);
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
            await conn.OpenAsync();

            int ultimoId = await ObtenerIdMax(conn, "AMN_Registros_Checador");

            const string query = @"INSERT INTO AMN_Registros_Checador  (Numero, RFC, Numero_Compañia, Fecha_Hora, Latitud, Longitud, Dispositivo_User_Agent, Dispositivo_Nombre, Tipo_Movimiento, Firma_Valida) 
            VALUES (@numero, @rfc, @compania, @fecha, @latitud, @longitud, @userAgent, @dispositivoNombre, @tipo, 'S')";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = ultimoId + 1;
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = numeroCompania;
            cmd.Parameters.Add("@fecha", SqlDbType.VarChar, 30).Value = FormatearFecha(DateTime.Now);
            cmd.Parameters.Add("@latitud", SqlDbType.Decimal).Value = latitud;
            cmd.Parameters.Add("@longitud", SqlDbType.Decimal).Value = longitud;
            cmd.Parameters.Add("@userAgent", SqlDbType.VarChar, 500).Value = userAgent ?? string.Empty;
            cmd.Parameters.Add("@dispositivoNombre", SqlDbType.VarChar, 150).Value = dispositivoNombre ?? string.Empty;
            cmd.Parameters.Add("@tipo", SqlDbType.VarChar, 20).Value = tipoMovimiento.ToUpperInvariant();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<string> ObtenerUltimoMovimientoHoyAsync(string rfc)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT TOP 1 Tipo_Movimiento  FROM AMN_Registros_Checador WHERE RFC = @rfc AND CONVERT(date, Fecha_Hora) = CONVERT(date, GETDATE()) ORDER BY Fecha_Hora DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString()?.ToUpperInvariant() ?? string.Empty;
        }

        public async Task<HistoricoAMNResponse[]> ObtenerHistoricoAmnAsync(string rfc, int numeroCompania)
        {
            var resultado = new List<HistoricoAMNResponse>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT Numero, RFC, Numero_Compañia, Fecha_Hora, Latitud, Longitud, Dispositivo_Nombre, Tipo_Movimiento FROM AMN_Registros_Checador 
                                 WHERE RFC = @rfc AND Numero_Compañia = @compañia;";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@rfc", SqlDbType.VarChar, 13).Value = rfc;
            cmd.Parameters.Add("@compañia", SqlDbType.Int).Value = numeroCompania;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.Add(new HistoricoAMNResponse
                {
                    Numero = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    Rfc = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    NumeroCompania = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    FechaHora = reader.IsDBNull(3) ? string.Empty : reader[3].ToString() ?? string.Empty,
                    Latitud = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4),
                    Longitud = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                    DispositivoNombre = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    TipoMovimiento = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
                });
            }

            return resultado.ToArray();
        }

    }
}
