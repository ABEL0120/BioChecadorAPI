using System.Text.Json;
using BioChecadorAPI.DTOs;

namespace BioChecadorAPI.Services
{
    public interface IPushSubscriptionStore
    {
        List<EmpleadoSuscripcionPersistenteDto> ObtenerTodas();
        Task AgregarOSuscribirDispositivoAsync(string rfc, int minutosAnticipacion, List<HorarioDiaDto> dias, string dispositivo, string pushSubscription, string? ultimoMovimiento = null);
        Task EliminarDispositivoAsync(string rfc, string dispositivo);
        void RegistrarMovimiento(string rfc, string tipoMovimiento);
        Task RegistrarMovimientoAsync(string rfc, string tipoMovimiento);
    }

    public class PushSubscriptionStore : IPushSubscriptionStore
    {
        private readonly string _rutaArchivo;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private List<EmpleadoSuscripcionPersistenteDto> _cache = new();
        private DateTime _ultimaFechaModificacionArchivo = DateTime.MinValue;

        public PushSubscriptionStore()
        {
            _rutaArchivo = Path.Combine(AppContext.BaseDirectory, "suscripciones_push.json");
            CargarArchivo();
        }

        private void CargarArchivo()
        {
            try
            {
                if (File.Exists(_rutaArchivo))
                {
                    var json = File.ReadAllText(_rutaArchivo);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _cache = JsonSerializer.Deserialize<List<EmpleadoSuscripcionPersistenteDto>>(json) ?? new();
                        _ultimaFechaModificacionArchivo = File.GetLastWriteTimeUtc(_rutaArchivo);
                    }
                }
            }
            catch
            {
                _cache = new();
            }
        }

        private void CargarArchivoSiCambio()
        {
            try
            {
                if (File.Exists(_rutaArchivo))
                {
                    var ultimaMod = File.GetLastWriteTimeUtc(_rutaArchivo);
                    if (ultimaMod > _ultimaFechaModificacionArchivo)
                    {
                        var json = File.ReadAllText(_rutaArchivo);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _cache = JsonSerializer.Deserialize<List<EmpleadoSuscripcionPersistenteDto>>(json) ?? new();
                            _ultimaFechaModificacionArchivo = ultimaMod;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void GuardarArchivo()
        {
            try
            {
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_cache, opciones);
                File.WriteAllText(_rutaArchivo, json);
                _ultimaFechaModificacionArchivo = File.GetLastWriteTimeUtc(_rutaArchivo);
            }
            catch
            {
            }
        }

        public List<EmpleadoSuscripcionPersistenteDto> ObtenerTodas()
        {
            lock (_cache)
            {
                CargarArchivoSiCambio();
                return _cache.Select(e => new EmpleadoSuscripcionPersistenteDto
                {
                    Rfc = e.Rfc,
                    MinutosAnticipacion = e.MinutosAnticipacion,
                    UltimoMovimiento = e.UltimoMovimiento,
                    FechaUltimoMovimiento = e.FechaUltimoMovimiento,
                    Dias = e.Dias != null ? new List<HorarioDiaDto>(e.Dias) : new List<HorarioDiaDto>(),
                    Dispositivos = e.Dispositivos != null ? e.Dispositivos.Select(d => new DispositivoPushDto
                    {
                        Dispositivo = d.Dispositivo,
                        PushSubscription = d.PushSubscription
                    }).ToList() : new List<DispositivoPushDto>()
                }).ToList();
            }
        }

        public async Task AgregarOSuscribirDispositivoAsync(
            string rfc,
            int minutosAnticipacion,
            List<HorarioDiaDto> dias,
            string dispositivo,
            string pushSubscription,
            string? ultimoMovimiento = null)
        {
            await _lock.WaitAsync();
            try
            {
                var rfcNorm = (rfc ?? string.Empty).Trim().ToUpperInvariant();
                var emp = _cache.FirstOrDefault(e => e.Rfc.Trim().ToUpperInvariant() == rfcNorm);

                if (emp == null)
                {
                    emp = new EmpleadoSuscripcionPersistenteDto
                    {
                        Rfc = rfcNorm,
                        MinutosAnticipacion = minutosAnticipacion > 0 ? minutosAnticipacion : 15,
                        Dias = dias ?? new List<HorarioDiaDto>(),
                        Dispositivos = new List<DispositivoPushDto>(),
                        UltimoMovimiento = !string.IsNullOrWhiteSpace(ultimoMovimiento) ? ultimoMovimiento.Trim().ToUpperInvariant() : null,
                        FechaUltimoMovimiento = !string.IsNullOrWhiteSpace(ultimoMovimiento) ? DateTime.Today.ToString("yyyy-MM-dd") : null
                    };
                    _cache.Add(emp);
                }
                else
                {
                    emp.MinutosAnticipacion = minutosAnticipacion > 0 ? minutosAnticipacion : 15;
                    if (dias != null && dias.Count > 0)
                    {
                        emp.Dias = dias;
                    }
                    if (!string.IsNullOrWhiteSpace(ultimoMovimiento))
                    {
                        emp.UltimoMovimiento = ultimoMovimiento.Trim().ToUpperInvariant();
                        emp.FechaUltimoMovimiento = DateTime.Today.ToString("yyyy-MM-dd");
                    }
                }

                var dispNombre = (dispositivo ?? string.Empty).Trim();
                var dispExistente = emp.Dispositivos.FirstOrDefault(d => string.Equals(d.Dispositivo.Trim(), dispNombre, StringComparison.OrdinalIgnoreCase));

                if (dispExistente == null)
                {
                    emp.Dispositivos.Add(new DispositivoPushDto
                    {
                        Dispositivo = dispNombre,
                        PushSubscription = pushSubscription
                    });
                }
                else
                {
                    dispExistente.PushSubscription = pushSubscription;
                }

                GuardarArchivo();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task EliminarDispositivoAsync(string rfc, string dispositivo)
        {
            await _lock.WaitAsync();
            try
            {
                var rfcNorm = (rfc ?? string.Empty).Trim().ToUpperInvariant();
                var emp = _cache.FirstOrDefault(e => e.Rfc.Trim().ToUpperInvariant() == rfcNorm);
                if (emp != null)
                {
                    var dispNombre = (dispositivo ?? string.Empty).Trim();
                    emp.Dispositivos.RemoveAll(d => string.Equals(d.Dispositivo.Trim(), dispNombre, StringComparison.OrdinalIgnoreCase));
                    if (emp.Dispositivos.Count == 0)
                    {
                        _cache.Remove(emp);
                    }
                    GuardarArchivo();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void RegistrarMovimiento(string rfc, string tipoMovimiento)
        {
            var rfcNorm = (rfc ?? string.Empty).Trim().ToUpperInvariant();
            lock (_cache)
            {
                var emp = _cache.FirstOrDefault(e => e.Rfc.Trim().ToUpperInvariant() == rfcNorm);
                if (emp != null)
                {
                    emp.UltimoMovimiento = (tipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();
                    emp.FechaUltimoMovimiento = DateTime.Today.ToString("yyyy-MM-dd");
                    GuardarArchivo();
                }
            }
        }

        public async Task RegistrarMovimientoAsync(string rfc, string tipoMovimiento)
        {
            await _lock.WaitAsync();
            try
            {
                RegistrarMovimiento(rfc, tipoMovimiento);
            }
            finally
            {
                _lock.Release();
            }
        }
    }

}
