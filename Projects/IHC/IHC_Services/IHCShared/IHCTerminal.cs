using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IHCShared;

public enum IHCType
{
    Input,
    Output
}

public class IHCTerminal
{
    [JsonPropertyName("terminaltype")]
    public IHCType TerminalType { get; set; }
    [JsonPropertyName("modulenumber")]
    public uint ModuleNumber { get; set; }
    [JsonPropertyName("terminalnumber")]
    public uint TerminalNumber { get; set; }
    [JsonPropertyName("state")]
    public bool State { get; set; }
    [JsonPropertyName("controllernumber")]
    public uint ControllerNumber { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("disabledbydefault")]
    public bool DisabledByDefault { get; set; }
    [JsonPropertyName("uniqueid")]
    public uint UniqueId { get; set; }

    public IHCTerminal(IHCType terminalType, 
        uint moduleNumber, 
        uint terminalNumber, 
        bool state = false, 
        string? name = null, 
        bool disabledByDefault = false)
    {
        this.TerminalType = terminalType;
        this.ModuleNumber = moduleNumber;
        this.TerminalNumber = terminalNumber;
        this.State = state;
        this.Name = name;
        this.DisabledByDefault = disabledByDefault;

        if (terminalType == IHCType.Input)
        {
            ControllerNumber = (moduleNumber - 1) * 20 + terminalNumber;
            UniqueId = ControllerNumber;
        }
        else
        {
            ControllerNumber = (moduleNumber - 1) * 10 + terminalNumber;
            UniqueId = 1000 + ControllerNumber;
        }
    }
}
