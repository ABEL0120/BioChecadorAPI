using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Services
{
    public interface IChecadorService
    {
        Task<ApiResponse<EstadoEmpleadoResponseDto>> VerificarRfcAsync(ConsultaRequestDto dto);
        Task<ApiResponse<bool>> EnrolarBiometriaAsync(EnrolarBiometriaDto dto);
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
    }
}
