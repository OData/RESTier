using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.Data.Domain.Submit;

namespace System.Web.OData.Domain.Filters
{
    /// <summary>
    /// An ExceptionFilter that is capable of serializing well-known exceptions to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ODataDomainExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override async Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            IHttpActionResult exceptionResult = null;

            ValidationException validationException = actionExecutedContext.Exception as ValidationException;
            if (validationException != null)
            {
                exceptionResult = new NegotiatedContentResult<IEnumerable<ValidationResultDto>>(
                    HttpStatusCode.InternalServerError,
                    validationException.ValidationResults.Select(r => new ValidationResultDto(r)),
                    actionExecutedContext.ActionContext.RequestContext.Configuration.Services.GetContentNegotiator(),
                    actionExecutedContext.Request,
                    new MediaTypeFormatterCollection());
            }

            if (exceptionResult != null)
            {
                actionExecutedContext.Response = await exceptionResult.ExecuteAsync(cancellationToken);
            }
        }
    }
}
