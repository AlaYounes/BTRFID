using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.Collections;
using fr.nexess.toolbox;

namespace fr.nexess.hao.rfid.device.elatec
{
    public class Twn4FrameRebuilder {

        // logger
        private LogProducer logProducer = new LogProducer(typeof(Twn4FrameRebuilder));
        protected static readonly Object locker = new Object();

        private event TagDecodedEventHandler    tagDecodedEventhandler = null;
        private event InfoDecodedEventHandler   infoDecodedEventHandler = null;
        private event EventHandler              modeChangedToAutoReadingEventHandler = null;
        private event EventHandler              modeChangedToCmdEventHandler = null;

        protected ArrayList managedBuffer = new ArrayList();

        private int decodingAttempts = 0;

        private const int MAX_FRAME_NUMBER = 10;
        private const int FRAME_SIZE_VERSION = 63;
        private string PartSnr14443 = "";
        private bool isfullSnr = false;

        #region CONSTRUCTOR
        public Twn4FrameRebuilder() {
        }
        #endregion


        #region EVENT_HANDLERS
        public event TagDecodedEventHandler TagDecoded {
            add {
                lock (locker) {
                    tagDecodedEventhandler += value;
                }
            }
            remove {
                lock (locker) {
                    tagDecodedEventhandler -= value;
                }
            }
        }
        public event InfoDecodedEventHandler InfoDecoded {
            add {
                lock (locker) {
                    infoDecodedEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    infoDecodedEventHandler -= value;
                }
            }
        }
        public event EventHandler ModeChangedToCommand {
            add {
                lock (locker) {
                    modeChangedToCmdEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    modeChangedToCmdEventHandler -= value;
                }
            }
        }
        public event EventHandler ModeChangedToAutoReading {
            add {
                lock (locker) {
                    modeChangedToAutoReadingEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    modeChangedToAutoReadingEventHandler -= value;
                }
            }
        }
        #endregion

        #region PUBLIC_METHODS

        public void rebuildFrames(String data) {

            if (data != null ) {

                // try split data buffer to several valid frames
                decodeFrameAndRaiseEvent(data);
            }
        }

        // <summary>
        /// build a well formed frame.
        /// Means add header and checksum around data to tramsmit.
        /// 
        /// </summary>
        public static byte[] buildFrame(byte[] cmd, byte[] parameters) {

            List<byte> list = new List<byte>();

            list.AddRange(cmd);

            if (parameters.Length > 0) {

                list.AddRange(parameters);
            }

            return list.ToArray();
        }
        #endregion

        #region PRIVATE_METHODS

        static byte[] ShiftDeBus (byte[] bufin)
        {      
        List<byte> bufout = new List<byte>(bufin);
        int lengthin = bufin.Length;
        int offset=0;
        for (int i=6; i<lengthin-1; i++)
	        {
                if ((bufin[i] == 0xF8) || (bufin[i] == 0xFB) || (bufin[i] == 0xFD) || (bufin[i] == 0xFE))
		        {
		        bufout[i+offset]=0xF8;
		        offset++;
                switch (bufin[i])
			        {
			        case 0xF8:				
				        bufout[i+offset]=0;
				        break;
			        case 0xFB:				
				        bufout[i+offset]=1;
				        break;
			        case 0xFD:				
				        bufout[i+offset]=2;
				        break;
			        case 0xFE:				
				        bufout[i+offset]=3;
				        break;
			        }
		        }
	             else
                    bufout[i + offset] = bufin[i];
	        }
        bufout[lengthin-1+offset]=bufin[lengthin-1];
        return bufout.ToArray();
        }

        byte[] UnshiftDeBus(byte[] bufin)
        {
            List<byte> bufout = new List<byte>();
            int lengthin = bufin.Length;
            int offset=0;
            for (int i=0; i<lengthin-offset; i++)
	            {
                    if (bufin[i + offset] == 0xF8)
		            {
                        switch (bufin[i + offset + 1])
			            {
                        case 0:
                            bufout.Add(0xF8);
                            break;
			            case 1:
                            bufout.Add(0xFB);
				            break;
			            case 2:
                            bufout.Add(0xFD);
				            break;
			            case 3:
                            bufout.Add(0xFE);
				            break;
			            }
		            offset++;
		            }
	             else
                        bufout.Add(bufin[i + offset]);
	            }
            return bufout.ToArray(); ;
        }

