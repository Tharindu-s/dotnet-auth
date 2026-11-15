using System.Security.Cryptography;
using auth.webapi.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    // Loads the RSA signing key once per process. Registered as a singleton.
    //
    // Key sources, in order:
    //   1. JWT:PrivateKey      - the PEM text itself (for example from a Kubernetes secret env var)
    //   2. JWT:PrivateKeyPath  - path to a PEM file (for example a mounted secret volume)
    //   3. Development only    - a generated key saved to dev-signing-key.pem in the content root
    //
    // The kid is the key's RFC 7638 thumbprint, so a new key always gets a new kid.
    public class SigningKeyService : ISigningKeyService
    {
        public const string DevKeyFileName = "dev-signing-key.pem";

        public RsaSecurityKey Key { get; }

        public SigningKeyService(IConfiguration configuration, IHostEnvironment environment, ILogger<SigningKeyService> logger)
        {
            var rsa = RSA.Create();
            var pem = configuration["JWT:PrivateKey"];
            var path = configuration["JWT:PrivateKeyPath"];

            if (!string.IsNullOrWhiteSpace(pem))
            {
                rsa.ImportFromPem(pem);
            }
            else if (!string.IsNullOrWhiteSpace(path))
            {
                rsa.ImportFromPem(File.ReadAllText(path));
            }
            else if (environment.IsDevelopment())
            {
                var devKeyPath = Path.Combine(environment.ContentRootPath, DevKeyFileName);
                if (!File.Exists(devKeyPath))
                {
                    using var generated = RSA.Create(2048);
                    File.WriteAllText(devKeyPath, generated.ExportRSAPrivateKeyPem());
                    logger.LogWarning("No signing key configured. Generated a development key at {Path}", devKeyPath);
                }
                rsa.ImportFromPem(File.ReadAllText(devKeyPath));
            }
            else
            {
                throw new InvalidOperationException(
                    "No JWT signing key configured. Set JWT:PrivateKey to a PEM-encoded RSA private key, or JWT:PrivateKeyPath to a PEM file.");
            }

            var key = new RsaSecurityKey(rsa);
            key.KeyId = Base64UrlEncoder.Encode(key.ComputeJwkThumbprint());
            Key = key;
        }
    }
}
