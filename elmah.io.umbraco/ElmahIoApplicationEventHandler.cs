using Elmah.Contrib.WebApi;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Umbraco.Core;
using Umbraco.Web.Routing;

namespace Elmah.Io.Umbraco
{
    public class ElmahIoApplicationEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentLastChanceFinderResolver.Current.SetFinder(new NotFoundLastChangeFinder());
            GlobalConfiguration.Configuration.Services.Add(typeof(IExceptionLogger), new ElmahExceptionLogger());
            base.ApplicationStarting(umbracoApplication, applicationContext);
        }
    }

    public class NotFoundLastChangeFinder : IContentFinder
    {
        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            if (contentRequest.Is404)
            {
                ErrorSignal.FromCurrentContext().Raise(new HttpException(404, "Page not found"));
            }

            return false;
        }
    }
}
