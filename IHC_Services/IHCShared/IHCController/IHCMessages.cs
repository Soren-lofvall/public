using System;
using System.Collections.Generic;
using System.Text;
using Utilities.MessageQueue;

namespace IHCShared.IHCController;

public enum IHCClientCommandType
{
    SetOutputState,
    ActivateInput
}
public record IHCClientCommand(
    IHCClientCommandType CommandType,
    IHCType TerminalType,
    uint ControllerNumber,
    bool? State = null) : IMessage;

public record TerminalStatusUpdated(IHCTerminal Terminal) : IMessage;

public record ModuleStatusUpdated(IHCModule Module) : IMessage;
