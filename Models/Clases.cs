
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Windows;

namespace apiClassroom.Models
{
    public class Clases
    {

        public class LoginRequest
        {
            public string email { get; set; }
            public string password { get; set; }
        }

        [Serializable]
        public class LoginResponse
        {
            public string token { get; set; }
            //public bool administrador { get; set; }
            public List<Error> error { get; set; }
            public string email { get; set; }
        }

        [Serializable]
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
        }

        public class PermisosRequest
        {
            public string token { get; set; }
        }


        public class Usuario
        {
            public int id { get; set; }
            public string nombreUsuario { get; set; }
            public string emailUsuario { get; set; }
            public override string ToString()
            {
                return $"ID: {id}, Nombre de Usuario: {nombreUsuario}, Email de Usuario: {emailUsuario}";
            }
        }

        public class CorreoRequest
        {
            public string Destinatario { get; set; }
            public string Asunto { get; set; }
            public string Cuerpo { get; set; }
        }

        public class CodigoRequest
        {
            public string codigo { get; set; }
            public string idUsuario { get; set; }
        }

        [Serializable]
        public class CodigoResponse
        {
            public string codigo { get; set; }
            public List<Error> error { get; set; }
        }

        public class passwordRequest
        {
            public string idUsuario { get; set; }
            public string password { get; set; }
            public string passwordRepeat { get; set; }
            public string codigoValidacion { get; set; }
        }

        [Serializable]
        public class passwordResponse
        {
            public string password { get; set; }
            public List<Error> error { get; set; }
        }
        public class UsuarioEmailRequest
        {
            public string email { get; set; }
        }

        [Serializable]
        public class tokenEmailResponse
        {
            public string token { get; set; }
            public int idUsuario { get; set; }
            public List<Error> error { get; set; }
        }
        public class UsuarioRequest
        {
            public string nombre { get; set; }
            public string email { get; set; }
            public string password { get; set; }
            public int rol { get; set; }
        }

        public class UsuarioResponse
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string email { get; set; }
            public int rol { get; set; }
        }
        public class Clase
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string codigo { get; set; }
            public int profesorId { get; set; }
        }

        public class SolicitudClase
        {
            public int id { get; set; }
            public int alumnoId { get; set; }
            public int claseId { get; set; }
            public DateTime fechaSolicitud { get; set; }
        }
    }
}