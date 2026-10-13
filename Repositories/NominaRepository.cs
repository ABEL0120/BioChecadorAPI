using System.Data;
using BioChecadorAPI.Data;
using BioChecadorAPI.Helpers;
using Microsoft.Data.SqlClient;
using static BioChecadorAPI.DTOs.NominaDto;
using static BioChecadorAPI.Helpers.AuthHelper;

namespace BioChecadorAPI.Repositories
{
    public interface INominaRepository
    {
        Task<List<CalendarioPeriodoDto>> ObtenerPeriodosTimbradosAsync(string? fechaInicial, string? fechaFinal);
        Task<(string apellidosNombre, string RFC, string nombreApellidos)> ObtenerDatosEmpleadoAsync(int compania, int m103);
        Task<string> ObtenerRfcCompaniaAsync(int compania);
    }

    public class NominaRepository : INominaRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public NominaRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<CalendarioPeriodoDto>> ObtenerPeriodosTimbradosAsync(string? fechaInicial, string? fechaFinal)
        {
            var lista = new List<CalendarioPeriodoDto>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            string query = @"SELECT Año, Proceso, ISNULL(F_Inicio, ''), ISNULL(F_Termino, ''), ISNULL(F_Pago, '') FROM Calendario WHERE (Timbrado = 'S' OR Acumulado = 'A') ";

            if (!string.IsNullOrWhiteSpace(fechaInicial) && !string.IsNullOrWhiteSpace(fechaFinal))
            {
                query += " AND F_Pago >= @fechaInicial AND F_Pago <= @fechaFinal";
            }
            else if (!string.IsNullOrWhiteSpace(fechaInicial))
            {
                query += " AND F_Pago >= @fechaInicial";
            }
            else if (!string.IsNullOrWhiteSpace(fechaFinal))
            {
                query += " AND F_Pago <= @fechaFinal";
            }

            query += " ORDER BY Año DESC, Proceso DESC, F_Pago DESC";

            using var cmd = new SqlCommand(query, conn);
            if (!string.IsNullOrWhiteSpace(fechaInicial)) cmd.Parameters.Add("@fechaInicial", SqlDbType.VarChar, 10).Value = fechaInicial;
            if (!string.IsNullOrWhiteSpace(fechaFinal)) cmd.Parameters.Add("@fechaFinal", SqlDbType.VarChar, 10).Value = fechaFinal;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new CalendarioPeriodoDto
                {
                    Anio = reader.GetInt32(0),
                    Proceso = reader.GetInt32(1),
                    FechaInicio = reader.GetString(2).Trim(),
                    FechaTermino = reader.GetString(3).Trim(),
                    FechaPago = reader.GetString(4).Trim()
                });
            }

            return lista;
        }

        public async Task<(string apellidosNombre, string RFC, string nombreApellidos)> ObtenerDatosEmpleadoAsync(int compania, int m103)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string query = "SELECT ISNULL(M104, '') AS Apellidos_Nombre, ISNULL(M105, ''), CONCAT(M146, ' ', M144, ' ', M145) AS Nombre_Apellidos FROM AMN WHERE Compañia = @compania AND M103 = @m103";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetString(0).Trim(), reader.GetString(1).Trim(), reader.GetString(2).Trim());
            }

            return (string.Empty, string.Empty, string.Empty);
        }

        public async Task<string> ObtenerRfcCompaniaAsync(int compania)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string query = "SELECT TOP 1 ISNULL(RFC, '') FROM Compañias WHERE Numero_Compañia = @compania";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                string rfcEncriptado = result.ToString()!.Trim();
                return DesEncripta(rfcEncriptado);
            }

            return string.Empty;
        }
    }
}
