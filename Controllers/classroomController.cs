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
    [System.Web.Http.RoutePrefix("api/Classroom")]
    public class classroomController : ApiController
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

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("Login")]
        public JObject Login([FromBody] Models.Clases.LoginRequest login)
        {
            var resultado = new Clases.LoginResponse();
            var error = new Clases.Error();
            JObject response = new JObject();
            string ip_publica;
            int Id = -1;

            var claveencriptada = Encriptar(login.password);

            if (string.IsNullOrWhiteSpace(login.email) || string.IsNullOrWhiteSpace(login.password))
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
            }
            else
            {
                ip_publica = GetUserIP();
                using (var conexion = new SqlConnection(ConexionBBDD))

                {
                    try
                    {
                        conexion.Open();

                        // —————— tu lógica de lectura de usuario ——————
                        string clave = "";
                        int intentosFallidos = 0;
                        DateTime? fecha_cad_usuario = null;
                        bool cuenta_bloqueada = false;
                        int rol = 0;

                        const string consulta = "SELECT * FROM usuarios WHERE email=@email";
                        using (var comando = new SqlCommand(consulta, conexion))
                        {
                            comando.Parameters.AddWithValue("@email", login.email);
                            using (var lector = comando.ExecuteReader())
                            {
                                if (lector.Read())
                                {
                                    Id = (int)lector["id"];
                                    clave = (string)lector["password"];
                                    rol = (int)lector["rol"];
                                    intentosFallidos = (int)lector["intentos_fallidos"];
                                    cuenta_bloqueada = (bool)lector["cuenta_bloqueada"];
                                    if (lector["fecha_cad_usuario"] != DBNull.Value)
                                        fecha_cad_usuario = (DateTime)lector["fecha_cad_usuario"];
                                    resultado.email = (string)lector["email"];
                                }
                                else
                                {
                                    MandarError((int)Errores.Error.UsuarioContrasenaIncorrecto,
                                                listaerrores[Errores.Error.UsuarioContrasenaIncorrecto]);
                                }
                            }
                        }


                        if (fecha_cad_usuario.HasValue && DateTime.Now > fecha_cad_usuario.Value)
                            MandarError((int)Errores.Error.UsuarioCaducado, listaerrores[Errores.Error.UsuarioCaducado]);
                        else if (cuenta_bloqueada)
                            MandarError((int)Errores.Error.UsuarioBloqueado, listaerrores[Errores.Error.UsuarioBloqueado]);
                        else if (claveencriptada != clave)
                        {
                            // … tu lógica de intentos fallidos y bloqueo …
                            MandarError((int)Errores.Error.UsuarioContrasenaIncorrecto, listaerrores[Errores.Error.UsuarioContrasenaIncorrecto]);
                        }
                        else
                        {
                            // ————————— Generar JWT —————————
                            var claims = new[]
                            {
                            new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()),
                            new Claim(JwtRegisteredClaimNames.UniqueName, resultado.email),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim("rol",                             rol.ToString())

                        };

                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
                            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                            var tokenDescriptor = new SecurityTokenDescriptor
                            {
                                Subject = new ClaimsIdentity(claims),
                                Expires = DateTime.UtcNow.AddMinutes(_jwtExpiry),
                                Issuer = _jwtIssuer,
                                Audience = _jwtAudience,
                                SigningCredentials = creds
                            };

                            var tokenHandler = new JwtSecurityTokenHandler();
                            var token = tokenHandler.CreateToken(tokenDescriptor);
                            var jwt = tokenHandler.WriteToken(token);

                            resultado.token = jwt;
                        }
                    }
                    catch (Exception ex)
                    {
                        MandarError((int)Errores.Error.ErrorEnBBDD, listaerrores[Errores.Error.ErrorEnBBDD]);
                    }
                }
            }

            // Si hubo errores, limpia el token
            if (errorList.Count > 0)
                resultado.token = "";

            resultado.error = errorList;
            return JObject.Parse(JsonConvert.SerializeObject(resultado));
        }
        [HttpPost]
        [Route("ConsultarPermisos")]
        public JObject ConsultarPermisos([FromBody] PermisosRequest request)
        {
            var respuesta = new JObject();
            ClaimsPrincipal principal;

            // 1) Validar el token JWT y extraer claims
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
                respuesta["estado"] = "error";
                respuesta["mensaje"] = "Token inválido o caducado.";
                return respuesta;
            }

            // (Opcional) Lista las claims para depuración
            //foreach (var c in principal.Claims)
            //    Debug.WriteLine($"Claim: {c.Type} = {c.Value}");

            // 2) Leer userId y rol de los claims de forma segura
            var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                           ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                           ?? principal.FindFirst("sub");

            if (subClaim == null)
            {
                respuesta["estado"] = "error";
                respuesta["mensaje"] = "Claim 'sub' no encontrada en el token.";
                return respuesta;
            }
            int idUsuario = int.Parse(subClaim.Value);

            var rolClaim = principal.FindFirst("rol");
            if (rolClaim == null)
            {
                respuesta["estado"] = "error";
                respuesta["mensaje"] = "Claim 'rol' no encontrada en el token.";
                return respuesta;
            }
            int rolUsuario = int.Parse(rolClaim.Value);

            // 3) Consultar permisos en la BBDD
            var permisos = new List<string>();
            using (var conexion = new SqlConnection(ConexionBBDD))
            {
                conexion.Open();

                const string sqlPermisos = @"
            SELECT nombrePermiso 
              FROM permisos 
             WHERE idRol = @rol";

                using (var cmd = new SqlCommand(sqlPermisos, conexion))
                {
                    cmd.Parameters.AddWithValue("@rol", rolUsuario);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            permisos.Add(reader["nombrePermiso"] as string);
                    }
                }
            }

            // 4) Devolver resultado
            respuesta["estado"] = "ok";
            respuesta["idUsuario"] = idUsuario;
            respuesta["rol"] = rolUsuario;
            respuesta["permisos"] = JArray.FromObject(permisos);
            return respuesta;
        }

        //_________________________________________________________________________________________________________________________________________



        /*  [System.Web.Http.HttpPost]
          [System.Web.Http.Route("Comprobar/Email")]
          public JObject ComprobarEmail(Models.Clases.UsuarioEmailRequest usuario)
          {
              Logger.InfoFormat($"***************************   COMPROBAR EMAIL ***************************");
              var tokenEmail = new Clases.tokenEmailResponse();

              // List<Clases.Error> errores = new List<Error>();
              errorList = new List<Error>();

              var datosUsuario = new Clases.Usuario();

              bool existeErrores = false;
              string codigoClave = string.Empty;
              RegistroLog registro = new RegistroLog();


              //Comprobamos si el email viene vacío o nulo
              if (string.IsNullOrWhiteSpace(usuario.email))
              {
                  int codigo = (int)Errores.Error.DatosInvalidos;
                  string descripcion = listaerrores[Errores.Error.DatosInvalidos];
                  MandarError(codigo, descripcion);
                  existeErrores = true;
              }

              //No hay errores de email vacío
              else
              {
                  EscribeLog(registro);
                  Logger.InfoFormat($"COMPROBAR EMAIL: correo enviado: {usuario.email}");
                  string consultaUsuario = "select * from usuarios where correo=@email";

                  using (MySqlConnection conexion = new MySqlConnection(ConexionBBDD))
                  {
                      try
                      {
                          conexion.Open();
                          using (var comando = new MySqlCommand(consultaUsuario, conexion))
                          {
                              comando.Parameters.AddWithValue("email", usuario.email);
                              using (MySqlDataReader lector = comando.ExecuteReader())
                              {
                                  //Si no lee registros:
                                  if (!lector.Read())
                                  {
                                      int codigo = (int)Errores.Error.UsuarioNoExiste;
                                      string descripcion = listaerrores[Errores.Error.UsuarioNoExiste];
                                      MandarError(codigo, descripcion);
                                      existeErrores = true;
                                  }
                                  //Guardamos los datos del usuario, quizá nos hagan falta
                                  else
                                  {
                                      datosUsuario.id = lector.GetInt32(lector.GetOrdinal("id"));
                                      datosUsuario.emailUsuario = lector.GetString(lector.GetOrdinal("correo"));
                                      datosUsuario.nombreUsuario = lector.GetString(lector.GetOrdinal("usuario"));
                                      codigoClave = GenerarCodigo();
                                      //Si el usuario existe, generamos un token
                                      tokenEmail.token = codigoClave;
                                      Logger.InfoFormat($"COMPROBAR EMAIL: Token generado: {tokenEmail.token}");
                                      Logger.InfoFormat($"COMPROBAR EMAIL: Los datos del usuario son {datosUsuario.ToString()}");
                                      registro = new RegistroLog();
                                      registro.modulo = "Email";
                                      registro.accion = "Comprobar email";
                                      registro.descripcion = $"El email enviado  por el usuario es {usuario.email}";
                                      registro.idUsuario = datosUsuario.id;
                                      registro.esUsuario = true;
                                      EscribeLog(registro);

                                      registro = new RegistroLog();
                                      registro.modulo = "Email";
                                      registro.accion = "Comprobar email";
                                      registro.descripcion = $"El token que se le ha enviado al usuario es {tokenEmail.token}";
                                      registro.idUsuario = datosUsuario.id;
                                      registro.esUsuario = true;
                                      EscribeLog(registro);
                                  }
                              }
                          }

                          if (existeErrores == false)
                          {
                              //Ahora tenemos que actualizar la tabla cambios_password
                              string insertarEnTabla = "insert into cambios_password (idUsuario, email, codigo, fechaCaducidadCodigo, fechaCreacion) values (@idUsuario, @correo, @codigo, @fechaCaducidadCodigo, @fechaCreacion)";
                              using (var comando = new MySqlCommand(insertarEnTabla, conexion))
                              {
                                  comando.Parameters.AddWithValue("idUsuario", datosUsuario.id);
                                  comando.Parameters.AddWithValue("correo", datosUsuario.emailUsuario);
                                  comando.Parameters.AddWithValue("codigo", codigoClave);
                                  comando.Parameters.AddWithValue("fechaCaducidadCodigo", DateTime.Now.AddMinutes(5));
                                  comando.Parameters.AddWithValue("fechaCreacion", DateTime.Now);
                                  comando.ExecuteNonQuery();
                                  Logger.InfoFormat($"COMPROBAR EMAIL: Token insertado en la tabla cambios_password");
                                  Logger.InfoFormat("Datos introducidos: idUsuario={0}, email={1}, codigo={2}, fechaCaducidadCodigo={3}, fechaCreacion={4}",
                                  datosUsuario.id, datosUsuario.emailUsuario, codigoClave, DateTime.Now.AddMinutes(5), DateTime.Now);
                              }
                          }
                      }
                      catch (Exception e)
                      {
                          int codigo = (int)Errores.Error.ErrorEnBBDD;
                          string descripcion = listaerrores[Errores.Error.ErrorEnBBDD];
                          MandarError(codigo, descripcion);
                          existeErrores = true;
                          Logger.ErrorFormat($"ERROR COMPROBAR EMAIL. \nException; {e.Message}\n{e.StackTrace}");
                          registro = new RegistroLog();
                          registro.modulo = "Email";
                          registro.accion = "Comprobar email";
                          registro.descripcion = $"Ha habido algún error. {e.Message}";
                          registro.idUsuario = datosUsuario.id;
                          registro.esUsuario = true;
                          EscribeLog(registro);
                      }
                  }//Fin conexión

              }

              string fecha = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));
              String asunto = $"Restablecimiento de tu contraseña en Asistente Web - {fecha}";

              string cuerpoCorreo = $@"
                  <p style='font-family: Arial, sans-serif; color: #333333; font-size: 1em;'>
                      Hola <b>{datosUsuario.nombreUsuario}</b>:
                  </p>
                  <p style='font-family: Arial, sans-serif; color: #333333; font-size: 1em;'>
                      Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <b>Asistente Web</b>. 
                      Si no has solicitado este cambio, por favor, ignora este mensaje.
                  </p>
                  <p style='font-family: Arial, sans-serif; color: #333333; font-size: 1em;'>
                      Para restablecer tu contraseña, usa el siguiente código:
                  </p>
                  <p style='font-family: Arial, sans-serif; font-size: 1.2em;'>
                      <b style='color: #005599;'>{codigoClave}</b>
                  </p>
                  <p style='font-family: Arial, sans-serif; color: #333333; font-size: 1em;'>
                      Este enlace es válido durante 
                      <b style='color: #005599;'>5 minutos</b>.
                      Si no restableces tu contraseña en ese tiempo, deberás solicitarlo de nuevo.
                  </p>
                  <br/><br/><br/><br/><br/><br/>
                  <table>
                      <tr>
                          <td valign='top' style='padding:0 8px 0 0;'>
                              <a href='http://www.tecnoaccesos.com'>
                                  <img src='https://i.ibb.co/s1fpGDW/Logo-Tecno.png' width='100' height='25'/>
                              </a>
                          </td>
                          <td valign='top' style='font-size:80%;font-family:Arial;padding:0 0 0 8px;'>
                              <div style='font-size:1.2em;'><b>Asistente Web Versión 1.0</b></div>
                              <b><span style='font-size:0.9em;color:#005599;'><hr size='2.5px' color='#005599'></hr></span></b>
                              <div style='line-height:1em;font-size:1em;'></div>
                              <div><span style='font-size:0.9em;color:#005599;'></span></div>
                              <div><span style='font-size:0.9em;color:#005599;'></span>
                                  <span><a href='http://www.tecnoaccesos.com' target='_blank' style='color:#005599;text-decoration:none;font-size:0.9em;'>www.tecnoaccesos.com</a></span>
                              </div>
                          </td>
                      </tr>
                  </table>
                  <div style='line-height:1em;font-size:1em;'></div>
                  <div style='color:#005599;font-size:10px;'>
                      Este mensaje de correo electrónico puede contener información confidencial o legalmente protegida y está destinado únicamente para el uso del destinatario(s) previsto. Cualquier divulgación, difusión, distribución, copia o la toma de cualquier acción basada en la información aquí contenida está prohibido.
                  </div>
                  <div style='color:#005599;font-size:10px;'>
                      This e-mail message may contain confidential or legally privileged information and is intended only for the use of the intended recipient(s). Any unauthorized disclosure, dissemination, distribution, copying or the taking of any action in reliance on the information herein is prohibited.
                  </div>
              ";
              if (existeErrores == false)
              {
  #if DEBUG
                  AlternateView vistaHtml = AlternateView.CreateAlternateViewFromString(cuerpoCorreo, null, "text/html");
                  var request = new Clases.CorreoRequest();
                  request.Destinatario = usuario.email;
                  request.Asunto = $"Restablecimiento de tu contraseña en Asistente Web - {fecha}";

                  try
                  {
                      using (SmtpClient smtpClient = new SmtpClient("smtp.gmail.com"))
                      {
                          smtpClient.Port = 587;
                          smtpClient.Credentials = new NetworkCredential("pruebasasistenteweb@gmail.com", "dqda kdch uekn djli");
                          smtpClient.EnableSsl = true;

                          // Creación del mensaje de correo
                          using (MailMessage mail = new MailMessage())
                          {
                              mail.From = new MailAddress("asistentewebpruebas@gmail.com");
                              mail.Subject = request.Asunto;
                              mail.AlternateViews.Add(vistaHtml);

                              mail.IsBodyHtml = true; // Si el cuerpo del correo contiene HTML
                              mail.SubjectEncoding = System.Text.Encoding.UTF8;

                              // Añadimos destinatario
                              mail.To.Add(request.Destinatario);

                              // Enviar el correo
                              smtpClient.Send(mail);
                              Logger.InfoFormat($"COMPROBAR EMAIL: Correo enviado exitosamente");
                              //v1Controller.Log.InfoFormat($"COMPROBAR EMAIL: EMAIL: Cuerpo enviado: {mail.Body}");
                          }
                      }
                      registro = new RegistroLog();
                      registro.modulo = "Email";
                      registro.accion = "Comprobar email";
                      registro.descripcion = $"Email con el código mandado al usuario {usuario.email} correctamente ";
                      registro.idUsuario = datosUsuario.id;
                      registro.esUsuario = true;
                      EscribeLog(registro);

                  }
                  catch (Exception ex)
                  {
                      int codigo = (int)Errores.Error.ErrorMandarEmail;
                      string descripcion = listaerrores[Errores.Error.ErrorMandarEmail];
                      MandarError(codigo, descripcion);
                      Logger.Error($"ERROR al enviar correo: {ex.Message}\nStackTrace: {ex.StackTrace}");
                      existeErrores = true;
                      registro = new RegistroLog();
                      registro.modulo = "Email";
                      registro.accion = "Comprobar email";
                      registro.descripcion = $"Hubo algún error {ex.Message}";
                      registro.idUsuario = datosUsuario.id;
                      registro.esUsuario = true;
                      EscribeLog(registro);
                  }
  #else
                  //Si comentamos la siguiente linea y descomentamos lo siguiente, usamos mi cuenta
                   Envia_Correo(cuerpoCorreo, asunto, usuario.email);
                   registro = new RegistroLog();
                   registro.modulo = "Email";
                   registro.accion = "Comprobar email";
                   registro.descripcion = $"Email con el código mandado al usuario {usuario.email} correctamente ";
                   registro.idUsuario = datosUsuario.id;
                   registro.esUsuario=true;
                   EscribeLog(registro);
  #endif
              }

              tokenEmail.token = !existeErrores ? codigoClave : "";
              tokenEmail.idUsuario = !existeErrores ? datosUsuario.id : -1;
              tokenEmail.error = existeErrores ? errorList : new List<Clases.Error>();

              //Para que el GC pueda eliminarlos al no tener referencias

              datosUsuario = null;

              Logger.InfoFormat($"***************************   FIN COMPROBAR EMAIL ***************************\n");
              string jsonString = JsonConvert.SerializeObject(tokenEmail);
              JObject json = JObject.Parse(jsonString);
              return json;
          }*/


        #region Funciones internas

        #region Login_ApiKey

        public string GetUserIP()
        {
            HttpRequestMessage request = null;
            request = request ?? Request;

            // Web-hosting. Needs reference to System.Web.dll

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                dynamic ctx = request.Properties["MS_HttpContext"];

                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }

            // Self-hosting. Needs reference to System.ServiceModel.dll.
            if (request.Properties.ContainsKey("System.ServiceModel.Channels.RemoteEndpointMessageProperty"))
            {
                dynamic remoteEndpoint = request.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"];

                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }

            // Self-hosting using Owin. Needs reference to Microsoft.Owin.dll.
            if (request.Properties.ContainsKey("MS_OwinContext"))
            {
                dynamic owinContext = request.Properties["MS_OwinContext"];

                if (owinContext != null)
                {
                    return owinContext.Request.RemoteIpAddress;
                }
            }

            return null;
        }

        #endregion Login_ApiKey

        #region Generales
   



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


        #endregion

        #endregion
    }
}
