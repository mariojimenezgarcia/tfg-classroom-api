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
    [System.Web.Http.RoutePrefix("api/notas")]
    public class notasController : ApiController
    {

        Dictionary<Errores.Error, string> listaerrores = Errores.GetListaErrores(); // Guardar errores
        List<Clases.Error> errorList = new List<Clases.Error>();
        string ConexionBBDD = "Server=tfgserver2025.database.windows.net,1433;Database=tfgclassroom;User Id=admintfgsql;Password=tfgclassroom2025_;Encrypt=True;TrustServerCertificate=True;";

        /*
         * ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
         *                                                                             𝐅𝐔𝐍𝐂𝐈𝐎𝐍𝐄𝐒 𝐄𝐗𝐓𝐄𝐑𝐍𝐀𝐒
         * ════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════
         */

        [HttpPost]
        [Route("ponerNota")]
        public JObject PonerNota([FromBody] Clases.PonerNotaRequest request)
        {
            var resultado = new Clases.PonerNotaResponse();
            errorList.Clear();

            // 1) Validar que token, idEntrega y nota sean válidos
            if (string.IsNullOrWhiteSpace(request.token) || request.idEntrega <= 0)
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 2) Leer el JWT y extraer userId + rol
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

            // 3) Verificar permisos: solo rol 1 o 2 pueden poner nota
            if (rol != 1 && rol != 2)
            {
                // Supongamos que 126 es “Permisos insuficientes para poner nota”
                MandarError(126, "No tienes permisos para asignar una nota.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 4) Verificar que la entrega exista (en la tabla 'entregas')
            bool entregaExiste = false;
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlCheckEntrega = @"
                        SELECT COUNT(*) 
                        FROM [dbo].[entregas]
                        WHERE id = @idEntrega;
                    ";
                    using (var cmdCheck = new SqlCommand(sqlCheckEntrega, conexion))
                    {
                        cmdCheck.Parameters.AddWithValue("@idEntrega", request.idEntrega);
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        entregaExiste = (count > 0);
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            if (!entregaExiste)
            {
                // Si no existe la entrega, devolvemos error de datos inválidos
                MandarError((int)Errores.Error.DatosInvalidos, "La entrega indicada no existe.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 5) Actualizar la columna 'nota' de esa entrega
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlUpdateNota = @"
                        UPDATE [dbo].[entregas]
                        SET nota = @nota
                        WHERE id = @idEntrega;
                    ";
                    using (var cmdUpd = new SqlCommand(sqlUpdateNota, conexion))
                    {
                        cmdUpd.Parameters.AddWithValue("@nota", request.nota);
                        cmdUpd.Parameters.AddWithValue("@idEntrega", request.idEntrega);
                        cmdUpd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 6) Si todo fue bien, devolvemos mensaje de confirmación
            resultado.mensaje = "Nota asignada correctamente a la entrega.";
            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
        }
        [HttpPost]
        [Route("verNotas")]
        public JObject VerNotas([FromBody] Clases.VerNotasRequest request)
        {
            var resultado = new Clases.VerNotasResponse();
            errorList.Clear();

            // 1) Validar que token no esté vacío
            if (string.IsNullOrWhiteSpace(request.token))
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 2) Leer el JWT y extraer userId + rol
            int userId = 0;
            int rol = 0;
            string nombreUsuario = string.Empty;
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

            // 3) Verificar permisos: solo rol = 1 (profesor) o rol = 3 (alumno) pueden usar este endpoint
            if (rol != 1 && rol != 3)
            {
                // Supongamos que 127 es “Permiso insuficiente para ver notas”
                MandarError(127, "No tienes permisos para ver estas notas.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 4) Obtener el nombre del usuario (para devolverlo en cada registro)
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlGetNombre = @"
                SELECT nombre
                FROM [dbo].[usuarios]
                WHERE id = @userId;
            ";
                    using (var cmdNombre = new SqlCommand(sqlGetNombre, conexion))
                    {
                        cmdNombre.Parameters.AddWithValue("@userId", userId);
                        object result = cmdNombre.ExecuteScalar();
                        if (result == null)
                        {
                            MandarError((int)Errores.Error.DatosInvalidos, "El usuario no existe.");
                            resultado.error = errorList;
                            return JObject.Parse(JsonConvert.SerializeObject(resultado));
                        }
                        nombreUsuario = result.ToString();
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 5) Consultar en “entregas” todas las filas con idusuario = userId y nota IS NOT NULL
            //    Y hacer JOIN con “tareas” para traer el Titulo
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlGetNotas = @"
                SELECT 
                    t.Titulo        AS tituloTarea,
                    e.nota          AS notaEntrega
                FROM [dbo].[entregas] AS e
                INNER JOIN [dbo].[tareas] AS t
                    ON e.idtarea = t.id
                WHERE 
                    e.idusuario = @userId
                    AND e.nota   IS NOT NULL
                ORDER BY e.idtarea;  -- puedes cambiar el ORDER BY si quieres, 
                                     -- p.ej. ORDER BY e.fecha_entrega DESC
            ";

                    using (var cmdGet = new SqlCommand(sqlGetNotas, conexion))
                    {
                        cmdGet.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmdGet.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var fila = new Clases.NotaData
                                {
                                    titulo = reader["tituloTarea"].ToString(),
                                    nota = Convert.ToDecimal(reader["notaEntrega"]),
                                    nombreUsuario = nombreUsuario
                                };
                                resultado.notas.Add(fila);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 6) Devolver la lista de notas (puede quedar vacía si no hay notas aún)
            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
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
