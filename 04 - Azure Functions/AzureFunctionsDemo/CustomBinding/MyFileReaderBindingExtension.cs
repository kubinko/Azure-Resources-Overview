using Microsoft.Azure.WebJobs;
using System;

namespace CustomBinding
{
    public static class MyFileReaderBindingExtension
    {
        public static IWebJobsBuilder AddMyFileReaderBinding(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<MyFileReaderBinding>();
            return builder;
        }
    }
}