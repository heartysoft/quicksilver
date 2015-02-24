using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Routing;
using Microsoft.Owin.Hosting;
using Owin;
using Topshelf;

namespace MyWindowsService
{
    class Program
    {
        private static void Main(string[] args)
        {

            // Start OWIN host 
            var root = "http://127.0.0.1:9555";

            HostFactory.Run(x =>
            {
                x.Service<MyWebServer>(s =>
                {
                    s.ConstructUsing(_ => new MyWebServer(root));
                    s.WhenStarted(_ => _.Start());
                    s.WhenStopped(_ => _.Stop());
                });
            });

        }
    }

    public class MyWebServer
    {
        private readonly string _root;
        private IDisposable _webapp;

        public MyWebServer(string root)
        {
            _root = root;
        }

        public void Start()
        {
            _webapp = WebApp.Start<StartUp>(_root);
        }

        public void Stop()
        {
            _webapp.Dispose();
        }
    }

    public class StartUp
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional, controller="home" }
            );
            appBuilder.UseWebApi(config);
        }
    }

    public class HomeController : ApiController
    {
        public List<string> Get()
        {
            return new[]
            {
                "Hello",
                "World",
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                ConfigurationManager.AppSettings["MySetting"]
            }.ToList();
        } 
    }
}
