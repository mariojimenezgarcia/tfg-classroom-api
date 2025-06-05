using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace apiClassroom.Models
{
	public class tareas
	{
        public class CrearTareaRequest
        {
            public string token { get; set; }
            public string titulo { get; set; }
            public int puntuacion { get; set; }
            public string fecha_de_entrega { get; set; } // en formato ISO o “yyyy-MM-dd HH:mm:ss”
            public int idclase { get; set; }
        }

        public class CrearTareaResponse
        {
            public string titulo { get; set; }
            public string fecha_actual { get; set; }
            public string nombre_usuario { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // ======== 2) VER TAREAS ========

        public class VerTareasRequest
        {
            public string token { get; set; }
            public int idclase { get; set; }
        }

        public class TareaData
        {
            public int id { get; set; }
            public string titulo { get; set; }
            public int puntuacion { get; set; }
            public string fecha_entrega { get; set; }
            public string fecha_creacion { get; set; }
            public string creado_por { get; set; }
        }

        public class VerTareasResponse
        {
            public string estado { get; set; }
            public List<TareaData> tareas { get; set; } = new List<TareaData>();
            public List<Error> error { get; set; } = new List<Error>();
        }

        // ======== 3) ENTREGAR TAREA ========

        public class EntregarTareaRequest
        {
            public string token { get; set; }
            public int idtarea { get; set; }
            public string asunto { get; set; }
            public string archivo { get; set; }
        }

        public class EntregarTareaResponse
        {
            public string estado { get; set; }
            public string mensaje { get; set; }
            public string fecha_entrega { get; set; }
            public List<Error> error { get; set; } = new List<Error>();
        }

        // ======== 4) VER ENTREGAS ========

        public class VerEntregasRequest
        {
            public string token { get; set; }
            public int idtarea { get; set; }
        }

        public class EntregaData
        {
            public int id { get; set; }
            public string asunto { get; set; }
            public string archivo { get; set; }
            public string fecha_entrega { get; set; }
            public string nombre_alumno { get; set; }
        }

        public class VerEntregasResponse
        {
            public string estado { get; set; }
            public List<EntregaData> entregas { get; set; } = new List<EntregaData>();
            public List<Error> error { get; set; } = new List<Error>();
        }
        public class Error
        {
            public int codigo { get; set; }
            public string descripcion { get; set; }
        }
    }
}