using System.Web.Http;
using System.Web.Http.Cors;    // <— Necesario para EnableCorsAttribute

namespace apiClassroom
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // 1) Habilitamos CORS globalmente: 
           
            var corsAttr = new EnableCorsAttribute(
                origins: "http://localhost:4200",  
                headers: "*",                      
                methods: "*");             
            config.EnableCors(corsAttr);

            // 2) Resto de configuración de rutas
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // … cualquier otra configuración global
        }
    }
}

