using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace apiClassroom.Models
{
    public class usuario
    {
        public class UsuarioRequest
        {
            public string nombre { get; set; }
            public string email { get; set; }
            public string password { get; set; }
            public int rol { get; set; }
        }

        public class CrearUsuarioResponse
        {
            public string estado { get; set; }
            public string mensaje { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // -------------------------------
        // 2) DTO y RESPONSE para Obtener todos los Usuarios
        // -------------------------------
        public class UsuarioData
        {
            public int id { get; set; }
            public string nombre { get; set; }
            public string email { get; set; }
            public int rol { get; set; }
        }

        public class ObtenerUsuariosResponse
        {
            public List<UsuarioData> usuarios { get; set; } = new List<UsuarioData>();
            public List<Error> error { get; set; } = new List<Error>();
        }

        // -------------------------------
        // 3) RESPONSE para Actualizar Usuario
        // -------------------------------
        public class ActualizarUsuarioResponse
        {
            public string estado { get; set; }
            public string mensaje { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // -------------------------------
        // 4) RESPONSE para Eliminar Usuario
        // -------------------------------
        public class EliminarUsuarioResponse
        {
            public string estado { get; set; }
            public string mensaje { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // -------------------------------
        // Clase genérica de error
        // -------------------------------
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
        }
    }
}