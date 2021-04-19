using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Compilation;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common.Helpers;
using MassTransit;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Common
{
    public class CustomExceptionHandler : ExceptionHandler
    {
        public override void Handle(ExceptionHandlerContext context)
        {
            var id = context.Exception.Data.Contains("EXCEPTIONID")
                ? context.Exception.Data["EXCEPTIONID"] as string
                : "";

            var ex = context.Exception;
            var req = context.ExceptionContext.Request;

            if (ex is RequestFaultException f)
            {
                // handling masstransit errors here
                var type = GetExceptionTypeFromFault(f);
                if (type == null)
                {
                    context.Result = GetDefaultErrorResult(id, req);
                    return;
                }

                switch (type.Name)
                {
                    case nameof(BadRequestException):
                        context.Result = GetResultFromFault(f.Fault, id, HttpStatusCode.BadRequest, req);
                        break;
                    case nameof(ForbiddenException):
                        context.Result = GetResultFromFault(f.Fault, id, HttpStatusCode.Forbidden, req);
                        break;
                    default:
                        Log.Warning(
                            "Exception of type {TYPE} and id {ID} was thrown and {NAME} classified the error as an InternalServerError, truncating details because it was not handled",
                            type.Name, id, nameof(CustomExceptionHandler));
                        context.Result = GetDefaultErrorResult(id, req);
                        break;
                }

                return;
            }

            // handling non masstransit errors here
            if (context.Exception is BadRequestException bex)
            {
                context.Result = GetResultFromString(bex.Message, id, HttpStatusCode.BadRequest, req);
                return;
            }

            if (context.Exception is ForbiddenException fex)
            {
                context.Result = GetResultFromString(fex.Message, id, HttpStatusCode.Forbidden, req);
                return;
            }

            Log.Warning(
                "Exception of type {TYPE} and id {ID} was thrown and {NAME} classified the error as an InternalServerError, truncating details because it was not handled",
                ex.GetType().Name, id, nameof(CustomExceptionHandler));
            context.Result = GetDefaultErrorResult(id, req);
        }

        private Type GetExceptionTypeFromFault(RequestFaultException ex)
        {
            var typeName = ex.Fault?.Exceptions?.Select(e => e.ExceptionType).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            try
            {
                return BuildManager.GetType(typeName, true);
            }
            catch (Exception)
            {
                // we tried
            }

            return null;
        }

        private CustomHttpErrorResult GetDefaultErrorResult(string id, HttpRequestMessage request)
        {
            return new CustomHttpErrorResult
            {
                Id = id,
                Request = request,
                Content = "An unexpected error has occurred",
                StatusCode = HttpStatusCode.InternalServerError
            };
        }

        private CustomHttpErrorResult GetResultFromString(string message, string id, HttpStatusCode statusCode, HttpRequestMessage request)
        {
            return new CustomHttpErrorResult
            {
                Id = id,
                Request = request,
                Content = message,
                StatusCode = statusCode
            };
        }

        private CustomHttpErrorResult GetResultFromFault(Fault f, string id, HttpStatusCode statusCode, HttpRequestMessage request)
        {
            var exception = f?.Exceptions?.FirstOrDefault();
            if (exception == null)
            {
                return GetDefaultErrorResult(id, request);
            }

            return GetResultFromString(exception.Message, id, statusCode, request);
        }

        private class CustomHttpErrorResult : IHttpActionResult
        {
            public HttpRequestMessage Request { get; set; }

            public string Content { get; set; }
            public string Id { get; set; }
            public HttpStatusCode StatusCode { get; set; }

            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    Content += $"\n\nExceptionId: {Id}";
                }

                var error = new JObject {["exceptionMessage"] = Content};

                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new JsonContent(error),
                    RequestMessage = Request,
                    StatusCode = StatusCode
                };

                return Task.FromResult(response);
            }
        }
    }
}