using auth.webapi;
using auth.webapi.Extentions;
using auth.webapi.Interfaces;
using auth.webapi.Middleware;
using auth.webapi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();
builder.Services.ConfigureDatabase();
builder.Services.ConfigureIdentity();
builder.Services.ConfigureJwtAuthentication();
builder.Services.ConfigureCors();
builder.Services.ConfigureRateLimiting();
builder.Services.ConfigureForwardedHeaders();
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Interfaces
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApplicationClientService, ApplicationClientService>();
builder.Services.AddScoped<IConfigurationHelperService, ConfigurationHelper>();

// Swagger/OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Load the signing key now, so a missing key fails at startup instead of on the first login
app.Services.GetRequiredService<ISigningKeyService>();

// Configure middleware
app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.UseHttpsRedirection();
app.UseCors(ServiceExtentions.FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Lets the integration tests reference Program through WebApplicationFactory<Program>
public partial class Program { }
