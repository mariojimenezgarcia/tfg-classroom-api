using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace apiClassroom.Utils
{
    public static class JwtUtils
    {
        public static int ExtraerIdUsuario(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Intenta extraer el claim 'sub' o 'nameid' como id de usuario
            var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier);
            if (subClaim == null) throw new Exception("No se encuentra el id de usuario en el token.");

            return int.Parse(subClaim.Value);
        }

        public static int ExtraerRol(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Tu claim para el rol se llama "rol"
            var rolClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "rol");
            if (rolClaim == null) throw new Exception("No se encuentra el rol en el token.");

            return int.Parse(rolClaim.Value);
        }
    }
}
