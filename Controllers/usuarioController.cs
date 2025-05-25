using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using apiClassroom.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using static apiClassroom.Models.Clases;

namespace apiClassroom.Controllers
{
    [RoutePrefix("api/usuarios")]
    public class UsuariosController : ApiController
    {
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";

        // CREATE: Insertar nuevo usuario
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CrearUsuario([FromBody] UsuarioRequest nuevoUsuario)
        {
            if (string.IsNullOrWhiteSpace(nuevoUsuario.nombre) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.email) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.password) ||
                nuevoUsuario.rol <= 0)
            {
                return BadRequest("Datos inválidos para crear usuario.");
            }

            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string insertQuery = @"
                        INSERT INTO dbo.Usuarios (nombre, email, password, intentos_fallidos, cuenta_bloqueada, fecha_cad_usuario, rol)
                        VALUES (@nombre, @email, @password, 0, 0, NULL, @rol)";

                    using (var comando = new SqlCommand(insertQuery, conexion))
                    {
                        comando.Parameters.AddWithValue("@nombre", nuevoUsuario.nombre);
                        comando.Parameters.AddWithValue("@email", nuevoUsuario.email);
                        comando.Parameters.AddWithValue("@password", Encriptar(nuevoUsuario.password));
                        comando.Parameters.AddWithValue("@rol", nuevoUsuario.rol);

                        comando.ExecuteNonQuery();
                    }
                }

                return Ok(new { estado = "ok", mensaje = "Usuario creado correctamente." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // READ: Obtener todos los usuarios
        [HttpGet]
        [Route("all")]
        public IHttpActionResult ObtenerUsuarios()
        {
            var listaUsuarios = new List<UsuarioResponse>();
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();
                    const string query = "SELECT id, nombre, email, rol FROM dbo.Usuarios";
                    using (var comando = new SqlCommand(query, conexion))
                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            listaUsuarios.Add(new UsuarioResponse
                            {
                                id = (int)lector["id"],
                                nombre = lector["nombre"].ToString(),
                                email = lector["email"].ToString(),
                                rol = (int)lector["rol"]
                            });
                        }
                    }
                }
                return Ok(listaUsuarios);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // UPDATE: Actualizar usuario por ID
        [HttpPut]
        [Route("update/{id}")]
        public IHttpActionResult ActualizarUsuario(int id, [FromBody] UsuarioRequest usuario)
        {
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();
                    const string updateQuery = @"
                        UPDATE dbo.Usuarios
                        SET nombre = @nombre, email = @email, password = @password, rol = @rol
                        WHERE id = @id";

                    using (var comando = new SqlCommand(updateQuery, conexion))
                    {
                        comando.Parameters.AddWithValue("@id", id);
                        comando.Parameters.AddWithValue("@nombre", usuario.nombre);
                        comando.Parameters.AddWithValue("@email", usuario.email);
                        comando.Parameters.AddWithValue("@password", Encriptar(usuario.password));
                        comando.Parameters.AddWithValue("@rol", usuario.rol);

                        int filas = comando.ExecuteNonQuery();
                        if (filas == 0)
                            return NotFound();
                    }
                }
                return Ok(new { estado = "ok", mensaje = "Usuario actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // DELETE: Eliminar usuario por ID
        [HttpDelete]
        [Route("delete/{id}")]
        public IHttpActionResult EliminarUsuario(int id)
        {
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();
                    const string deleteQuery = "DELETE FROM dbo.Usuarios WHERE id = @id";
                    using (var comando = new SqlCommand(deleteQuery, conexion))
                    {
                        comando.Parameters.AddWithValue("@id", id);
                        int filas = comando.ExecuteNonQuery();
                        if (filas == 0)
                            return NotFound();
                    }
                }
                return Ok(new { estado = "ok", mensaje = "Usuario eliminado correctamente." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private static string Encriptar(string clave)
        {
            byte[] encriptado = System.Text.Encoding.Unicode.GetBytes(clave);
            return Convert.ToBase64String(encriptado);
        }

     
    }
}
