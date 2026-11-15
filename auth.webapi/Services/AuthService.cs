using auth.webapi.Data;
using auth.webapi.DTO.Auth;
using auth.webapi.DTO.Auth.Login;
using auth.webapi.DTO.Auth.Register;
using auth.webapi.DTO.Auth.Session;
using auth.webapi.DTO.Auth.Token;
using auth.webapi.Helpers;
using auth.webapi.Interfaces;
using auth.webapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace auth.webapi.Services
{
    // The calling app is verified by RequireApplicationClientAttribute before these methods run
    public class AuthService : IAuthService
    {
        public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        // A rotated token presented again within this window is treated as a race between
        // two tabs or requests, not theft: the request fails but the family isn't revoked
        public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<AuthService> logger, ITokenService tokenService, IEmailService emailService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _tokenService = tokenService;
            _emailService = emailService;
            _context = context;
        }

        public async Task<CurrentUserDto> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new UserNotFoundException();
            var roles = await _userManager.GetRolesAsync(user);

            return new CurrentUserDto
            {
                Id = user.Id,
                FullName = user.FullName!,
                UserName = user.UserName!,
                Email = user.Email!,
                City = user.City!,
                AppId = user.ApplicationClientId,
                Roles = roles
            };
        }

        public async Task<List<UserSessionDto>> GetUserSessionsAsync(string userId, string? currentRefreshToken)
        {
            var now = DateTime.UtcNow;
            var currentHash = string.IsNullOrEmpty(currentRefreshToken) ? null : _tokenService.HashRefreshToken(currentRefreshToken);

            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.Expires > now)
                .OrderByDescending(rt => rt.Created)
                .Select(rt => new UserSessionDto
                {
                    TokenId = rt.Id,
                    Device = rt.Device,
                    IPAddress = rt.IPAddress,
                    IssuedAt = rt.Created,
                    ExpiresAt = rt.Expires,
                    IsCurrent = rt.TokenHash == currentHash
                })
                .ToListAsync();
        }

        public async Task RevokeSessionAsync(string userId, int sessionId)
        {
            // A session is the active token of a family; the older tokens in it are already revoked
            var revoked = await _context.RefreshTokens
                .Where(rt => rt.Id == sessionId && rt.UserId == userId && !rt.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));

            if (revoked == 0)
                throw new SessionNotFoundException();
        }

        public async Task<ResponseUserDto> LoginUserAsync(LoginUserDto loginDto, ApplicationClient app, string ipAddress, string userAgent)
        {
            var normalizedEmail = _userManager.NormalizeEmail(loginDto.Email.Trim());
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.ApplicationClientId == app.Id);

            if (user == null)
                throw new InvalidCredentialsException();

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
                throw new AccountLockedException();

            if (!result.Succeeded)
                throw new InvalidCredentialsException();

            var tokens = await IssueTokensAsync(user, app, Guid.NewGuid(), ipAddress, userAgent);
            return ToResponse(user, tokens);
        }

        public async Task<ResponseUserDto> RegisterUserAsync(CreateUserDto createUserDto, ApplicationClient app, string ipAddress, string userAgent)
        {
            var email = createUserDto.Email.Trim();
            var normalizedEmail = _userManager.NormalizeEmail(email);

            var emailTaken = await _userManager.Users
                .AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.ApplicationClientId == app.Id);

            if (emailTaken)
                throw new EmailAlreadyExistsException("User with this email already exists");

            var appUser = new AppUser
            {
                Email = email,
                City = createUserDto.City,
                FullName = createUserDto.FullName,
                UserName = email.ToLowerInvariant(),
                ApplicationClientId = app.Id,
            };

            var createdUser = await _userManager.CreateAsync(appUser, createUserDto.Password);
            if (!createdUser.Succeeded)
                throw new UserCreationFailedException(string.Join(" ", createdUser.Errors.Select(e => e.Description)));

            var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
            if (!roleResult.Succeeded)
                throw new UserCreationFailedException("Failed to create the user (role issue)");

            var tokens = await IssueTokensAsync(appUser, app, Guid.NewGuid(), ipAddress, userAgent);

            var safeName = System.Net.WebUtility.HtmlEncode(appUser.FullName);
            await _emailService.SendEmailAsync(appUser.Email, "Welcome to Our App", $"<h1>Hello {safeName}!</h1><p>Thanks for registering!</p>");

            return ToResponse(appUser, tokens);
        }

        public async Task<ResponseTokenRefreshRequest> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent)
        {
            var now = DateTime.UtcNow;
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);

            var currentToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .Include(rt => rt.App)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

            if (currentToken == null)
                throw new InvalidRefreshTokenException();

            if (currentToken.IsRevoked)
            {
                var wasRotated = currentToken.ReplacedByTokenId != null;
                var outsideGracePeriod = currentToken.RevokedAt < now - ReuseGracePeriod;
                if (wasRotated && outsideGracePeriod)
                {
                    // An old token from this login came back after it was rotated, so someone else
                    // has a copy. Revoke every token in the family to end both copies of the session.
                    _logger.LogWarning("Refresh token reuse detected for user {UserId}, family {FamilyId}. Revoking the family.",
                        currentToken.UserId, currentToken.FamilyId);
                    await RevokeFamilyAsync(currentToken.FamilyId, now);
                }
                throw new InvalidRefreshTokenException();
            }

            if (currentToken.Expires <= now)
                throw new RefreshTokenExpiredException();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var newTokens = await IssueTokensAsync(currentToken.User, currentToken.App, currentToken.FamilyId, ipAddress, userAgent);

            // Conditional update, so two concurrent refreshes with the same token can't both succeed
            var rotated = await _context.RefreshTokens
                .Where(rt => rt.Id == currentToken.Id && !rt.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, now)
                    .SetProperty(rt => rt.ReplacedByTokenId, newTokens.Entity.Id));

            if (rotated == 0)
            {
                // Another request rotated this token first
                await transaction.RollbackAsync();
                throw new InvalidRefreshTokenException();
            }

            await transaction.CommitAsync();

            return new ResponseTokenRefreshRequest
            {
                AccessToken = newTokens.AccessToken,
                RefreshToken = newTokens.RefreshToken,
                RefreshTokenExpiry = newTokens.Entity.Expires
            };
        }

        public async Task LogoutAsync(string userId, string? refreshToken, ApplicationClient app)
        {
            // Logout is idempotent: an unknown or already-revoked token is not an error
            if (string.IsNullOrEmpty(refreshToken))
                return;

            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            await _context.RefreshTokens
                .Where(rt => rt.TokenHash == tokenHash && rt.UserId == userId && rt.ApplicationClientId == app.Id && !rt.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow));
        }

        private async Task RevokeFamilyAsync(Guid familyId, DateTime now)
        {
            await _context.RefreshTokens
                .Where(rt => rt.FamilyId == familyId && !rt.IsRevoked)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(rt => rt.IsRevoked, true)
                    .SetProperty(rt => rt.RevokedAt, now));
        }

        private async Task<IssuedTokens> IssueTokensAsync(AppUser user, ApplicationClient app, Guid familyId, string ipAddress, string userAgent)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.CreateToken(user, app, roles);
            var refreshToken = _tokenService.CreateRefreshToken();

            var entity = new RefreshToken
            {
                TokenHash = _tokenService.HashRefreshToken(refreshToken),
                FamilyId = familyId,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.Add(RefreshTokenLifetime),
                UserId = user.Id,
                Device = userAgent,
                IPAddress = ipAddress,
                ApplicationClientId = app.Id
            };

            _context.RefreshTokens.Add(entity);
            await _context.SaveChangesAsync();

            return new IssuedTokens(accessToken, refreshToken, entity);
        }

        private static ResponseUserDto ToResponse(AppUser user, IssuedTokens tokens)
        {
            return new ResponseUserDto
            {
                Id = user.Id,
                FullName = user.FullName!,
                Email = user.Email!,
                UserName = user.UserName!,
                City = user.City!,
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                RefreshTokenExpiry = tokens.Entity.Expires
            };
        }

        private record IssuedTokens(string AccessToken, string RefreshToken, RefreshToken Entity);
    }
}
