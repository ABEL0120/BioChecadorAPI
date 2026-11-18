using BioChecadorAPI.Data;
using BioChecadorAPI.Helpers;
using BioChecadorAPI.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BioChecadorAPI.Repositories
{
    public interface ISensorFisicoRepository
    {
        Task<List<AMN_Biometria_Fisica>> ObtenerHuellasActivasAsync();
        Task<string> ObtenerUltimoMovimientoHoyAsync(string rfc, string fechaHoy);
        Task<(bool Insertado, int Numero, int NumeroCompania, string FechaHora)> InsertarChecadaSensorAsync(
            string rfc,
            string tipoMovimiento,
            string dispositivoNombre,
            string userAgent,
            decimal latitud,
            decimal longitud);
        Task<List<UltimaChecadaSensorRow>> ObtenerUltimasChecadasAsync(int limite, string? dispositivoNombre);
    }

    public sealed class UltimaChecadaSensorRow
    {
        public int Numero { get; set; }
        public string Rfc { get; set; } = string.Empty;
        public int NumeroCompania { get; set; }
        public string FechaHora { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public string DispositivoNombre { get; set; } = string.Empty;
    }

    public class SensorFisicoRepository : ISensorFisicoRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public SensorFisicoRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<AMN_Biometria_Fisica>> ObtenerHuellasActivasAsync()
        {
            using var conn = _connectionFactory.CreateConnection();
            const string query = @"SELECT
                Numero,
                RTRIM(RFC) AS RFC,
                Template,
                ISNULL(Fecha_Alta, '') AS Fecha_Alta,
                ISNULL(Baja, '') AS Baja
            FROM AMN_Biometria_Fisica
            WHERE Baja = '' AND Template IS NOT NULL";

            var rows = await conn.QueryAsync<AMN_Biometria_Fisica>(query);
            return rows.ToList();
        }

        public async Task<string> ObtenerUltimoMovimientoHoyAsync(string rfc, string fechaHoy)
        {
            using var conn = _connectionFactory.CreateConnection();
            const string query = @"SELECT TOP 1 UPPER(RTRIM(Tipo_Movimiento))
            FROM AMN_Registros_Checador
            WHERE RFC = @Rfc AND LEFT(Fecha_Hora, 10) = @FechaHoy
            ORDER BY Numero DESC";

            var resultado = await conn.QueryFirstOrDefaultAsync<string?>(query, new { Rfc = rfc, FechaHoy = fechaHoy });
            return resultado?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        public async Task<(bool Insertado, int Numero, int NumeroCompania, string FechaHora)> InsertarChecadaSensorAsync(
            string rfc,
            string tipoMovimiento,
            string dispositivoNombre,
            string userAgent,
            decimal latitud,
            decimal longitud)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            const string queryEmpleado = @"SELECT TOP 1
                ISNULL(Compañia, 0) AS NumeroCompania,
                ISNULL(TRY_CAST(LTRIM(RTRIM(M103)) AS INT), 0) AS NumeroEmpleado
            FROM AMN
            WHERE M105 = @Rfc AND M130 = ''";

            var empleado = await conn.QueryFirstOrDefaultAsync<EmpleadoSensorRow>(
                queryEmpleado,
                new { Rfc = rfc },
                transaction: tx
            );

            int numeroCompania = empleado?.NumeroCompania ?? 0;
            int numeroEmpleado = empleado?.NumeroEmpleado ?? 0;

            const string queryNumero = @"SELECT ISNULL(MAX(Numero), 0) + 1 FROM AMN_Registros_Checador WITH (UPDLOCK, HOLDLOCK)";
            int siguienteNumero = await conn.ExecuteScalarAsync<int>(queryNumero, transaction: tx);

            string fechaHora = BDHelper.FormatearFechaHora(DateTime.Now);
            string tipoNormalizado = tipoMovimiento.Trim().ToUpperInvariant();

            string dispositivoSeguro = string.IsNullOrWhiteSpace(dispositivoNombre)
                ? "Windows Device (Sensor Fisico)"
                : dispositivoNombre.Trim();
            if (dispositivoSeguro.Length > 150)
            {
                dispositivoSeguro = dispositivoSeguro[..150];
            }

            string userAgentSeguro = userAgent ?? string.Empty;
            if (userAgentSeguro.Length > 500)
            {
                userAgentSeguro = userAgentSeguro[..500];
            }

            const string queryInsert = @"INSERT INTO AMN_Registros_Checador
                (Numero, RFC, Numero_Compañia, Fecha_Hora, Dispositivo_User_Agent, Dispositivo_Nombre, Tipo_Movimiento, Firma_Valida, Latitud, Longitud, Fecha_Hora_Registro)
            VALUES
                (@Numero, @Rfc, @NumeroCompania, @FechaHora, @UserAgent, @DispositivoNombre, @TipoMovimiento, 'S', @Latitud, @Longitud, @FechaHoraRegistro)";

            int rows = await conn.ExecuteAsync(queryInsert, new
            {
                Numero = siguienteNumero,
                Rfc = rfc,
                NumeroCompania = numeroCompania,
                FechaHora = fechaHora,
                UserAgent = userAgentSeguro,
                DispositivoNombre = dispositivoSeguro,
                TipoMovimiento = tipoNormalizado,
                Latitud = latitud,
                Longitud = longitud,
                FechaHoraRegistro = fechaHora
            }, transaction: tx);

            await tx.CommitAsync();

            if (rows > 0)
            {
                await BDHelper.InsertarKardexAsync(tipoNormalizado, conn, numeroCompania, numeroEmpleado, "X");
            }

            return (rows > 0, siguienteNumero, numeroCompania, fechaHora);
        }

        public async Task<List<UltimaChecadaSensorRow>> ObtenerUltimasChecadasAsync(int limite, string? dispositivoNombre)
        {
            using var conn = _connectionFactory.CreateConnection();
            int limiteSeguro = Math.Clamp(limite, 1, 100);
            bool filtrarDispositivo = !string.IsNullOrWhiteSpace(dispositivoNombre);

            string query = filtrarDispositivo
                ? @"SELECT TOP (@Limite)
                    Numero,
                    RTRIM(RFC) AS Rfc,
                    Numero_Compañia AS NumeroCompania,
                    Fecha_Hora AS FechaHora,
                    UPPER(RTRIM(Tipo_Movimiento)) AS TipoMovimiento,
                    ISNULL(Dispositivo_Nombre, '') AS DispositivoNombre
                FROM AMN_Registros_Checador
                WHERE Dispositivo_Nombre = @DispositivoNombre
                ORDER BY Numero DESC"
                : @"SELECT TOP (@Limite)
                    Numero,
                    RTRIM(RFC) AS Rfc,
                    Numero_Compañia AS NumeroCompania,
                    Fecha_Hora AS FechaHora,
                    UPPER(RTRIM(Tipo_Movimiento)) AS TipoMovimiento,
                    ISNULL(Dispositivo_Nombre, '') AS DispositivoNombre
                FROM AMN_Registros_Checador
                ORDER BY Numero DESC";

            var rows = await conn.QueryAsync<UltimaChecadaSensorRow>(query, new
            {
                Limite = limiteSeguro,
                DispositivoNombre = dispositivoNombre?.Trim()
            });

            return rows.ToList();
        }

        private sealed class EmpleadoSensorRow
        {
            public int NumeroCompania { get; set; }
            public int NumeroEmpleado { get; set; }
        }
    }
}
