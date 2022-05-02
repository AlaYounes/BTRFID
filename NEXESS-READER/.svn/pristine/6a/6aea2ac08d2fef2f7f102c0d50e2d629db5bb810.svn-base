using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace stid.sdk {

    /// <summary>
    /// Wrapping unit for SSCPlibEPC.dll functions
    /// </summary>
    public class StidReaderApi {

        // Communication crypto mode
        public const byte m_plain = 0;  // Plain communication
        // RFU public const byte m_sig   = 1 ;  // Authenticated communication
        // RFU public const byte m_enc   = 2 ;  // Enciphered commuinication
        // RFU public const byte m_sigenc= 3 ;  // Authenticated AND enciphered communication

        // Serial communication baudrate
        public const byte br_9600   = 0;
        public const byte br_19200  = 1;
        public const byte br_38400  = 2;
        public const byte br_57600  = 3;
        public const byte br_115200 = 4;

        // Communication type
        public const byte ct_rs232 = 0; // RS232 serial communication
        public const byte ct_rs485 = 1; // RS485 addressable serial communication
        public const byte ct_usb   = 2; // Native USB not yet Reader implemented, you sould use ct_rs232 with USB converter provided
        public const byte ct_tcp   = 3; // TCP reader not yet exists
        public const byte ct_udp   = 4; // UDP reader not yet exists


        // Library errors
        public const UInt16 SSCPLIBEPC_ERROR_MUST_INITIALIZE     = 0xFF01; // Library NOT initialized
        public const UInt16 SSCPLIBEPC_ERROR_ALREADY_INITIALIZED = 0xFF02; // Library is already initialized
        public const UInt16 SSCPLIBEPC_ERROR_BAD_COM_PORT        = 0xFF03; // COMPORT selected is already openned by another software, or is unreachable
        public const UInt16 SSCPLIBEPC_ERROR_BAD_PARAMETER       = 0xFF04; // Bad parameter (for ex. bad name for COMPORT)
        public const UInt16 SSCPLIBEPC_ERROR_EXCEPTION           = 0xFF05; // Library has launched an exception (for ex. COMPORT is no more accessible or not connected, no more free RAM, ...)


        // Reader errors

        public const UInt16 SSCP_OK = 0x00; // operation ok
        // to have errors decription you should use SSCPepc_GetErrorMsg
        public const int SSCP_READER_AUTH_ERR             = 0x01; // Authentication error
        public const int SSCP_READER_BAD_PARAM_DATA_ERR   = 0x02; // Bad parameter data , or IF RFU is not 0 (case of incorrect DeCipher), or EncDec crypto error
        public const int SSCP_READER_CRC_ERR              = 0x03; // CRC Frame error
        public const int SSCP_READER_REC_DATA_LEN_ERR     = 0x04; // Frame received length error, not length expected (shorter!)
        public const int SSCP_READER_SIGN_ERR             = 0x05; // In Authentication mode (sign) Frame received contain Bad Signature
        public const int SSCP_READER_COM_TIMEOUT          = 0x06; // Response too late or no response
        public const int SSCP_READER_BAD_COMMAND_CODE     = 0x07; //
        public const int SSCP_READER_BAD_COMMAND_TYPE     = 0x08; //
        public const int SSCP_READER_UPDATE_FIRMWARE_ERROR= 0x09;
        public const int SSCP_READER_BAD_CMD_ACK          = 0x0A; // BAD CMD ACK, ACK received different from CMD sent   +
        public const int SSCP_READER_COMM_MODE_NOT_ALLOWED= 0x0B;
        public const int SSCP_READER_UHF_HARDWARE_ERROR   = 0x20; // Pb hardware UHF Reader

        public const int SSCP_EPC_OTHER_ERROR         		    = 0x01;
        public const int SSCP_EPC_BAD_CHIP_PARAMETER  			= 0x02;
        public const int SSCP_EPC_MEMORY_OVER_RUN     			= 0x03;
        public const int SSCP_EPC_MEMORY_LOCKED_ERROR 			= 0x04;
        public const int SSCP_EPC_RF_ERROR           			= 0x07; // OR no Tag Match
        public const int SSCP_EPC_BAD_PWD_ALREADY_LOCKED 		= 0x08;
        public const int SSCP_EPC_INSUFFICIENT_POWER  			= 0x0B;
        public const int SSCP_EPC_BAD_PWD             			= 0x0F;
        public const int SSCP_EPC_OTHER_VERIFY_ERROR  			= 0x11;
        public const int SSCP_EPC_MEMORY_LOCKED_VERIFY_ERROR    = 0x14;
        public const int SSCP_EPC_RF_VERIFY_ERROR               = 0x17;
        public const int SSCP_EPC_INSUFFICIENT_POWER_VERIFY     = 0x1B;


        // Path to library
        public const string LIB_PATH_NAME = @".\SSCPlibEPC.dll";


        #region Object and general communication functions

        /// 
        /// DLL Entry Point
        /// The FIRST command to invoke, initializes library !
        ///
        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Initialize")]
        public static extern UInt16 SSCP_Initialize();

        //The LAST command, frees the library
        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Terminate")]
        public static extern UInt16 SSCP_Terminate();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_GetCOMType")]
        public static extern UInt16 SSCP_GetCOMType(ref byte comtype);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_SetCOMType")]
        public static extern UInt16 SSCP_SetCOMType(byte comtype);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_SetAutoConnect")]
        public static extern UInt16 SSCP_SetAutoConnect(byte autoconnect);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_GetAutoConnect")]
        public static extern UInt16 SSCP_GetAutoConnect(ref byte autoconnect);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_SetMode")]
        public static extern UInt16 SSCP_SetMode(byte mode);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_GetMode")]
        public static extern UInt16 SSCP_GetMode(ref byte mode);

        [DllImport(LIB_PATH_NAME, CharSet = CharSet.Unicode, EntryPoint = "SSCP_Serial_SetPort")]
        public static extern UInt16 SSCP_Serial_SetPort(string COMPort);

        [DllImport(LIB_PATH_NAME, CharSet = CharSet.Unicode, EntryPoint = "SSCP_Serial_GetPort")]
        public static extern UInt16 SSCP_Serial_GetPort(ref string COMPort);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_SetBaudRate")]
        public static extern UInt16 SSCP_Serial_SetBaudRate(byte baudrate);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_GetBaudRate")]
        public static extern UInt16 SSCP_Serial_GetBaudRate(ref byte baudrate);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_SetTimeout")]
        public static extern UInt16 SSCP_Serial_SetTimeout(int readconstant,
                                                                int readmultiplier,
                                                                int writeconstant,
                                                                int writemultiplier);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_GetTimeout")]
        public static extern UInt16 SSCP_Serial_GetTimeout(ref int readconstant,
                                                                ref int readmultiplier,
                                                                ref int writeconstant,
                                                                ref int writemultiplier);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Connect")]
        public static extern UInt16 SSCP_Connect();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Disconnect")]
        public static extern UInt16 SSCP_Disconnect();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_Get485Address")]
        public static extern UInt16 SSCP_Serial_Get485Address(ref int address);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_Serial_Set485Address")]
        public static extern UInt16 SSCP_Serial_Set485Address(int address);


        [DllImport(LIB_PATH_NAME, CharSet = CharSet.Unicode, EntryPoint = "SSCP_TCP_c_GetIPAdr")]
        public static extern UInt16 SSCP_TCP_c_GetIPAdr(ref string IPAdr);

        [DllImport(LIB_PATH_NAME, CharSet = CharSet.Unicode, EntryPoint = "SSCP_TCP_c_SetIPAdr")]
        public static extern UInt16 SSCP_TCP_c_SetIPAdr(string IPAdr);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_c_GetPort")]
        public static extern UInt16 SSCP_TCP_c_GetPort(ref UInt16 Port);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_c_SetPort")]
        public static extern UInt16 SSCP_TCP_c_SetPort(UInt16 Port);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_c_GetTimeOut")]
        public static extern UInt16 SSCP_TCP_c_GetTimeOut(ref int TimeOut);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_c_SetTimeOut")]
        public static extern UInt16 SSCP_TCP_c_SetTimeOut(int TimeOut);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_s_GetActive")]
        public static extern UInt16 SSCP_TCP_s_GetActive(ref byte Active);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_s_SetActive")]
        public static extern UInt16 SSCP_TCP_s_SetActive(byte Active);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_s_GetPort")]
        public static extern UInt16 SSCP_TCP_s_GetPort(ref UInt16 Port);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCP_TCP_s_SetPort")]
        public static extern UInt16 SSCP_TCP_s_SetPort(UInt16 Port);

        #endregion


        #region Reader functions

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_SetRFSettings")]
        public static extern UInt16 SSCPreader_SetRFSettings([In] byte[] A0_15);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_SetRFSettings_Saved")]
        public static extern UInt16 SSCPreader_SetRFSettings_Saved([In] byte[] A0_15);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_ResetRFSettings")]
        public static extern UInt16 SSCPreader_ResetRFSettings();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_GetRFSettings")]
        public static extern UInt16 SSCPreader_GetRFSettings([Out] byte[] A0_15);

        #region Reader functions V1.1
        // V1.1 -> A07
        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_RF_ON")]
        public static extern UInt16 SSCPreader_RF_ON();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_RF_OFF")]
        public static extern UInt16 SSCPreader_RF_OFF();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_AutonomousStart")]
        public static extern UInt16 SSCPreader_AutonomousStart();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_AutonomousStop")]
        public static extern UInt16 SSCPreader_AutonomousStop();

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_AutonomousOutput")]
        public static extern UInt16 SSCPreader_AutonomousOutput(byte OutConf,
                                                                byte AutoStart,
                                                                byte Len);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_GetHealthParams")]
        public static extern UInt16 SSCPreader_GetHealthParams(ref byte Tune0,
                                                               ref byte Tune1,
                                                               ref byte Tune2,
                                                               ref byte Tune3,
                                                               ref byte TempC,
                                                               ref byte TempPA);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_SetOptoOutputParam")]
        public static extern UInt16 SSCPreader_SetOptoOutputParam(byte Time);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_ChangeRegulation")]
        public static extern UInt16 SSCPreader_ChangeRegulation(byte Reboot,
                                                                byte Regulation);

        // V1.1
        #endregion


        #region Reader functions V1.2
        // V1.2 -> A09

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_SetBaudRate")]
        public static extern UInt16 SSCPreader_SetBaudRate(byte Baudrate);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_Set485Address")]
        public static extern UInt16 SSCPreader_Set485Address(byte Address);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_GetInfos")]
        public static extern UInt16 SSCPreader_GetInfos(byte autoport,
                                                        byte autobaud,
                                                        ref byte baudrate,
                                                        ref byte addr485,
                                                        ref byte version,
                                                        ref UInt16 date,
                                                        ref string infoports);
        // V1.2
        #endregion


        #region Reader functions V1.3

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_SetRFParam")]
        public static extern UInt16 SSCPreader_SetRFParam(byte Save,
                                                          UInt16 Adr,
                                                          UInt32 Values);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPreader_RetreiveRFParams")]
        public static extern UInt16 SSCPreader_RetreiveRFParams();

        #endregion

        #endregion
        
        #region EPC functions

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Inventory")]
        public static extern UInt16 SSCPepc_Inventory(ref byte nbTags, [Out] byte[] Tags);

        /* V1.1
        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Read")]
        public static extern UInt16 SSCPepc_Read(byte MaskBank,
                                                 byte MaskLen,
                                                 byte MaskOffset,
                                                 [In] byte[] Mask,
                                                 byte BankID,
                                                 byte BankOffset,
                                                 byte Len,
                                                 byte LogPort,
                                                 UInt32 PWD,
                                                 ref byte MatchNb,
                                                 [Out] byte[] data);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Write")]
        public static extern UInt16 SSCPepc_Write(byte MaskBank,
                                                 byte MaskLen,
                                                 byte MaskOffset,
                                                 [In] byte[] Mask,
                                                 byte BankID,
                                                 byte BankOffset,
                                                 byte Len,
                                                 byte LogPort,
                                                 UInt32 PWD,
                                                 [In] byte[] data);*/
        // V1.2
        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Read")]
        public static extern UInt16 SSCPepc_Read(byte MaskBank,
                                                 byte MaskLen,
                                                 byte MaskOffset,
                                                 [In] byte[] Mask,
                                                 byte BankID,
                                                 UInt16 BankOffset,
                                                 byte Len,
                                                 byte LogPort,
                                                 UInt32 PWD,
                                                 ref byte MatchNb,
                                                 [Out] byte[] data);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Write")]
        public static extern UInt16 SSCPepc_Write(byte MaskBank,
                                                  byte MaskLen,
                                                  byte MaskOffset,
                                                  [In] byte[] Mask,
                                                  byte BankID,
                                                  UInt16 BankOffset,
                                                  byte Len,
                                                  byte LogPort,
                                                  UInt32 PWD,
                                                  [In] byte[] data);


        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Inventory_With_Report")]
        public static extern UInt16 SSCPepc_Inventory_With_Report(byte RSSI, byte RFU1, byte RFU2, byte RFU3,
                                                                  ref byte nbTags, [Out] byte[] Tags);
        //V1.2

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Kill")]
        public static extern UInt16 SSCPepc_Kill(byte MaskBank,
                                                 byte MaskLen,
                                                 byte MaskOffset,
                                                 [In] byte[] Mask,
                                                 byte RFU,
                                                 byte LogPort,
                                                 UInt32 kPWD);

        [DllImport(LIB_PATH_NAME, EntryPoint = "SSCPepc_Lock")]
        public static extern UInt16 SSCPepc_Lock(byte MaskBank,
                                                 byte MaskLen,
                                                 byte MaskOffset,
                                                 [In] byte[] Mask,
                                                 UInt16 PayloadMask,
                                                 UInt16 PayloadAction,
                                                 byte LogPort,
                                                 UInt32 PWD);



        /// ERROR message function
        [DllImport(LIB_PATH_NAME, CharSet = CharSet.Unicode, EntryPoint = "SSCPepc_GetErrorMsg")]
        public static extern UInt16 SSCPepc_GetErrorMsg(UInt16 LID,      // Language ID
                                                        UInt16 Error,
                                                        ref string ErrorMsg);
        #endregion

        public StidReaderApi() {
            //
            // TODO: Add constructor logic here
            //
        }
    }
}
