using Elmah.Contrib.WebApi;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Umbraco.Core.Composing;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace Elmah.Io.Umbraco
{
    public class ElmahIoComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.SetContentLastChanceFinder<NotFoundLastChangeFinder>();
            GlobalConfiguration.Configuration.Services.Add(typeof(IExceptionLogger), new ElmahExceptionLogger());
        }
    }

    public class NotFoundLastChangeFinder : IContentLastChanceFinder
    {
        public bool TryFindContent(PublishedRequest frequest)
        {
            if (frequest.Is404)
            {
                ErrorSignal.FromCurrentContext().Raise(new HttpException(404, "Page not found"));
            }

            return false;
        }
    }
}
