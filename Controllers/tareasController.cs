using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using apiClassroom.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static apiClassroom.Models.Clases;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;


namespace apiClassroom.Controllers
{
    [RoutePrefix("api/tareas")]
    public class tareasController : ApiController
    {
        Dictionary<Errores.Error, string> listaerrores = Errores.GetListaErrores(); // Guardar errores
        List<Clases.Error> errorList = new List<Clases.Error>();
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";
        private readonly string _jwtSecret = System.Configuration.ConfigurationManager.AppSettings["Jwt:Secret"];
        private readonly string _jwtIssuer = System.Configuration.ConfigurationManager.AppSettings["Jwt:Issuer"];
        private readonly string _jwtAudience = System.Configuration.ConfigurationManager.AppSettings["Jwt:Audience"];
        private readonly int _jwtExpiry = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Jwt:ExpiryMinutes"] ?? "60");
        [HttpPost]
        [Route("CrearTarea")]
        public JObject CrearTarea([FromBody] JObject requestBody)
        {
            var respuesta = new JObject();
            errorList = new List<Clases.Error>();

            // Validar y extraer token
            var token = requestBody["token"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                MandarError(400, "Token no proporcionado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch
            {
                MandarError(401, "Token inválido o expirado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // Extraer claims
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
              ?? principal.FindFirst(ClaimTypes.NameIdentifier)
              ?? principal.FindFirst("sub");

            if (subClaim == null)
            {
                MandarError(401, "Token inválido: no contiene el claim 'sub'.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            int idUsuario = int.Parse(subClaim.Value);

            var rolClaim = principal.FindFirst("rol");

            if (rolClaim == null || (rolClaim.Value != "1" && rolClaim.Value != "2"))

            {
                MandarError(403, "No tienes permisos para crear tareas.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // Extraer datos de la tarea
            if (requestBody["titulo"] == null || requestBody["puntuacion"] == null ||
                requestBody["fecha_de_entrega"] == null || requestBody["idclase"] == null)
            {
                MandarError(400, "Faltan campos obligatorios.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            string titulo = requestBody["titulo"].ToString();

            if (!int.TryParse(requestBody["puntuacion"]?.ToString(), out int puntuacion))
                puntuacion = 0;

            if (!DateTime.TryParse(requestBody["fecha_de_entrega"]?.ToString(), out DateTime fechaEntrega))
            {
                MandarError(400, "Fecha de entrega inválida.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            if (!int.TryParse(requestBody["idclase"]?.ToString(), out int idClase))
            {
                MandarError(400, "idclase inválido.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }


            DateTime fechaActual = DateTime.Now;
            string nombreUsuario = "";

            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                // Obtener nombre del usuario
                using (var cmdUsuario = new SqlCommand("SELECT nombre FROM usuarios WHERE id = @id", conexion))
                {
                    cmdUsuario.Parameters.AddWithValue("@id", idUsuario);
                    var result = cmdUsuario.ExecuteScalar();
                    if (result == null)
                    {
                        MandarError(404, "Usuario no encontrado.");
                        respuesta["error"] = JArray.FromObject(errorList);
                        return respuesta;
                    }
                    nombreUsuario = result.ToString();
                }

                // Insertar la tarea
                string insertSql = @"
        INSERT INTO tareas (Titulo, puntuacion, fecha_de_entrega, fecha_actual, idclase, idusuario)
        VALUES (@Titulo, @Puntuacion, @FechaEntrega, @FechaActual, @IdClase, @IdUsuario)";

                using (var cmd = new SqlCommand(insertSql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Titulo", titulo);
                    cmd.Parameters.AddWithValue("@Puntuacion", puntuacion);
                    cmd.Parameters.AddWithValue("@FechaEntrega", fechaEntrega);
                    cmd.Parameters.AddWithValue("@FechaActual", fechaActual);
                    cmd.Parameters.AddWithValue("@IdClase", idClase);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);

                    cmd.ExecuteNonQuery();
                }
            }

            respuesta["titulo"] = titulo;
            respuesta["fecha_actual"] = fechaActual.ToString("yyyy-MM-dd HH:mm:ss");
            respuesta["nombre_usuario"] = nombreUsuario;
            return respuesta;
        }

        private void MandarError(int code, string description)
        {
            var err = new Clases.Error();
            err.codigo = code;
            err.descripcion = description;

            if (errorList.Count == 0)
            {
                errorList.Add(err);
            }
        }
        [HttpPost]
        [Route("VerTareas")]
        public JObject VerTareas([FromBody] JObject requestBody)
        {
            var respuesta = new JObject();
            errorList = new List<Clases.Error>();

            // 1. Validar token
            var token = requestBody["token"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                MandarError(400, "Token no proporcionado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch
            {
                MandarError(401, "Token inválido o expirado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 2. Extraer usuario y clase
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
              ?? principal.FindFirst("sub")
              ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError(401, "El token no contiene el identificador del usuario (claim 'sub').");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            int idUsuario = int.Parse(subClaim.Value);

            int idClase = int.Parse(requestBody["idclase"]?.ToString() ?? "0");

            if (idClase == 0)
            {
                MandarError(400, "idclase inválido.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 3. Consultar tareas de la clase
            JArray tareasArray = new JArray();
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                string query = @"
            SELECT t.id, t.Titulo, t.puntuacion, t.fecha_de_entrega, t.fecha_actual, u.nombre AS nombre_usuario
            FROM tareas t
            INNER JOIN usuarios u ON t.idusuario = u.id
            WHERE t.idclase = @IdClase
            ORDER BY t.fecha_actual DESC";

                using (var cmd = new SqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@IdClase", idClase);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            JObject tarea = new JObject
                            {
                                ["id"] = (int)reader["id"],
                                ["titulo"] = reader["Titulo"].ToString(),
                                ["puntuacion"] = (int)reader["puntuacion"],
                                ["fecha_entrega"] = ((DateTime)reader["fecha_de_entrega"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                ["fecha_creacion"] = ((DateTime)reader["fecha_actual"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                ["creado_por"] = reader["nombre_usuario"].ToString()
                            };
                            tareasArray.Add(tarea);
                        }
                    }
                }
            }

            // 4. Devolver tareas
            respuesta["estado"] = "ok";
            respuesta["tareas"] = tareasArray;
            return respuesta;
        }
        [HttpPost]
        [Route("EntregarTarea")]
        public JObject EntregarTarea([FromBody] JObject requestBody)
        {
            var respuesta = new JObject();
            errorList = new List<Clases.Error>();

            // 1. Extraer y validar token
            var token = requestBody["token"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                MandarError(400, "Token no proporcionado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch
            {
                MandarError(401, "Token inválido o expirado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
              ?? principal.FindFirst("sub")
              ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError(401, "El token no contiene el identificador del usuario (claim 'sub').");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            int idUsuario = int.Parse(subClaim.Value);

            int rol = int.Parse(principal.FindFirst("rol")?.Value ?? "0");

            // 2. Verificar rol permitido
            if (rol != 1 && rol != 3)
            {
                MandarError(403, "No tienes permisos para entregar tareas.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 3. Extraer datos del cuerpo
            int idTarea = int.Parse(requestBody["idtarea"]?.ToString() ?? "0");
            string asunto = requestBody["asunto"]?.ToString();
            string archivo = requestBody["archivo"]?.ToString();
            DateTime fechaActual = DateTime.Now;

            if (idTarea == 0 || string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(archivo))
            {
                MandarError(400, "Faltan datos obligatorios.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 4. Verificar si la tarea está aún activa
            DateTime fechaLimiteEntrega;
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                using (var cmd = new SqlCommand("SELECT fecha_de_entrega FROM tareas WHERE id = @id", conexion))
                {
                    cmd.Parameters.AddWithValue("@id", idTarea);
                    var result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        MandarError(404, "Tarea no encontrada.");
                        respuesta["error"] = JArray.FromObject(errorList);
                        return respuesta;
                    }

                    fechaLimiteEntrega = (DateTime)result;

                    if (fechaActual > fechaLimiteEntrega)
                    {
                        MandarError(403, "La entrega ha expirado.");
                        respuesta["error"] = JArray.FromObject(errorList);
                        return respuesta;
                    }
                }

                // 5. Insertar entrega
                string insertSql = @"
            INSERT INTO entregas (idtarea, idusuario, asunto, archivo, fecha_entrega)
            VALUES (@idtarea, @idusuario, @asunto, @archivo, @fecha)";

                using (var cmd = new SqlCommand(insertSql, conexion))
                {
                    cmd.Parameters.AddWithValue("@idtarea", idTarea);
                    cmd.Parameters.AddWithValue("@idusuario", idUsuario);
                    cmd.Parameters.AddWithValue("@asunto", asunto);
                    cmd.Parameters.AddWithValue("@archivo", archivo);
                    cmd.Parameters.AddWithValue("@fecha", fechaActual);

                    cmd.ExecuteNonQuery();
                }
            }

            // 6. Respuesta final
            respuesta["estado"] = "ok";
            respuesta["mensaje"] = "Entrega registrada correctamente.";
            respuesta["fecha_entrega"] = fechaActual.ToString("yyyy-MM-dd HH:mm:ss");
            return respuesta;
        }
        [HttpPost]
        [Route("VerEntregas")]
        public JObject VerEntregas([FromBody] JObject requestBody)
        {
            var respuesta = new JObject();
            errorList = new List<Clases.Error>();

            // 1. Validar token
            var token = requestBody["token"]?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                MandarError(400, "Token no proporcionado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out _);
            }
            catch
            {
                MandarError(401, "Token inválido o expirado.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 2. Extraer claims
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
              ?? principal.FindFirst("sub")
              ?? principal.FindFirst(ClaimTypes.NameIdentifier);

            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError(401, "El token no contiene el identificador del usuario (claim 'sub').");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            int idUsuario = int.Parse(subClaim.Value);

            int rol = int.Parse(principal.FindFirst("rol")?.Value ?? "0");

            // 3. Solo rol 2 puede ver entregas
            if (rol != 1 && rol != 2)
            {
                MandarError(403, "No tienes permisos para ver entregas.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 4. Obtener idtarea
            int idTarea = int.Parse(requestBody["idtarea"]?.ToString() ?? "0");
            if (idTarea == 0)
            {
                MandarError(400, "idtarea no válido.");
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 5. Consultar entregas
            JArray entregasArray = new JArray();
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                string query = @"
            SELECT e.id, e.asunto, e.archivo, e.fecha_entrega, u.nombre AS nombre_alumno
            FROM entregas e
            INNER JOIN usuarios u ON e.idusuario = u.id
            WHERE e.idtarea = @idtarea
            ORDER BY e.fecha_entrega DESC";

                using (var cmd = new SqlCommand(query, conexion))
                {
                    cmd.Parameters.AddWithValue("@idtarea", idTarea);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            JObject entrega = new JObject
                            {
                                ["id"] = (int)reader["id"],
                                ["asunto"] = reader["asunto"].ToString(),
                                ["archivo"] = reader["archivo"].ToString(),
                                ["fecha_entrega"] = ((DateTime)reader["fecha_entrega"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                ["nombre_alumno"] = reader["nombre_alumno"].ToString()
                            };
                            entregasArray.Add(entrega);
                        }
                    }
                }
            }

            respuesta["estado"] = "ok";
            respuesta["entregas"] = entregasArray;
            return respuesta;
        }
    }
}