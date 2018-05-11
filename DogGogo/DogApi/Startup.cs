using Autofac;
using Autofac.Integration.WebApi;
using Beginor.Owin.StaticFile;
using DogService;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;
using SharpDapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Dependencies;

namespace DogApi
{
    public class Startup
    {
        public static IDependencyResolver DependencyResolver { get; set; }
        public void Configuration(IAppBuilder app)
        {
            // In OWIN you create your own HttpConfiguration rather than
            // re-using the GlobalConfiguration.
            var config = new HttpConfiguration();

            app.UseCors(Microsoft.Owin.Cors.CorsOptions.AllowAll);

            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings =
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };

            config.Routes.MapHttpRoute(
                "DefaultApi",
                "api/{controller}/{action}/{id}",
                new { id = RouteParameter.Optional });
            //config.Routes.MapHttpRoute(
            //    "DefaultActionApi",
            //    "{controller}/{action}/{id}",
            //    new { id = RouteParameter.Optional });

            var builder = new ContainerBuilder();
            // Register Web API controller in executing assembly.
            builder.RegisterApiControllers(GetType().Assembly).PropertiesAutowired();

            //注册 ApplicationService
            builder.RegisterAssemblyTypes(GetType().Assembly)
                .Where(type => typeof(BaseDao).IsAssignableFrom(type))
                .AsSelf().PropertiesAutowired()
                .InstancePerRequest();

            //注册数据库对象
            builder.Register<IDapperConnection>(ctx => new DapperConnection(new SqlConnection("server=localhost;port=3306;user id=root; password=lyx123456; database=studyplan; pooling=true; charset=utf8mb4"))).InstancePerRequest();

            // Create and assign a dependency resolver for Web API to use.
            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            DependencyResolver = config.DependencyResolver;

            // The Autofac middleware should be the first middleware added to the IAppBuilder.
            // If you "UseAutofacMiddleware" then all of the middleware in the container
            // will be injected into the pipeline right after the Autofac lifetime scope
            // is created/injected.
            //
            // Alternatively, you can control when container-based
            // middleware is used by using "UseAutofacLifetimeScopeInjector" along with
            // "UseMiddlewareFromContainer". As long as the lifetime scope injector
            // comes first, everything is good.
            app.UseAutofacMiddleware(container);

            // Again, the alternative to "UseAutofacMiddleware" is something like this:
            // app.UseAutofacLifetimeScopeInjector(container);
            //app.UseMiddlewareFromContainer<FirstMiddleware>();
            //app.UseMiddlewareFromContainer<ContextMiddleware>();

            // Make sure the Autofac lifetime scope is passed to Web API.

            app.UseAutofacWebApi(config);
            app.UseWebApi(config);

            app.UseStaticFile(new StaticFileMiddlewareOptions
            {
                RootDirectory = "../web",
                DefaultFile = "index.html",
                EnableETag = true,
                EnableHtml5LocationMode = true
            });
        }
    }
}
