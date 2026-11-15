using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace auth.webapi.Filters
{
    // Requires X-App-Id plus either a matching X-Api-Key (confidential clients) or an allowed
    // Origin (public clients). The verified app is available through HttpContext.GetApplicationClient().
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireApplicationClientAttribute : TypeFilterAttribute
    {
        public const string AppIdHeader = "X-App-Id";
        public const string ApiKeyHeader = "X-Api-Key";

        public RequireApplicationClientAttribute() : base(typeof(ApplicationClientFilter)) { }
    }

    public class ApplicationClientFilter : IAsyncActionFilter
    {
        internal const string ItemKey = "ApplicationClient";

        private readonly IApplicationClientService _applicationClientService;

        public ApplicationClientFilter(IApplicationClientService applicationClientService)
        {
            _applicationClientService = applicationClientService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var headers = context.HttpContext.Request.Headers;
            if (!Guid.TryParse(headers[RequireApplicationClientAttribute.AppIdHeader], out var appId))
                throw new ApplicationClientAuthenticationException();

            var apiKey = headers[RequireApplicationClientAttribute.ApiKeyHeader].ToString();
            var origin = headers.Origin.ToString();

            context.HttpContext.Items[ItemKey] = await _applicationClientService.AuthenticateAsync(appId, apiKey, origin);
            await next();
        }
    }

    public static class ApplicationClientHttpContextExtensions
    {
        public static ApplicationClient GetApplicationClient(this HttpContext httpContext)
        {
            return httpContext.Items[ApplicationClientFilter.ItemKey] as ApplicationClient
                ?? throw new InvalidOperationException($"No application client on this request. Add [{nameof(RequireApplicationClientAttribute)}] to the action.");
        }
    }
}
