using IHCShared;
using IHCShared.IHCController;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Utilities.MessageQueue;

namespace ESPHome_Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddHostedService<ESPHomeServer>();

            // Setup TerminalStatusUpdated MessageQueue
            builder.Services.AddMessageQueue<TerminalStatusUpdated>(100);
            // Setup IHCControllerServiceCommand MessageQueue
            builder.Services.AddMessageQueue<IHCClientCommand>(100);

            // Add services
            builder.Services.AddSingleton<IHCCache>();
            builder.Services.AddHostedService<IHCClientService>();
            builder.Services.Configure<IHCClientOptions>(
                    builder.Configuration.GetSection("IHCControllerHub"));
           
            var app = builder.Build();

            // Force create singleton
            _ = app.Services.GetRequiredService<IHCCache>();

            await app.RunAsync();
        }
    }
}
