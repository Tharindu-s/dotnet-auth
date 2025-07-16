namespace auth.webapi.Interfaces
{
    public interface IConfigurationHelperService
    {
        string GetRequiredConfig(string key);
        IConfigurationSection GetSection(string key);
    }
}