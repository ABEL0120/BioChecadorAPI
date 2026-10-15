using BioChecadorAPI.DTOs;
using BioChecadorAPI.Helpers;
using BioChecadorAPI.Repositories;
using System.Globalization;
using static BioChecadorAPI.DTOs.PermisosDto;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Services
{
    public interface IPermisosService
    {
        Task<ApiResponse<DatosInicialesPermisosDto>> ConsultarDatosInicialesAsync(int compania, int m103);
        Task<ApiResponse<CalcularTiempoPermisoResponseDto>> CalcularTiempoAsync(CalcularTiempoPermisoRequestDto dto);
        Task<ApiResponse<IEnumerable<PermisoEmpleadoDto>>> ConsultarHistorialAsync(int compania, int m103);
        Task<ApiResponse<int>> SolicitarPermisoAsync(CrearPermisoDto dto);
        Task<ApiResponse<bool>> ActualizarPermisoAsync(ActualizarPermisoDto dto);
        Task<ApiResponse<bool>> CancelarPermisoAsync(CancelarPermisoDto dto);
    }

    public class PermisosService : IPermisosService
    {
        private readonly IPermisosRepository _permisosRepository;

        public PermisosService(IPermisosRepository permisosRepository)
        {
            _permisosRepository = permisosRepository;
        }

        public async Task<ApiResponse<DatosInicialesPermisosDto>> ConsultarDatosInicialesAsync(int compania, int m103)
        {
            if (compania <= 0 || m103 <= 0)
            {
                return new ApiResponse<DatosInicialesPermisosDto>
                {
                    Success = false,
                    Message = "Parámetros inválidos."
                };
            }

            try
            {
                var datos = await _permisosRepository.ObtenerDatosInicialesAsync(compania, m103);
                if (datos == null)
                {
                    return new ApiResponse<DatosInicialesPermisosDto>
                    {
                        Success = false,
                        Message = "Empleado no encontrado."
                    };
                }

                var turno = await _permisosRepository.ObtenerTurnoFilaAsync(compania, m103);
                if (turno != null)
                {
                    DateTime hoy = DateTime.Today;
                    if (PermisosHelper.ObtenerHorarioDia(turno, hoy, out DateTime? ent, out DateTime? sal, out DateTime? cIni, out DateTime? cFin))
                    {
                        if (ent.HasValue && sal.HasValue)
                        {
                            datos.HorarioHoy = $"{ent.Value:HH:mm} - {sal.Value:HH:mm}";
                        }
                        if (cIni.HasValue && cFin.HasValue)
                        {
                            datos.ComidaHoy = $"{cIni.Value:HH:mm} - {cFin.Value:HH:mm}";
                        }
                    }
                }

                return new ApiResponse<DatosInicialesPermisosDto>
                {
                    Success = true,
                    Message = "Datos iniciales obtenidos con éxito.",
                    Data = datos
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DatosInicialesPermisosDto>
                {
                    Success = false,
                    Message = $"Error al obtener datos iniciales: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<CalcularTiempoPermisoResponseDto>> CalcularTiempoAsync(CalcularTiempoPermisoRequestDto dto)
        {
            string formato = "yyyy/MM/dd HH:mm";
            string fechaHoraIniStr = $"{dto.FechaInicio.Trim()} {dto.HoraInicio.Trim()}";
            string fechaHoraFinStr = $"{dto.FechaFin.Trim()} {dto.HoraFin.Trim()}";

            if (!DateTime.TryParseExact(fechaHoraIniStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d1) ||
                !DateTime.TryParseExact(fechaHoraFinStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d2))
            {
                return new ApiResponse<CalcularTiempoPermisoResponseDto>
                {
                    Success = false,
                    Message = "Formato de fecha u hora inválido.",
                    Data = new CalcularTiempoPermisoResponseDto
                    {
                        EsValido = false,
                        ErrorValidacion = "Formato de fecha u hora inválido."
                    }
                };
            }

            int minutosBrutos = (int)(d2 - d1).TotalMinutes;
            if (minutosBrutos <= 0)
            {
                return new ApiResponse<CalcularTiempoPermisoResponseDto>
                {
                    Success = true,
                    Message = "La hora o fecha de fin debe ser posterior a la de inicio.",
                    Data = new CalcularTiempoPermisoResponseDto
                    {
                        EsValido = false,
                        ErrorValidacion = "La hora de fin debe ser posterior a la hora de inicio.",
                        TiempoTexto = "0h 0m"
                    }
                };
            }

            var turno = await _permisosRepository.ObtenerTurnoFilaAsync(dto.Compania, dto.M103);
            int minutosComida = PermisosHelper.CalcularMinutosComida(turno, d1, d2);
            int minutosNetos = minutosBrutos - minutosComida;

            if (minutosNetos <= 0)
            {
                return new ApiResponse<CalcularTiempoPermisoResponseDto>
                {
                    Success = true,
                    Message = "El horario queda dentro de la hora de comida de tu turno.",
                    Data = new CalcularTiempoPermisoResponseDto
                    {
                        EsValido = false,
                        ErrorValidacion = "El horario capturado queda dentro de tu hora de comida; no genera tiempo de permiso.",
                        MinutosBrutos = minutosBrutos,
                        MinutosComidaDescontados = minutosComida,
                        MinutosNetos = 0,
                        TiempoTexto = "0h 0m (comida)",
                        CruzaComida = true
                    }
                };
            }

            bool permiteVacaciones = minutosNetos >= 420;
            string detalle = minutosComida > 0
                ? $"Se descontaron {minutosComida} minutos por horario de comida."
                : string.Empty;

            var res = new CalcularTiempoPermisoResponseDto
            {
                EsValido = true,
                MinutosBrutos = minutosBrutos,
                MinutosComidaDescontados = minutosComida,
                MinutosNetos = minutosNetos,
                TiempoTexto = PermisosHelper.FormatearTiempo(minutosNetos),
                PermiteVacaciones = permiteVacaciones,
                CruzaComida = minutosComida > 0,
                MensajeDetalle = detalle
            };

            return new ApiResponse<CalcularTiempoPermisoResponseDto>
            {
                Success = true,
                Message = "Cálculo realizado con éxito.",
                Data = res
            };
        }

        public async Task<ApiResponse<IEnumerable<PermisoEmpleadoDto>>> ConsultarHistorialAsync(int compania, int m103)
        {
            if (compania <= 0 || m103 <= 0)
            {
                return new ApiResponse<IEnumerable<PermisoEmpleadoDto>>
                {
                    Success = false,
                    Message = "Parámetros inválidos."
                };
            }

            try
            {
                var historial = await _permisosRepository.ObtenerHistorialAsync(compania, m103);
                return new ApiResponse<IEnumerable<PermisoEmpleadoDto>>
                {
                    Success = true,
                    Message = "Historial consultado con éxito.",
                    Data = historial
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<IEnumerable<PermisoEmpleadoDto>>
                {
                    Success = false,
                    Message = $"Error al consultar historial: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<int>> SolicitarPermisoAsync(CrearPermisoDto dto)
        {
            try
            {
                string hoyStr = DateTime.Today.ToString("yyyy/MM/dd");
                if (string.Compare(dto.FechaInicio.Trim(), hoyStr, StringComparison.Ordinal) < 0)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "No se pueden registrar permisos con fecha anterior al día de hoy."
                    };
                }

                string formato = "yyyy/MM/dd HH:mm";
                string fechaHoraIniStr = $"{dto.FechaInicio.Trim()} {dto.HoraInicio.Trim()}";
                string fechaHoraFinStr = $"{dto.FechaFin.Trim()} {dto.HoraFin.Trim()}";

                if (!DateTime.TryParseExact(fechaHoraIniStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d1) ||
                    !DateTime.TryParseExact(fechaHoraFinStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d2))
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Formato de fechas u horas incorrecto."
                    };
                }

                int minutosBrutos = (int)(d2 - d1).TotalMinutes;
                if (minutosBrutos <= 0)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "La hora y fecha de fin deben ser posteriores a las de inicio."
                    };
                }

                var turno = await _permisosRepository.ObtenerTurnoFilaAsync(dto.Compania, dto.M103);
                int minutosComida = PermisosHelper.CalcularMinutosComida(turno, d1, d2);
                int minutosNetos = minutosBrutos - minutosComida;

                if (minutosNetos <= 0)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "El horario capturado queda completamente dentro de la hora de comida de tu turno."
                    };
                }

                bool tieneTraslape = await _permisosRepository.ExisteTraslapePermisoAsync(
                    dto.Compania, dto.M103, dto.FechaInicio, dto.HoraInicio, dto.FechaFin, dto.HoraFin, 0);

                if (tieneTraslape)
                {
                    return new ApiResponse<int>
                    {
                        Success = false,
                        Message = "Ya tienes un permiso registrado que se empalma con este horario."
                    };
                }

int nuevoNumero = await _permisosRepository.CrearPermisoAsync(dto, minutosNetos, 0);
                return new ApiResponse<int>
                {
                    Success = true,
                    Message = $"Permiso solicitado exitosamente con folio #{nuevoNumero}. Se encuentra en espera de autorización.",
                    Data = nuevoNumero
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = $"Error al registrar permiso: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> ActualizarPermisoAsync(ActualizarPermisoDto dto)
        {
            try
            {
                var estatus = await _permisosRepository.ObtenerEstatusPermisoAsync(dto.Compania, dto.M103, dto.Numero);
                if (string.IsNullOrWhiteSpace(estatus))
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "El permiso no existe."
                    };
                }

                if (estatus.ToUpperInvariant() != "PENDIENTE")
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Solo pueden modificarse los permisos que aún están en estatus PENDIENTE."
                    };
                }

                string formato = "yyyy/MM/dd HH:mm";
                string fechaHoraIniStr = $"{dto.FechaInicio.Trim()} {dto.HoraInicio.Trim()}";
                string fechaHoraFinStr = $"{dto.FechaFin.Trim()} {dto.HoraFin.Trim()}";

                if (!DateTime.TryParseExact(fechaHoraIniStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d1) ||
                    !DateTime.TryParseExact(fechaHoraFinStr, formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d2))
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Formato de fechas u horas incorrecto."
                    };
                }

                int minutosBrutos = (int)(d2 - d1).TotalMinutes;
                if (minutosBrutos <= 0)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "La hora y fecha de fin deben ser posteriores a las de inicio."
                    };
                }

                var turno = await _permisosRepository.ObtenerTurnoFilaAsync(dto.Compania, dto.M103);
                int minutosComida = PermisosHelper.CalcularMinutosComida(turno, d1, d2);
                int minutosNetos = minutosBrutos - minutosComida;

                if (minutosNetos <= 0)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "El horario capturado queda completamente dentro de la hora de comida de tu turno."
                    };
                }

                bool tieneTraslape = await _permisosRepository.ExisteTraslapePermisoAsync(
                    dto.Compania, dto.M103, dto.FechaInicio, dto.HoraInicio, dto.FechaFin, dto.HoraFin, dto.Numero);

                if (tieneTraslape)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Ya tienes otro permiso registrado que se empalma con este nuevo horario."
                    };
                }



                bool actualizado = await _permisosRepository.ActualizarPermisoAsync(dto, minutosNetos);
                return new ApiResponse<bool>
                {
                    Success = actualizado,
                    Message = actualizado ? "Permiso actualizado con éxito." : "No se pudo actualizar el permiso.",
                    Data = actualizado
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error al actualizar permiso: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> CancelarPermisoAsync(CancelarPermisoDto dto)
        {
            try
            {
                var estatus = await _permisosRepository.ObtenerEstatusPermisoAsync(dto.Compania, dto.M103, dto.Numero);
                if (string.IsNullOrWhiteSpace(estatus))
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "El permiso no existe."
                    };
                }

                if (estatus.ToUpperInvariant() != "PENDIENTE")
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Solo pueden cancelarse los permisos que aún están en estatus PENDIENTE."
                    };
                }

                bool cancelado = await _permisosRepository.CancelarPermisoAsync(dto.Compania, dto.M103, dto.Numero);
                return new ApiResponse<bool>
                {
                    Success = cancelado,
                    Message = cancelado ? "Permiso cancelado con éxito." : "No se pudo cancelar el permiso.",
                    Data = cancelado
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error al cancelar permiso: {ex.Message}"
                };
            }
        }
    }
}
