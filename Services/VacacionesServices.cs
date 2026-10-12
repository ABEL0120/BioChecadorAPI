using BioChecadorAPI.Data;
using BioChecadorAPI.DTOs;
using BioChecadorAPI.Repositories;
using static BioChecadorAPI.DTOs.VacacionesDto;
using static BioChecadorAPI.Helpers.ResponseHelper;
using static BioChecadorAPI.Helpers.VacacionesHelper;

namespace BioChecadorAPI.Services
{
    public interface IVacacionesService
    {
        Task<ApiResponse<DatosInicialesVacacionesDto>> ConsultarDatosInicialesAsync(int compania, int m103);
        Task<ApiResponse<CalcularFechasResponseDto>> CalcularFechasAsync(CalcularFechasRequestDto dto);
        Task<ApiResponse<IEnumerable<SolicitudVacacionDto>>> ConsultarHistorialAsync(int compania, int m103);
        Task<ApiResponse<int>> EnviarSolicitudAsync(CrearSolicitudDto dto);
        Task<ApiResponse<bool>> CancelarSolicitudAsync(CancelarSolicitudDto dto);
        Task<ApiResponse<bool>> ActualizarSolicitudAsync(ActualizarSolicitudDto dto);

    }

    public class VacacionesService : IVacacionesService
    {
        private readonly IVacacionesRepository _vacacionesRepository;
        private readonly IEmailData _emailData;

        public VacacionesService(IVacacionesRepository vacacionesRepository, IEmailData emailData)
        {
            _vacacionesRepository = vacacionesRepository;
            _emailData = emailData;
        }

