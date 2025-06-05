using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace apiClassroom.Models
{

	public class login
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
    }
}