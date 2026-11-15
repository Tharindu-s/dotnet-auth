namespace auth.webapi.DTO.Auth
{
    public class CurrentUserDto
    {
        public required string Id { get; set; }
        public required string FullName { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string City { get; set; }
        public required Guid AppId { get; set; }
        public required IList<string> Roles { get; set; }
        public bool MustChangePassword { get; set; }
    }
}
