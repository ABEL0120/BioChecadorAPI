using BioChecadorAPI.DTOs;
using BioChecadorAPI.Helpers;
using BioChecadorAPI.Repositories;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Services
{
    public interface ISensorFisicoService
    {
        Task<ApiResponse<List<HuellaFisicaDto>>> ObtenerHuellasActivasAsync();
        Task<ApiResponse<UltimoMovimientoSensorHoyDto>> ObtenerUltimoMovimientoHoyAsync(string rfc);
        Task<(int StatusCode, ApiResponse<SensorFisicoResponseDto> Response)> RegistrarChecadaAsync(MarcarSensorFisicoRequestDto dto);
        Task<ApiResponse<List<UltimaChecadaSensorDto>>> ObtenerUltimasChecadasAsync(int limite, string? dispositivoNombre);
    }

    public class SensorFisicoService : ISensorFisicoService
    {
        private readonly ISensorFisicoRepository _repository;
        private readonly IPushSubscriptionStore _subStore;

        public SensorFisicoService(
            ISensorFisicoRepository repository,
            IPushSubscriptionStore subStore)
        {
            _repository = repository;
            _subStore = subStore;
        }

        public async Task<ApiResponse<List<HuellaFisicaDto>>> ObtenerHuellasActivasAsync()
        {
            var huellas = await _repository.ObtenerHuellasActivasAsync();
            var lista = huellas
                .Where(h => h.Template != null && h.Template.Length > 0 && !string.IsNullOrWhiteSpace(h.RFC))
                .Select(h => new HuellaFisicaDto
                {
                    Numero = h.Numero,
                    Rfc = h.RFC.Trim().ToUpperInvariant(),
                    TemplateBase64 = Convert.ToBase64String(h.Template)
                })
                .ToList();

            return new ApiResponse<List<HuellaFisicaDto>>
            {
                Success = true,
                Message = lista.Count == 0 ? "No hay huellas físicas registradas." : "Huellas cargadas correctamente.",
                Data = lista
            };
        }

        public async Task<ApiResponse<UltimoMovimientoSensorHoyDto>> ObtenerUltimoMovimientoHoyAsync(string rfc)
        {
            string rfcLimpio = (rfc ?? string.Empty).Trim().ToUpperInvariant();
            string fechaHoy = BDHelper.FormatearFecha(DateTime.Now);
            string ultimo = await _repository.ObtenerUltimoMovimientoHoyAsync(rfcLimpio, fechaHoy);
            var (requiereSeleccion, siguiente, opciones) = ResolverMovimiento(ultimo);

            return new ApiResponse<UltimoMovimientoSensorHoyDto>
            {
                Success = true,
                Message = "Estado consultado correctamente.",
                Data = new UltimoMovimientoSensorHoyDto
                {
                    Rfc = rfcLimpio,
                    UltimoMovimientoHoy = ultimo,
                    RequiereSeleccion = requiereSeleccion,
                    SiguienteMovimientoSugerido = siguiente,
                    Opciones = opciones
                }
            };
        }

        public async Task<(int StatusCode, ApiResponse<SensorFisicoResponseDto> Response)> RegistrarChecadaAsync(
            MarcarSensorFisicoRequestDto dto)
        {
            string rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            string fechaHoy = BDHelper.FormatearFecha(DateTime.Now);
            string ultimoMovimiento = await _repository.ObtenerUltimoMovimientoHoyAsync(rfcLimpio, fechaHoy);

            string tipoMovimiento;
            if (!string.IsNullOrWhiteSpace(dto.TipoMovimientoOpcional))
            {
                tipoMovimiento = dto.TipoMovimientoOpcional.Trim().ToUpperInvariant();
                if (tipoMovimiento is not ("ENTRADA" or "SALIDA_COMIDA" or "ENTRADA_COMIDA" or "SALIDA"))
                {
                    return (StatusCodes.Status400BadRequest, new ApiResponse<SensorFisicoResponseDto>
                    {
                        Success = false,
                        Message = "El tipo de movimiento especificado no es válido."
                    });
                }
            }
            else
            {
                var (requiereSeleccion, siguiente, opciones) = ResolverMovimiento(ultimoMovimiento);
                if (requiereSeleccion || string.IsNullOrEmpty(siguiente))
                {
                    return (StatusCodes.Status200OK, new ApiResponse<SensorFisicoResponseDto>
                    {
                        Success = true,
                        Message = "Tienes una entrada activa. Selecciona tu salida.",
                        Data = new SensorFisicoResponseDto
                        {
                            RequiereSeleccion = true,
                            Rfc = rfcLimpio,
                            UltimoMovimientoHoy = ultimoMovimiento,
                            Opciones = opciones ?? new[] { "SALIDA_COMIDA", "SALIDA" }
                        }
                    });
                }

                tipoMovimiento = siguiente;
            }

            var resultado = await _repository.InsertarChecadaSensorAsync(
                rfcLimpio,
                tipoMovimiento,
                dto.DispositivoNombre,
                dto.UserAgent,
                dto.Latitud,
                dto.Longitud
            );

            if (!resultado.Insertado)
            {
                return (StatusCodes.Status400BadRequest, new ApiResponse<SensorFisicoResponseDto>
                {
                    Success = false,
                    Message = "No fue posible registrar la checada en la base de datos."
                });
            }

            _subStore.RegistrarMovimiento(rfcLimpio, tipoMovimiento);

            string etiqueta = ObtenerEtiquetaMovimiento(tipoMovimiento);
            string lineaHistorial = ConstruirLineaHistorial(resultado.FechaHora, rfcLimpio, etiqueta);

            return (StatusCodes.Status200OK, new ApiResponse<SensorFisicoResponseDto>
            {
                Success = true,
                Message = "Asistencia registrada correctamente.",
                Data = new SensorFisicoResponseDto
                {
                    RequiereSeleccion = false,
                    Rfc = rfcLimpio,
                    UltimoMovimientoHoy = tipoMovimiento,
                    Numero = resultado.Numero,
                    NumeroCompania = resultado.NumeroCompania,
                    TipoMovimiento = tipoMovimiento,
                    EtiquetaMovimiento = etiqueta,
                    FechaHora = resultado.FechaHora,
                    LineaHistorial = lineaHistorial
                }
            });
        }

        public async Task<ApiResponse<List<UltimaChecadaSensorDto>>> ObtenerUltimasChecadasAsync(
            int limite,
            string? dispositivoNombre)
        {
            var registros = await _repository.ObtenerUltimasChecadasAsync(limite, dispositivoNombre);
            var lista = registros.Select(r =>
            {
                string rfcLimpio = r.Rfc.Trim().ToUpperInvariant();
                string etiqueta = ObtenerEtiquetaMovimiento(r.TipoMovimiento);
                return new UltimaChecadaSensorDto
                {
                    Numero = r.Numero,
                    Rfc = rfcLimpio,
                    NumeroCompania = r.NumeroCompania,
                    FechaHora = r.FechaHora,
                    TipoMovimiento = r.TipoMovimiento,
                    EtiquetaMovimiento = etiqueta,
                    DispositivoNombre = r.DispositivoNombre,
                    LineaHistorial = ConstruirLineaHistorial(r.FechaHora, rfcLimpio, etiqueta)
                };
            }).ToList();

            return new ApiResponse<List<UltimaChecadaSensorDto>>
            {
                Success = true,
                Message = "Historial consultado correctamente.",
                Data = lista
            };
        }

        private static (bool RequiereSeleccion, string? SiguienteMovimiento, string[]? Opciones) ResolverMovimiento(
            string ultimoMovimiento)
        {
            string ultimo = (ultimoMovimiento ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrEmpty(ultimo) || ultimo == "SALIDA")
            {
                return (false, "ENTRADA", null);
            }

            if (ultimo == "SALIDA_COMIDA")
            {
                return (false, "ENTRADA_COMIDA", null);
            }

            if (ultimo == "ENTRADA_COMIDA")
            {
                return (false, "SALIDA", null);
            }

            return (true, null, new[] { "SALIDA_COMIDA", "SALIDA" });
        }

        private static string ObtenerEtiquetaMovimiento(string tipoMovimiento)
        {
            return (tipoMovimiento ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "ENTRADA" => "ENTRADA REGISTRADA",
                "SALIDA_COMIDA" => "SALIDA A COMER",
                "ENTRADA_COMIDA" => "REGRESO DE COMER",
                "SALIDA" => "SALIDA OFICIAL",
                _ => tipoMovimiento ?? string.Empty
            };
        }

        private static string ConstruirLineaHistorial(string fechaHora, string rfc, string etiqueta)
        {
            string horaCorta = fechaHora.Length >= 19 ? fechaHora.Substring(11) : fechaHora;
            return $"[{horaCorta}]  {rfc,-13}  {etiqueta}";
        }
    }
}
