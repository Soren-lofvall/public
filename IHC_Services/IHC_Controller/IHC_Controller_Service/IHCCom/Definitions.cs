using System;
using System.Collections.Generic;
using System.Text;

namespace IHC_Controller_Service.IHCCom
{
    public class Definitions
    {
        // Transmission bytes
        //public enum ControlBytes : byte
        //{
        //    SOH = 0x01,
        //    STX = 0x02,
        //    ACK = 0x06,
        //    ETB = 0x17
        //}

        public const byte SOH = 0x01;
        public const byte STX = 0x02;
        public const byte ACK = 0x06;
        public const byte ETB = 0x17;

        // Commands
        //public enum Commands : byte
        //{
        //    DATA_READY = 0x30,
        //    SET_OUTPUT = 0x7A,
        //    GET_OUTPUTS = 0x82,
        //    OUTP_STATE = 0x83,
        //    GET_INPUTS = 0x86,
        //    INP_STATE = 0x87,
        //    ACT_INPUT = 0x88
        //}
        
        public const byte DATA_READY = 0x30;
        public const byte SET_OUTPUT = 0x7A;
        public const byte GET_OUTPUTS = 0x82;
        public const byte OUTP_STATE = 0x83;
        public const byte GET_INPUTS = 0x86;
        public const byte INP_STATE = 0x87;
        public const byte ACT_INPUT = 0x88;

        //public enum ReceiverId : byte
        //{
        //    ID_DISPLAY = 0x09,
        //    ID_MODEM = 0x0A,
        //    ID_IHC = 0x12,
        //    ID_AC = 0x1B,
        //    ID_PC = 0x1C,
        //    ID_PC2 = 0x1D
        //}
        // Receiver IDs
        public const byte ID_DISPLAY = 0x09;
        public const byte ID_MODEM = 0x0A;
        public const byte ID_IHC = 0x12;
        public const byte ID_AC = 0x1B;
        public const byte ID_PC = 0x1C;
        public const byte ID_PC2 = 0x1D;

    }
}
