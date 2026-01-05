using IHCShared;
using Microsoft.AspNetCore.Mvc;

namespace IHC_WebApi.Controllers
{
    public interface IIHCTerminalsController
    {
        Task<IActionResult> GetAllInputModules();
        Task<IActionResult> GetAllOutputModules();
        Task<IActionResult> GetAllModules();
        Task<IActionResult> GetOutputModulesCount();
        Task<IActionResult> GetInputModulesCount();

        Task<IActionResult> GetAllInputTerminals();
        Task<IActionResult> GetAllOutputTerminals();
        Task<IActionResult> GetAllTerminals();
        Task<IActionResult> GetTerminals(TerminalsByTypeAndModuleNumberRequest content);
        Task<IActionResult> GetInputTerminal(ControllerNumberRequest content);
        Task<IActionResult> GetOutputTerminal(ControllerNumberRequest content);
        Task<IActionResult> ActivateInput(ControllerNumberRequest content);
        Task<IActionResult> SetOutputState(SetOutputStateRequest content);
    }
}