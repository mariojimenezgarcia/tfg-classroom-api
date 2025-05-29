using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;

namespace apiClassroom.Models
{

    public class Errores
    {

        public enum Error
        {
            UsuarioIncorrecto = 100, // Cuando el usuario no es correcto
            ContrasenaIncorrecto = 101, // Cuando la clave no es correcto
            DatosInvalidos = 102, // Cuando alguno de los datos que se esperan vienen a null o vacíos
            ErrorEnBBDD = 108, // Cuando hay un error en la consulta de la BBDD
            UsuarioSinPermisos = 114, //El usuario no tiene permisos
            DatosIntroducidosMalBBDD = 116, //Se han introducido mal los datos en la BBDD
           
        }

        public static Dictionary<Error, string> GetListaErrores()
        {
            Dictionary<Error, string> errores = new Dictionary<Error, string>();
            errores.Add(Error.UsuarioIncorrecto, "Usuario no registrado "); // Cuando el usuario o la clave no son correctos
            errores.Add(Error.ContrasenaIncorrecto, "Contraseña incorrecta "); // Cuando el usuario o la clave no son correctos
            errores.Add(Error.DatosInvalidos, "Datos invalidos"); // Cuando alguno de los datos que se esperan vienen a null o vacíos
            errores.Add(Error.ErrorEnBBDD, "Error en el acceso a los datos"); // Cuando hay un error en la consulta de la BBDD
            errores.Add(Error.UsuarioSinPermisos, "El usuario no tiene ningún permiso");
            errores.Add(Error.DatosIntroducidosMalBBDD, "Los datos introducidos en la BBDD no son correctos");
         

            return errores;
        }

    }


}