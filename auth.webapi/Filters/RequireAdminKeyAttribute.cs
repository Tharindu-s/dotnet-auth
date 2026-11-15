using System.Security.Cryptography;
using System.Text;
using auth.webapi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace auth.webapi.Filters
{
    // Requires an X-Admin-Key header that matches Security:AdminApiKey.
    // Fails closed: if the setting is missing, every request is rejected.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdminKeyAttribute : TypeFilterAttribute
    {
        public const string AdminKeyHeader = "X-Admin-Key";

        public RequireAdminKeyAttribute() : base(typeof(AdminKeyFilter)) { }
    }

    public class AdminKeyFilter : IAuthorizationFilter
    {
        private readonly IConfiguration _configuration;

        public AdminKeyFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var expected = _configuration["Security:AdminApiKey"];
            if (string.IsNullOrEmpty(expected))
                throw new AppUnauthorizedException("Admin endpoints are disabled. Set Security:AdminApiKey to enable them.");

            var provided = context.HttpContext.Request.Headers[RequireAdminKeyAttribute.AdminKeyHeader].ToString();

            // Hash both sides so the comparison is fixed-length and fixed-time
            var matches = CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
                SHA256.HashData(Encoding.UTF8.GetBytes(expected)));

            if (!matches)
                throw new AppUnauthorizedException("Invalid admin key.");
        }
    }
}
