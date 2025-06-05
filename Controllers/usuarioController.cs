using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using apiClassroom.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using static apiClassroom.Models.usuario;
using apiClassroom.funciones;


namespace apiClassroom.Controllers
{
    [RoutePrefix("api/usuarios")]
    public class UsuariosController : ApiController
    {
        Dictionary<Errores.Error, string> listaerrores = Errores.GetListaErrores();
        List<usuario.Error> errorList = new List<usuario.Error>();
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";

        [HttpPost]
        [Route("create")]
        public JObject CrearUsuario([FromBody] usuario.UsuarioRequest nuevoUsuario)
        {
            var respuesta = new usuario.CrearUsuarioResponse();
            errorList.Clear();

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(nuevoUsuario.nombre) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.email) ||
                string.IsNullOrWhiteSpace(nuevoUsuario.password) ||
                nuevoUsuario.rol <= 0)
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                return JObject.FromObject(respuesta);
            }

            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string insertQuery = @"
                        INSERT INTO dbo.Usuarios (nombre, email, password, rol)
                        VALUES (@nombre, @email, @password, @rol)";

                    using (var comando = new SqlCommand(insertQuery, conexion))
                    {
                        comando.Parameters.AddWithValue("@nombre", nuevoUsuario.nombre);
                        comando.Parameters.AddWithValue("@email", nuevoUsuario.email);
                        comando.Parameters.AddWithValue("@password", Funciones.Encriptar(nuevoUsuario.password));
                        comando.Parameters.AddWithValue("@rol", nuevoUsuario.rol);

                        comando.ExecuteNonQuery();
                    }
                }

                respuesta.estado = "ok";
                respuesta.mensaje = "Usuario creado correctamente.";
                respuesta.error = errorList;
                return JObject.FromObject(respuesta);
            }
            catch (Exception ex)
            {
                // En caso de error de BBDD
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                return JObject.FromObject(respuesta);
            }
        }

        // ========================================================
        // 2) READ: Obtener todos los usuarios
        // ========================================================
        [HttpGet]
        [Route("all")]
        public JObject ObtenerUsuarios()
        {
            var respuesta = new usuario.ObtenerUsuariosResponse();
            errorList.Clear();

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
                            respuesta.usuarios.Add(new usuario.UsuarioData
                            {
                                id = (int)lector["id"],
                                nombre = lector["nombre"].ToString(),
                                email = lector["email"].ToString(),
                                rol = (int)lector["rol"]
                            });
                        }
                    }
                }

                respuesta.error = errorList;
                return JObject.FromObject(respuesta);
            }
            catch (Exception ex)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                return JObject.FromObject(respuesta);
            }
        }

        // ========================================================
        // 3) UPDATE: Actualizar usuario por ID
        // ========================================================
        [HttpPut]
        [Route("update/{id}")]
        public JObject ActualizarUsuario(int id, [FromBody] usuario.UsuarioRequest usuario)
        {
            var respuesta = new usuario.ActualizarUsuarioResponse();
            errorList.Clear();

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(usuario.nombre) ||
                string.IsNullOrWhiteSpace(usuario.email) ||
                string.IsNullOrWhiteSpace(usuario.password) ||
                usuario.rol <= 0)
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                return JObject.FromObject(respuesta);
            }

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
                        comando.Parameters.AddWithValue("@password", Funciones.Encriptar(usuario.password));
                        comando.Parameters.AddWithValue("@rol", usuario.rol);

                        int filas = comando.ExecuteNonQuery();
                        if (filas == 0)
                        {
                            MandarError((int)Errores.Error.DatosInvalidos, "Usuario no encontrado.");
                            return JObject.FromObject(respuesta);
                        }
                    }
                }

                respuesta.estado = "ok";
                respuesta.mensaje = "Usuario actualizado correctamente.";
                respuesta.error = errorList;
                return JObject.FromObject(respuesta);
            }
            catch (Exception ex)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                return JObject.FromObject(respuesta);
            }
        }

        // ========================================================
        // 4) DELETE: Eliminar usuario por ID
        // ========================================================
        [HttpDelete]
        [Route("delete/{id}")]
        public JObject EliminarUsuario(int id)
        {
            var respuesta = new usuario.EliminarUsuarioResponse();
            errorList.Clear();

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
                        {
                            MandarError((int)Errores.Error.DatosInvalidos, "Usuario no encontrado.");
                            return JObject.FromObject(respuesta);
                        }
                    }
                }

                respuesta.estado = "ok";
                respuesta.mensaje = "Usuario eliminado correctamente.";
                respuesta.error = errorList;
                return JObject.FromObject(respuesta);
            }
            catch (Exception ex)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                return JObject.FromObject(respuesta);
            }
        }
        private void MandarError(int code, string description)
        {
            var err = new usuario.Error
            {
                codigo = code,
                descripcion = description
            };

            if (errorList.Count == 0)
            {
                errorList.Add(err);
            }
        }


    }
}
