using System.Text.Json;

using BioChecadorAPI.DTOs;

using BioChecadorAPI.Repositories;

namespace BioChecadorAPI.Services
{
    public interface IPushSubscriptionStore
    {
        List<EmpleadoSuscripcionPersistenteDto> ObtenerTodas();
        Task AgregarOSuscribirDispositivoAsync(string rfc, int minutosAnticipacion, List<HorarioDiaDto> dias, string dispositivo, string pushSubscription, string? ultimoMovimiento = null);
        Task EliminarDispositivoAsync(string rfc, string dispositivo);
        void RegistrarMovimiento(string rfc, string tipoMovimiento);
        Task RegistrarMovimientoAsync(string rfc, string tipoMovimiento);
        Task SincronizarDesdeBDAsync(IAmnRepository amnRepo, IChecadorService checadorService);
    }

    public class PushSubscriptionStore : IPushSubscriptionStore

    {

        private readonly string _rutaArchivo;

        private readonly SemaphoreSlim _lock = new(1, 1);

        private List<EmpleadoSuscripcionPersistenteDto> _cache = new();

        private DateTime _ultimaFechaModificacionArchivo = DateTime.MinValue;



        public PushSubscriptionStore()

        {

            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BioChecadorAPI");

            if (!Directory.Exists(appDataDir))

            {

                Directory.CreateDirectory(appDataDir);

            }
            _rutaArchivo = Path.Combine(appDataDir, "suscripciones_push.json");
            var rutaAntigua = Path.Combine(AppContext.BaseDirectory, "suscripciones_push.json");
            if (!File.Exists(_rutaArchivo) && File.Exists(rutaAntigua))
            {
                try
                {
                    File.Copy(rutaAntigua, _rutaArchivo, true);
                }
                catch { }
            }
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
                    if (ultimaMod != _ultimaFechaModificacionArchivo)
                    {
                        var json = File.ReadAllText(_rutaArchivo);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _cache = JsonSerializer.Deserialize<List<EmpleadoSuscripcionPersistenteDto>>(json) ?? new();
                        }
                        else
                        {
                            _cache = new();
                        }
                        _ultimaFechaModificacionArchivo = ultimaMod;
                    }
                }
                else
                {
                    _cache = new();
                    _ultimaFechaModificacionArchivo = DateTime.MinValue;
                }
            }
            catch
            {
                _cache = new();
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

        public async Task SincronizarDesdeBDAsync(IAmnRepository amnRepo, IChecadorService checadorService)
        {
            await _lock.WaitAsync();
            try
            {
                var activasEnBD = await amnRepo.ObtenerSuscripcionesPushActivasAsync();
                bool huboCambios = false;
                var listaEmp = _cache.ToList();
                foreach (var emp in listaEmp)
                {
                    var dispList = emp.Dispositivos.ToList();
                    foreach (var d in dispList)
                    {
                        bool existeEnBd = activasEnBD.Any(b =>
                            b.Rfc.Trim().ToUpperInvariant() == emp.Rfc.Trim().ToUpperInvariant() &&
                            string.Equals(b.Dispositivo.Trim(), d.Dispositivo.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(b.SubscriptionJson)
                        );
                        if (!existeEnBd)
                        {
                            emp.Dispositivos.Remove(d);
                            huboCambios = true;
                        }
                    }
                    if (emp.Dispositivos.Count == 0)
                    {
                        _cache.Remove(emp);
                        huboCambios = true;
                    }
                }
                foreach (var (rfc, disp, sub) in activasEnBD)
                {
                    var rfcNorm = (rfc ?? string.Empty).Trim().ToUpperInvariant();
                    var dispNorm = (disp ?? string.Empty).Trim();
                    var emp = _cache.FirstOrDefault(e => e.Rfc.Trim().ToUpperInvariant() == rfcNorm);
                    var dispExistente = emp?.Dispositivos?.FirstOrDefault(d =>
                        string.Equals(d.Dispositivo.Trim(), dispNorm, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(d.PushSubscription)
                    );
                    if (dispExistente == null)
                    {
                        await amnRepo.EliminarSuscripcionPushAsync(rfcNorm, dispNorm);
                    }
                }

                if (huboCambios)
                {
                    GuardarArchivo();
                }
            }
            catch
            {
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
