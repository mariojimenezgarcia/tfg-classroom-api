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
            UsuarioContrasenaIncorrecto = 100, // Cuando el usuario o la clave no son correctos
            DatosInvalidos = 101, // Cuando alguno de los datos que se esperan vienen a null o vacíos
            ApiKeyInvalida = 102, // Cuando la apikey no es válida
            UsuarioCaducado = 103, // Cuando el usuario ha caducado
            UsuarioBloqueado = 104, // Cuando el usuario está bloqueado
            ApiKeyCaducada = 105, // Cuando caduca la APIkey
            FuncionNoDisponible = 106, // Cuando no tiene permisos para llamar a esa función
            HostDesconocido = 107, // Cuando la ip de origen no está autorizada
            ErrorEnBBDD = 108, // Cuando hay un error en la consulta de la BBDD
            NoExistenDatosParaLaConsulta = 109, // Si la consulta no devuelve datos
            FechaCaducidadUsuario = 113, //La fecha de caducidad de la cuenta de usuario ha vencido
            UsuarioSinPermisos = 114, //El usuario no tiene permisos
            UsuarioSinNubes = 115, //El usuario no tiene acceso a ninguna nube
            DatosIntroducidosMalBBDD = 116, //Se han introducido mal los datos en la BBDD
            UsuarioNoExiste = 119, //El usuario no existe
            ErrorMandarEmail = 120, //Error al mandar el email
            CodigoVerificacionIncorrecto = 121, //El código de validación no es correcto
            CodigoCaducado = 122, //El código de validación ha caducado
            ContraseñaVacia = 123, //La contraseña no puede estar vacía
            ContraseñasNoCoinciden = 124, //Las contraseñas no coinciden
            CodigoVerificacionVacio = 125, //El código de validación no puede estar vacío
            CodigoYaProcesado = 126, //El código de validación ya ha sido procesado
            IpNoAutorizada = 127, //La ip no está autorizada
            ErrorEnTransaccion = 128, //Hubo un error en la transaccion
        }

        public static Dictionary<Error, string> GetListaErrores()
        {
            Dictionary<Error, string> errores = new Dictionary<Error, string>();
            errores.Add(Error.UsuarioContrasenaIncorrecto, "Usuario o contraseña incorrecto"); // Cuando el usuario o la clave no son correctos
            errores.Add(Error.DatosInvalidos, "Datos invalidos"); // Cuando alguno de los datos que se esperan vienen a null o vacíos
            errores.Add(Error.ApiKeyInvalida, "Apikey invalida"); // Cuando la apikey no es válida
            errores.Add(Error.UsuarioCaducado, "Usuario caducado"); // Cuando el usuario ha caducado
            errores.Add(Error.UsuarioBloqueado, "Usuario bloqueado"); // Cuando el usuario está bloqueado
            errores.Add(Error.ApiKeyCaducada, "Apikey caducada"); // Cuando caduca la APIkey
            errores.Add(Error.FuncionNoDisponible, "Función no disponible"); // Cuando no tiene permisos para llamar a esa función
            errores.Add(Error.HostDesconocido, "Host desconocido"); // Cuando la ip de origen no está autorizada
            errores.Add(Error.ErrorEnBBDD, "Error en el acceso a los datos"); // Cuando hay un error en la consulta de la BBDD
            errores.Add(Error.NoExistenDatosParaLaConsulta, "No existen datos para esa consulta"); // Si la consulta no devuelve datos
            errores.Add(Error.FechaCaducidadUsuario, "El usuario ya no tiene acceso");
            errores.Add(Error.UsuarioSinPermisos, "El usuario no tiene ningún permiso");
            errores.Add(Error.UsuarioSinNubes, "El usuario no tiene acceso a ninguna nube");
            errores.Add(Error.DatosIntroducidosMalBBDD, "Los datos introducidos en la BBDD no son correctos");
            errores.Add(Error.UsuarioNoExiste, "El usuario no existe");
            errores.Add(Error.ErrorMandarEmail, "Error al mandar el email");
            errores.Add(Error.CodigoVerificacionIncorrecto, "El código de verificación no es correcto");
            errores.Add(Error.CodigoCaducado, "El código de verificación ha caducado");
            errores.Add(Error.ContraseñaVacia, "La contraseña no puede estar vacía");
            errores.Add(Error.ContraseñasNoCoinciden, "Las contraseñas no coinciden");
            errores.Add(Error.CodigoVerificacionVacio, "El código de verificación no puede estar vacío");
            errores.Add(Error.CodigoYaProcesado, "El código de verificación ya ha sido procesado");
            errores.Add(Error.IpNoAutorizada, "Acceso no válido");
            errores.Add(Error.ErrorEnTransaccion, "Ha habido algún fallo a hacer la transacción");
            return errores;
        }

    }


}