        private static byte[] computeCrc(List<byte> list) {

            byte[] myCrc = new byte[] {0,0};

            for (int i = 2; i < list.Count(); i++ )
            {
                calcCrc(list.ElementAt(i), ref myCrc[0], ref myCrc[1]);
            }

            return myCrc;
        }

        private static void calcCrc(byte databyte, ref byte CRCL, ref byte CRCH)
        {
        byte tmp;
	    tmp=CRCL;
	    CRCL=CRCH;
	    CRCH=tmp;
	    tmp=(byte)(databyte^CRCH);
	    tmp=(byte)(tmp^(tmp<<4));
	    CRCL=(byte)(CRCL^(tmp>>4));
	    CRCL=(byte)(CRCL^(tmp<<3));
        CRCH =(byte)(tmp ^ (tmp >> 5)); 
        }

        private static Boolean isValidCrc(Byte[] byteFrame) {

            Boolean result = true;
            /*
            if (byteFrame == null) {
                return result;
            }

            byte crcToCheck = byteFrame[byteFrame.Length - 1];

            byte[] byteFrameWithoutCrc = new byte[byteFrame.Length - 1];
            Buffer.BlockCopy(byteFrame,             // src buffer
                             0,                     // src offset
                             byteFrameWithoutCrc,   // destination buffer
                             0,                     // destination offset
                             byteFrame.Length - 1); // count

            byte crcToCompare = computeCrc(byteFrameWithoutCrc);

            if (crcToCheck == crcToCompare) {
                result = true;
            }
            */
            return result;
        }

        private List<byte[]> splitBufferToValidFrames(ref ArrayList byteList) {

            List<byte[]> validFrames = new List<byte[]>();

            if (byteList == null) {
                // nothing to do, return...
                return validFrames;
            }

            Byte[] rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));
            if (rawDataBuffer.Length == 0)
                return validFrames;

            if (rawDataBuffer[0] != 0x80 || (rawDataBuffer.Length > 1 && (rawDataBuffer[0] == 0x80 && (rawDataBuffer[1] == rawDataBuffer.Length - 2)))) {
                if (rawDataBuffer[0] == 0x80 && rawDataBuffer[1] >= 0x05 && rawDataBuffer[1] <= 0x07) {
                    if (rawDataBuffer[3] == 0) {
                        isfullSnr = true;
                        PartSnr14443 = "";
                    }
                }
                if (rawDataBuffer[0] == 0x00) {
                    isfullSnr = false;
                    PartSnr14443 = "";
                }
                if (PartSnr14443.Length > 0) {
                    int toto = 0;
                }
                byte[] frame = new byte[rawDataBuffer.Length];
                Buffer.BlockCopy(rawDataBuffer,     // src buffer
                                         0,               // src offset
                                         frame,             // destination buffer
                                         0,                 // destination offset
                                         rawDataBuffer.Length);       // count
                validFrames.Add(frame);
                byteList.Clear();
            } else {
                if (rawDataBuffer.Length > 15 || (rawDataBuffer[0] != 0x80)) {
                    byteList.Clear();
                    PartSnr14443 = "";
                }
            }


