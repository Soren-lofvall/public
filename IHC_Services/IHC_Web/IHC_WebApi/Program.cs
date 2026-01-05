
using Asp.Versioning;
using IHC_WebApi.Hubs;
using IHCShared;
using IHCShared.IHCController;
using Utilities.MessageQueue;

namespace IHC_WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = builder.Configuration;
            // Add SignalR
            builder.Services.AddSignalR();

            // Setup TerminalStatusUpdated MessageQueue
            builder.Services.AddMessageQueue<TerminalStatusUpdated>(100);
            // Setup IHCControllerServiceCommand MessageQueue
            builder.Services.AddMessageQueue<IHCClientCommand>(100);

            // Setup IHC Controller communication
            builder.Services.AddSingleton<IHCCache>();

            builder.Services.Configure<IHCClientOptions>(
                    builder.Configuration.GetSection("IHCControllerHub"));
            builder.Services.AddHostedService<IHCClientService>();

            builder.Services.AddSingleton<StatusChangedDispatcher>();

            // Add HTTP controllers
            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddApiVersioning(options => {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0); // major version, minor version
                options.ReportApiVersions = true;
            });
            
            builder.Services.AddApiVersioning(options => {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0); // major version, minor version
            })
                .AddApiExplorer(options =>
                {
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            var app = builder.Build();

            // Force create singleton
            _ = app.Services.GetRequiredService<IHCCache>();
            _ = app.Services.GetRequiredService<StatusChangedDispatcher>();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.UseSwaggerUI(settings =>
                {
                    settings.SwaggerEndpoint("/openapi/v1.json", "IHC_WebApi v1");
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<IHCTerminalStatusHub>("/ihc_terminal_status_hub");

            app.Run();
        }
    }
}
