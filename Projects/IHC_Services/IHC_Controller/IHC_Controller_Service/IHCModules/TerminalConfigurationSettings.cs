using IHCShared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace IHC_Controller_Service.IHCModules;

public class TerminalsConfig
{
    public List<TerminalConfig>? TerminalConfigs { get; set; }
    public string? DefaultNameFormat { get; set; }
    public bool DisableNotListedEntities { get; set; }
}

public class TerminalConfig
{
    public IHCType Type { get; set; }
    public uint ControllerNumber { get; set; }
    public string? Name { get; set; }
    public bool? DisabledByDefault { get; set; }
}

public class TerminalConfigurationSettings
{
    private TerminalsConfig Config { get; set; }

    private Dictionary<string, TerminalConfig> TerminalList { get; set; }
    
    public TerminalConfigurationSettings(IOptions<TerminalsConfig> options)
    {
        Config = options.Value;

        if (Config.TerminalConfigs == null)
        {
            Config.TerminalConfigs = [];
        }

        TerminalList = Config.TerminalConfigs.ToDictionary(
            key => $"{key.Type}_{key.ControllerNumber}",
            value => value);
    }

    public void SetConfigurationSettings(IHCTerminal terminal)
    {
        var terminalConfig = TerminalList.GetValueOrDefault($"{terminal.TerminalType}_{terminal.ControllerNumber}");
        if (terminalConfig == null || terminalConfig.Name == null)
        {
            // No custom name set, use default format
            terminal.Name = Config.DefaultNameFormat ?? "{Type} {ModuleNumber}-{ControllerNumber}";
            
            terminal.Name = terminal.Name
                .Replace("{Type}", terminal.TerminalType.ToString())
                .Replace("{ModuleNumber}", terminal.ModuleNumber.ToString())
                .Replace("{TerminalNumber}", terminal.TerminalNumber.ToString())
                .Replace("{DisabledByDefault}", terminal.DisabledByDefault.ToString())
                .Replace("{UniqueId}", terminal.UniqueId.ToString())
                .Replace("{ControllerNumber}", terminal.ControllerNumber.ToString());
        }
        else
            // Custom name set in config
            terminal.Name = terminalConfig.Name;

        if (terminalConfig == null) 
            // No config found for this terminal, set DisabledByDefault from global setting
            terminal.DisabledByDefault = Config.DisableNotListedEntities;
        else if (terminalConfig.DisabledByDefault == null) 
            // Config found but no setting for DisabledByDefault
            terminal.DisabledByDefault = true;
        else
            // Config found with setting for DisabledByDefault
            terminal.DisabledByDefault = terminalConfig.DisabledByDefault.Value;
    }
}
