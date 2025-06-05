using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using apiClassroom.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using static apiClassroom.Models.tareas;

namespace apiClassroom.Controllers
{
    [RoutePrefix("api/tareas")]
    public class tareasController : ApiController
    {
        Dictionary<Errores.Error, string> listaerrores = Errores.GetListaErrores();
        List<tareas.Error> errorList = new List<tareas.Error>();
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";
        private readonly string _jwtSecret = System.Configuration.ConfigurationManager.AppSettings["Jwt:Secret"];
        private readonly string _jwtIssuer = System.Configuration.ConfigurationManager.AppSettings["Jwt:Issuer"];
        private readonly string _jwtAudience = System.Configuration.ConfigurationManager.AppSettings["Jwt:Audience"];
        private readonly int _jwtExpiry = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Jwt:ExpiryMinutes"] ?? "60");

        // ============================
        // 1) CREAR TAREA
        // ============================
        [HttpPost]
        [Route("CrearTarea")]
        public JObject CrearTarea([FromBody] tareas.CrearTareaRequest request)
        {
            var resultado = new JObject();
            errorList = new List<tareas.Error>();

            // 1) Validar que token no esté vacío
            if (string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }

            // 2) Validar y decodificar el JWT
            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(request.token, new TokenValidationParameters
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
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }

            // 3) Extraer claim “sub” (userId) y “rol”
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                         ?? principal.FindFirst("sub");
            if (subClaim == null)
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }
            int idUsuario = int.Parse(subClaim.Value);

            var rolClaim = principal.FindFirst("rol");
            if (rolClaim == null || (rolClaim.Value != "1" && rolClaim.Value != "2"))
            {
                MandarError((int)Errores.Error.NoPermisos, listaerrores[Errores.Error.NoPermisos]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }

            // 4) Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(request.titulo)
                || request.puntuacion < 0
                || string.IsNullOrWhiteSpace(request.fecha_de_entrega)
                || request.idclase <= 0)
            {
                MandarError((int)Errores.Error.FaltanCampos, listaerrores[Errores.Error.FaltanCampos]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }

            // 5) Parsear la fecha de entrega
            if (!DateTime.TryParse(request.fecha_de_entrega, out DateTime fechaEntrega))
            {
                MandarError((int)Errores.Error.FechaMal, listaerrores[Errores.Error.FechaMal]);
                resultado["error"] = JArray.FromObject(errorList);
                return resultado;
            }

            int puntuacion = request.puntuacion;
            int idClase = request.idclase;
            DateTime fechaActual = DateTime.Now;
            string nombreUsuario = "";

            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                // 6) Obtener el nombre del usuario que crea la tarea
                using (var cmdUsuario = new SqlCommand("SELECT nombre FROM usuarios WHERE id = @id", conexion))
                {
                    cmdUsuario.Parameters.AddWithValue("@id", idUsuario);
                    var resultadoConsulta = cmdUsuario.ExecuteScalar();
                    if (resultadoConsulta == null)
                    {
                        MandarError((int)Errores.Error.UsuarioIncorrecto, listaerrores[Errores.Error.UsuarioIncorrecto]);
                        resultado["error"] = JArray.FromObject(errorList);
                        return resultado;
                    }
                    nombreUsuario = resultadoConsulta.ToString();
                }

                // 7) Insertar la tarea en la BBDD
                string insertSql = @"
                    INSERT INTO tareas
                        (Titulo, puntuacion, fecha_de_entrega, fecha_actual, idclase, idusuario)
                    VALUES
                        (@Titulo, @Puntuacion, @FechaEntrega, @FechaActual, @IdClase, @IdUsuario)";
                using (var cmd = new SqlCommand(insertSql, conexion))
                {
                    cmd.Parameters.AddWithValue("@Titulo", request.titulo);
                    cmd.Parameters.AddWithValue("@Puntuacion", puntuacion);
                    cmd.Parameters.AddWithValue("@FechaEntrega", fechaEntrega);
                    cmd.Parameters.AddWithValue("@FechaActual", fechaActual);
                    cmd.Parameters.AddWithValue("@IdClase", idClase);
                    cmd.Parameters.AddWithValue("@IdUsuario", idUsuario);
                    cmd.ExecuteNonQuery();
                }
            }

            // 8) Construir respuesta
            var resp = new tareas.CrearTareaResponse
            {
                titulo = request.titulo,
                fecha_actual = fechaActual.ToString("yyyy-MM-dd HH:mm:ss"),
                nombre_usuario = nombreUsuario,
                error = errorList
            };

            return JObject.FromObject(resp);
        }

        // ============================
        // 2) VER TAREAS
        // ============================
        [HttpPost]
        [Route("VerTareas")]
        public JObject VerTareas([FromBody] tareas.VerTareasRequest request)
        {
            var respuesta = new JObject();
            errorList = new List<tareas.Error>();

            // 1) Validar token
            if (string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 2) Validar y decodificar JWT
            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(request.token, new TokenValidationParameters
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
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 3) Extraer “sub” (userId)
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirst("sub")
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }
            int idUsuario = int.Parse(subClaim.Value);

           
            int idClase = request.idclase;

