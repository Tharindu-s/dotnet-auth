namespace auth.webapi.Services
{
    // The "Argus" config section: the admin console app and its first admin
    public class ArgusOptions
    {
        public const string AdminRole = "Admin";

        // Origins the Argus frontend is served from. Applied to the Argus app on every start.
        public List<string> AllowedOrigins { get; set; } = new();

        public string AdminEmail { get; set; } = "admin@argus.local";

        // Initial password for the first admin. If empty, a random one is generated and written to the log.
        public string? AdminPassword { get; set; }

        // Recovery: on the next start, reset the admin's password (to AdminPassword or a generated one),
        // unlock the account, and require a new password at sign-in. Turn it off again afterwards.
        public bool ResetAdminPassword { get; set; }
    }
}
