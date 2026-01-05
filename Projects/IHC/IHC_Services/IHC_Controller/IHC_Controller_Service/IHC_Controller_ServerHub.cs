using IHC_Controller_Service.IHCModules;
using IHCShared;
using IHCShared.IHCController;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Utilities.MessageQueue;

namespace IHC_Controller_Service
{
    public class IHC_Controller_ServerHub(
        IMessageQueue<IHCClientCommand> commandQueue, 
        Modules ihcModules) 
        : Hub<IIHCClient>, IIHCServerHub
    {
        public async Task<IEnumerable<IHCTerminal>> GetAllTerminals()
        {
            List<IHCTerminal> allTerminals = [.. await GetAllInputTerminals(), .. await GetAllOutputTerminals()];
            return allTerminals;
        }

        public async Task<IEnumerable<IHCTerminal>> GetAllInputTerminals()
        {
            return await ihcModules.GetAllTerminalsAsync(IHCType.Input);
        }
        public async Task<IEnumerable<IHCTerminal>> GetAllOutputTerminals()
        {
            return await ihcModules.GetAllTerminalsAsync(IHCType.Output);
        }

        public async Task<IEnumerable<IHCTerminal>> GetTerminalsForModule(IHCModule module)
        {
            if (module.ModuleType == IHCType.Input)
            {
                return await ihcModules.GetTerminalsForModuleAsync(module);
            }

            return await ihcModules.GetTerminalsForModuleAsync(module);
        }

        public async Task<uint> GetOutputModulesCount()
        {
            return await ihcModules.GetModulesCount(IHCType.Output);
        }

        public async Task<uint> GetInputModulesCount()
        {
            return await ihcModules.GetModulesCount(IHCType.Input);
        }

        public async Task ActivateInput(uint controllerNumber)
        {
            IHCClientCommand iHCCommand = new(IHCClientCommandType.ActivateInput, IHCType.Input, controllerNumber);
            await commandQueue.EnqueueAsync(iHCCommand);
        }

        public async Task SetOutputState(uint controllerNumber, bool state)
        {
            IHCClientCommand iHCCommand = new(IHCClientCommandType.SetOutputState, IHCType.Output, controllerNumber, state);
            await commandQueue.EnqueueAsync(iHCCommand);
        }

        public async Task<IEnumerable<IHCModule>> GetAllInputModules()
        {
            return await ihcModules.GetAllModulesAsync(IHCType.Input);
        }

        public async Task<IEnumerable<IHCModule>> GetAllOutputModules()
        {
            return await ihcModules.GetAllModulesAsync(IHCType.Output);
        }

        public async Task<IEnumerable<IHCModule>> GetAllModules()
        {
            List<IHCModule> allModules = [.. await GetAllInputModules(), .. await GetAllOutputModules()];
            return allModules;
        }
    }
}
