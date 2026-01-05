using IHCShared;

namespace IHCShared
{
    public interface IIHCServerHub
    {
        Task<IEnumerable<IHCModule>> GetAllInputModules();
        Task<IEnumerable<IHCModule>> GetAllOutputModules();
        Task<IEnumerable<IHCModule>> GetAllModules();
        Task<uint> GetInputModulesCount();
        Task<uint> GetOutputModulesCount();

        Task<IEnumerable<IHCTerminal>> GetAllInputTerminals();
        Task<IEnumerable<IHCTerminal>> GetAllOutputTerminals();
        Task<IEnumerable<IHCTerminal>> GetAllTerminals();

        Task ActivateInput(uint controllerNumber);
        Task SetOutputState(uint controllerNumber, bool state);
    }

    public interface IIHCClient
    {
        Task TerminalStateChanged(IHCTerminal terminal);
    }
}