namespace auth.webapi.DTO.Admin
{
    public class PagedResult<T>
    {
        public required List<T> Items { get; set; }
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