            // 5) Consultar tareas de la clase dada
            var listaTareas = new List<tareas.TareaData>();
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
                            var tarea = new tareas.TareaData
                            {
                                id = (int)reader["id"],
                                titulo = reader["Titulo"].ToString(),
                                puntuacion = (int)reader["puntuacion"],
                                fecha_entrega = ((DateTime)reader["fecha_de_entrega"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                fecha_creacion = ((DateTime)reader["fecha_actual"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                creado_por = reader["nombre_usuario"].ToString()
                            };
                            listaTareas.Add(tarea);
                        }
                    }
                }
            }

            var resp = new tareas.VerTareasResponse
            {
                estado = "ok",
                tareas = listaTareas,
                error = errorList
            };

            return JObject.FromObject(resp);
        }

        // ============================
        // 3) ENTREGAR TAREA
        // ============================
        [HttpPost]
        [Route("EntregarTarea")]
        public JObject EntregarTarea([FromBody] tareas.EntregarTareaRequest request)
        {
            var respuesta = new JObject();
            errorList = new List<tareas.Error>();

            // 1) Validar token
            if (string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 2) Validar y decodificar JWT
            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(request.token, new TokenValidationParameters
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
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 3) Extraer “sub” (userId) y “rol”
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirst("sub")
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }
            int idUsuario = int.Parse(subClaim.Value);

            var rolClaim = principal.FindFirst("rol");
            int rol = rolClaim != null ? int.Parse(rolClaim.Value) : 0;
            if (rol != 1 && rol != 3)
            {
                MandarError((int)Errores.Error.NoPermisos, listaerrores[Errores.Error.NoPermisos]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 4) Validar campos de la petición
            if (request.idtarea <= 0
                || string.IsNullOrWhiteSpace(request.asunto)
                || string.IsNullOrWhiteSpace(request.archivo))
            {
                MandarError((int)Errores.Error.FaltanCampos, listaerrores[Errores.Error.FaltanCampos]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }
            int idTarea = request.idtarea;
            string asunto = request.asunto;
            string archivo = request.archivo;
            DateTime fechaActual = DateTime.Now;

            DateTime fechaLimiteEntrega;
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                // 5) Verificar fecha límite de entrega
                using (var cmd = new SqlCommand("SELECT fecha_de_entrega FROM tareas WHERE id = @id", conexion))
                {
                    cmd.Parameters.AddWithValue("@id", idTarea);
                    var result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        MandarError((int)Errores.Error.NoTarea, listaerrores[Errores.Error.NoTarea]);

                        respuesta["error"] = JArray.FromObject(errorList);
                        return respuesta;
                    }
                    fechaLimiteEntrega = (DateTime)result;
                    if (fechaActual > fechaLimiteEntrega)
                    {
                        MandarError((int)Errores.Error.FechaEntrega, listaerrores[Errores.Error.FechaEntrega]);
                        respuesta["error"] = JArray.FromObject(errorList);
                        return respuesta;
                    }
                }

                // 6) Insertar registro en entregas
                string insertSql = @"
                    INSERT INTO entregas
                        (idtarea, idusuario, asunto, archivo, fecha_entrega)
                    VALUES
                        (@idtarea, @idusuario, @asunto, @archivo, @fecha)";
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

            var respEntregar = new tareas.EntregarTareaResponse
            {
                estado = "ok",
                mensaje = "Entrega registrada correctamente.",
                fecha_entrega = fechaActual.ToString("yyyy-MM-dd HH:mm:ss"),
                error = errorList
            };

            return JObject.FromObject(respEntregar);
        }

        // ============================
        // 4) VER ENTREGAS
        // ============================
        [HttpPost]
        [Route("VerEntregas")]
        public JObject VerEntregas([FromBody] tareas.VerEntregasRequest request)
        {
            var respuesta = new JObject();
            errorList = new List<tareas.Error>();

            // 1) Validar token
            if (string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 2) Validar y decodificar JWT
            ClaimsPrincipal principal;
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                principal = tokenHandler.ValidateToken(request.token, new TokenValidationParameters
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
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            // 3) Extraer “sub” (userId) y “rol”
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                         ?? principal.FindFirst("sub")
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier);
            if (subClaim == null || string.IsNullOrWhiteSpace(subClaim.Value))
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }
            int idUsuario = int.Parse(subClaim.Value);

            var rolClaim = principal.FindFirst("rol");
            int rol = rolClaim != null ? int.Parse(rolClaim.Value) : 0;
            if (rol != 1 && rol != 2)
            {
                MandarError((int)Errores.Error.NoPermisos, listaerrores[Errores.Error.NoPermisos]);
                respuesta["error"] = JArray.FromObject(errorList);
                return respuesta;
            }

            int idTarea = request.idtarea;

            // 5) Consultar entregas de la tarea
            var listaEntregas = new List<tareas.EntregaData>();
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
                            var entrega = new tareas.EntregaData
                            {
                                id = (int)reader["id"],
                                asunto = reader["asunto"].ToString(),
                                archivo = reader["archivo"].ToString(),
                                fecha_entrega = ((DateTime)reader["fecha_entrega"]).ToString("yyyy-MM-dd HH:mm:ss"),
                                nombre_alumno = reader["nombre_alumno"].ToString()
                            };
                            listaEntregas.Add(entrega);
                        }
                    }
                }
            }

            var respVerEntregas = new tareas.VerEntregasResponse
            {
                estado = "ok",
                entregas = listaEntregas,
                error = errorList
            };

            return JObject.FromObject(respVerEntregas);
        }

        private void MandarError(int code, string description)
        {
            var err = new tareas.Error
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
