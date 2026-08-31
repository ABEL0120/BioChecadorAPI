using BioChecadorAPI.DTOs;
using System.Data;

namespace BioChecadorAPI.Helpers
{
    public static class ChecadorHelper
    {
        private static readonly Dictionary<string, string[]> SecuenciasDias = new()
        {
            ["LUN"] = new[] { "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo" },
            ["MAR"] = new[] { "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo", "Lunes" },
            ["MIE"] = new[] { "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo", "Lunes", "Martes" },
            ["JUE"] = new[] { "Jueves", "Viernes", "Sábado", "Domingo", "Lunes", "Martes", "Miércoles" },
            ["VIE"] = new[] { "Viernes", "Sábado", "Domingo", "Lunes", "Martes", "Miércoles", "Jueves" },
            ["SAB"] = new[] { "Sábado", "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes" },
            ["DOM"] = new[] { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" }
        };

        private static readonly Dictionary<string, HashSet<string>> TransicionesValidas = new(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new() { "ENTRADA", "RETARDO" },
            ["ENTRADA"] = new() { "SALIDA_COMIDA", "SALIDA" },
            ["RETARDO"] = new() { "SALIDA_COMIDA", "SALIDA" },
            ["SALIDA_COMIDA"] = new() { "ENTRADA_COMIDA" },
            ["ENTRADA_COMIDA"] = new() { "SALIDA_COMIDA", "SALIDA" },
            ["SALIDA"] = new()
        };

        private static string GenerarMensajeError(string actual, string nuevo) => (actual, nuevo) switch
        {
            ("", _) => $"No puedes registrar {nuevo} sin tener una ENTRADA registrada hoy.",
            ("SALIDA", _) => "Ya registraste tu SALIDA de la jornada. No se permiten más registros hoy.",
            ("SALIDA_COMIDA", not "ENTRADA_COMIDA") => "Tienes una SALIDA A COMIDA activa. Debes registrar tu ENTRADA DE COMIDA.",
            ("ENTRADA" or "RETARDO" or "ENTRADA_COMIDA", "ENTRADA" or "RETARDO") => "Ya cuentas con una entrada activa registrada el día de hoy.",
            ("ENTRADA" or "RETARDO", "ENTRADA_COMIDA") => "No puedes registrar ENTRADA DE COMIDA sin una SALIDA A COMIDA previa.",
            _ => $"No se permite registrar {nuevo.Replace("_", " ")} inmediatamente después de {actual.Replace("_", " ")}."
        };

        public static double CalcularDistanciaMetros(double lat1, double lon1, double lat2, double lon2)
        {
            const double RadioTierraMetros = 6371000;
            var dLat = (lat2 - lat1) * (Math.PI / 180.0);
            var dLon = (lon2 - lon1) * (Math.PI / 180.0);
            var a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                    Math.Cos(lat1 * (Math.PI / 180.0)) * Math.Cos(lat2 * (Math.PI / 180.0)) *
                    Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
            var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            return RadioTierraMetros * c;
        }

        public static TurnoDetalleDto MapearTurno(IDataRecord reader, string inicioNomina = "SAB")
        {
            string descripcion = reader["TurnoDescripcion"]?.ToString()?.Trim() ?? string.Empty;
            string patronDias = reader["TurnoPatron"]?.ToString()?.Trim() ?? "DDXXXXX";
            string claveInicio = (inicioNomina ?? "SAB").Trim().ToUpperInvariant();
            if (!SecuenciasDias.TryGetValue(claveInicio, out var nombresDias))
            {
                nombresDias = SecuenciasDias["SAB"];
            }
            var turno = new TurnoDetalleDto
            {
                Descripcion = descripcion,
                DiasPatron = patronDias,
                SecuenciaDias = claveInicio
            };
            for (int i = 0; i < 7; i++)
            {
                int diaPrefijo = i + 1;
                bool esLaborable = patronDias.Length > i && char.ToUpperInvariant(patronDias[i]) == 'X';
                var dia = new HorarioDiaDto
                {
                    DiaIndice = i,
                    DiaNombre = nombresDias[i],
                    EsLaborable = esLaborable,
                    Entrada = FormatearHora(reader[$"T1{diaPrefijo}0"]),
                    Salida = FormatearHora(reader[$"T1{diaPrefijo}1"]),
                    ToleranciaEntradaMinutos = ParsearEntero(reader[$"T1{diaPrefijo}2"]),
                    SalidaComida = FormatearHora(reader[$"T1{diaPrefijo}3"]),
                    RegresoComida = FormatearHora(reader[$"T1{diaPrefijo}4"]),
                    ToleranciaComidaMinutos = ParsearEntero(reader[$"T1{diaPrefijo}5"])
                };
                turno.Dias.Add(dia);
            }
            return turno;
        }

        public static string? FormatearHora(object? valor)
        {
            if (valor == null || valor == DBNull.Value) return null;

            string horaStr = valor.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(horaStr) || horaStr == "0" || horaStr == "0000" || horaStr == "0 00")
            {
                return null;
            }

            horaStr = horaStr.PadLeft(4, '0');
            if (horaStr.Length >= 4 &&
                int.TryParse(horaStr.AsSpan(0, 2), out int h) &&
                int.TryParse(horaStr.AsSpan(2, 2), out int m))
            {
                if (h >= 0 && h < 24 && m >= 0 && m < 60)
                {
                    return $"{h:D2}:{m:D2}";
                }
            }

            return null;
        }

        public static int ParsearEntero(object? valor)
        {
            if (valor == null || valor == DBNull.Value) return 0;
            int.TryParse(valor.ToString()?.Trim(), out int res);
            return res;
        }

        public static (bool EsValido, string Mensaje) ValidarTransicion(string? ultimoMovimiento, string? nuevoMovimiento)
        {
            string actual = ultimoMovimiento?.Trim().ToUpperInvariant() ?? string.Empty;
            string nuevo = nuevoMovimiento?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!TransicionesValidas.TryGetValue(actual, out var permitidos))
            {
                return (false, "Estado de movimiento no reconocido.");
            }

            if (permitidos.Contains(nuevo))
            {
                return (true, string.Empty);
            }

            return (false, GenerarMensajeError(actual, nuevo));
        }
    }
}
