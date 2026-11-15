using System.Security.Cryptography;
using auth.webapi.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace auth.webapi.Services
{
    // Loads the RSA signing key once per process. Registered as a singleton.
    //
    // Key sources, in order:
    //   1. JWT:PrivateKey      - the PEM text itself (for example from a Kubernetes secret env var)
    //   2. JWT:PrivateKeyPath  - path to a PEM file (for example a mounted secret volume). With
    //                            JWT:GenerateIfMissing, a new key is written there on first start
    //                            (for example into a Docker volume, as docker-compose.yml does).
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
                if (configuration.GetValue<bool>("JWT:GenerateIfMissing"))
                    GenerateKeyFileIfMissing(path, logger);
                rsa.ImportFromPem(File.ReadAllText(path));
            }
            else if (environment.IsDevelopment())
            {
                var devKeyPath = Path.Combine(environment.ContentRootPath, DevKeyFileName);
                GenerateKeyFileIfMissing(devKeyPath, logger);
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

        private static void GenerateKeyFileIfMissing(string path, ILogger logger)
        {
            if (File.Exists(path))
                return;

            var directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (directory != null)
                Directory.CreateDirectory(directory);

            using var generated = RSA.Create(2048);
            try
            {
                // CreateNew: if another process wrote the file in the meantime, keep theirs
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                using var writer = new StreamWriter(stream);
                writer.Write(generated.ExportRSAPrivateKeyPem());
                logger.LogWarning("No signing key found. Generated a new one at {Path}", path);
            }
            catch (IOException) when (File.Exists(path))
            {
            }
        }
    }
}
