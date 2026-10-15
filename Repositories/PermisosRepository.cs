using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using BioChecadorAPI.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using static BioChecadorAPI.DTOs.PermisosDto;

namespace BioChecadorAPI.Repositories
{
    public interface IPermisosRepository
    {
        Task<IEnumerable<PermisoEmpleadoDto>> ObtenerHistorialAsync(int compania, int m103);
        Task<DatosInicialesPermisosDto?> ObtenerDatosInicialesAsync(int compania, int m103);
        Task<DataRow?> ObtenerTurnoFilaAsync(int compania, int m103);
        Task<int> ObtenerSaldoVacacionesAsync(int compania, int m103);
        Task<bool> ExisteTraslapePermisoAsync(int compania, int m103, string fi, string hi, string ff, string hf, int excluirNumero);
        Task<bool> ExisteTraslapeVacacionesAsync(int compania, int m103, string fi, string ff);
        Task<int> CrearPermisoAsync(CrearPermisoDto dto, int minutosNetos, int solicitudVacaciones = 0);
        Task<int> ObtenerSolicitudVacacionesIdAsync(int compania, int m103, int numero);
        Task<bool> ActualizarPermisoAsync(ActualizarPermisoDto dto, int minutosNetos);
        Task<bool> CancelarPermisoAsync(int compania, int m103, int numero);
        Task<string?> ObtenerEstatusPermisoAsync(int compania, int m103, int numero);
    }

    public class PermisosRepository : IPermisosRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public PermisosRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<PermisoEmpleadoDto>> ObtenerHistorialAsync(int compania, int m103)
        {
            var historial = new List<PermisoEmpleadoDto>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT p.Compañia, p.Numero, p.M103, p.Secuencia_M103, ISNULL(a.M104, '') AS NombreEmpleado,
                p.Fecha_Inicio_Permiso, p.Fecha_Finaliza_Permiso, p.Hora_Inicia_Permiso, p.Hora_Finaliza_Permiso,
                p.Minutos_Total_Permiso, p.Motivo_Permiso, p.Fecha_Registro, p.Hora_Registro, p.VoBo,
                p.Personal_VoBo, ISNULL(p.Personal_Nombre_VoBo, '') AS Personal_Nombre_VoBo,
                p.Aplica_Vacaciones, p.Solicitud_Vacaciones
                FROM Permisos_Empleado p
                INNER JOIN AMN a ON a.M103 = p.M103 AND a.Compañia = p.Compañia
                WHERE p.Compañia = @compania AND p.M103 = @m103
                ORDER BY p.Fecha_Inicio_Permiso DESC, p.Numero DESC";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int minutos = reader.IsDBNull(9) ? 0 : reader.GetInt32(9);
                historial.Add(new PermisoEmpleadoDto
                {
                    Compania = reader.GetInt32(0),
                    Numero = reader.GetInt32(1),
                    M103 = reader.GetInt32(2),
                    SecuenciaM103 = reader.GetInt32(3),
                    NombreEmpleado = reader.GetString(4).Trim(),
                    FechaInicioPermiso = reader.GetString(5).Trim(),
                    FechaFinalizaPermiso = reader.GetString(6).Trim(),
                    HoraIniciaPermiso = reader.GetString(7).Trim(),
                    HoraFinalizaPermiso = reader.GetString(8).Trim(),
                    MinutosTotalPermiso = minutos,
                    TiempoTexto = PermisosHelper.FormatearTiempo(minutos),
                    MotivoPermiso = reader.GetString(10).Trim(),
                    FechaRegistro = reader.GetString(11).Trim(),
                    HoraRegistro = reader.GetString(12).Trim(),
                    VoBo = reader.GetString(13).Trim(),
                    PersonalVoBo = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                    PersonalNombreVoBo = reader.GetString(15).Trim(),
                    AplicaVacaciones = reader.GetString(16).Trim(),
                    SolicitudVacaciones = reader.IsDBNull(17) ? 0 : reader.GetInt32(17)
                });
            }

