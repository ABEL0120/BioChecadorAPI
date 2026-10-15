using System.Data;
using System.Globalization;

namespace BioChecadorAPI.Helpers
{
    public static class PermisosHelper
    {
        public static DateTime? ObtenerHoraTurno(object? valor, DateTime dia)
        {
            if (valor == null || valor == DBNull.Value) return null;
            string s = valor.ToString()?.Replace(":", "").Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s) || s == "0000" || !int.TryParse(s, out _)) return null;
            s = s.PadLeft(4, '0');
            if (!int.TryParse(s.Substring(0, 2), out int horas) || !int.TryParse(s.Substring(2, 2), out int minutos)) return null;
            return dia.Date.AddHours(horas).AddMinutes(minutos);
        }

        public static bool ObtenerHorarioDia(DataRow? turno, DateTime dia, out DateTime? entrada, out DateTime? salida, out DateTime? comidaIni, out DateTime? comidaFin)
        {
            entrada = null;
            salida = null;
            comidaIni = null;
            comidaFin = null;

            if (turno == null) return false;

            int n = ((int)dia.DayOfWeek + 6) % 7 + 1;
            string colEntrada = $"T1{n}0";
            string colSalida = $"T1{n}1";
            string colComidaIni = $"T1{n}3";
            string colComidaFin = $"T1{n}4";

            if (turno.Table.Columns.Contains(colEntrada)) entrada = ObtenerHoraTurno(turno[colEntrada], dia);
            if (turno.Table.Columns.Contains(colSalida)) salida = ObtenerHoraTurno(turno[colSalida], dia);

            DateTime? c1 = null;
            DateTime? c2 = null;
            if (turno.Table.Columns.Contains(colComidaIni)) c1 = ObtenerHoraTurno(turno[colComidaIni], dia);
            if (turno.Table.Columns.Contains(colComidaFin)) c2 = ObtenerHoraTurno(turno[colComidaFin], dia);

            if (c1.HasValue && c2.HasValue && c2.Value > c1.Value)
            {
                comidaIni = c1;
                comidaFin = c2;
            }

            return entrada.HasValue && salida.HasValue;
        }

        public static int CalcularMinutosComida(DataRow? turno, DateTime d1, DateTime d2)
        {
            if (turno == null || d2 <= d1) return 0;
            int minutos = 0;
            DateTime dia = d1.Date;

            while (dia <= d2.Date)
            {
                if (ObtenerHorarioDia(turno, dia, out _, out _, out DateTime? cIni, out DateTime? cFin) && cIni.HasValue && cFin.HasValue)
                {
                    DateTime desde = d1 > cIni.Value ? d1 : cIni.Value;
                    DateTime hasta = d2 < cFin.Value ? d2 : cFin.Value;
                    if (hasta > desde)
                    {
                        minutos += (int)(hasta - desde).TotalMinutes;
                    }
                }
                dia = dia.AddDays(1);
            }

            return minutos;
        }

        public static string FormatearTiempo(int minutos)
        {
            if (minutos <= 0) return "0h 0m";
            return $"{minutos / 60}h {minutos % 60}m";
        }
    }
}
