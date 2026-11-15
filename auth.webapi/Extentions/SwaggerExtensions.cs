using auth.webapi.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace auth.webapi;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            c.OperationFilter<RequiredHeadersOperationFilter>();
        });

        return services;
    }
}

// Adds header fields to Swagger UI for endpoints that need the app or admin headers
public class RequiredHeadersOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<RequireApplicationClientAttribute>().Any())
        {
            AddHeader(operation, RequireApplicationClientAttribute.AppIdHeader, "Application client ID");
            AddHeader(operation, RequireApplicationClientAttribute.ApiKeyHeader, "Application client API key (not used by public clients)", required: false);
        }

        if (metadata.OfType<RequireAdminKeyAttribute>().Any())
            AddHeader(operation, RequireAdminKeyAttribute.AdminKeyHeader, "Value of Security:AdminApiKey");
    }

    private static void AddHeader(OpenApiOperation operation, string name, string description, bool required = true)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = required,
            Description = description,
            Schema = new OpenApiSchema { Type = "string" }
        });
    }
}
