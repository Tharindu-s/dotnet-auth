namespace auth.webapi.DTO.ApplicationClient
{
    public class ResponseCreateApplicationClientDto : ApplicationClientDto
    {
        // Shown once and never stored, only its hash is. Null for public clients.
        public string? RawApiKey { get; set; }
    }
}
