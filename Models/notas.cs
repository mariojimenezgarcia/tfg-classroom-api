using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace apiClassroom.Models
{
	public class notas
	{
        public class PonerNotaRequest
        {
            public string token { get; set; }
            public int idEntrega { get; set; }
            public decimal nota { get; set; } // o int, según tu diseño; aquí uso decimal por si quieres notas con decimales
        }

        // 2. Response para poner nota
        public class PonerNotaResponse
        {
            public string mensaje { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // Tu clase Error ya existente:
        public class VerNotasRequest
        {
            public string token { get; set; }
        }

        // 2. DTO para cada fila de “ver notas”
        public class NotaData
        {
            public string titulo { get; set; }
            public decimal nota { get; set; }
            public string nombreUsuario { get; set; }
        }

        // 3. Response para ver notas
        public class VerNotasResponse
        {
            public List<NotaData> notas { get; set; } = new List<NotaData>();
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class VerClasesRequest
        {
            public string token { get; set; }
        }
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
        }
    }
}