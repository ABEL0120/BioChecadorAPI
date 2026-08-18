using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;
using static BioChecadorAPI.Helpers.ResponseHelper;
using static BioChecadorAPI.Helpers.CaculatorHelper;


namespace BioChecadorAPI.Services
{
    public interface IChecadorService
    {
        Task<ApiResponse<EstadoEmpleadoResponseDto>> VerificarRfcAsync(ConsultaRequestDto dto);
        Task<ApiResponse<bool>> EnrolarBiometriaAsync(EnrolarBiometriaDto dto);
        Task<ApiResponse<RegistroChecadaResponseDto>> MarcarAsistenciaAsync(MarcarAsistenciaDto dto);
    }

    public class ChecadorService : IChecadorService
    {
        private readonly IAmnRepository _amnRepository;

        public ChecadorService(IAmnRepository amnRepository)
        {
            _amnRepository = amnRepository;
        }

        public async Task<ApiResponse<EstadoEmpleadoResponseDto>> VerificarRfcAsync(ConsultaRequestDto dto)
        {
            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();
            var estado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio);
            if (estado == null)
            {
                return new ApiResponse<EstadoEmpleadoResponseDto>
                {
                    Success = false,
                    Message = "Empleado no registrado",
                    Data = new EstadoEmpleadoResponseDto
                    {
                        Existe = false,
                        TieneBiometria = false,
                        Mensaje = "Empleado no registrado",
                        Rfc = rfcLimpio
                    }
                };
            }

            estado.Mensaje = estado.TieneBiometria
                ? "Empleado verificado, listo para marcar asistencia."
                : "Empleado encontrado sin biometría registrada. Se requiere enrolamiento.";

            return new ApiResponse<EstadoEmpleadoResponseDto>
            {
                Success = true,
                Message = estado.Mensaje,
                Data = estado
            };
        }

        public async Task<ApiResponse<bool>> EnrolarBiometriaAsync(EnrolarBiometriaDto dto)
        {
            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();

            var empleado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio);
            if (empleado == null)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Empleado no registrado.",
                    Data = false
                };
            }

            if (empleado.TieneBiometria)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "El empleado ya cuenta con biometría registrada.",
                    Data = false
                };
            }

            var guardado = await _amnRepository.GuardarBiometriaAsync(
                rfcLimpio,
                dto.CredentialId.Trim(),
                dto.PublicKey.Trim(),
                dto.Dispositivo
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

        public async Task<ApiResponse<RegistroChecadaResponseDto>> MarcarAsistenciaAsync(MarcarAsistenciaDto dto)
        {
            var rfcLimpio = dto.Rfc.Trim().ToUpperInvariant();

            var empleado = await _amnRepository.ConsultarEstadoPorRfcAsync(rfcLimpio);
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
            if (fueraDeRango)
            {
                return new ApiResponse<RegistroChecadaResponseDto>
                {
                    Success = false,
                    Message = $"Ubicación fuera del rango permitido. Distancia: {Math.Round(distancia, 2)}m (Máximo: {empleado.RadioToleranciaMetros}m)."
                };
            }

            var guardado = await _amnRepository.InsertarChecadaAsync(
                rfcLimpio,
                empleado.NumeroCompania,
                dto.Latitud,
                dto.Longitud,
                dto.Dispositivo,
                dto.TipoMovimiento,
                distancia,
                fueraDeRango
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
    }
}
