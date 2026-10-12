using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using static BioChecadorAPI.DTOs.VacacionesDto;

namespace BioChecadorAPI.Helpers
{
    public static class VacacionesHelper
    {
        public static bool EsDiaValido(DateTime fecha, List<DateTime> inhabiles, string kardex, int inicioSemanaCsharp)
        {
            if (inhabiles.Any(d => d.Date == fecha.Date)) return false;

            if (!string.IsNullOrEmpty(kardex) && kardex.Length >= 7)
            {
                int dayIndex = ((int)fecha.DayOfWeek - inicioSemanaCsharp + 7) % 7;
                char k = kardex[dayIndex];
                if (k == 'D' || k == 'd') return false;
            }
            return true;
        }



        public static CrearSolicitudDto ClonarDtoParaBloque(CrearSolicitudDto original, DateTime fi, DateTime ff, DateTime fr, int dias)
        {
            return new CrearSolicitudDto
            {
                Compania = original.Compania,
                M103 = original.M103,
                NombreEmpleado = original.NombreEmpleado,
                Periodo = original.Periodo,
                CorreoEmpleado = original.CorreoEmpleado,
                FechaInicio = fi.ToString("yyyy/MM/dd"),
                FechaFin = ff.ToString("yyyy/MM/dd"),
                FechaRetorno = fr.ToString("yyyy/MM/dd"),
                DiasSolicitados = dias,
                ComentariosEmpleado = original.ComentariosEmpleado,
                JefeInmediato = original.JefeInmediato,
                TieneRestriccion = original.TieneRestriccion,
                FechasRestringidas = original.FechasRestringidas,
                TieneDiasAnticipados = original.TieneDiasAnticipados,
                DiasAnticipados = original.DiasAnticipados,
                PeriodoAnticipado = original.PeriodoAnticipado,
                FechasAnticipadas = original.FechasAnticipadas
            };
        }

        public static void EnviarCorreoNotificacion(int folio, CrearSolicitudDto dto, string emailOrigen, string passwordAppOrigen)
        {
            Task.Run(() =>
            {
                try
                {
                    string diasAnticipadosHtml = "";
                    if (dto.TieneDiasAnticipados == "S" && dto.DiasAnticipados > 0)
                    {
                        diasAnticipadosHtml = $@"
                        <div style='background-color: #fff3cd; border-left: 4px solid #ffeeba; padding: 15px; margin-top: 20px; border-radius: 4px;'>
                            <p style='margin: 0; color: #856404; font-size: 14px;'>
                                <strong>Aviso Importante:</strong> La solicitud comprende <b>{dto.DiasAnticipados} Días Anticipados</b> para el siguiente periodo.
                            </p>
                        </div>";
                    }

                    string mensaje = $@"
                    <div style='font-family: ""Segoe UI"", Arial, sans-serif; background-color: #f4f7f6; padding: 40px 20px; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                            
                            <div style='background-color: #2563eb; padding: 25px; text-align: center;'>
                                <h2 style='margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;'>Solicitud de Vacaciones</h2>
                            </div>
                            
                            <div style='padding: 30px;'>
                                <p style='font-size: 16px; line-height: 1.5; color: #4b5563; margin-top: 0;'>
                                    Se ha registrado una nueva solicitud en el sistema. A continuación se detallan los datos:
                                </p>
                                
                                <table style='width: 100%; border-collapse: collapse; margin: 25px 0;'>
                                    <tr>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; width: 40%; color: #6b7280; font-weight: 600; font-size: 14px;'>Folio</td>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-size: 15px; font-weight: 600;'>#{folio}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280; font-weight: 600; font-size: 14px;'>Empleado</td>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-size: 15px;'>{dto.M103} - {dto.NombreEmpleado}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280; font-weight: 600; font-size: 14px;'>Periodo</td>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-size: 15px;'>{dto.Periodo}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #6b7280; font-weight: 600; font-size: 14px;'>Fechas Solicitadas</td>
                                        <td style='padding: 12px 0; border-bottom: 1px solid #e5e7eb; color: #111827; font-size: 15px;'>Del {dto.FechaInicio} al {dto.FechaFin}</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 12px 0; color: #6b7280; font-weight: 600; font-size: 14px;'>Día de Retorno</td>
                                        <td style='padding: 12px 0; color: #111827; font-size: 15px;'>{dto.FechaRetorno}</td>
                                    </tr>
                                </table>

                                {diasAnticipadosHtml}

                                <div style='text-align: center; margin-top: 35px; margin-bottom: 15px;'>
                                    <a href='http://interaccionoperativa.com/Nom_Web' style='background-color: #2563eb; color: #ffffff; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 15px; display: inline-block;'>
                                        Ver Solicitud en el Sistema
                                    </a>
                                </div>
                            </div>
                            
                            <div style='background-color: #f9fafb; border-top: 1px solid #f3f4f6; padding: 20px; text-align: center;'>
                                <p style='margin: 0; font-size: 12px; color: #9ca3af;'>
                                    Este es un mensaje autogenerado por el módulo de nómina. Por favor, no respondas a este correo.
                                </p>
                            </div>

                        </div>
                    </div>";

                    var mail = new MailMessage
                    {
                        From = new MailAddress(emailOrigen, "Recursos Humanos", Encoding.UTF8),
                        Subject = $"Solicitud de Vacaciones #{folio} - {dto.NombreEmpleado}",
                        IsBodyHtml = true
                    };

                    mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(mensaje, Encoding.UTF8, MediaTypeNames.Text.Html));
                    mail.To.Add(dto.CorreoEmpleado);

                    using var smtp = new SmtpClient("smtp.googlemail.com", 587)
                    {
                        Credentials = new System.Net.NetworkCredential(emailOrigen, passwordAppOrigen),
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network
                    };

                    smtp.Send(mail);
                }
                catch (Exception)
                {
                }
            });
        }
    }
}