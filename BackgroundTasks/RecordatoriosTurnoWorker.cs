using System.Collections.Concurrent;
using System.Globalization;
using BioChecadorAPI.Services;
using BioChecadorAPI.DTOs;

namespace BioChecadorAPI.BackgroundTasks
{
    public class AlertaEnvioItem
    {
        public string Rfc { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Cuerpo { get; set; } = string.Empty;
        public List<DispositivoPushDto> Dispositivos { get; set; } = new();
    }

    public class RecordatoriosTurnoWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPushSubscriptionStore _subStore;
        private readonly ConcurrentDictionary<string, HashSet<int>> _etapasEnviadasHoy = new();
        private DateTime _fechaUltimaLimpieza = DateTime.Today;

        public RecordatoriosTurnoWorker(IServiceProvider serviceProvider, IPushSubscriptionStore subStore)
        {
            _serviceProvider = serviceProvider;
            _subStore = subStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int intervaloMinutos = 1;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ahoraLocal = DateTime.Now;
                    int minutoSiguiente = (ahoraLocal.Minute / intervaloMinutos + 1) * intervaloMinutos;
                    var siguientePunto = new DateTime(
                        ahoraLocal.Year,
                        ahoraLocal.Month,
                        ahoraLocal.Day,
                        ahoraLocal.Hour,
                        0,
                        0,
                        DateTimeKind.Local
                    ).AddMinutes(minutoSiguiente).AddMilliseconds(200);

                    var tiempoEspera = siguientePunto - DateTime.Now;
                    if (tiempoEspera.TotalMilliseconds > 0)
                    {
                        await Task.Delay(tiempoEspera, stoppingToken);
                    }

                    if (DateTime.Today > _fechaUltimaLimpieza)
                    {
                        _etapasEnviadasHoy.Clear();
                        _fechaUltimaLimpieza = DateTime.Today;
                    }

                    await ProcesarRecordatoriosAsync(stoppingToken);
                }
                catch
                {
                }
            }
        }

        private async Task ProcesarRecordatoriosAsync(CancellationToken stoppingToken)
        {
            var empleados = _subStore.ObtenerTodas();
            if (empleados.Count == 0) return;

            var hoy = DateTime.Today;
            var hoyStr = hoy.ToString("yyyy-MM-dd");
            var ahora = DateTime.Now.TimeOfDay;
            var diaHoyNombre = Normalizar(hoy.ToString("dddd", new CultureInfo("es-MX")));

            var alertasAEnviar = new List<AlertaEnvioItem>();

            foreach (var emp in empleados)
            {
                if (emp.Dispositivos == null || emp.Dispositivos.Count == 0) continue;

                var diaConfig = emp.Dias?.FirstOrDefault(d => Normalizar(d.DiaNombre) == diaHoyNombre);
                if (diaConfig == null || !diaConfig.EsLaborable) continue;

                var ultimoMov = emp.UltimoMovimiento;
                if (emp.FechaUltimoMovimiento != hoyStr)
                {
                    ultimoMov = null;
                }

                string? proximoTipo = null;
                string? horaEventoStr = null;
                string titulo = "";
                string mensaje = "";

                if (!string.IsNullOrEmpty(ultimoMov))
                {
                    if (ultimoMov == "ENTRADA" || ultimoMov == "RETARDO")
                    {
                        if (!string.IsNullOrWhiteSpace(diaConfig.SalidaComida))
                        {
                            proximoTipo = "SALIDA_COMIDA";
                            horaEventoStr = diaConfig.SalidaComida;
                            titulo = "Recordatorio de Comida";
                            mensaje = "Tu hora de comida inicia";
                        }
                        else
                        {
                            proximoTipo = "SALIDA";
                            horaEventoStr = diaConfig.Salida;
                            titulo = "Recordatorio de Fin de Turno";
                            mensaje = "Tu turno finaliza";
                        }
                    }
                    else if (ultimoMov == "SALIDA_COMIDA")
                    {
                        proximoTipo = "ENTRADA_COMIDA";
                        horaEventoStr = diaConfig.RegresoComida;
                        titulo = "Recordatorio de Fin de Comida";
                        mensaje = "Tu tiempo de comida termina";
                    }
                    else if (ultimoMov == "ENTRADA_COMIDA")
                    {
                        proximoTipo = "SALIDA";
                        horaEventoStr = diaConfig.Salida;
                        titulo = "Recordatorio de Fin de Turno";
                        mensaje = "Tu turno finaliza";
                    }
                }
                else
                {
                    if (EstaPendienteOProximo(diaConfig.Entrada, ahora))
                    {
                        proximoTipo = "ENTRADA";
                        horaEventoStr = diaConfig.Entrada;
                        titulo = "Recordatorio de Entrada";
                        mensaje = "Tu turno de trabajo inicia";
                    }
                    else if (!string.IsNullOrWhiteSpace(diaConfig.SalidaComida) && EstaPendienteOProximo(diaConfig.SalidaComida, ahora))
                    {
                        proximoTipo = "SALIDA_COMIDA";
                        horaEventoStr = diaConfig.SalidaComida;
                        titulo = "Recordatorio de Comida";
                        mensaje = "Tu hora de comida inicia";
                    }
                    else if (!string.IsNullOrWhiteSpace(diaConfig.RegresoComida) && EstaPendienteOProximo(diaConfig.RegresoComida, ahora))
                    {
                        proximoTipo = "ENTRADA_COMIDA";
                        horaEventoStr = diaConfig.RegresoComida;
                        titulo = "Recordatorio de Fin de Comida";
                        mensaje = "Tu tiempo de comida termina";
                    }
                    else if (EstaPendienteOProximo(diaConfig.Salida, ahora))
                    {
                        proximoTipo = "SALIDA";
                        horaEventoStr = diaConfig.Salida;
                        titulo = "Recordatorio de Fin de Turno";
                        mensaje = "Tu turno finaliza";
                    }
                }

                if (string.IsNullOrEmpty(proximoTipo) || string.IsNullOrWhiteSpace(horaEventoStr)) continue;

                if (!TryParseHora(horaEventoStr, out var horaEvento)) continue;

                var minutosFaltantes = (horaEvento - ahora).TotalMinutes;

                int? etapaActual = null;
                if (minutosFaltantes <= 11.5 && minutosFaltantes >= 8.0)
                {
                    etapaActual = 1;
                }
                else if (minutosFaltantes <= 4.5 && minutosFaltantes >= 1.0)
                {
                    etapaActual = 2;
                }

                if (!etapaActual.HasValue) continue;

                var claveAlerta = $"{hoy:yyyyMMdd}_{emp.Rfc}_{proximoTipo}";

                var etapas = _etapasEnviadasHoy.GetOrAdd(claveAlerta, _ => new HashSet<int>());
                lock (etapas)
                {
                    if (etapas.Contains(etapaActual.Value)) continue;
                    etapas.Add(etapaActual.Value);
                }

                int minAprox = (int)Math.Ceiling(minutosFaltantes);
                string textoMin = minAprox <= 1 ? "en 1 minuto" : $"en {minAprox} minutos";
                string cuerpo = $"{mensaje} {textoMin} (a las {horaEventoStr}). Recuerda checar tu asistencia.";

                alertasAEnviar.Add(new AlertaEnvioItem
                {
                    Rfc = emp.Rfc,
                    Titulo = titulo,
                    Cuerpo = cuerpo,
                    Dispositivos = emp.Dispositivos.ToList()
                });
            }

            if (alertasAEnviar.Count == 0) return;

            using var scope = _serviceProvider.CreateScope();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
            var dispositivosFallidos = new ConcurrentBag<(string Rfc, string Dispositivo)>();

            await Parallel.ForEachAsync(alertasAEnviar, new ParallelOptions
            {
                MaxDegreeOfParallelism = 25,
                CancellationToken = stoppingToken
            }, async (alerta, ct) =>
            {
                foreach (var disp in alerta.Dispositivos)
                {
                    try
                    {
                        using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, ctsTimeout.Token);

                        var ok = await pushService.EnviarNotificacionAsync(disp.PushSubscription, alerta.Titulo, alerta.Cuerpo);
                        if (!ok)
                        {
                            dispositivosFallidos.Add((alerta.Rfc, disp.Dispositivo));
                        }
                    }
                    catch
                    {
                        dispositivosFallidos.Add((alerta.Rfc, disp.Dispositivo));
                    }
                }
            });

            if (!dispositivosFallidos.IsEmpty)
            {
                foreach (var (rfc, disp) in dispositivosFallidos)
                {
                    await _subStore.EliminarDispositivoAsync(rfc, disp);
                }
            }
        }

        private static bool EstaPendienteOProximo(string? horaStr, TimeSpan ahora)
        {
            if (string.IsNullOrWhiteSpace(horaStr)) return false;
            if (!TryParseHora(horaStr, out var hora)) return false;
            return (hora - ahora).TotalMinutes >= -5;
        }

        private static bool TryParseHora(string str, out TimeSpan ts)
        {
            if (TimeSpan.TryParse(str, out ts)) return true;
            if (DateTime.TryParse(str, out var dt))
            {
                ts = dt.TimeOfDay;
                return true;
            }
            ts = default;
            return false;
        }

        private static string Normalizar(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return s.Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    .Aggregate("", (current, c) => current + c)
                    .ToLowerInvariant()
                    .Trim();
        }
    }
}
