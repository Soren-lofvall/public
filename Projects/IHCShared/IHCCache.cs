using IHCShared.IHCController;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using Utilities.MessageQueue;

namespace IHCShared
{
    public class IHCCache(
        IMessageQueue<TerminalStatusUpdated> statusQueue,
        ILogger<IHCCache> logger)
    {
        private readonly ILogger logger = logger;
        private readonly IMessageQueue<TerminalStatusUpdated> updateStatusMessageQueue = statusQueue;

        private readonly SortedDictionary<string, IHCModule> modules = [];
        private readonly SortedDictionary<string, IHCTerminal> terminals = [];
        private readonly Lock modulesLock = new();
        private readonly Lock terminalsLock = new();

        public static string GetModuleKey(IHCType terminalType, uint controllerNumber)
        {
            return $"{terminalType}:{controllerNumber}";
        }

        public static string GetModuleKey(IHCModule module)
        {
            return GetModuleKey(module.ModuleType, module.ControllerNumber);
        }

        public async Task UpdateModuleAsync(IHCModule module)
        {
            lock (modulesLock)
            {
                modules[GetModuleKey(module)] = module;
            }
            logger.LogInformation($"Updating module cache: {module.ModuleType} : {module.ControllerNumber}");
        }

        public IEnumerable<IHCModule> GetAllModules(IHCType? typeFilter = null)
        {
            lock (modulesLock)
            {
                if (typeFilter.HasValue)
                {
                    return modules.Values.Where(t => t.ModuleType == typeFilter.Value);
                }

                return modules.Values;
            }
        }

        public IHCModule? GetModule(IHCType typeFilter, uint controllerNumber)
        {
            lock (modulesLock)
            {
                modules.TryGetValue(GetModuleKey(typeFilter, controllerNumber), out IHCModule? module);
                return module;
            }
        }

        public static string GetTerminalKey(IHCType terminalType, uint controllerNumber)
        {
            return $"{terminalType}:{controllerNumber}";
        }

        public static string GetTerminalKey(IHCTerminal terminal)
        {
            return GetTerminalKey(terminal.TerminalType, terminal.ControllerNumber);
        }

        public IHCModule? GetModuleForTerminal(IHCTerminal terminal)
        {
            return GetModule(terminal.TerminalType, terminal.ModuleNumber);
        }

        public IHCTerminal GetTerminalById(uint id)
        { 
            lock (terminalsLock)
            {
                return terminals.Values.First(t => t.UniqueId == id);
            }
        }

        public async Task UpdateTerminalAsync(IHCTerminal terminal, CancellationToken stoppingToken)
        {
            lock (terminalsLock)
            {
                terminals[GetTerminalKey(terminal)] = terminal;
            }
            await updateStatusMessageQueue.EnqueueAsync(new TerminalStatusUpdated(terminal), stoppingToken);
            logger.LogInformation($"Updating terminal cache: {terminal.TerminalType} : {terminal.ControllerNumber} : {terminal.State}");
        }

        public IEnumerable<IHCTerminal> GetAllTerminals(IHCType? terminalTypeFilter = null)
        {
            lock (terminalsLock)
            {
                if (terminalTypeFilter.HasValue)
                {
                    return terminals.Values.Where(t => t.TerminalType == terminalTypeFilter.Value);
                }

                return terminals.Values;
            }
        }

        public IHCTerminal? GetTerminal(IHCType terminalTypeFilter, uint controllerNumber)
        {
            lock (terminalsLock)
            {
                terminals.TryGetValue(GetTerminalKey(terminalTypeFilter, controllerNumber), out IHCTerminal? terminal);
                return terminal;
            }
        }

        public uint GetModulesCount(IHCType typeFilter)
        {
            lock (modulesLock)
            {
                return (uint)modules.Count(m => m.Key.StartsWith($"{typeFilter}"));
            }
        }
    }
}
