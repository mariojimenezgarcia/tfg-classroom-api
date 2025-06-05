
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Windows;

namespace apiClassroom.Models
{
    public class Clases
    {


        [Serializable]
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
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
       
        // DTO que representa una fila de la tabla Clases
        public class ClaseData
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string CodigoAcceso { get; set; }
            public int UsuariosId { get; set; }
            public string Curso { get; set; }
            public string Aula { get; set; }
            public string Color { get; set; }
            
        }

        // Response de verClases
        public class VerClasesResponse
        {
            public List<ClaseData> clases { get; set; } = new List<ClaseData>();
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class VerAlumnosClasesRequest
        {
            public string token { get; set; }
            public int idClase { get; set; }
        }

        // Response: lista de nombres de alumnos + posibles errores
        public class VerAlumnosClaseResponse
        {
            public List<string> alumnos { get; set; } = new List<string>();
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class VerClasesRequest
        {
            public string token { get; set; }
        }


    }
}