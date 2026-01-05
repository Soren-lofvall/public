using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace IHCShared;

public class IHCModule(IHCType moduleType, uint controllerNumber, string Name, uint terminalsCount)
{
    [JsonPropertyName("moduletype")]
    public IHCType ModuleType { get; set; } = moduleType;
    [JsonPropertyName("controllernumber")]
    public uint ControllerNumber { get; private set; } = controllerNumber;
    [JsonPropertyName("name")]
    public string Name { get; set; } = Name;
    [JsonPropertyName("terminalscount")]
    public uint TerminalsCount { get; set; } = terminalsCount;
    [JsonPropertyName("uniqueid")]
    public uint UniqueId { get; set; } = (uint)moduleType * 1000 + controllerNumber;
}
