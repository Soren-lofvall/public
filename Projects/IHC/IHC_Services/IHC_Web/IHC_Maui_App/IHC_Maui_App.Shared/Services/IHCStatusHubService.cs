using IHCShared;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Maui_App.Shared.Services;

public class IHCStatusHubService : IAsyncDisposable
{
    private HubConnection? hubConnection;
    public event Action<IHCTerminal>? OnTerminalStatusChanged;
    private readonly IConfiguration configuration;

    public IHCStatusHubService(IConfiguration config)
    {
        this.configuration = config;
    }

    public async Task StartAsync()
    {
        if (hubConnection != null)
            return;

        var baseUrl = new Uri(configuration.GetValue<string>("IHCControllerUrl", "https://localhost:6001"));
        var url = new Uri(baseUri: baseUrl, "/ihc_terminal_status_hub");

        hubConnection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<IHCTerminal>("TerminalStatusUpdated", (terminal) =>
        {
            OnTerminalStatusChanged?.Invoke(terminal);
        });

        await hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection != null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