        public async Task<ApiResponse<DatosInicialesVacacionesDto>> ConsultarDatosInicialesAsync(int compania, int m103)
        {
            if (compania <= 0 || m103 <= 0)
                return new ApiResponse<DatosInicialesVacacionesDto> { Success = false, Message = "Parámetros inválidos." };
            try
            {
                var datos = await _vacacionesRepository.ObtenerDatosInicialesAsync(compania, m103);
                if (datos == null)
                    return new ApiResponse<DatosInicialesVacacionesDto> { Success = false, Message = "Empleado no encontrado." };

                return new ApiResponse<DatosInicialesVacacionesDto> { Success = true, Message = "Datos obtenidos.", Data = datos };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DatosInicialesVacacionesDto> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<CalcularFechasResponseDto>> CalcularFechasAsync(CalcularFechasRequestDto dto)
        {
            try
            {
                var inhabiles = await _vacacionesRepository.ObtenerDiasInhabilesAsync(dto.Compania);
                var restringidasStrs = await _vacacionesRepository.ObtenerFechasRestringidasAsync(dto.Compania, DateTime.Now.Year);
                var restringidas = restringidasStrs.Select(r => DateTime.Parse(r)).ToList();
                var (kardex, _) = await _vacacionesRepository.ObtenerKardexYTurnoAsync(dto.Compania, dto.M103);

                if (!DateTime.TryParse(dto.FechaInicio, out DateTime fi))
                    return new ApiResponse<CalcularFechasResponseDto> { Success = false, Message = "Fecha de inicio inválida." };

                DateTime current = fi;
                DateTime ff = fi;
                int count = 0;
                List<DateTime> cruzadas = new List<DateTime>();
                List<string> diasEfectivosCalendario = new List<string>();
                List<string> diasInhabilesCalendario = new List<string>();
                int inicioSemanaCsharp = 6;

                while (count < dto.DiasSolicitados)
                {
                    bool esRestringido = restringidas.Any(r => r.Date == current.Date);
                    bool esValido = EsDiaValido(current, inhabiles, kardex, inicioSemanaCsharp);

                    if (esRestringido)
                    {
                        cruzadas.Add(current);
                        diasInhabilesCalendario.Add(current.ToString("yyyy/MM/dd"));
                    }
                    else if (esValido)
                    {
                        count++;
                        ff = current;
                        diasEfectivosCalendario.Add(current.ToString("yyyy/MM/dd"));
                    }
                    else
                    {
                        diasInhabilesCalendario.Add(current.ToString("yyyy/MM/dd"));
                    }

                    if (count < dto.DiasSolicitados)
                        current = current.AddDays(1);
                }

                DateTime retorno = ff.AddDays(1);
                while (!EsDiaValido(retorno, inhabiles, kardex, inicioSemanaCsharp) || restringidas.Any(r => r.Date == retorno.Date))
                {
                    diasInhabilesCalendario.Add(retorno.ToString("yyyy/MM/dd"));
                    retorno = retorno.AddDays(1);
                }

                var datosIniciales = await _vacacionesRepository.ObtenerDatosInicialesAsync(dto.Compania, dto.M103);
                var periodoSeleccionado = datosIniciales?.PeriodosHistorial.FirstOrDefault(p => p.Periodo == dto.Periodo);
                int diasDisponibles = periodoSeleccionado?.DiasDisponibles ?? 0;
                string tieneAnticipados = "";
                int diasAnticipados = 0;
                string periodoAnticipado = "";

                if (dto.DiasSolicitados > diasDisponibles)
                {
                    tieneAnticipados = "S";
                    diasAnticipados = dto.DiasSolicitados - diasDisponibles;

                    var partes = dto.Periodo.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[0], out int anio1) && int.TryParse(partes[1], out int anio2))
                    {
                        periodoAnticipado = $"{anio1 + 1}-{anio2 + 1}";
                    }
                }

                return new ApiResponse<CalcularFechasResponseDto>
                {
                    Success = true,
                    Message = "Cálculo exitoso.",
                    Data = new CalcularFechasResponseDto
                    {
                        FechaFin = ff.ToString("yyyy/MM/dd"),
                        FechaRetorno = retorno.ToString("yyyy/MM/dd"),
                        CruzaRestringidas = cruzadas.Any(),
                        FechasRestringidasCruzadas = string.Join(",", cruzadas.Select(c => c.ToString("yyyy/MM/dd"))),
                        TieneDiasAnticipados = tieneAnticipados,
                        DiasAnticipados = diasAnticipados,
                        PeriodoAnticipado = periodoAnticipado,
                        DiasEfectivos = diasEfectivosCalendario,
                        DiasInhabiles = diasInhabilesCalendario
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<CalcularFechasResponseDto> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }


        public async Task<ApiResponse<IEnumerable<SolicitudVacacionDto>>> ConsultarHistorialAsync(int compania, int m103)
        {
            if (compania <= 0 || m103 <= 0)
            {
                return new ApiResponse<IEnumerable<SolicitudVacacionDto>>
                {
                    Success = false,
                    Message = "Los parámetros Compañía y M103 son obligatorios.",
                    Data = null
                };
            }

            try
            {
                var historial = await _vacacionesRepository.ObtenerHistorialAsync(compania, m103);
                if (historial == null || !historial.Any())
                {
                    return new ApiResponse<IEnumerable<SolicitudVacacionDto>>
                    {
                        Success = true,
                        Message = "El empleado no cuenta con historial.",
                        Data = Array.Empty<SolicitudVacacionDto>()
                    };
                }

                return new ApiResponse<IEnumerable<SolicitudVacacionDto>>
                {
                    Success = true,
                    Message = "Historial consultado con éxito.",
                    Data = historial
                };
            }
            catch (Exception)
            {
                return new ApiResponse<IEnumerable<SolicitudVacacionDto>>
                {
                    Success = false,
                    Message = "Error al intentar obtener el historial."
                };
            }
        }

        public async Task<ApiResponse<int>> EnviarSolicitudAsync(CrearSolicitudDto dto)
        {
            try
            {
                var (kardex, turno) = await _vacacionesRepository.ObtenerKardexYTurnoAsync(dto.Compania, dto.M103);
                int ultimoFolioGenerado = 0;
                int inicioSemanaCsharp = 6;
                if (dto.TieneRestriccion == "S" && !string.IsNullOrWhiteSpace(dto.FechasRestringidas))
                {
                    var fechasRestringidasArray = dto.FechasRestringidas.Split(',').Select(f => DateTime.Parse(f)).ToList();
                    var inhabiles = await _vacacionesRepository.ObtenerDiasInhabilesAsync(dto.Compania);
                    DateTime fiGlobal = DateTime.Parse(dto.FechaInicio);
                    DateTime ffGlobal = DateTime.Parse(dto.FechaFin);
                    DateTime frGlobal = DateTime.Parse(dto.FechaRetorno);
                    DateTime current = fiGlobal;
                    foreach (var fechaRestringida in fechasRestringidasArray.OrderBy(d => d))
                    {
                        if (current >= fechaRestringida) continue;
                        int diasBloque = 0;
                        DateTime fiBloque = current;
                        DateTime ffBloque = current;
                        while (current < fechaRestringida && current <= ffGlobal)
                        {
                            if (EsDiaValido(current, inhabiles, kardex, inicioSemanaCsharp))
                            {
                                if (diasBloque == 0) fiBloque = current;
                                diasBloque++;
                                ffBloque = current;
                            }
                            current = current.AddDays(1);
                        }
                        if (diasBloque > 0)
                        {
                            var subDto = ClonarDtoParaBloque(dto, fiBloque, ffBloque, fechaRestringida, diasBloque);
                            ultimoFolioGenerado = await _vacacionesRepository.CrearSolicitudAsync(subDto, turno, kardex);
                            EnviarCorreoNotificacion(ultimoFolioGenerado, subDto, _emailData.Email, _emailData.PasswordApp);
                        }
                        current = fechaRestringida.AddDays(1);
                    }
                    if (current <= ffGlobal)
                    {
                        int diasFinales = 0;
                        DateTime fiFinal = current;
                        DateTime ffFinal = current;
                        while (current <= ffGlobal)
                        {
                            if (EsDiaValido(current, inhabiles, kardex, inicioSemanaCsharp))
                            {
                                if (diasFinales == 0) fiFinal = current;
                                diasFinales++;
                                ffFinal = current;
                            }
                            current = current.AddDays(1);
                        }
                        if (diasFinales > 0)
                        {
                            var subDto = ClonarDtoParaBloque(dto, fiFinal, ffFinal, frGlobal, diasFinales);
                            ultimoFolioGenerado = await _vacacionesRepository.CrearSolicitudAsync(subDto, turno, kardex);
                            EnviarCorreoNotificacion(ultimoFolioGenerado, subDto, _emailData.Email, _emailData.PasswordApp);
                        }
                    }
                }
                else
                {
                    ultimoFolioGenerado = await _vacacionesRepository.CrearSolicitudAsync(dto, turno, kardex);
                    EnviarCorreoNotificacion(ultimoFolioGenerado, dto, _emailData.Email, _emailData.PasswordApp);
                }
                return new ApiResponse<int>
                {
                    Success = true,
                    Message = "Solicitud enviada correctamente. Se ha notificado por correo.",
                    Data = ultimoFolioGenerado
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = $"Error interno al procesar la solicitud: {ex.Message}",
                    Data = 0
                };
            }
        }


        public async Task<ApiResponse<bool>> CancelarSolicitudAsync(CancelarSolicitudDto dto)
        {
            if (dto.Compania <= 0 || dto.M103 <= 0 || dto.Numero <= 0)
            {
                return new ApiResponse<bool> { Success = false, Message = "Parámetros inválidos." };
            }

            try
            {
                bool cancelada = await _vacacionesRepository.CancelarSolicitudAsync(dto);

                if (!cancelada)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "No se pudo cancelar. Es posible que ya esté autorizada o cancelada."
                    };
                }

                return new ApiResponse<bool> { Success = true, Message = "Solicitud cancelada con éxito.", Data = true };
            }
            catch (Exception)
            {
                return new ApiResponse<bool> { Success = false, Message = "Error al intentar cancelar." };
            }
        }

        public async Task<ApiResponse<bool>> ActualizarSolicitudAsync(ActualizarSolicitudDto dto)
        {
            if (dto.Compania <= 0 || dto.M103 <= 0 || dto.Numero <= 0)
                return new ApiResponse<bool> { Success = false, Message = "Parámetros inválidos." };

            try
            {
                bool actualizada = await _vacacionesRepository.ActualizarSolicitudAsync(dto);

                if (!actualizada)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "No se pudo actualizar la solicitud. Es posible que ya esté autorizada o cancelada."
                    };
                }

                return new ApiResponse<bool> { Success = true, Message = "Solicitud actualizada con éxito.", Data = true };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Error al actualizar: {ex.Message}" };
            }
        }

    }
}
