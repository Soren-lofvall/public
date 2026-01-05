using IHCShared;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Controller_Service.IHCModules;

public class ModuleConfig
{
    public uint ControllerNumber { get; set; }
    public uint TerminalsCount { get; set; }
    public string? Name { get; set; }
}

public class HardwareConfig
{
    public List<ModuleConfig>? InputModules { get; set; }
    public List<ModuleConfig>? OutputModules { get; set; }
    public string? DefaultNameFormat { get; set; }
}

public class HardwareConfigSettings
{
    public HardwareConfig Hardware { get; set; }

    public HardwareConfigSettings(IOptions<HardwareConfig> options)
    {
        Hardware = options.Value;
    }

    public void SetModuleConfiguration(IHCModule module)
    {
        if (module.Name == null)
        {
            module.Name = Hardware.DefaultNameFormat ?? "IHC {ModuleType} Module {ControllerNumber}";
            module.Name = module.Name
                .Replace("{ModuleType}", module.ModuleType.ToString())
                .Replace("{TerminalsCount}", module.TerminalsCount.ToString())
                .Replace("{UniqueId}", module.UniqueId.ToString())
                .Replace("{ControllerNumber}", module.ControllerNumber.ToString());
        }

        if (module.ModuleType == IHCType.Output)
            module.TerminalsCount = 8;
    }
}