using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace apiClassroom.Models
{
	public class anuncios
	{
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
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
        }
    }
}