            return historial;
        }

        public async Task<DatosInicialesPermisosDto?> ObtenerDatosInicialesAsync(int compania, int m103)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT a.M103, ISNULL(a.M104, ''), ISNULL(a.Correo, ''), ISNULL(a.Jefe_Inmediato, 0),
                ISNULL(j.Nombre, ''), ISNULL(j.Correo, ''),
                ISNULL(t.D147, ''),
                ISNULL(a.M106, '')
                FROM AMN a
                LEFT JOIN Jefes_Inmediatos j ON a.Compañia = j.Compañia AND a.Jefe_Inmediato = j.Numero
                LEFT JOIN Turnos t ON a.Compañia = t.Compañia AND a.M147 = t.M147
                WHERE a.Compañia = @compania AND a.M103 = @m103";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;

            await conn.OpenAsync();
            var dto = new DatosInicialesPermisosDto();
            string fechaIngresoStr = string.Empty;
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) return null;
                dto.NumeroEmpleado = reader.GetInt32(0);
                dto.NombreEmpleado = reader.GetString(1).Trim();
                dto.CorreoEmpleado = reader.GetString(2).Trim();
                dto.JefeInmediato = reader.GetInt32(3);
                dto.JefeInmediatoNombre = reader.GetString(4).Trim();
                dto.JefeInmediatoCorreo = reader.GetString(5).Trim();
                dto.TurnoDescripcion = reader.GetString(6).Trim();
                fechaIngresoStr = reader.GetString(7).Trim();
            }

            int antiguedad = 0;
            if (DateTime.TryParse(fechaIngresoStr, out DateTime fechaIngreso))
            {
                DateTime hoy = DateTime.Now;
                antiguedad = hoy.Year - fechaIngreso.Year;
                if (fechaIngreso.Date > hoy.AddYears(-antiguedad))
                {
                    antiguedad--;
                }
            }

            const string queryPeriodos = @"SELECT TOP 1 Periodo, ISNULL(Dias_Disponibles, 0)
                FROM Dias_Vacaciones
                WHERE Compañia = @compania AND M103 = @m103 AND Antiguedad <= @antiguedad
                ORDER BY Periodo DESC";

            using var cmdPeriodos = new SqlCommand(queryPeriodos, conn);
            cmdPeriodos.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmdPeriodos.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmdPeriodos.Parameters.Add("@antiguedad", SqlDbType.Int).Value = antiguedad;

            using (var readerPeriodo = await cmdPeriodos.ExecuteReaderAsync())
            {
                if (await readerPeriodo.ReadAsync())
                {
                    dto.PeriodoVacacionesActual = readerPeriodo.GetString(0).Trim();
                    dto.DiasVacacionesDisponibles = readerPeriodo.GetInt32(1);
                }
            }

            return dto;
        }
        public async Task<DataRow?> ObtenerTurnoFilaAsync(int compania, int m103)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT t.* FROM Turnos t
                INNER JOIN AMN a ON a.M147 = t.M147 AND a.Compañia = t.Compañia
                WHERE a.M103 = @m103 AND a.Compañia = @compania";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);
            if (dt.Rows.Count > 0) return dt.Rows[0];
            return null;
        }

        public async Task<int> ObtenerSaldoVacacionesAsync(int compania, int m103)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT ISNULL(SUM(Dias_Disponibles), 0) FROM Dias_Vacaciones
                WHERE Compañia = @compania AND M103 = @m103";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;

            await conn.OpenAsync();
            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }

        public async Task<bool> ExisteTraslapePermisoAsync(int compania, int m103, string fi, string hi, string ff, string hf, int excluirNumero)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT COUNT(1) FROM Permisos_Empleado
                WHERE Compañia = @compania AND M103 = @m103
                AND VoBo <> 'NEGADO' AND Numero <> @excluirNumero
                AND Fecha_Inicio_Permiso + ' ' + Hora_Inicia_Permiso < @ff_hf
                AND Fecha_Finaliza_Permiso + ' ' + Hora_Finaliza_Permiso > @fi_hi";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmd.Parameters.Add("@excluirNumero", SqlDbType.Int).Value = excluirNumero;
            cmd.Parameters.Add("@ff_hf", SqlDbType.VarChar, 20).Value = $"{ff.Trim()} {hf.Trim()}";
            cmd.Parameters.Add("@fi_hi", SqlDbType.VarChar, 20).Value = $"{fi.Trim()} {hi.Trim()}";

            await conn.OpenAsync();
            var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<bool> ExisteTraslapeVacacionesAsync(int compania, int m103, string fi, string ff)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT COUNT(1) FROM Solicitud_Vacaciones
                WHERE Compañia = @compania AND M103 = @m103
                AND Cancelada <> 'S' AND Estatus <> 'N'
                AND Fecha_Inicio <= @ff AND Fecha_Fin >= @fi";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmd.Parameters.Add("@ff", SqlDbType.VarChar, 10).Value = ff.Trim();
            cmd.Parameters.Add("@fi", SqlDbType.VarChar, 10).Value = fi.Trim();

            await conn.OpenAsync();
            var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }

        public async Task<int> CrearPermisoAsync(CrearPermisoDto dto, int minutosNetos, int solicitudVacaciones = 0)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string queryMaxNumero = @"SELECT ISNULL(MAX(Numero), 0) + 1 FROM Permisos_Empleado WHERE Compañia = @compania";
            using var cmdMaxNumero = new SqlCommand(queryMaxNumero, conn);
            cmdMaxNumero.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            int nuevoNumero = Convert.ToInt32(await cmdMaxNumero.ExecuteScalarAsync() ?? 1);

            const string queryMaxSec = @"SELECT ISNULL(MAX(Secuencia_M103), 0) + 1 FROM Permisos_Empleado WHERE Compañia = @compania AND M103 = @m103";
            using var cmdMaxSec = new SqlCommand(queryMaxSec, conn);
            cmdMaxSec.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmdMaxSec.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            int nuevaSecuencia = Convert.ToInt32(await cmdMaxSec.ExecuteScalarAsync() ?? 1);

            DateTime ahora = DateTime.Now;
            string fechaReg = ahora.ToString("yyyy/MM/dd");
            string horaReg = ahora.ToString("HH:mm:ss");

            const string queryInsert = @"INSERT INTO Permisos_Empleado (
                Compañia, Numero, M103, Secuencia_M103, Fecha_Inicio_Permiso, Fecha_Finaliza_Permiso,
                Hora_Inicia_Permiso, Hora_Finaliza_Permiso, Minutos_Total_Permiso, Motivo_Permiso,
                Fecha_Registro, Hora_Registro, VoBo, Personal_VoBo, Personal_Nombre_VoBo, Aplica_Vacaciones, Solicitud_Vacaciones
            ) VALUES (
                @compania, @numero, @m103, @secuencia, @fechaInicio, @fechaFin,
                @horaInicio, @horaFin, @minutos, @motivo,
                @fechaReg, @horaReg, 'PENDIENTE', 0, '', @aplicaVacaciones, @solicitudVacaciones
            )";

            using var cmdInsert = new SqlCommand(queryInsert, conn);
            cmdInsert.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmdInsert.Parameters.Add("@numero", SqlDbType.Int).Value = nuevoNumero;
            cmdInsert.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            cmdInsert.Parameters.Add("@secuencia", SqlDbType.Int).Value = nuevaSecuencia;
            cmdInsert.Parameters.Add("@fechaInicio", SqlDbType.VarChar, 10).Value = dto.FechaInicio.Trim();
            cmdInsert.Parameters.Add("@fechaFin", SqlDbType.VarChar, 10).Value = dto.FechaFin.Trim();
            cmdInsert.Parameters.Add("@horaInicio", SqlDbType.VarChar, 10).Value = dto.HoraInicio.Trim();
            cmdInsert.Parameters.Add("@horaFin", SqlDbType.VarChar, 10).Value = dto.HoraFin.Trim();
            cmdInsert.Parameters.Add("@minutos", SqlDbType.Int).Value = minutosNetos;
            cmdInsert.Parameters.Add("@motivo", SqlDbType.VarChar, 500).Value = dto.Motivo.Trim().ToUpperInvariant();
            cmdInsert.Parameters.Add("@fechaReg", SqlDbType.VarChar, 10).Value = fechaReg;
            cmdInsert.Parameters.Add("@horaReg", SqlDbType.VarChar, 10).Value = horaReg;
            cmdInsert.Parameters.Add("@aplicaVacaciones", SqlDbType.VarChar, 1).Value = dto.AplicaVacaciones.Trim().ToUpperInvariant() == "S" ? "S" : "N";
            cmdInsert.Parameters.Add("@solicitudVacaciones", SqlDbType.Int).Value = solicitudVacaciones;

            await cmdInsert.ExecuteNonQueryAsync();
            return nuevoNumero;
        }

        public async Task<bool> ActualizarPermisoAsync(ActualizarPermisoDto dto, int minutosNetos)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string query = @"UPDATE Permisos_Empleado
                SET Fecha_Inicio_Permiso = @fechaInicio,
                    Fecha_Finaliza_Permiso = @fechaFin,
                    Hora_Inicia_Permiso = @horaInicio,
                    Hora_Finaliza_Permiso = @horaFin,
                    Minutos_Total_Permiso = @minutos,
                    Motivo_Permiso = @motivo,
                    Aplica_Vacaciones = @aplicaVacaciones
                WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero AND VoBo = 'PENDIENTE'";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = dto.Numero;
            cmd.Parameters.Add("@fechaInicio", SqlDbType.VarChar, 10).Value = dto.FechaInicio.Trim();
            cmd.Parameters.Add("@fechaFin", SqlDbType.VarChar, 10).Value = dto.FechaFin.Trim();
            cmd.Parameters.Add("@horaInicio", SqlDbType.VarChar, 10).Value = dto.HoraInicio.Trim();
            cmd.Parameters.Add("@horaFin", SqlDbType.VarChar, 10).Value = dto.HoraFin.Trim();
            cmd.Parameters.Add("@minutos", SqlDbType.Int).Value = minutosNetos;
            cmd.Parameters.Add("@motivo", SqlDbType.VarChar, 500).Value = dto.Motivo.Trim().ToUpperInvariant();
            cmd.Parameters.Add("@aplicaVacaciones", SqlDbType.VarChar, 1).Value = dto.AplicaVacaciones.Trim().ToUpperInvariant() == "S" ? "S" : "N";

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> CancelarPermisoAsync(int compania, int m103, int numero)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string query = @"DELETE FROM Permisos_Empleado
                WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero AND VoBo = 'PENDIENTE'";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = numero;

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<string?> ObtenerEstatusPermisoAsync(int compania, int m103, int numero)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT VoBo FROM Permisos_Empleado
                WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = numero;

            await conn.OpenAsync();
            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? res.ToString()?.Trim() : null;
        }

        public async Task<int> ObtenerSolicitudVacacionesIdAsync(int compania, int m103, int numero)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT ISNULL(Solicitud_Vacaciones, 0) FROM Permisos_Empleado
                WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = numero;

            await conn.OpenAsync();
            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
        }
    }
}
