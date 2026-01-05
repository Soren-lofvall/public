using IHCShared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace IHCShared.IHC_Controller
{
    public class IHCServerHub(HubConnection connection) : IIHCServerHub
    {
        public Task<IEnumerable<IHCTerminal>> GetAllInputTerminals() =>
            connection.InvokeAsync<IEnumerable<IHCTerminal>>(nameof(GetAllInputTerminals));
        public Task<IEnumerable<IHCTerminal>> GetAllOutputTerminals() =>
            connection.InvokeAsync<IEnumerable<IHCTerminal>>(nameof(GetAllOutputTerminals));
        public Task<IEnumerable<IHCTerminal>> GetAllTerminals() =>
            connection.InvokeAsync<IEnumerable<IHCTerminal>>(nameof(GetAllTerminals));

        public Task<uint> GetInputModulesCount() =>
            connection.InvokeAsync<uint>(nameof(GetInputModulesCount));
        public Task<uint> GetOutputModulesCount() =>
            connection.InvokeAsync<uint>(nameof(GetOutputModulesCount));
        public Task<IEnumerable<IHCModule>> GetAllModules() =>
            connection.InvokeAsync<IEnumerable<IHCModule>>(nameof(GetAllModules));
        public Task<IEnumerable<IHCModule>> GetAllInputModules() =>
            connection.InvokeAsync<IEnumerable<IHCModule>>(nameof(GetAllInputModules));
        public Task<IEnumerable<IHCModule>> GetAllOutputModules() =>
            connection.InvokeAsync<IEnumerable<IHCModule>>(nameof(GetAllOutputModules));


        public Task ActivateInput(uint controllerNumber) =>
            connection.InvokeAsync(nameof(ActivateInput), controllerNumber);
        public Task SetOutputState(uint controllerNumber, bool state) =>
            connection.InvokeAsync(nameof(SetOutputState), controllerNumber, state);
    }

    public class IHCClient 
    {
        private readonly HubConnection _connection;
        public IHCServerHub Server { get; }
        public Uri Uri { get; private set; }

        public IHCClient(Uri baseUrl,
            Action? onReconnecting = null,
            Action? onReconnected = null,
            Action? onClosed = null,
            Action<IHCTerminal>? onTerminalStatusChanged = null)
        {
            Uri = new Uri(baseUrl, "ihc_controller_hub");
            _connection = new HubConnectionBuilder()
                .WithUrl(Uri)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += async (error) => onReconnecting?.Invoke();
            _connection.Reconnected += async (id) => onReconnected?.Invoke();
            _connection.Closed += async (error) => onClosed?.Invoke();

            // Strongly-typed bindings for server-to-client calls
            _connection.On<IHCTerminal>(nameof(IIHCClient.TerminalStateChanged),
                (terminal) => onTerminalStatusChanged?.Invoke(terminal));

            // Strongly-typed client-to-server via proxy
            Server = new IHCServerHub(_connection);
        }

        public bool IsConnected => _connection.State == HubConnectionState.Connected;
        public Task StartAsync(CancellationToken ct = default) => _connection.StartAsync(ct);
        public Task StopAsync(CancellationToken ct = default) => _connection.StopAsync(ct);
    }
}
