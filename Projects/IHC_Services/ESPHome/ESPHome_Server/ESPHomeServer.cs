using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ESPHome_Server;

public class ESPHomeServer : BackgroundService
{
    private readonly IServiceProvider services;
    private readonly ILogger<ESPHomeServer> logger;
    private readonly int port;

    public ESPHomeServer(
        IServiceProvider services,
        IConfiguration config,
        ILogger<ESPHomeServer> logger)
    {
        this.services = services;
        this.logger = logger;

        port = config.GetValue<int>("ESPHomeServerApi:Port", 6053);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        logger.LogInformation($"IHC Controller Server now listening on port {port}");
        while (stoppingToken.IsCancellationRequested == false)
        {
            logger.LogInformation("Waiting for incoming ESPHome connection...");
            try
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                Session session = new(client, services);
                _ = Task.Run(async () => await session.RunAsync(stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            Task.Delay(100, stoppingToken).Wait(stoppingToken);
        }
    }
}
