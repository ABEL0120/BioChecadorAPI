using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;
using BioChecadorAPI.Repository;
using System.Text;
using static BioChecadorAPI.Helpers.ChecadorHelper;
using static BioChecadorAPI.Helpers.ResponseHelper;

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
    }

    public class ChecadorService : IChecadorService
    {
        private readonly IAmnRepository _amnRepository;

        public ChecadorService(IAmnRepository amnRepository)
        {
            _amnRepository = amnRepository;
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

            var fueraDeRango = distancia > empleado.RadioToleranciaMetros;
            if (fueraDeRango && empleado.TrabajoRemoto == "N")
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = $"Ubicación fuera del rango permitido. Distancia: {Math.Round(distancia, 2)}m (Máximo: {empleado.RadioToleranciaMetros}m)."
                };
            }

            var ultimoMovimientoHoy = await _amnRepository.ObtenerUltimoMovimientoHoyAsync(rfcLimpio);
            var (esValido, mensajeError) = ValidarTransicion(ultimoMovimientoHoy, dto.TipoMovimiento);
            if (!esValido)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = mensajeError
                };
            }

            var guardado = await _amnRepository.InsertarChecadaAsync(
                rfcLimpio,
                empleado.NumeroCompania,
                dto.Latitud,
                dto.Longitud,
                string.Empty,
                dto.Dispositivo,
                dto.TipoMovimiento,
                empleado.NumeroEmpleado
            );

            if (!guardado)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = "Error al registrar la checada en la base de datos."
                };
            }

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
    }
}