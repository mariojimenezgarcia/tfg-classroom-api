using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using apiClassroom.Models;

namespace apiClassroom.funciones
{
	public class Funciones
	{
      
        public static string GenerarCodigoAlfanumerico(int longitud)
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

        public static string Encriptar(string clave)
        {
            byte[] encriptado = Encoding.Unicode.GetBytes(clave);
            return Convert.ToBase64String(encriptado);
        }
    }
}