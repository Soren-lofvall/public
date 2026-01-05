using IHCShared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using System.Runtime;
using System.Text;

namespace IHC_Controller_Service.IHCModules
{
    public record UpdateStates(List<byte> NewStates, uint DataSumValue);

    public class Modules
    {
        private readonly ILogger logger;
        private readonly HardwareConfigSettings hardwareSettings;
        private readonly IHubContext<IHC_Controller_ServerHub, IIHCClient> signalRHub;

        private readonly TerminalConfigurationSettings terminalNames;

        private readonly Dictionary<IHCType, uint> previousValues = [];  
        private readonly ConcurrentDictionary<uint, Module> inputModules = [];
        private readonly ConcurrentDictionary<uint, Module> outputModules = [];

        public Modules(
            IHubContext<IHC_Controller_ServerHub, IIHCClient> hub, 
            TerminalConfigurationSettings terminalNames,
            HardwareConfigSettings hardwareSettings,
            ILogger<Modules> logger) 
        { 
            this.hardwareSettings = hardwareSettings;
            this.signalRHub = hub;
            this.logger = logger;
            this.terminalNames = terminalNames;
            
            previousValues[IHCType.Input] = 0;
            previousValues[IHCType.Output] = 0;

            InitializeModules();
        }

        private void InitializeModules()
        {
            if (hardwareSettings.Hardware.InputModules == null || hardwareSettings.Hardware.InputModules.Count == 0)
            {
                logger.LogWarning("IHCInterface: No input modules configured.");
            }
            else
            {
                foreach (var module in hardwareSettings.Hardware.InputModules)
                {
                    Module inputModule = new(IHCType.Input, module.ControllerNumber, module.Name!, module.TerminalsCount, terminalNames, logger);
                    hardwareSettings.SetModuleConfiguration(inputModule);
                    inputModules[module.ControllerNumber] = inputModule;
                }
            }

            if (hardwareSettings.Hardware.OutputModules == null || hardwareSettings.Hardware.OutputModules.Count == 0)
            {
                logger.LogWarning("IHCInterface: No output modules configured.");
            }
            else
            {
                foreach (var module in hardwareSettings.Hardware.OutputModules)
                {
                    Module outputModule = new(IHCType.Output, module.ControllerNumber, module.Name!, module.TerminalsCount, terminalNames, logger);
                    hardwareSettings.SetModuleConfiguration(outputModule);
                    outputModules[module.ControllerNumber] = outputModule;
                }
            }
        }

        public async Task UpdateStates(IHCType type, UpdateStates states, CancellationToken cancellationToken = default)
        {
            if (states.DataSumValue == previousValues[type])
            {
                logger.LogDebug($"IHCInterface: {type} states unchanged.");
                // No changes in input states
                return;
            }
            previousValues[type] = states.DataSumValue;

            if (type == IHCType.Input)
                await UpdateInputs(states);
            else
                await UpdateOutputs(states);
        }

        private async Task UpdateInputs(UpdateStates states)
        {
            uint moduleNo = 0;
            foreach (var module in inputModules.Values)
            {
                if (moduleNo * 2 + 1 >= states.NewStates.Count)
                    break;

                uint ioModuleState = (uint)(states.NewStates[(int)(moduleNo * 2)] + (states.NewStates[(int)(moduleNo * 2) + 1] << 8));

                foreach (var terminal in module.UpdateStates(ioModuleState))
                {
                    await signalRHub.Clients.All.TerminalStateChanged(terminal);
                }
                moduleNo++;
            }
        }

        private async Task UpdateOutputs(UpdateStates states)
        {
            uint moduleNo = 0;
            foreach (var module in outputModules.Values)
            {
                if (moduleNo >= states.NewStates.Count)
                    break;

                uint ioModuleState = states.NewStates[(int)moduleNo];

                foreach (var terminal in module.UpdateStates(ioModuleState))
                {
                    await signalRHub.Clients.All.TerminalStateChanged(terminal);
                }
                moduleNo++;
            }
        }

        private IEnumerable<IHCTerminal> GetAllTerminals(IHCType type)
        {
            IEnumerable<Module> moduleList;
            if (type == IHCType.Input)
            {
                moduleList = inputModules.Values;
            }
            else
            {
                moduleList = outputModules.Values;
            }

            foreach (var module in moduleList)
            {
                foreach (var terminal in module.Terminals)
                {
                    yield return terminal;
                }
            }
        }

        public async Task<IEnumerable<IHCTerminal>> GetAllTerminalsAsync(IHCType type, CancellationToken cancellationToken = default)
        {
            return GetAllTerminals(type);
        }

        public async Task<IEnumerable<IHCTerminal>> GetTerminalsForModuleAsync(IHCModule module, CancellationToken cancellationToken = default)
        {
            if (module.ModuleType == IHCType.Input)
                return inputModules[module.ControllerNumber].Terminals;
            else
                return outputModules[module.ControllerNumber].Terminals;
        }

        public async Task<IEnumerable<IHCModule>> GetAllModulesAsync(IHCType type, CancellationToken cancellationToken = default)
        {
            if (type == IHCType.Input)
                return inputModules.Values;
            else
                return outputModules.Values;
        }

        public async Task<uint> GetModulesCount(IHCType type, CancellationToken cancellationToken = default)
        {
            if (type == IHCType.Input)
                return (uint)inputModules.Count;
            else
                return (uint)outputModules.Count;
        }
    }
}
