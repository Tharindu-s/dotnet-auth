namespace auth.webapi.DTO.ApplicationClient
{
    public class RotatedApiKeyDto
    {
        // Shown once. The previous key stopped working when this one was issued.
        public required string RawApiKey { get; set; }
    }
}
