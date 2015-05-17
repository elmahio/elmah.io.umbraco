using System.Web;
using Umbraco.Core;
using Umbraco.Web.Routing;

namespace Elmah.Io.Umbraco
{
    public class ElmahIoApplicationEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ContentLastChanceFinderResolver.Current.SetFinder(new NotFoundLastChangeFinder());
            base.ApplicationStarting(umbracoApplication, applicationContext);
        }
    }

    public class NotFoundLastChangeFinder : IContentFinder
    {
        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            if (contentRequest.Is404)
            {
                ErrorSignal.FromCurrentContext().Raise(new HttpException(404, "Page not fount"));
            }

            return false;
        }
    }
}