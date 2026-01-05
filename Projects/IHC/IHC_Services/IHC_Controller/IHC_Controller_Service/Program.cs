using IHC_Controller_Service.IHCCom;
using IHC_Controller_Service.IHCModules;
using IHCShared;
using IHCShared.IHCController;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime;
using System.Text.Json.Serialization;
using Utilities.MessageQueue;

namespace IHC_Controller_Service
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = builder.Configuration;
            builder.WebHost.UseUrls(configuration["IHCControllerHub:Url"] ?? "http://*:5001");
            builder.Services.AddSignalR();

            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });


            // Setup IHCControllerServiceCommand MessageQueue
            builder.Services.AddMessageQueue<IHCClientCommand>(100);

            builder.Services.Configure<TerminalsConfig>(
                    builder.Configuration.GetSection("Terminals"));
            builder.Services.AddSingleton<TerminalConfigurationSettings>();

            builder.Services.Configure<HardwareConfig>(
                builder.Configuration.GetSection("Hardware"));
            builder.Services.AddSingleton<HardwareConfigSettings>();

            builder.Services.AddSingleton<Modules>();

            builder.Services.AddHostedService<RS485Controller>();

            var app = builder.Build();

            app.Services.GetRequiredService<Modules>();

            app.MapHub<IHC_Controller_ServerHub>("/ihc_controller_hub");

            await app.RunAsync();
        }
    }
}
