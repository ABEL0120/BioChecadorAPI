using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using static BioChecadorAPI.Helpers.BDHelper;

namespace BioChecadorAPI.Repositories
{
    public interface IVacacionesRepository
    {
        Task<IEnumerable<VacacionesDto.SolicitudVacacionDto>> ObtenerHistorialAsync(int compania, int m103);
        Task<VacacionesDto.DatosInicialesVacacionesDto?> ObtenerDatosInicialesAsync(int compania, int m103);
        Task<List<string>> ObtenerFechasRestringidasAsync(int compania, int anio);
        Task<List<DateTime>> ObtenerDiasInhabilesAsync(int compania);
        Task<(string Kardex, int Turno)> ObtenerKardexYTurnoAsync(int compania, int m103);
        Task<int> CrearSolicitudAsync(VacacionesDto.CrearSolicitudDto dto, int turno, string kardex);
        Task<bool> CancelarSolicitudAsync(VacacionesDto.CancelarSolicitudDto dto);
        Task<bool> ActualizarSolicitudAsync(VacacionesDto.ActualizarSolicitudDto dto);
    }

    public class VacacionesRepository : IVacacionesRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public VacacionesRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<VacacionesDto.SolicitudVacacionDto>> ObtenerHistorialAsync(int compania, int m103)
        {
            var historial = new List<VacacionesDto.SolicitudVacacionDto>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT Compañia, M103, Numero, Periodo, Correo_Empleado, Fecha_Registro, Fecha_Inicio, Fecha_Fin, Fecha_Retorno, Dias_Solicitados, 
            Comentarios_Empleado, Estatus, Cancelada, Jefe_Inmediato FROM Solicitud_Vacaciones WHERE Compañia = @compania AND M103 = @m103 ORDER BY Numero DESC";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                historial.Add(new VacacionesDto.SolicitudVacacionDto
                {
                    Compania = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    M103 = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Numero = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                    Periodo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                    CorreoEmpleado = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim(),
                    FechaRegistro = reader.IsDBNull(5) ? string.Empty : reader.GetString(5).Trim(),
                    FechaInicio = reader.IsDBNull(6) ? string.Empty : reader.GetString(6).Trim(),
                    FechaFin = reader.IsDBNull(7) ? string.Empty : reader.GetString(7).Trim(),
                    FechaRetorno = reader.IsDBNull(8) ? string.Empty : reader.GetString(8).Trim(),
                    DiasSolicitados = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                    ComentariosEmpleado = reader.IsDBNull(10) ? string.Empty : reader.GetString(10).Trim(),
                    Estatus = reader.IsDBNull(11) ? string.Empty : reader.GetString(11).Trim(),
                    Cancelada = reader.IsDBNull(12) ? string.Empty : reader.GetString(12).Trim(),
                    JefeInmediato = reader.IsDBNull(13) ? 0 : reader.GetInt32(13)
                });
            }

            return historial;
        }

        public async Task<VacacionesDto.DatosInicialesVacacionesDto?> ObtenerDatosInicialesAsync(int compania, int m103)
        {
            var dto = new VacacionesDto.DatosInicialesVacacionesDto();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            const string queryAmn = @"SELECT a.M103, ISNULL(a.M104, ''), ISNULL(a.Correo, ''),ISNULL(a.Jefe_Inmediato, 0),
            ISNULL(j.Nombre, ''),ISNULL(j.Correo, ''),ISNULL(a.M106, '') FROM AMN a
            LEFT JOIN Jefes_Inmediatos j ON a.Compañia = j.Compañia AND a.Jefe_Inmediato = j.Numero WHERE a.Compañia = @compania AND a.M103 = @m103";
            string fechaIngresoStr = "";
            using var cmdAmn = new SqlCommand(queryAmn, conn);
            cmdAmn.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmdAmn.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            using (var reader = await cmdAmn.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync()) return null;
                dto.NumeroEmpleado = reader.GetInt32(0);
                dto.NombreEmpleado = reader.GetString(1).Trim();
                dto.CorreoEmpleado = reader.GetString(2).Trim();
                dto.JefeInmediato = reader.GetInt32(3);
                dto.JefeInmediatoNombre = reader.GetString(4).Trim();
                dto.JefeInmediatoCorreo = reader.GetString(5).Trim();
                fechaIngresoStr = reader.GetString(6).Trim();
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
            const string queryPeriodos = @"SELECT Periodo, ISNULL(Dias_Disponibles, 0) FROM Dias_Vacaciones WHERE Compañia = @compania AND M103 = @m103 AND Antiguedad <= @antiguedad ORDER BY Periodo DESC";
            using var cmdPeriodos = new SqlCommand(queryPeriodos, conn);
            cmdPeriodos.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmdPeriodos.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            cmdPeriodos.Parameters.Add("@antiguedad", SqlDbType.Int).Value = antiguedad;
            bool isFirst = true;
            using (var reader = await cmdPeriodos.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var p = reader.GetString(0).Trim();
                    var dias = reader.GetInt32(1);
                    var periodoDto = new VacacionesDto.PeriodoVacacionDto
                    {
                        Periodo = p,
                        DiasDisponibles = dias,
                        Descripcion = $"{p} ({(dias < 10 ? "0" : "")}{dias})"
                    };
                    if (isFirst)
                    {
                        dto.PeriodoActual = periodoDto;
                        isFirst = false;
                    }
                    dto.PeriodosHistorial.Add(periodoDto);
                }
            }
            const string queryRestringidos = @"SELECT Mes, Dia FROM Dias_Restrictivos_Vacaciones WHERE Baja <> '*' AND Compañia = @compania";
            using var cmdRestringidos = new SqlCommand(queryRestringidos, conn);
            cmdRestringidos.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            using (var reader = await cmdRestringidos.ExecuteReaderAsync())
            {
                int anioActual = DateTime.Now.Year;
                while (await reader.ReadAsync())
                {
                    int mes = reader.GetInt32(0);
                    int dia = reader.GetInt32(1);
                    dto.FechasRestringidas.Add($"{anioActual}/{mes:D2}/{dia:D2}");
                    dto.FechasRestringidas.Add($"{anioActual + 1}/{mes:D2}/{dia:D2}");
                }
            }
            return dto;
        }

        public async Task<List<string>> ObtenerFechasRestringidasAsync(int compania, int anio)
        {
            var fechas = new List<string>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT Mes, Dia FROM Dias_Restrictivos_Vacaciones WHERE Baja <> '*' AND Compañia = @compania";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int mes = reader.GetInt32(0);
                int dia = reader.GetInt32(1);
                fechas.Add($"{anio}/{mes:D2}/{dia:D2}");
                fechas.Add($"{anio + 1}/{mes:D2}/{dia:D2}");
            }
            return fechas;
        }

        public async Task<List<DateTime>> ObtenerDiasInhabilesAsync(int compania)
        {
            var fechas = new List<DateTime>();
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT Fecha FROM Dias_Inhabiles WHERE Baja <> '*' AND Compañia = @compania";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                {
                    if (DateTime.TryParse(reader.GetString(0), out DateTime f))
                    {
                        fechas.Add(f);
                    }
                }
            }
            return fechas;
        }

        public async Task<(string Kardex, int Turno)> ObtenerKardexYTurnoAsync(int compania, int m103)
        {
            int turno = 0;
            string kardex = "DDXXXXX";
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT ISNULL(a.M147, 0) AS Turno, ISNULL(t.T102, '') AS Kardex FROM AMN a LEFT JOIN Turnos t ON t.M147 = a.M147 AND t.Compañia = a.Compañia 
            WHERE a.M103 = @m103 AND a.Compañia = @compania";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = m103;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                turno = reader.GetInt32(0);
                kardex = reader.GetString(1).Trim();
                if (string.IsNullOrWhiteSpace(kardex)) kardex = "XXXXXDD";
            }
            return (kardex, turno);
        }

        public async Task<int> CrearSolicitudAsync(VacacionesDto.CrearSolicitudDto dto, int turno, string kardex)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            const string queryMax = @"SELECT ISNULL(MAX(Numero), 0) + 1 FROM Solicitud_Vacaciones WHERE Compañia = @compania AND M103 = @m103";
            using var cmdMax = new SqlCommand(queryMax, conn);
            cmdMax.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmdMax.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            var nuevoNumero = (int)(await cmdMax.ExecuteScalarAsync() ?? 1);
            const string queryInsert = @"INSERT INTO Solicitud_Vacaciones (
                Compañia, M103, Numero, Periodo, Correo_Empleado, Fecha_Registro, Fecha_Inicio, 
                Fecha_Fin, Fecha_Retorno, Dias_Solicitados, Comentarios_Empleado, Estatus, Fecha_Vobo, 
                Comentarios_Aprobacion, Cancelada, Fecha_Cancelacion, Comentarios_Cancelacion, Tiene_Restriccion, 
                Fechas_Restringidas, Tiene_Dias_Anticipados, Dias_Anticipados, Periodo_Anticipado, Jefe_Inmediato, 
                Fechas_Anticipadas, Tipo_Registro, Jefe_Autorizo, Turno_Actual, Kardex_Actual) VALUES (
                @compania, @m103, @numero, @periodo, @correoEmpleado, @fechaRegistro, @fechaInicio, 
                @fechaFin, @fechaRetorno, @diasSolicitados, @comentariosEmpleado, '', '', 
                '', '', '', '', @tieneRestriccion, @fechasRestringidas, @tieneAnticipados, @diasAnticipados, 
                @periodoAnticipado, @jefeInmediato, @fechasAnticipadas, 'M', 0, @turno, @kardex)";
            using var cmdInsert = new SqlCommand(queryInsert, conn);
            cmdInsert.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmdInsert.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            cmdInsert.Parameters.Add("@numero", SqlDbType.Int).Value = nuevoNumero;
            cmdInsert.Parameters.Add("@periodo", SqlDbType.VarChar, 20).Value = dto.Periodo ?? string.Empty;
            cmdInsert.Parameters.Add("@correoEmpleado", SqlDbType.VarChar, 120).Value = dto.CorreoEmpleado ?? string.Empty;
            cmdInsert.Parameters.Add("@fechaRegistro", SqlDbType.VarChar, 10).Value = FormatearFecha(DateTime.Now);
            cmdInsert.Parameters.Add("@fechaInicio", SqlDbType.VarChar, 10).Value = dto.FechaInicio ?? string.Empty;
            cmdInsert.Parameters.Add("@fechaFin", SqlDbType.VarChar, 10).Value = dto.FechaFin ?? string.Empty;
            cmdInsert.Parameters.Add("@fechaRetorno", SqlDbType.VarChar, 10).Value = dto.FechaRetorno ?? string.Empty;
            cmdInsert.Parameters.Add("@diasSolicitados", SqlDbType.Int).Value = dto.DiasSolicitados;
            cmdInsert.Parameters.Add("@comentariosEmpleado", SqlDbType.VarChar, 500).Value = dto.ComentariosEmpleado ?? string.Empty;
            cmdInsert.Parameters.Add("@tieneRestriccion", SqlDbType.VarChar, 1).Value = dto.TieneRestriccion ?? string.Empty;
            cmdInsert.Parameters.Add("@fechasRestringidas", SqlDbType.VarChar, 200).Value = dto.FechasRestringidas ?? string.Empty;
            cmdInsert.Parameters.Add("@tieneAnticipados", SqlDbType.VarChar, 1).Value = dto.TieneDiasAnticipados ?? string.Empty;
            cmdInsert.Parameters.Add("@diasAnticipados", SqlDbType.Int).Value = dto.DiasAnticipados;
            cmdInsert.Parameters.Add("@periodoAnticipado", SqlDbType.VarChar, 20).Value = dto.PeriodoAnticipado ?? string.Empty;
            cmdInsert.Parameters.Add("@jefeInmediato", SqlDbType.Int).Value = dto.JefeInmediato;
            cmdInsert.Parameters.Add("@fechasAnticipadas", SqlDbType.VarChar, 200).Value = dto.FechasAnticipadas ?? string.Empty;
            cmdInsert.Parameters.Add("@turno", SqlDbType.Int).Value = turno;
            cmdInsert.Parameters.Add("@kardex", SqlDbType.VarChar, 10).Value = kardex ?? string.Empty;

            await cmdInsert.ExecuteNonQueryAsync();

            return nuevoNumero;
        }

        public async Task<bool> CancelarSolicitudAsync(VacacionesDto.CancelarSolicitudDto dto)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();
            const string query = @"DELETE FROM Solicitud_Vacaciones WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero AND Estatus = ''";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = dto.Numero;
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> ActualizarSolicitudAsync(VacacionesDto.ActualizarSolicitudDto dto)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string query = @"UPDATE Solicitud_Vacaciones 
            SET Fecha_Inicio = @fechaInicio, Fecha_Fin = @fechaFin, Fecha_Retorno = @fechaRetorno, Dias_Solicitados = @diasSolicitados, Comentarios_Empleado = @comentarios, Tiene_Restriccion = @tieneRestriccion, Fechas_Restringidas = @fechasRestringidas, Tiene_Dias_Anticipados = @tieneAnticipados, Dias_Anticipados = @diasAnticipados, Periodo_Anticipado = @periodoAnticipado, Fechas_Anticipadas = @fechasAnticipadas
            WHERE Compañia = @compania AND M103 = @m103 AND Numero = @numero AND Estatus = ''";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@compania", SqlDbType.Int).Value = dto.Compania;
            cmd.Parameters.Add("@m103", SqlDbType.Int).Value = dto.M103;
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = dto.Numero;
            cmd.Parameters.Add("@fechaInicio", SqlDbType.VarChar, 10).Value = dto.FechaInicio;
            cmd.Parameters.Add("@fechaFin", SqlDbType.VarChar, 10).Value = dto.FechaFin;
            cmd.Parameters.Add("@fechaRetorno", SqlDbType.VarChar, 10).Value = dto.FechaRetorno;
            cmd.Parameters.Add("@diasSolicitados", SqlDbType.Int).Value = dto.DiasSolicitados;
            cmd.Parameters.Add("@comentarios", SqlDbType.VarChar, 500).Value = dto.ComentariosEmpleado?.ToUpper() ?? string.Empty;
            cmd.Parameters.Add("@tieneRestriccion", SqlDbType.VarChar, 1).Value = dto.TieneRestriccion ?? string.Empty;
            cmd.Parameters.Add("@fechasRestringidas", SqlDbType.VarChar, 200).Value = dto.FechasRestringidas ?? string.Empty;
            cmd.Parameters.Add("@tieneAnticipados", SqlDbType.VarChar, 1).Value = dto.TieneDiasAnticipados ?? string.Empty;
            cmd.Parameters.Add("@diasAnticipados", SqlDbType.Int).Value = dto.DiasAnticipados;
            cmd.Parameters.Add("@periodoAnticipado", SqlDbType.VarChar, 20).Value = dto.PeriodoAnticipado ?? string.Empty;
            cmd.Parameters.Add("@fechasAnticipadas", SqlDbType.VarChar, 200).Value = dto.FechasAnticipadas ?? string.Empty;

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }



    }
}