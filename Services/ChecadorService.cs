using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;
using System.Globalization;
using System.Text;
using static BioChecadorAPI.Helpers.ChecadorHelper;
using static BioChecadorAPI.Helpers.ResponseHelper;
using System.Text.Json;

namespace BioChecadorAPI.Services
{
    public interface IChecadorService
    {
        Task<ApiResponse<EstadoEmpleadoResponseDto>> VerificarRfcAsync(VerificarRfcRequestDto request);
        Task<ApiResponse<bool>> EnrolarBiometriaAsync(EnrolarBiometriaDto dto);
        Task<ApiResponse<RegistroChecadaResponseDto>> MarcarAsistenciaAsync(MarcarAsistenciaDto dto);
        Task<ApiResponse<HistoricoAMNResponse[]>> ConsultarHistoricoAMN(HistoricoAMNDto dto);
        Task<ApiResponse<SolicitudResponseDto>> MandarSolicitudAMN(SolicitudCreacionDto dto);
        Task<ApiResponse<SolicitudResponseDto>> ConsultarSolicitudEmpleado(SolicitudCreacionDto dto);
        Task<(int StatusCode, ApiResponse<ChecadorFisicoResponseDto> Response)> RegistrarChecadaChecadorFisicoAsync(ChecadorFisicoRequestDto dto, string userAgent);
        Task<ApiResponse<UltimoMovimientoMovilDto?>> ObtenerUltimoMovimientoMovilAsync(int desdeNumero);
    }

    public class ChecadorService : IChecadorService
    {
        private readonly IAmnRepository _amnRepository;
        private readonly IPushSubscriptionStore _subStore;
        private readonly IChecadorFisicoTokenService _tokenService;
        private readonly IChecadorFisicoPasskeyService _passkeyService;

        public ChecadorService(
            IAmnRepository amnRepository,
            IPushSubscriptionStore subStore,
            IChecadorFisicoTokenService tokenService,
            IChecadorFisicoPasskeyService passkeyService)
        {
            _amnRepository = amnRepository;
            _subStore = subStore;
            _tokenService = tokenService;
            _passkeyService = passkeyService;
        }

        public async Task<ApiResponse<EstadoEmpleadoResponseDto>> VerificarRfcAsync(VerificarRfcRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Rfc))
            {
                return new ApiResponse<EstadoEmpleadoResponseDto>
                {
                    Success = false,
                    Message = "El RFC es obligatorio.",
                    Data = null
                };
            }

