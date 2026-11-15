using System.Security.Cryptography;
using auth.webapi.Data;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    // Runs at startup. Makes sure the Argus app (the admin console's own public client) and its first
    // admin exist. Safe to run on every start and from several replicas at once.
    public class ArgusBootstrapper
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ArgusOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ArgusBootstrapper> _logger;

        public ArgusBootstrapper(ApplicationDbContext context, UserManager<AppUser> userManager, IOptions<ArgusOptions> options,
            IConfiguration configuration, ILogger<ArgusBootstrapper> logger)
        {
            _context = context;
            _userManager = userManager;
            _options = options.Value;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RunAsync()
        {
            var app = await EnsureArgusAppAsync();
            await EnsureAdminAsync(app);
        }

        private async Task<ApplicationClient> EnsureArgusAppAsync()
        {
            var origins = ApplicationClientService.NormalizeOrigins(_options.AllowedOrigins);
            if (origins.Count == 0)
                throw new InvalidOperationException("Set Argus:AllowedOrigins to the URL the Argus frontend is served from, such as http://localhost:8081.");

            var app = await _context.ApplicationClient.FirstOrDefaultAsync(a => a.IsSystem);
            if (app != null)
            {
                if (!app.AllowedOrigins.SequenceEqual(origins))
                {
                    app.AllowedOrigins = origins;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated the Argus app's allowed origins to {Origins}", string.Join(", ", origins));
                }
                return app;
            }

            app = new ApplicationClient
            {
                Name = "Argus",
                IsSystem = true,
                IsPublic = true,
                RegistrationEnabled = false,
                AllowedOrigins = origins,
                // Console tokens are only for this service, so their audience is this service's own
                Audience = _configuration["JWT:Audience"] ?? throw new InvalidOperationException("JWT:Audience not found in configuration.")
            };
            _context.ApplicationClient.Add(app);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created the Argus app {AppId}", app.Id);
                return app;
            }
            catch (DbUpdateException)
            {
                // Another replica created it first (the unique index on IsSystem stopped this one)
                _context.ChangeTracker.Clear();
                return await _context.ApplicationClient.SingleAsync(a => a.IsSystem);
            }
        }

        private async Task EnsureAdminAsync(ApplicationClient app)
        {
            var normalizedEmail = _userManager.NormalizeEmail(_options.AdminEmail.Trim());
            var admin = await _userManager.Users
                .FirstOrDefaultAsync(u => u.ApplicationClientId == app.Id && u.NormalizedEmail == normalizedEmail);

            if (admin != null)
            {
                if (_options.ResetAdminPassword)
                    await ResetAdminPasswordAsync(admin);
                return;
            }

            // Only the very first start creates an admin. Changing AdminEmail later doesn't add another one.
            if (await _userManager.Users.AnyAsync(u => u.ApplicationClientId == app.Id))
                return;

            var (password, generated) = InitialPassword();
            var email = _options.AdminEmail.Trim();
            admin = new AppUser
            {
                Email = email,
                UserName = email.ToLowerInvariant(),
                EmailConfirmed = true,
                FullName = "Argus admin",
                City = "",
                ApplicationClientId = app.Id,
                MustChangePassword = true
            };

            IdentityResult result;
            try
            {
                result = await _userManager.CreateAsync(admin, password);
            }
            catch (DbUpdateException)
            {
                // Another replica created the admin first
                return;
            }

            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateUserName)))
                    return;
                throw new InvalidOperationException(
                    $"Could not create the Argus admin {email}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
            }

            var roleResult = await _userManager.AddToRoleAsync(admin, ArgusOptions.AdminRole);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not give {email} the {ArgusOptions.AdminRole} role.");

            LogPassword("Created the Argus admin", email, password, generated);
        }

        private async Task ResetAdminPasswordAsync(AppUser admin)
        {
            var (password, generated) = InitialPassword();

            admin.MustChangePassword = true;
            await _userManager.RemovePasswordAsync(admin);
            var result = await _userManager.AddPasswordAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Could not reset the Argus admin's password: {string.Join(" ", result.Errors.Select(e => e.Description))}");

            await _userManager.SetLockoutEndDateAsync(admin, null);
            await _userManager.ResetAccessFailedCountAsync(admin);

            LogPassword("Reset the password of the Argus admin", admin.Email!, password, generated);
            _logger.LogWarning("Argus:ResetAdminPassword is on. Turn it off, or the password is reset on every start.");
        }

        private (string Password, bool Generated) InitialPassword()
        {
            if (!string.IsNullOrWhiteSpace(_options.AdminPassword))
                return (_options.AdminPassword, false);

            // 20 random URL-safe characters, plus a suffix that covers every character class the password rules ask for
            return (Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(15)) + "-Aa1", true);
        }

        private void LogPassword(string action, string email, string password, bool generated)
        {
            if (generated)
                _logger.LogWarning("{Action} {Email} with the generated password {Password} . Sign in to Argus and change it.", action, email, password);
            else
                _logger.LogInformation("{Action} {Email} with the password from Argus:AdminPassword. Sign in to Argus and change it.", action, email);
        }
    }
}
