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
    [System.Web.Http.RoutePrefix("api/anuncios")]
    public class anunciosController : ApiController
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
        [Route("crearAnuncios")]
        public JObject Anuncios([FromBody] Clases.AnuncioRequest request)
        {
            // 0) Prepara la respuesta y limpia errores previos
            var resultado = new Clases.AnuncioResponse();
            errorList.Clear();

            // 1) Verificar que token, contenido e idClase no estén vacíos / inválidos
            if (string.IsNullOrWhiteSpace(request.token) ||
                string.IsNullOrWhiteSpace(request.contenido) ||
                request.idClase <= 0)
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

            // 3) Verificar permisos (solo roles 1, 2 o 3 pueden anunciar)
            if (rol != 1 && rol != 2 && rol != 3)
            {
                // Código 123: permiso insuficiente para publicar anuncio
                MandarError(123, "No tienes permisos para crear un anuncio.");
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 4) Comprobar que el usuario está inscrito en la clase (en UsuarioClase)
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlCheckMembership = @"
                SELECT COUNT(*)
                FROM [dbo].[UsuarioClase]
                WHERE usuarioId = @usuarioId
                  AND claseId   = @claseId;
            ";
                    using (var cmdCheck = new SqlCommand(sqlCheckMembership, conexion))
                    {
                        cmdCheck.Parameters.AddWithValue("@usuarioId", userId);
                        cmdCheck.Parameters.AddWithValue("@claseId", request.idClase);

                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count == 0)
                        {
                            // El usuario no está inscrito en esa clase
                            MandarError(125, "No estás inscrito en esta clase; no puedes crear anuncios.");
                            resultado.error = errorList;
                            return JObject.Parse(JsonConvert.SerializeObject(resultado));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallo al comprobar membresía → error en la BBDD
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 5) Insertar en la tabla Anuncios
            int nuevoAnuncioId = 0;
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlInsertAnuncio = @"
                INSERT INTO [dbo].[Anuncios]
                    (idUsuario, idClase, contenido)
                VALUES
                    (@idUsuario, @idClase, @contenido);
                SELECT SCOPE_IDENTITY();
            ";

                    using (var cmdIns = new SqlCommand(sqlInsertAnuncio, conexion))
                    {
                        cmdIns.Parameters.AddWithValue("@idUsuario", userId);
                        cmdIns.Parameters.AddWithValue("@idClase", request.idClase);
                        cmdIns.Parameters.AddWithValue("@contenido", request.contenido);

                        object scalarResult = cmdIns.ExecuteScalar();
                        nuevoAnuncioId = Convert.ToInt32(scalarResult);
                    }

                    // 6) Recuperar nombreUsuario y fechaCreacion del anuncio recién creado
                    const string sqlSelectAnuncio = @"
                SELECT 
                    u.nombre    AS nombreUsuario,
                    a.contenido AS contenido,
                    a.fechaCreacion
                FROM [dbo].[Anuncios] AS a
                INNER JOIN [dbo].[usuarios] AS u
                    ON a.idUsuario = u.id
                WHERE a.id = @idAnuncio;
            ";

                    using (var cmdSel = new SqlCommand(sqlSelectAnuncio, conexion))
                    {
                        cmdSel.Parameters.AddWithValue("@idAnuncio", nuevoAnuncioId);
                        using (var reader = cmdSel.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                resultado.nombreUsuario = reader["nombreUsuario"].ToString();
                                resultado.contenido = reader["contenido"].ToString();

                                // Formateamos la fecha en “yyyy-MM-dd HH:mm:ss”
                                DateTime dt = reader.GetDateTime(reader.GetOrdinal("fechaCreacion"));
                                resultado.fechaCreacion = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                // Si por alguna razón no encontramos el anuncio recién insertado
                                MandarError((int)Errores.Error.ErrorEnBBDD,
                                            "No se pudo recuperar el anuncio tras insertarlo.");
                                resultado.error = errorList;
                                return JObject.Parse(JsonConvert.SerializeObject(resultado));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error genérico de base de datos
                MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                resultado.error = errorList;
                return JObject.Parse(JsonConvert.SerializeObject(resultado));
            }

            // 7) Todo ha ido bien: devolvemos la respuesta con datos de la fila insertada
            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
        }

        [HttpPost]
        [Route("VisualizarAnuncios")]
        public JObject VisualizarAnuncios([FromBody] Clases.VisualizarAnunciosRequest request)
        {
            // 0) Preparamos la respuesta y limpiamos errores previos
            var resultado = new Clases.VisualizarAnunciosResponse();
            errorList.Clear();

            // 1) Validar que token e idClase sean válidos
            if (string.IsNullOrWhiteSpace(request.token) || request.idClase <= 0)
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

            // 3) Verificar que el usuario esté inscrito en la clase (en UsuarioClase)
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlCheckMembership = @"
                SELECT COUNT(*) 
                FROM [dbo].[UsuarioClase]
                WHERE usuarioId = @usuarioId
                  AND claseId   = @claseId;
            ";
                    using (var cmdCheck = new SqlCommand(sqlCheckMembership, conexion))
                    {
                        cmdCheck.Parameters.AddWithValue("@usuarioId", userId);
                        cmdCheck.Parameters.AddWithValue("@claseId", request.idClase);

                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count == 0)
                        {
                            // El usuario NO está inscrito en esa clase → no puede ver anuncios
                            MandarError(124, "No estás inscrito en la clase solicitada.");
                            resultado.error = errorList;
                            return JObject.Parse(JsonConvert.SerializeObject(resultado));
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

            // 4) Recuperar todos los anuncios de esa clase
            try
            {
                using (var conexion = new SqlConnection(ConexionBBDD))
                {
                    conexion.Open();

                    const string sqlSelectAnuncios = @"
                SELECT 
                    u.nombre    AS nombreUsuario,
                    a.contenido,
                    a.fechaCreacion
                FROM [dbo].[Anuncios] AS a
                INNER JOIN [dbo].[usuarios] AS u
                  ON a.idUsuario = u.id
                WHERE a.idClase = @claseId
                ORDER BY a.fechaCreacion DESC;
            ";

                    using (var cmdSel = new SqlCommand(sqlSelectAnuncios, conexion))
                    {
                        cmdSel.Parameters.AddWithValue("@claseId", request.idClase);
                        using (var reader = cmdSel.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var anuncio = new Clases.AnuncioData
                                {
                                    nombreUsuario = reader["nombreUsuario"].ToString(),
                                    contenido = reader["contenido"].ToString(),
                                    // Formatear la fecha en “yyyy-MM-dd HH:mm:ss”
                                    fechaCreacion = reader.GetDateTime(reader.GetOrdinal("fechaCreacion"))
                                                             .ToString("yyyy-MM-dd HH:mm:ss")
                                };
                                resultado.anuncios.Add(anuncio);
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

            // 5) Devolver la lista de anuncios (puede ser vacía) junto a lista de errores (vacía si todo OK)
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

    }
}
