using IHCShared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ESPHome_Server
{
    public static class ExtensionMethods
    {
        //public static uint GetDeviceId(this IIHCTerminal terminal, uint outputOffset)
        //{
        //    if (terminal.TerminalType == IHCType.Output)
        //        return outputOffset + terminal.ModuleNumber;
        //    else
        //        return terminal.ModuleNumber;
        //}

        //public static uint GetDeviceId(uint controllerNumber, IHCType terminalType, uint outputOffset)
        //{
        //    if (terminalType == IHCType.Output)
        //        return controllerNumber / 10 + 1 + outputOffset;
        //    else
        //        return controllerNumber / 20 + 1;
        //}
    }
}
