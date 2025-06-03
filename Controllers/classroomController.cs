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
            int Id = 0;
            //encripta la password para guaradarla codificada en la base de datos
            var claveencriptada = Encriptar(login.password);
            //si email o password vienen null
            if (string.IsNullOrWhiteSpace(login.email) || string.IsNullOrWhiteSpace(login.password))
            {
                MandarError((int)Errores.Error.DatosInvalidos, listaerrores[Errores.Error.DatosInvalidos]);
            }
            else
            {
                using (var conexion = new SqlConnection(ConexionBBDD))

                {
                    try
                    {
                        conexion.Open();

                        // lógica de lectura de usuario
                        string clave = "";
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
                                    resultado.email = (string)lector["email"];
                                }
                                else
                                {
                                    MandarError((int)Errores.Error.UsuarioIncorrecto,
                                                listaerrores[Errores.Error.UsuarioIncorrecto]);
                                }
                            }
                        }
                        //verificamos que la clave BBDD es la misma a la introdcida
                        if (claveencriptada != clave)
                        {
                            MandarError((int)Errores.Error.ContrasenaIncorrecto, listaerrores[Errores.Error.ContrasenaIncorrecto]);
                        }
                        else
                        {
                            // ————————— Generar JWT —————————
                            var claims = new[]
                            {
                            new Claim(JwtRegisteredClaimNames.Sub, Id.ToString()),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim("rol",   rol.ToString())

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
