
namespace auth.webapi.DTO.ApplicationClient
{
    public class ResponseCreateApplicationClientDto
    {
        public Guid AppId { get; set; }
        public string RawApiKey { get; set; } = null!; // wont be stored anywhere and will be shown once
    }
}