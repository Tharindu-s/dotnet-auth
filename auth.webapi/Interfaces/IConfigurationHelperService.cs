namespace auth.webapi.Interfaces
{
    public interface IConfigurationHelperService
    {
        string GetRequiredConfig(string key);
    }
}