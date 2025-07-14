using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auth.webapi.Interfaces;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace auth.webapi.Services
{
    public class ConfigurationHelper : IConfigurationHelperService
    {
        private readonly IConfiguration _configuration;
        public ConfigurationHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GetRequiredConfig(string key)
        {
            return _configuration[key] ?? throw new InvalidOperationException($"{key} not found in configuration.");
        }
    }
}