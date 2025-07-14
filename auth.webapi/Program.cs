using System.IdentityModel.Tokens.Jwt;
using auth.webapi;
using auth.webapi.Extentions;
using auth.webapi.Interfaces;
using auth.webapi.Middleware;
using auth.webapi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLogging();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureIdentity();
builder.Services.ConfigureJwtAuthentication(builder.Configuration);
builder.Services.ConfigureCors();
builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Interfaces
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IConfigurationHelperService, ConfigurationHelper>();

// Swagger/OpenAPI
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// Configure middleware
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
//     app.UseSwaggerUI();
// }
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

// Add before app.UseAuthentication();
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
    if (token != null)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var expiration = jwtToken.ValidTo;
            Console.WriteLine($"Token expires at: {expiration} UTC, Current time: {DateTime.UtcNow} UTC");
            Console.WriteLine($"Is token expired: {DateTime.UtcNow > expiration}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading token: {ex.Message}");
        }
    }
    await next();
});

app.UseHttpsRedirection();
app.UseSwagger();
app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
