using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using apiClassroom.Models;
using apiClassroom.Utils;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using static apiClassroom.Models.Clases;

namespace apiClassroom.Controllers
{
    [RoutePrefix("api/usuarios")]
    public class ClasesController : ApiController
    {
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";

        // CREATE: Insertar nuevo usuario
        //para crear clase (solo profesor)
        [HttpPost]
        [Route("CrearClase")]
        public IHttpActionResult CrearClase([FromBody] string nombreClase)
        {
            var headers = Request.Headers;
            var token = headers.Authorization.Parameter;
            int id = JwtUtils.ExtraerIdUsuario(token);
            int rol = JwtUtils.ExtraerRol(token);

            if (rol != 1)
                return BadRequest("Solo los profesores pueden crear clases.");

            string codigo = GenerarCodigo();
            int claseId;

            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();
                string sql = "INSERT INTO clases (nombre, codigo, UsuariosId) OUTPUT INSERTED.id VALUES (@nombre, @codigo, @UsuariosId)";
                using (var comando = new SqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@nombre", nombreClase);
                    comando.Parameters.AddWithValue("@codigo", codigo);
                    comando.Parameters.AddWithValue("@UsuariosId", id);
                    claseId = (int)comando.ExecuteScalar();
                }
            }
            return Ok(new { id = claseId, nombre = nombreClase, codigo, UsuariosId = id });
        }

        private static string GenerarCodigo(int length = 6)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //unirse a clase
        [HttpPost]
        [Route("UnirseClase")]
        public IHttpActionResult UnirseClase([FromBody] string codigoClase)
        {
            var headers = Request.Headers;
            var token = headers.Authorization.Parameter;
            int idUsuario = JwtUtils.ExtraerIdUsuario(token);
            int rol = JwtUtils.ExtraerRol(token);

            if (rol != 0)
                return BadRequest("Solo los alumnos pueden unirse a clases.");

            int claseId;
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();
                string sql = "SELECT id FROM clases WHERE codigo=@codigo";
                using (var comando = new SqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("@codigo", codigoClase);
                    var result = comando.ExecuteScalar();
                    if (result == null)
                        return BadRequest("Código de clase no válido.");
                    claseId = (int)result;
                }

                // Comprueba que el alumno NO esté ya en la clase
                string checkSql = "SELECT COUNT(*) FROM solicitudesclase WHERE alumnoId=@alumnoId AND claseId=@claseId";
                using (var checkCmd = new SqlCommand(checkSql, conexion))
                {
                    checkCmd.Parameters.AddWithValue("@alumnoId", idUsuario);
                    checkCmd.Parameters.AddWithValue("@claseId", claseId);
                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return BadRequest("Ya estás en esta clase.");
                }

                string sqlInsert = "INSERT INTO solicitudesclase (alumnoId, claseId, aprobado, fechaSolicitud) VALUES (@alumnoId, @claseId, @aprobado, @fechaSolicitud)";
                using (var comando = new SqlCommand(sqlInsert, conexion))
                {
                    comando.Parameters.AddWithValue("@alumnoId", idUsuario);
                    comando.Parameters.AddWithValue("@claseId", claseId);
                    comando.Parameters.AddWithValue("@aprobado", true); // Se acepta directamente
                    comando.Parameters.AddWithValue("@fechaSolicitud", DateTime.Now);
                    comando.ExecuteNonQuery();
                }
            }
            return Ok(new { mensaje = "¡Te has unido a la clase correctamente!", claseId });
        }


    }
}
