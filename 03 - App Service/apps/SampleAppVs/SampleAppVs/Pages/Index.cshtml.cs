using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace SampleAppVs.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration c, ILogger<IndexModel> logger)
        {
            _logger = logger;
            _config = c;
        }

        public string SomeValue { get => "My setting: " + _config.GetValue<string>("SomeSetting"); }

        public string ConnString { get => "Connection string: " + Environment.GetEnvironmentVariable("SQLCONNSTR_CONNECTION"); }

        public void OnGet()
        {

        }
    }
}
