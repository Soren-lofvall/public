using IHCShared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IHCShared;

public class ControllerNumberRequest
{
    [JsonPropertyName("controllernumber")]
    public uint ControllerNumber { get; set; }
}

public class TerminalsByTypeAndModuleNumberRequest
{
    [JsonPropertyName("terminaltype")]
    public IHCType TerminalType { get; set; }
    [JsonPropertyName("modulenumber")]
    public uint ModuleNumber { get; set; }
}

public class SetOutputStateRequest
{
    [JsonPropertyName("controllernumber")]
    public uint ControllerNumber { get; set; }
    [JsonPropertyName("state")]
    public bool State { get; set; }

}