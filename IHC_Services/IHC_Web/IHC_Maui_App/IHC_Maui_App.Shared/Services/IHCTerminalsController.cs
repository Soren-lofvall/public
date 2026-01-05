using IHCShared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IHC_Maui_App.Shared.Services;

public class IHCTerminalsController : BaseHttpClient, IIHCTerminalsController
{
    public IHCTerminalsController(HttpClient httpClient, ILogger<IHCTerminalsController> logger)
         : base(httpClient, logger)
    {
        
    }

    public async Task ActivateInput(ControllerNumberRequest content)
    {
        await PutAsync<HttpResponseMessage, ControllerNumberRequest>("ActivateInput", content);
    }

    public async Task<IEnumerable<IHCTerminal>> GetAllInputTerminals()
    {
        return await GetAsync<IEnumerable<IHCTerminal>>("GetAllInputTerminals");
    }

    public async Task<IEnumerable<IHCTerminal>> GetAllOutputTerminals()
    {
        return await GetAsync<IEnumerable<IHCTerminal>>("GetAllOutputTerminals");
    }

    public async Task<IEnumerable<IHCTerminal>> GetAllTerminals()
    {
        return await GetAsync<IEnumerable<IHCTerminal>>("GetAllTerminals");
    }

    public async Task<IEnumerable<IHCTerminal>> GetTerminals(TerminalsByTypeAndModuleNumberRequest content)
    {
        return await GetAsync<IEnumerable<IHCTerminal>>("GetTerminals", content);
    }

    public async Task<uint> GetInputModulesCount()
    {
        return await GetAsync<uint>("GetInputModulesCount");
    }

    public async Task<IHCTerminal> GetInputTerminal(ControllerNumberRequest content)
    {
        return await GetAsync<IHCTerminal>("GetInputTerminal", content);
    }

    public async Task<uint> GetOutputModulesCount()
    {
        return await GetAsync<uint>("GetOutputModulesCount");
    }

    public async Task<IHCTerminal> GetOutputTerminal(ControllerNumberRequest content)
    {
        return await GetAsync<IHCTerminal>("GetOutputTerminal", content);
    }

    public async Task SetOutputState(SetOutputStateRequest content)
    {
        await PutAsync<HttpResponseMessage, SetOutputStateRequest>("SetOutputState", content);
    }

    public async Task<IEnumerable<IHCModule>> GetAllInputModules()
    {
        return await GetAsync<IEnumerable<IHCModule>>("GetAllInputModules");
    }

    public async Task<IEnumerable<IHCModule>> GetAllOutputModules()
    {
        return await GetAsync<IEnumerable<IHCModule>>("GetAllOutputModules");
    }

    public async Task<IEnumerable<IHCModule>> GetAllModules()
    {
        return await GetAsync<IEnumerable<IHCModule>>("GetAllModules");
    }
}
