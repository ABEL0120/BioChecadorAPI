using System.Data;
using System.Globalization;

namespace BioChecadorAPI.Helpers
{
    public class CalculoTiempoPermisoResultado
    {
        public bool EsValido { get; set; } = true;
        public string ErrorValidacion { get; set; } = string.Empty;
        public int MinutosBrutos { get; set; }
        public int MinutosComidaDescontados { get; set; }
        public int MinutosNetos { get; set; }
        public int DiasLaborables { get; set; }
        public bool CruzaComida { get; set; }
        public string TiempoTexto { get; set; } = "0h 0m";
        public string MensajeDetalle { get; set; } = string.Empty;
        public bool PermiteVacaciones { get; set; }
    }

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

        public static CalculoTiempoPermisoResultado CalcularTiempoPermiso(DataRow? turno, DateTime d1, DateTime d2)
        {
            if (d2.Date < d1.Date)
            {
                return new CalculoTiempoPermisoResultado
                {
                    EsValido = false,
                    ErrorValidacion = "La fecha de fin no puede ser anterior a la de inicio.",
                    TiempoTexto = "0h 0m"
                };
            }

            if (d2.Date == d1.Date && d2 <= d1)
            {
                return new CalculoTiempoPermisoResultado
                {
                    EsValido = false,
                    ErrorValidacion = "La hora de fin debe ser posterior a la de inicio.",
                    TiempoTexto = "0h 0m"
                };
            }

            int totalMinutosBrutos = 0;
            int totalMinutosComida = 0;
            int totalMinutosNetos = 0;
            int diasLaborablesCount = 0;
            bool cruzaComidaAlgunaVez = false;
            bool mismoDia = d1.Date == d2.Date;

            for (DateTime dia = d1.Date; dia <= d2.Date; dia = dia.AddDays(1))
            {
                bool esLaborable = ObtenerHorarioDia(turno, dia, out DateTime? entrada, out DateTime? salida, out DateTime? comidaIni, out DateTime? comidaFin);

                if (turno == null)
                {
                    bool esFinSemana = dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday;
                    if (!esFinSemana)
                    {
                        esLaborable = true;
                        entrada = dia.Date.AddHours(9);
                        salida = dia.Date.AddHours(18);
                        comidaIni = dia.Date.AddHours(14);
                        comidaFin = dia.Date.AddHours(15);
                    }
                    else
                    {
                        esLaborable = false;
                    }
                }

                if (!esLaborable || !entrada.HasValue || !salida.HasValue)
                {
                    continue;
                }

                diasLaborablesCount++;

                DateTime inicioEfectivo;
                DateTime finEfectivo;

                if (mismoDia)
                {
                    inicioEfectivo = d1 > entrada.Value ? d1 : entrada.Value;
                    finEfectivo = d2 < salida.Value ? d2 : salida.Value;
                }
                else
                {
                    if (dia == d1.Date)
                    {
                        inicioEfectivo = d1 > entrada.Value ? d1 : entrada.Value;
                        finEfectivo = salida.Value;
                    }
                    else if (dia == d2.Date)
                    {
                        inicioEfectivo = entrada.Value;
                        finEfectivo = d2 < salida.Value ? d2 : salida.Value;
                    }
                    else
                    {
                        inicioEfectivo = entrada.Value;
                        finEfectivo = salida.Value;
                    }
                }

                if (finEfectivo > inicioEfectivo)
                {
                    int minutosDiaBrutos = (int)(finEfectivo - inicioEfectivo).TotalMinutes;
                    int minutosDiaComida = 0;

                    if (comidaIni.HasValue && comidaFin.HasValue)
                    {
                        DateTime cDesde = inicioEfectivo > comidaIni.Value ? inicioEfectivo : comidaIni.Value;
                        DateTime cHasta = finEfectivo < comidaFin.Value ? finEfectivo : comidaFin.Value;
                        if (cHasta > cDesde)
                        {
                            minutosDiaComida = (int)(cHasta - cDesde).TotalMinutes;
                            cruzaComidaAlgunaVez = true;
                        }
                    }

                    int minutosDiaNetos = minutosDiaBrutos - minutosDiaComida;
                    if (minutosDiaNetos > 0)
                    {
                        totalMinutosBrutos += minutosDiaBrutos;
                        totalMinutosComida += minutosDiaComida;
                        totalMinutosNetos += minutosDiaNetos;
                    }
                }
            }

            if (diasLaborablesCount == 0)
            {
                return new CalculoTiempoPermisoResultado
                {
                    EsValido = false,
                    ErrorValidacion = "El periodo seleccionado corresponde a días de descanso de tu turno; no requiere permiso.",
                    TiempoTexto = "0h 0m",
                    DiasLaborables = 0
                };
            }

            if (totalMinutosNetos <= 0)
            {
                if (cruzaComidaAlgunaVez)
                {
                    return new CalculoTiempoPermisoResultado
                    {
                        EsValido = false,
                        ErrorValidacion = "El horario capturado queda dentro de tu hora de comida; no genera tiempo de permiso.",
                        MinutosBrutos = totalMinutosBrutos,
                        MinutosComidaDescontados = totalMinutosComida,
                        MinutosNetos = 0,
                        TiempoTexto = "0h 0m (comida)",
                        CruzaComida = true,
                        DiasLaborables = diasLaborablesCount
                    };
                }

                return new CalculoTiempoPermisoResultado
                {
                    EsValido = false,
                    ErrorValidacion = "El horario seleccionado queda fuera del horario laboral de tu turno.",
                    MinutosBrutos = 0,
                    MinutosComidaDescontados = 0,
                    MinutosNetos = 0,
                    TiempoTexto = "0h 0m",
                    DiasLaborables = diasLaborablesCount
                };
            }

            bool permiteVacaciones = totalMinutosNetos >= 420 || diasLaborablesCount > 1;
            string detalle;
            if (diasLaborablesCount > 1)
            {
                detalle = totalMinutosComida > 0
                    ? $"Comprende {diasLaborablesCount} días hábiles. Se descontaron {totalMinutosComida} min por comida."
                    : $"Comprende {diasLaborablesCount} días hábiles de turno.";
            }
            else
            {
                detalle = totalMinutosComida > 0
                    ? $"Se descontaron {totalMinutosComida} minutos por horario de comida."
                    : string.Empty;
            }

            string tiempoTexto = diasLaborablesCount > 1
                ? $"{diasLaborablesCount} días ({FormatearTiempo(totalMinutosNetos)})"
                : FormatearTiempo(totalMinutosNetos);

            return new CalculoTiempoPermisoResultado
            {
                EsValido = true,
                MinutosBrutos = totalMinutosBrutos,
                MinutosComidaDescontados = totalMinutosComida,
                MinutosNetos = totalMinutosNetos,
                DiasLaborables = diasLaborablesCount,
                CruzaComida = totalMinutosComida > 0,
                TiempoTexto = tiempoTexto,
                MensajeDetalle = detalle,
                PermiteVacaciones = permiteVacaciones
            };
        }
    }
}
