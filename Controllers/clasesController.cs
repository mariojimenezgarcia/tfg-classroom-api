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
using System.Linq;





namespace clases.Controllers
{
    [System.Web.Http.RoutePrefix("api/Clases")]
    public class clasesController : ApiController
    {

        Dictionary<Errores.Error, string> listaerrores = Errores.GetListaErrores(); // Guardar errores
        List<Clases.Error> errorList = new List<Clases.Error>();
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";
        private readonly string _jwtSecret = System.Configuration.ConfigurationManager.AppSettings["Jwt:Secret"];
        private readonly string _jwtIssuer = System.Configuration.ConfigurationManager.AppSettings["Jwt:Issuer"];
        private readonly string _jwtAudience = System.Configuration.ConfigurationManager.AppSettings["Jwt:Audience"];
        private readonly int _jwtExpiry = int.Parse(System.Configuration.ConfigurationManager.AppSettings["Jwt:ExpiryMinutes"] ?? "60");
        /*
         * ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
         *                                                                             𝐅𝐔𝐍𝐂𝐈𝐎𝐍𝐄𝐒 𝐄𝐗𝐓𝐄𝐑𝐍𝐀𝐒
         * ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
         */
        [HttpPost]
        [Route("CrearClase")]
        public JObject CrearClase([FromBody] Clases.CrearClaseRequest request)
        {
            // Preparamos la respuesta y limpiamos errores previos
            var resultado = new Clases.CrearClaseResponse();
            errorList.Clear();

            // 1) Validar que vengan los datos mínimos
            if (string.IsNullOrWhiteSpace(request.nombre) ||
                string.IsNullOrWhiteSpace(request.curso) ||
                string.IsNullOrWhiteSpace(request.aula) ||
                string.IsNullOrWhiteSpace(request.color) ||
                string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 2) Decodificar/validar token y extraer userId + rol
            int userId = 0;
            int rol = 0;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var tokenJwt = handler.ReadJwtToken(request.token);

                var subClaim = tokenJwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                var rolClaim = tokenJwt.Claims.FirstOrDefault(c => c.Type == "rol");

                if (subClaim == null || rolClaim == null)
                {
                    MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                    resultado.error = errorList;
                    return JObject.Parse(JsonConvert.SerializeObject(resultado));
                }

                userId = int.Parse(subClaim.Value);
                rol = int.Parse(rolClaim.Value);
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 3) Verificar que solo rol = 1 ó 2 pueden crear clase
            if (rol != 1 && rol != 2)
            {
                MandarError(120, "No tienes permisos para crear una clase.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 4) Generar código de 6 caracteres (alfanumérico)
            string codigoAcceso = GenerarCodigoAlfanumerico(6);

            // 5) Insertar en Clases y luego en UsuarioClase
            int nuevaClaseId = 0;

            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    // 5.1) Insertar en Clases
                    const string sqlInsertClase = @"
                        INSERT INTO [dbo].[Clases]
                            (Nombre, CodigoAcceso, UsuariosId, Curso, Aula, Color)
                        VALUES
                            (@nombre, @codigoAcceso, @usuariosId, @curso, @aula, @color);
                        SELECT SCOPE_IDENTITY();
                    ";

                    using (var cmdClase = new SqlCommand(sqlInsertClase, conexion))
                    {
                        cmdClase.Parameters.AddWithValue("@nombre", request.nombre);
                        cmdClase.Parameters.AddWithValue("@codigoAcceso", codigoAcceso);
                        cmdClase.Parameters.AddWithValue("@usuariosId", userId);
                        cmdClase.Parameters.AddWithValue("@curso", request.curso);
                        cmdClase.Parameters.AddWithValue("@aula", request.aula);
                        cmdClase.Parameters.AddWithValue("@color", request.color);

                        object result = cmdClase.ExecuteScalar();
                        nuevaClaseId = Convert.ToInt32(result);
                    }

                    // 5.2) Insertar en UsuarioClase (relacionando profesor con la clase recién creada)
                    const string sqlInsertUsuarioClase = @"
                        INSERT INTO [dbo].[UsuarioClase] 
                            (usuarioId, claseId)
                        VALUES 
                            (@usuarioId, @claseId);
                    ";

                    using (var cmdUC = new SqlCommand(sqlInsertUsuarioClase, conexion))
                    {
                        cmdUC.Parameters.AddWithValue("@usuarioId", userId);
                        cmdUC.Parameters.AddWithValue("@claseId", nuevaClaseId);
                        cmdUC.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 6) Devolver resultado exitoso
            resultado.Id = nuevaClaseId;
            resultado.CodigoAcceso = codigoAcceso;
            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
        }
        [HttpPost]
        [Route("UnirseClase")]
        public JObject UnirseClase([FromBody] Clases.UnirseClaseRequest request)
        {
            // Preparamos la respuesta y limpiamos la lista de errores
            var resultado = new Clases.UnirseClaseResponse();
            errorList.Clear();

            // 1) Validar que token y códigoAcceso no estén vacíos
            if (string.IsNullOrWhiteSpace(request.token) ||
                string.IsNullOrWhiteSpace(request.codigoAcceso))
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 2) Decodificar/validar token y extraer userId + rol
            int userId = 0;
            int rol = 0;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var tokenJwt = handler.ReadJwtToken(request.token);

                var subClaim = tokenJwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                var rolClaim = tokenJwt.Claims.FirstOrDefault(c => c.Type == "rol");

                if (subClaim == null || rolClaim == null)
                {
                    MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                    resultado.error = errorList;
                    return JObject.Parse(JsonConvert.SerializeObject(resultado));
                }

                userId = int.Parse(subClaim.Value);
                rol = int.Parse(rolClaim.Value);
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.TokenInvalido, listaerrores[Errores.Error.TokenInvalido]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 3) Verificar que solo rol 1 o 3 puedan unirse a la clase
            if (rol != 1 && rol != 3)
            {
                // Supongamos que 121 es “Permisos insuficientes para unirse”
                MandarError(121, "No tienes permisos para unirte a una clase.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 4) Verificar que ese codigoAcceso existe en Clases
            int claseId = 0;
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlBuscarClase = @"
                SELECT Id 
                FROM [dbo].[Clases]
                WHERE CodigoAcceso = @codigoAcceso;
            ";
                    using (var cmdBusca = new SqlCommand(sqlBuscarClase, conexion))
                    {
                        cmdBusca.Parameters.AddWithValue("@codigoAcceso", request.codigoAcceso);
                        object resultadoConsulta = cmdBusca.ExecuteScalar();

                        if (resultadoConsulta == null)
                        {
                            // No existe ninguna clase con ese código
                            MandarError((int)Errores.Error.DatosInvalidos, "Código de clase no encontrado.");
                            resultado.error = errorList;
                            return JObject.Parse(JsonConvert.SerializeObject(resultado));
                        }

                        // Si existe, recuperamos el Id de la clase
                        claseId = Convert.ToInt32(resultadoConsulta);
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 5) Comprobar si el alumno (userId) YA ESTÁ en esa clase (en UsuarioClase)
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlCheckExist = @"
                SELECT COUNT(*) 
                FROM [dbo].[UsuarioClase]
                WHERE usuarioId = @usuarioId
                  AND claseId   = @claseId;
            ";
                    using (var cmdCheck = new SqlCommand(sqlCheckExist, conexion))
                    {
                        cmdCheck.Parameters.AddWithValue("@usuarioId", userId);
                        cmdCheck.Parameters.AddWithValue("@claseId", claseId);

                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count > 0)
                        {
                            // El usuario ya estaba registrado en esa clase
                            MandarError(122, "Usuario ya estaba registrado en esta clase.");
                            resultado.error = errorList;
                            return JObject.Parse(JsonConvert.SerializeObject(resultado));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Si hay fallo al comprobar existencia, devolvemos error de BBDD
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 6) Insertar en UsuarioClase: (usuarioId = userId, claseId = claseId)
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlInsertUsuarioClase = @"
                INSERT INTO [dbo].[UsuarioClase]
                    (usuarioId, claseId)
                VALUES
                    (@usuarioId, @claseId);
            ";
                    using (var cmdUC = new SqlCommand(sqlInsertUsuarioClase, conexion))
                    {
                        cmdUC.Parameters.AddWithValue("@usuarioId", userId);
                        cmdUC.Parameters.AddWithValue("@claseId", claseId);
                        cmdUC.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 7) Si llegamos aquí, todo fue correcto: devolvemos mensaje de éxito
            resultado.mensaje = "Usuario registrado en la clase correctamente.";
            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
        }

        private string GenerarCodigoAlfanumerico(int longitud)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                               + "abcdefghijklmnopqrstuvwxyz"
                               + "0123456789";
            var sb = new StringBuilder();
            var rng = new Random();

            for (int i = 0; i < longitud; i++)
            {
                int idx = rng.Next(0, chars.Length);
                sb.Append(chars[idx]);
            }

            return sb.ToString();
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

        public static string Encriptar(string clave)
        {
            byte[] encriptado = System.Text.Encoding.Unicode.GetBytes(clave);
            return Convert.ToBase64String(encriptado);
        }
    }
}
