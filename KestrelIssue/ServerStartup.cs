using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KestreLIssue
{
    public class ServerStartup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            app.Run(async context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.OK;
                await context.Response.WriteAsync("OK");
            });
        }
    }
}