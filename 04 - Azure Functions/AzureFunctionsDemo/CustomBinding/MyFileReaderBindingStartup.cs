using CustomBinding;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;


[assembly: WebJobsStartup(typeof(MyFileReaderBindingStartup))]
namespace CustomBinding
{
    public class MyFileReaderBindingStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddMyFileReaderBinding();
        }
    }
}