            return validFrames;
        }

        private RESPONSE decodeFrame(String stringFrame) {

            RESPONSE response = RESPONSE.CMD_UNKOWN;

            if (stringFrame.Length >= (Twn4Protocol.RESPONSE.GET_READER_INFO.VERSION_FRAME_SIZE_MIN*2))
            {
                response = RESPONSE.GET_READER_INFO;
            }
            else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(Twn4Protocol.RESPONSE.TAG_RECEIVED.HEADER_OK)))
            {
                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == (byteFrame[4]+5))
                {
                    response = RESPONSE.TAG_RECEIVED;
                }
            }
            else {

                response = RESPONSE.CMD_UNKOWN;
            }

            return response;
        }

        private static String concatReceivedData(List<String> data) {

            String flattedString = "";

            if (data != null) {

                foreach (String str in data) {

                    flattedString += str;
                }
            }

            return flattedString;
        }

        private void decodeFrameAndRaiseEvent(String receivedData) {

            try {
                logProducer.Logger.Info("Received frame: " + receivedData);
                switch (decodeFrame(receivedData)) {
                    case RESPONSE.GET_READER_INFO:

                        Dictionary<String, String> infos = getInfoFromBuffer(receivedData);

                        raiseInfoDecodedEvent(infos);

                        break;
                    case RESPONSE.TAG_RECEIVED:

                        byte[] snr = getSnrFromBuffer(receivedData);

                        raiseTagDecodedEvent(toolbox.ConversionTool.byteArrayToString(snr));

                        break;
                    case RESPONSE.CMD_UNKOWN:
                        break;
                    default:
                        break;
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception decodin frame, " + ex.Message);
            }

        }

        private void checkDecodingAttemptNumber(List<byte[]> validFrames) {
            if (validFrames.Count == 0) {

                decodingAttempts++;

                if (decodingAttempts >= MAX_FRAME_NUMBER) {

                    managedBuffer.Clear();
                    decodingAttempts = 0;
                }
            } else {
                decodingAttempts = 0;
            }
        }

        // Exemple of frame ISO15693: 0001824008E00700001F8831FF
        //                  ISO14443A: 00018020046E401BA8
        // 0001 = Result OK
        // 8020 = ISO1443A
        // 04 = Size of the SNR
        private static Dictionary<String, String> getInfoFromBuffer(String buffer) {

            Dictionary<String,String> infos = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(buffer)) {
                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);
                int lenVersion = frame[Twn4Protocol.RESPONSE.GET_READER_INFO.LENGTH_INDEX];
                byte[] softVersion = new byte[lenVersion];

                System.Buffer.BlockCopy(frame, Twn4Protocol.RESPONSE.GET_READER_INFO.VERSION_FRAME_BEGIN,
                                        softVersion, 0, lenVersion);
                string hexa = toolbox.ConversionTool.byteArrayToString(softVersion);
                string ascii = toolbox.ConversionTool.hexaToAscii(hexa);
                infos.Add("SOFT_VERSION", ascii);
            }

            return infos;
        }

        private void raiseModeChangedToCmdEvent() {

            if (modeChangedToCmdEventHandler != null) {
                modeChangedToCmdEventHandler(this, EventArgs.Empty);
            }
        }

        private void raiseModeChangedToAutoReadingEvent() {

            if (modeChangedToAutoReadingEventHandler != null) {
                modeChangedToAutoReadingEventHandler(this, EventArgs.Empty);
            }
        }

        private void raiseInfoDecodedEvent(Dictionary<String, String> infos) {

            if (infoDecodedEventHandler != null) {
                infoDecodedEventHandler(this, new InfoDecodedEventArgs(infos));
            }
        }

        private static byte[] getSnrFromBuffer(String buffer) {

            byte[] tag = new byte[0];

            if (!String.IsNullOrEmpty(buffer)) {
                // reverse t
                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);
                tag = new byte[8];              
                int lenTagScanned = frame[Twn4Protocol.RESPONSE.TAG_RECEIVED.LENGTH_INDEX];
                if (buffer.StartsWith(toolbox.ConversionTool.byteArrayToString(Twn4Protocol.RESPONSE.TAG_RECEIVED.HEADER_ISO14443A))) {
                    int posEndFrame = frame.Length - 1;
                    byte[] frameTag = new byte[lenTagScanned];

                    for (int i = posEndFrame, j = 0; i > posEndFrame - lenTagScanned; i--, j++) {
                        frameTag[j] = frame[i];
                    }
                    System.Buffer.BlockCopy(frameTag, 0, 
                        tag, 8 - lenTagScanned, lenTagScanned);
                }
                else {
                    System.Buffer.BlockCopy(frame, Twn4Protocol.RESPONSE.TAG_RECEIVED.SNR_PREFIX_SIZE,
                        tag, 8 - lenTagScanned, lenTagScanned);
                }
            }

            return tag;
        }

        private void raiseTagDecodedEvent(String snr) {

            if (tagDecodedEventhandler != null) {
                tagDecodedEventhandler(this, new TagDecodedEventArgs(snr));
            }
        }

        private Byte[] unstackFrameFromBuffer(ref ArrayList buffer, int nbElementToRemove, int from) {

            buffer.RemoveRange(from, nbElementToRemove);

            return (byte[])buffer.ToArray(typeof(byte));
        }

        private void clearUndecodableBytesFromManagedBuffer(ref ArrayList buffer, int nbElementToRemove) {

            if (nbElementToRemove > 0 && nbElementToRemove <= buffer.Count) {
                buffer.RemoveRange(0, nbElementToRemove);
            }
        }
        #endregion
    }

    public enum RESPONSE {
        GET_READER_INFO,
        TAG_RECEIVED,
        CMD_UNKOWN
    }

    #region EVENT_HANDLER_DEFINITION
    public delegate void TagDecodedEventHandler(object sender, TagDecodedEventArgs e);

    public class TagDecodedEventArgs : EventArgs {
        private String snr = "";

        public TagDecodedEventArgs(String snr) {
            this.snr = snr;
        }

        public String Snr {
            get {
                return this.snr;
            }
        }
    }

    public delegate void InfoDecodedEventHandler(object sender, InfoDecodedEventArgs e);

    public class InfoDecodedEventArgs : EventArgs {
        private Dictionary<String,String> info = new Dictionary<string, string>();

        public InfoDecodedEventArgs(Dictionary<String, String> info) {
            this.info = info;
        }

        public Dictionary<String, String> Info {
            get {
                return this.info;
            }
        }
    }
    #endregion

    #region PROTOCOL

    public struct Twn4Protocol {

        // requests
        
        public struct CMD {
            public static byte[] GetVersionString = new byte[] { 0x00, 0x04, 0xFF };
            public static byte[] SearchTag = new byte[] { 0x05, 0x00, 0x10 };
            //(TagTypesLF: FFFFFFFF, TagTypesHF: FFFFFFFF)
            // MiFARE 14443= 01000000 , ISO15693= 04000000, Legic RF= 08000000, HIDICLASS= 10000000 
            // LF_EM4 = EM4102 EM4150 EM4026 EM4305 = 49010000
            // LF_HITAG = LF HITAG2 HITAG1S = 49010000
            //public static byte[] SetTagType = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x0D, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType = new byte[] { 0x05, 0x02, 0x4F, 0x01, 0x00, 0x00, 0x1D, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_14443 = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_ISO15693 = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_LEGIC = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_HIDICLASS = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_LF_EM4 = new byte[] { 0x05, 0x02, 0x49, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            public static byte[] SetTagType_LF_HITAG = new byte[] { 0x05, 0x02, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            // LEDs
            public static byte[] LEDInit = new byte[] { 0x04, 0x10, 0x07 }; // cmd 0410 + all leds
            public static byte[] LEDOff = new byte[] { 0x04, 0x12, 0x07 }; // cmd 0410 + all leds
            public static byte[] LEDOnGreen = new byte[] { 0x04, 0x11, 0x02 }; // cmd 0411 + green led
            public static byte[] LEDOnRed = new byte[] { 0x04, 0x11, 0x01 }; // cmd 0411 + red led
            public static byte[] LEDBlinkGreen = new byte[] { 0x04, 0x14, 0x02, 0xF4, 0x01, 0xF4, 0x01 }; // cmd 0414 + green led + timeon + timeoff
            public static byte[] LEDBlinkRed = new byte[] { 0x04, 0x14, 0x01, 0xF4, 0x01, 0xF4, 0x01 }; // cmd 0414 + red led + timeon + timeoff

            // Beep 
            public static byte[] Beep = new byte[] { 0x04, 0x07, 0x64, 0x60, 0x09, 0xF4, 0x01, 0xF4, 0x01 }; // cmd 0407 + volume + timeon + timeoff
        }


        public struct RESPONSE {

            public struct GET_READER_INFO {

                public const int VERSION_FRAME_SIZE_MIN= 26;
                public const int VERSION_FRAME_BEGIN = 2;
                public static byte[] HEADER= { 0x00 };
                public const int LENGTH_INDEX = 1;
            }

            public struct TAG_RECEIVED {

                public const int SNR_PREFIX_SIZE= 5;
                public const int LENGTH_INDEX = 4;
                public static byte[] HEADER_OK = { 0x00, 0x01 };
                public static byte[] HEADER_ISO14443A = { 0x00, 0x01, 0x80, 0x20 };
            }

        }
    }
    #endregion
}
