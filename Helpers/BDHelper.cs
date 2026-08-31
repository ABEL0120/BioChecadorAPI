using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace BioChecadorAPI.Helpers
{
    public static class BDHelper
    {
        public static async Task<int> ObtenerIdMax(SqlConnection conn, string tabla, string columna = "Numero")
        {
            if (string.IsNullOrWhiteSpace(tabla))
            {
                return 0;
            }
            string query = $"SELECT ISNULL(MAX({columna}), 0) FROM {tabla}";
            using var cmd = new SqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        public static async Task<int> InsertarKardexAsync(string tipoMovimiento, SqlConnection conn, int numeroCompania, int numeroEmpleado, string asistio)
        {
            string tipo = tipoMovimiento?.Trim().ToUpperInvariant() ?? string.Empty;
            if (tipo != "ENTRADA" && tipo != "RETARDO") return 0;            
            if (numeroEmpleado <= 0)    return 0;            
            var ahora = DateTime.Now;
            int anio = ahora.Year;
            int mes = ahora.Month;
            int dia = ahora.Day;
            string query = $@"IF EXISTS (SELECT 1 FROM KARDEX  WHERE Compañia = @compania AND Año = @anio AND Mes = @mes AND M103 = @empleado)
            BEGIN UPDATE KARDEX SET D{dia} = @asistio WHERE Compañia = @compania AND Año = @anio AND Mes = @mes AND M103 = @empleado
            END ELSE BEGIN INSERT INTO KARDEX (Compañia, Año, M103, Mes, D{dia})  VALUES (@compania, @anio, @empleado, @mes, @asistio) END";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = numeroCompania;
            cmd.Parameters.Add("@anio", SqlDbType.Int).Value = anio;
            cmd.Parameters.Add("@empleado", SqlDbType.Int).Value = numeroEmpleado;
            cmd.Parameters.Add("@mes", SqlDbType.Int).Value = mes;
            cmd.Parameters.Add("@asistio", SqlDbType.VarChar, 1).Value = asistio;
            return await cmd.ExecuteNonQueryAsync();
        }

        public static string FormatearFecha(DateTime fecha)
        {
            return fecha.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

    }
}