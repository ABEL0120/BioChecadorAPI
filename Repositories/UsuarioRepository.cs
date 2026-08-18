using BioChecadorAPI.Data;
using BioChecadorAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BioChecadorAPI.Repository
{
    public interface IUsuarioRepository
    {
        Task<bool> ExisteCorreo(string correo);
        Task<int> ObtenerSiguienteNumero();
        Task<bool> InsertarUsuario(Usuario usuario);
        Task<Usuario?> ObtenerCorreo(Usuario usuario);
    }

    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly IDbConnectionData _connectionFactory;

        public UsuarioRepository(IDbConnectionData connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ExisteCorreo(string correo)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = "SELECT COUNT(1) FROM Usuarios WHERE Correo = @correo AND Baja = ''";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@correo", SqlDbType.VarChar, 255).Value = correo;
            await conn.OpenAsync();
            var count = (int?)await cmd.ExecuteScalarAsync() ?? 0;
            return count > 0;
        }

        public async Task<int> ObtenerSiguienteNumero()
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = "SELECT ISNULL(MAX(Numero), 0) + 1 FROM Usuarios";
            using var cmd = new SqlCommand(query, conn);
            await conn.OpenAsync();
            return (int)await cmd.ExecuteScalarAsync()!;
        }

        public async Task<bool> InsertarUsuario(Usuario usuario)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"INSERT INTO Usuarios (Numero, Nombre, Clave_Seguridad, Correo, Usuario, Baja)
                VALUES (@numero, @nombre, @clave, @correo, @usuario, @baja)";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@numero", SqlDbType.Int).Value = usuario.Numero;
            cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = usuario.Nombre;
            cmd.Parameters.Add("@clave", SqlDbType.VarChar, 255).Value = usuario.Clave_Seguridad;
            cmd.Parameters.Add("@correo", SqlDbType.VarChar, 255).Value = usuario.Correo;
            cmd.Parameters.Add("@usuario", SqlDbType.VarChar, 10).Value = usuario.User;
            cmd.Parameters.Add("@baja", SqlDbType.VarChar, 1).Value = usuario.Baja;

            await conn.OpenAsync();
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<Usuario?> ObtenerCorreo(Usuario usuario)
        {
            using var conn = (SqlConnection)_connectionFactory.CreateConnection();
            const string query = @"SELECT Numero, Nombre, Usuario, Correo FROM Usuarios WHERE Correo = @correo AND Baja <> '*'";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.Add("@correo", SqlDbType.VarChar, 255).Value = usuario.Correo;
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }
            return new Usuario
            {
                Numero = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Correo = reader.GetString(3),
                User = reader.GetString(4)
            };
        }
    }
}
