
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
        public class CrearClaseRequest
        {
            public string nombre { get; set; }
            public string curso { get; set; }
            public string aula { get; set; }
            public string color { get; set; }
            public string token { get; set; }
        }

        // 2. Clase para la respuesta de CrearClase
        public class CrearClaseResponse
        {
            // Id de la clase recién creada
            public int Id { get; set; }

            // El código de acceso generado de 7 dígitos
            public string CodigoAcceso { get; set; }

            // Lista de errores (si los hay)
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class UnirseClaseRequest
        {
            public string token { get; set; }
            public string codigoAcceso { get; set; }
        }

        // Response de unirse a una clase
        public class UnirseClaseResponse
        {
            // Mensaje de éxito, p.ej. “Usuario registrado en la clase”
            public string mensaje { get; set; }

            // Lista de errores (vacía si todo ha ido bien)
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class AnuncioRequest
        {
            public string token { get; set; }
            public string contenido { get; set; }
            public int idClase { get; set; }
        }

        public class AnuncioResponse
        {
            public string nombreUsuario { get; set; }
            public string contenido { get; set; }
            public string fechaCreacion { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class VisualizarAnunciosRequest
        {
            public string token { get; set; }
            public int idClase { get; set; }
        }

        // DTO para devolver cada anuncio
        public class AnuncioData
        {
            public string nombreUsuario { get; set; }
            public string contenido { get; set; }
            public string fechaCreacion { get; set; }
        }

        // Response de visualizar anuncios
        public class VisualizarAnunciosResponse
        {
            // Lista de anuncios encontrados
            public List<AnuncioData> anuncios { get; set; } = new List<AnuncioData>();

            // Lista de errores (vacía si todo OK)
            public List<Error> error { get; set; } = new List<Error>();
        }
    }
}