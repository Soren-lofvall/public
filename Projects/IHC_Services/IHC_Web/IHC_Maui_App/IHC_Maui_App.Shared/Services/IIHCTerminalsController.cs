using IHCShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Maui_App.Shared.Services;

public interface IIHCTerminalsController
{
    Task<IEnumerable<IHCModule>> GetAllInputModules();
    Task<IEnumerable<IHCModule>> GetAllOutputModules();
    Task<IEnumerable<IHCModule>> GetAllModules();
    Task<uint> GetInputModulesCount();
    Task<uint> GetOutputModulesCount();

    Task<IEnumerable<IHCTerminal>> GetAllInputTerminals();
    Task<IEnumerable<IHCTerminal>> GetAllOutputTerminals();
    Task<IEnumerable<IHCTerminal>> GetAllTerminals();
    Task<IEnumerable<IHCTerminal>> GetTerminals(TerminalsByTypeAndModuleNumberRequest content);
    Task<IHCTerminal> GetInputTerminal(ControllerNumberRequest content);
    Task<IHCTerminal> GetOutputTerminal(ControllerNumberRequest content);

    Task ActivateInput(ControllerNumberRequest content);
    Task SetOutputState(SetOutputStateRequest content);
}
