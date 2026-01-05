using IHC_Maui_App.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Maui_App.Shared;

public static class ServiceRegistration
{
    public static IServiceCollection AddSharedServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        services.AddHttpClient<IIHCTerminalsController, IHCTerminalsController>(client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>("IHCControllerUrl", "https://localhost:6000"));
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // optional settings
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestVersion = new Version(1, 0);
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        })
        .AddStandardResilienceHandler().Configure(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.Retry.MaxRetryAttempts = 5;
            options.Retry.Delay = TimeSpan.Zero;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.FailureRatio = 0.9;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        }); ;

        services.AddSingleton<IHCStatusHubService>();

        return services;
    }

}
