using BioChecadorAPI.DTOs;
using BioChecadorAPI.Helpers;
using BioChecadorAPI.Models;
using BioChecadorAPI.Repository;
using static BioChecadorAPI.Helpers.ResponseHelper;

namespace BioChecadorAPI.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<UsuarioResponseDto>> RegistrarAsync(RegistroUsuarioDto dto);
        Task<ApiResponse<UsuarioResponseDto>> LoginAsync(LoginUsuarioDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private static string HashearClave(string clave) => AuthHelper.HashearClave(clave);

        public AuthService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ApiResponse<UsuarioResponseDto>> RegistrarAsync(RegistroUsuarioDto dto)
        {
            var correoNormalizado = dto.Correo.Trim().ToLowerInvariant();
            var existe = await _usuarioRepository.ExisteCorreo(correoNormalizado);
            if (existe)
            {
                return new ApiResponse<UsuarioResponseDto>
                {
                    Success = false,
                    Message = "El correo ya se encuentra registrado."
                };
            }

            var nuevoNumero = await _usuarioRepository.ObtenerSiguienteNumero();
            var nuevoUsuario = new Usuario           
            {
                Numero = nuevoNumero,
                Nombre = dto.Nombre.Trim(),
                Correo = correoNormalizado,
                User = dto.User.Trim(),
                Clave_Seguridad = HashearClave(dto.Clave_Seguridad),
                Baja = string.Empty
            };

            var guardado = await _usuarioRepository.InsertarUsuario(nuevoUsuario);
            if (!guardado)
            {
                return new ApiResponse<UsuarioResponseDto>
                {
                    Success = false,
                    Message = "Error al guardar el usuario en la base de datos."
                };
            }

            return new ApiResponse<UsuarioResponseDto>
            {
                Success = true,
                Message = "Usuario registrado exitosamente.",
                Data = new UsuarioResponseDto
                {
                    Numero = nuevoUsuario.Numero,
                    Nombre = nuevoUsuario.Nombre,
                    Correo = nuevoUsuario.Correo,
                    User = nuevoUsuario.User
                }
            };
        }

        public async Task<ApiResponse<UsuarioResponseDto>> LoginAsync(LoginUsuarioDto dto)
        {
            var correoNormalizado = dto.Correo.Trim().ToLowerInvariant();
            var usuario = new Usuario { Correo = correoNormalizado };
            var usuarioExistente = await _usuarioRepository.ObtenerCorreo(usuario);
            if (usuarioExistente == null)
            {
                return new ApiResponse<UsuarioResponseDto>
                {
                    Success = false,
                    Message = "Credenciales Incorrectas."
                };
            }
            var claveHasheada = HashearClave(dto.Clave_Seguridad);
            if (usuarioExistente.Clave_Seguridad != claveHasheada)
            {
                return new ApiResponse<UsuarioResponseDto>
                {
                    Success = false,
                    Message = "Credenciales Incorrectas."
                };
            }
            return new ApiResponse<UsuarioResponseDto>
            {
                Success = true,
                Message = "Login exitoso.",
                Data = new UsuarioResponseDto
                {
                    Numero = usuarioExistente.Numero,
                    Nombre = usuarioExistente.Nombre,
                    Correo = usuarioExistente.Correo,
                    User = usuarioExistente.User
                }
            };
        }
    }
}