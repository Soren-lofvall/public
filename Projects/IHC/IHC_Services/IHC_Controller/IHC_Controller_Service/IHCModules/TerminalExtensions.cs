using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Controller_Service.IHCModules
{
    internal static class TerminalExtensions
    {
        public static uint AdjustTerminalNumber(this uint byteIndex)
        {
            uint terminalNumber = byteIndex;
            if (byteIndex > 8)
                terminalNumber += 2; // Skip none existant terminals 9-10 on modules 

            return terminalNumber;
        }

    }
}