            string rfcLimpio = request.Rfc.Trim().ToUpperInvariant();
            var empleado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio, request.DispositivoNombre);

            if (empleado == null)
            {
                return new ApiResponse<EstadoEmpleadoResponseDto>
                {
                    Success = false,
                    Message = "El RFC ingresado no se encuentra registrado en el sistema.",
                    Data = null
                };
            }

            var rfcNorm = empleado.Rfc.Trim().ToUpperInvariant();
            var dispNorm = (request.DispositivoNombre ?? string.Empty).Trim();

            // 1. Verificar existencia en JSON
            var todasSuscripciones = _subStore.ObtenerTodas();
            var empJson = todasSuscripciones.FirstOrDefault(e => e.Rfc.Trim().ToUpperInvariant() == rfcNorm);
            var dispJson = empJson?.Dispositivos?.FirstOrDefault(d => 
                (string.IsNullOrWhiteSpace(dispNorm) || string.Equals(d.Dispositivo.Trim(), dispNorm, StringComparison.OrdinalIgnoreCase)) && 
                !string.IsNullOrWhiteSpace(d.PushSubscription)
            );
            bool existeEnJson = dispJson != null;

            // 2. Verificar existencia en Base de Datos (AMN_Biometria)
            var subBd = await _amnRepository.ObtenerSuscripcionPushAsync(rfcNorm, dispNorm);
            bool existeEnBd = !string.IsNullOrWhiteSpace(subBd);

            // Regla estricta: Si falta en cualquiera de los dos (JSON o BD), se limpia en ambos y tienePushActivo es false
            if (existeEnJson && existeEnBd)
            {
                empleado.TieneSuscripcionPush = true;
            }
            else
            {
                if (existeEnJson)
                {
                    await _subStore.EliminarDispositivoAsync(rfcNorm, dispNorm);
                }
                if (existeEnBd)
                {
                    await _amnRepository.EliminarSuscripcionPushAsync(rfcNorm, dispNorm);
                }
                empleado.TieneSuscripcionPush = false;
            }

            return new ApiResponse<EstadoEmpleadoResponseDto>
            {
                Success = true,
                Message = "Empleado verificado con éxito.",
                Data = empleado
            };
        }

        public async Task<ApiResponse<bool>> EnrolarBiometriaAsync(EnrolarBiometriaDto dto)
        {
            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            var dispositivo = dto.Dispositivo.Trim().ToUpperInvariant();

            var empleado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio, dispositivo);
            if (empleado == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Empleado no registrado.",
                    Data = false
                };
            }

            //if (empleado.TieneBiometria)
            //{
            //    return new ApiResponse<bool>
            //    {
            //        Success = false,
            //        Message = "El empleado ya cuenta con biometría registrada.",
            //        Data = false
            //    };
            //}

            byte[] publicKeyBytes;
            try
            {
                publicKeyBytes = Convert.FromBase64String(dto.PublicKey);
            }
            catch
            {
                publicKeyBytes = Encoding.UTF8.GetBytes(dto.PublicKey);
            }

            var guardado = await _amnRepository.GuardarBiometriaAsync(
                rfcLimpio,
                dto.CredentialId.Trim(),
                publicKeyBytes,
                dto.Dispositivo,
                string.Empty
            );

            if (!guardado)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error al guardar el registro biométrico en la base de datos.",
                    Data = false
                };
            }

            return new ApiResponse<bool>
            {
                Success = true,
                Message = "Biometría registrada exitosamente.",
                Data = true
            };
        }


        public async Task<ApiResponse<SolicitudResponseDto>> MandarSolicitudAMN(SolicitudCreacionDto dto)
        {
            try
            {
                bool solicitudExistente = await _amnRepository.ConsultarExistenciaSolicitud(dto);
                if (solicitudExistente)
                {
                    return new ApiResponse<SolicitudResponseDto>
                    {
                        Success = false,
                        Message = "Ya cuentas con una solicitud pendiente en revisión. Por favor, espera la respuesta del administrador. Si transcurren 24 horas sin respuesta, podrás enviar una nueva solicitud."
                    };
                }
                var fechaExpiracion = await _amnRepository.EnviarSolicitudAsync(dto);
                if (string.IsNullOrEmpty(fechaExpiracion))
                {
                    return new ApiResponse<SolicitudResponseDto>
                    {
                        Success = false,
                        Message = "Error al registrar la solicitud en la base de datos."
                    };
                }

                return new ApiResponse<SolicitudResponseDto>
                {
                    Success = true,
                    Message = $"Solicitud enviada correctamente. Su solicitud expira en 1 dia habil",
                    Data = new SolicitudResponseDto
                    {
                        FechaExpiracion = fechaExpiracion
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SolicitudResponseDto>
                {
                    Success = false,
                    Message = $"Error al intentar enviar la solicitud.",
                    Data = { }
                };
            }
        }

        public async Task<ApiResponse<SolicitudResponseDto>> ConsultarSolicitudEmpleado(SolicitudCreacionDto dto)
        {
            bool solicitudExistente = await _amnRepository.ConsultarExistenciaSolicitud(dto);
            if (solicitudExistente)
            {
                return new ApiResponse<SolicitudResponseDto>
                {
                    Success = false,
                    Message = "Ya cuentas con una solicitud pendiente en revisión. Por favor, espera la respuesta del administrador. Si transcurren 24 horas sin respuesta, podrás enviar una nueva solicitud."
                };
            }
            else
            {
                return new ApiResponse<SolicitudResponseDto>
                {
                    Success = true,
                    Message = "No hay solicitudes pendientes.",
                    Data = null
                };
            }
        }

        public async Task<ApiResponse<RegistroChecadaResponseDto>> MarcarAsistenciaAsync(MarcarAsistenciaDto dto)
        {
            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            var dispositivo = dto.Dispositivo.Trim().ToUpperInvariant();

            var empleado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio, dispositivo);
            if (empleado == null)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = "Empleado no registrado."
                };
            }

            if (!empleado.TieneBiometria)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = "El empleado no cuenta con biometría registrada. Debe enrolarse primero."
                };
            }

            var credencialValida = await _amnRepository.ValidarCredencialBiometricaAsync(rfcLimpio, dto.CredentialId.Trim());
            if (!credencialValida)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = "La credencial biométrica no coincide con el registro del empleado."
                };
            }

            var distancia = CalcularDistanciaMetros(
                (double)dto.Latitud,
                (double)dto.Longitud,
                (double)empleado.LatitudEmpresa,
                (double)empleado.LongitudEmpresa
            );

            // Sin restriccion de geocercas: permite checar en cualquier lugar sin bloquear
            var fueraDeRango = false;

            var guardado = await _amnRepository.InsertarChecadaAsync(
                rfcLimpio,
                empleado.NumeroCompania,
                dto.Latitud,
                dto.Longitud,
                string.Empty,
                dto.Dispositivo,
                dto.TipoMovimiento,
                empleado.NumeroEmpleado,
                dto.FechaHora
            );

            if (!guardado)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = "Error al registrar la checada en la base de datos."
                };
            }

            _subStore.RegistrarMovimiento(rfcLimpio, dto.TipoMovimiento);

            return new ApiResponse<RegistroChecadaResponseDto>
            {
                Success = true,
                Message = "Asistencia registrada correctamente.",
                Data = new RegistroChecadaResponseDto
                {
                    Rfc = rfcLimpio,
                    Nombre = empleado.Nombre,
                    Empresa = empleado.RazonSocial,
                    FechaHora = DateTime.Now,
                    DistanciaMetros = Math.Round(distancia, 2),
                    DentroDeRango = !fueraDeRango,
                    Mensaje = "Checada procesada con éxito."
                }
            };
        }

        public async Task<ApiResponse<HistoricoAMNResponse[]>> ConsultarHistoricoAMN(HistoricoAMNDto dto)
        {
            try
            {
                var registros = await _amnRepository.ObtenerHistoricoAmnAsync(dto);
                if (registros == null || registros.Length == 0)
                {
                    return new ApiResponse<HistoricoAMNResponse[]>
                    {
                        Success = true,
                        Message = "No se encontraron registros para los criterios especificados.",
                        Data = Array.Empty<HistoricoAMNResponse>()
                    };
                }
                return new ApiResponse<HistoricoAMNResponse[]>
                {
                    Success = true,
                    Message = "Consulta exitosa.",
                    Data = registros
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<HistoricoAMNResponse[]>
                {
                    Success = false,
                    Message = $"Error al obtener el historial.",
                    Data = Array.Empty<HistoricoAMNResponse>()
                };
            }
        }

        public async Task<(int StatusCode, ApiResponse<ChecadorFisicoResponseDto> Response)> RegistrarChecadaChecadorFisicoAsync(
            ChecadorFisicoRequestDto dto,
            string userAgent)
        {
            if (!_tokenService.ValidarToken(dto.TokenChecadorFisico))
            {
                return (StatusCodes.Status401Unauthorized, new ApiResponse<ChecadorFisicoResponseDto>
                {
                    Success = false,
                    Message = "Token de checador físico inválido o expirado."
                });
            }

            var validacionPasskey = await _passkeyService.ValidarAssertionAsync(dto.Passkey);
            if (!validacionPasskey.Valido || validacionPasskey.Credencial == null)
            {
                return (StatusCodes.Status401Unauthorized, new ApiResponse<ChecadorFisicoResponseDto>
                {
                    Success = false,
                    Message = validacionPasskey.Mensaje
                });
            }

            string rfc = validacionPasskey.Credencial.Rfc.Trim().ToUpperInvariant();
            string fechaHoy = DateTime.Now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            string ultimoMovimiento = await _amnRepository.ObtenerUltimoMovimientoDiaAsync(rfc, fechaHoy);

            string tipoMovimiento;
            if (!string.IsNullOrWhiteSpace(dto.TipoMovimientoOpcional))
            {
                tipoMovimiento = dto.TipoMovimientoOpcional.Trim().ToUpperInvariant();
                if (tipoMovimiento is not ("ENTRADA" or "SALIDA_COMIDA" or "ENTRADA_COMIDA" or "SALIDA"))
                {
                    return (StatusCodes.Status400BadRequest, new ApiResponse<ChecadorFisicoResponseDto>
                    {
                        Success = false,
                        Message = "El tipo de movimiento especificado no es válido."
                    });
                }
            }
            else
            {
                if (ultimoMovimiento == "ENTRADA")
                {
                    return (StatusCodes.Status200OK, new ApiResponse<ChecadorFisicoResponseDto>
                    {
                        Success = true,
                        Message = "Requiere selección de tipo de movimiento.",
                        Data = new ChecadorFisicoResponseDto
                        {
                            RequiereSeleccion = true,
                            Rfc = rfc,
                            Opciones = new[] { "SALIDA_COMIDA", "SALIDA" }
                        }
                    });
                }

                tipoMovimiento = ultimoMovimiento switch
                {
                    "SALIDA_COMIDA" => "ENTRADA_COMIDA",
                    "ENTRADA_COMIDA" => "SALIDA",
                    _ => "ENTRADA"
                };
            }

            string dispositivo = string.IsNullOrWhiteSpace(dto.DispositivoNombre)
                ? validacionPasskey.Credencial.DispositivoNombre
                : dto.DispositivoNombre.Trim();

            var insercion = await _amnRepository.InsertarChecadaChecadorFisicoAsync(
                rfc,
                dto.Latitud,
                dto.Longitud,
                userAgent,
                dispositivo,
                tipoMovimiento
            );

            if (!insercion.Insertado)
            {
                return (StatusCodes.Status400BadRequest, new ApiResponse<ChecadorFisicoResponseDto>
                {
                    Success = false,
                    Message = "No fue posible registrar la checada."
                });
            }

            _subStore.RegistrarMovimiento(rfc, tipoMovimiento);

            return (StatusCodes.Status200OK, new ApiResponse<ChecadorFisicoResponseDto>
            {
                Success = true,
                Message = "Asistencia registrada correctamente.",
                Data = new ChecadorFisicoResponseDto
                {
                    RequiereSeleccion = false,
                    Rfc = rfc,
                    TipoMovimiento = tipoMovimiento,
                    FechaHora = insercion.FechaHora
                }
            });
        }

        public async Task<ApiResponse<UltimoMovimientoMovilDto?>> ObtenerUltimoMovimientoMovilAsync(int desdeNumero)
        {
            var registro = await _amnRepository.ObtenerUltimoMovimientoMovilAsync(desdeNumero);
            return new ApiResponse<UltimoMovimientoMovilDto?>
            {
                Success = true,
                Message = registro == null ? "Sin movimientos recientes." : "Movimiento encontrado.",
                Data = registro
            };
        }
    }
}

