using IHCShared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace IHC_Controller_Service.IHCModules
{
    public class Module : IHCModule
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<uint, IHCTerminal> terminals = [];

        public Module(IHCType type, uint moduleNumber, string name, uint terminalsCount, TerminalConfigurationSettings terminalNames, ILogger logger)
            : base(type, moduleNumber, name, terminalsCount)
        {
            this.logger = logger;
            InitializeTerminals(moduleNumber, terminalNames);
        }

        public IEnumerable<IHCTerminal> Terminals
        {
            get
            {
                return terminals.Values;
            }
        }

        public IHCTerminal this[uint terminalNumber]
        {
            get
            {
                if (terminals.TryGetValue(terminalNumber, out IHCTerminal? terminal))
                {
                    return terminal;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(terminalNumber), $"{ModuleType} terminal number {terminalNumber} does not exist in module {ControllerNumber}.");
                }
            }
            private set
            {
                terminals[terminalNumber] = value;
            }
        }

        private void InitializeTerminals(uint moduleNumber, TerminalConfigurationSettings terminalNames)
        {
            for (uint terminalNumber = 1; terminalNumber <= TerminalsCount; ++terminalNumber)
            {
                var adjustedTerminalNumber = terminalNumber.AdjustTerminalNumber();

                IHCTerminal terminal = new(ModuleType, moduleNumber, adjustedTerminalNumber);
                terminalNames.SetConfigurationSettings(terminal);
                this[adjustedTerminalNumber] = terminal;
            }
        }

        public IEnumerable<IHCTerminal> UpdateStates(uint ioModuleState)
        {
            for (uint byteCounter = 0; byteCounter < TerminalsCount; ++byteCounter)
            {
                bool state = (((ioModuleState & (1 << (int) byteCounter)) > 0));

                uint terminalNumber = (byteCounter + 1).AdjustTerminalNumber();

                IHCTerminal terminal = this[terminalNumber];
                if (terminal.State != state)
                {
                    string stateStr = state ? "ON" : "OFF";
                    string outStr = $"{DateTime.Now} - {terminal.Name} : {terminal.TerminalType} {terminal.ControllerNumber} is {stateStr}";
                    logger.LogInformation(outStr);
                    terminal.State = state;

                    yield return terminal;
                }
            }
        }
    }
}
