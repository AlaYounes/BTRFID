using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.Collections;
using fr.nexess.toolbox;

namespace fr.nexess.hao.rfid.device.deister
{
    public class prdi3FrameRebuilder {

        // logger
        private LogProducer logProducer = new LogProducer(typeof(prdi3FrameRebuilder));
        protected static readonly Object locker = new Object();

        private event TagDecodedEventHandler    tagDecodedEventhandler = null;
        private event InfoDecodedEventHandler   infoDecodedEventHandler = null;
        private event EventHandler              modeChangedToAutoReadingEventHandler = null;
        private event EventHandler              modeChangedToCmdEventHandler = null;

        protected ArrayList managedBuffer = new ArrayList();

        private int decodingAttempts = 0;

        private const int MAX_FRAME_NUMBER = 10;

        #region CONSTRUCTOR
        public prdi3FrameRebuilder() {
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

        public void rebuildFrames(List<String> data) {

            if (data != null && data.Count > 0) {

                // try split data buffer to several valid frames
                decodeFrameAndRaiseEvent(data);
            }
        }

        // <summary>
        /// build a well formed frame.
        /// Means add header and checksum around data to tramsmit.
        /// 
        /// </summary>
        public static byte[] buildFrame(byte cmd, byte[] parameters, byte readerAddress) {

            List<byte> list = new List<byte>();

            list.Add(DeBusProtocol.DUMMY);
            list.Add(DeBusProtocol.DUMMY);
            list.Add(DeBusProtocol.SOC);
            list.Add(readerAddress);
            list.Add(DeBusProtocol.SERVER_ADDRESS);

            list.Add(cmd);

            if (parameters.Length > 0) {

                list.AddRange(parameters);
            }

            list.AddRange(computeCrc(list));
            list.Add(DeBusProtocol.STOP);

            return ShiftDeBus(list.ToArray());
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
            bool exit = false;
            try {
                while (!exit) {

                    // find the first occurence of the frame header
                    if (Array.Exists(rawDataBuffer, b => b == DeBusProtocol.DUMMY) == true) {
                        int posHeader = Array.IndexOf(rawDataBuffer, (Byte)DeBusProtocol.DUMMY);
                        if (posHeader > 0) {
                            // rebuild the buffer and ignore the beginning
                            unstackFrameFromBuffer(ref byteList, posHeader, 0);
                            rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));
                            posHeader = 0;
                        }
                        int sizeHeader;
                        // define the size of header
                        if ((rawDataBuffer.Length > posHeader + 1) && (rawDataBuffer[posHeader + 1] == DeBusProtocol.DUMMY)) {
                            sizeHeader = 5;
                        } else //case legic v2 where header is 0xFDFF or 0xFEFF
                          {
                            sizeHeader = 4;
                        }
                        // find the first occurence of frame end
                        if (Array.Exists(rawDataBuffer, b => b == DeBusProtocol.STOP) == true) {                            
                            int posEnd = Array.IndexOf(rawDataBuffer, (Byte)DeBusProtocol.STOP);
                            try {
                                // extract frame
                                int frameLen = posEnd - posHeader - sizeHeader + 1;
                                byte[] frame = new byte[frameLen];
                                Buffer.BlockCopy(rawDataBuffer,     // src buffer
                                                 posHeader + sizeHeader,               // src offset
                                                 frame,             // destination buffer
                                                 0,                 // destination offset
                                                 frameLen);       // count

                                frame = UnshiftDeBus(frame);
                                // rebuild the buffer
                                unstackFrameFromBuffer(ref byteList, posEnd + 1, 0);
                                rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));

                                validFrames.Add(frame);
                            }
                            catch(Exception ex) {
                                logProducer.Logger.Error("Exception decoding Frame with STOP, " + ex.Message);
                                unstackFrameFromBuffer(ref byteList, posEnd + 1, 0);
                                rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));
                            }
                            if (rawDataBuffer.Length == 0) {
                                exit = true;
                            }
                        } else {
                            exit = true;
                        }
                    } else {
                        // clear the buffer, no interest
                        clearUndecodableBytesFromManagedBuffer(ref byteList, rawDataBuffer.Length);
                        exit = true;
                    }

                }// end while
            } catch(Exception ex) {
                logProducer.Logger.Error("Exception decoding Frame received, " + ex.Message);
            }
            return validFrames;
        }

        private RESPONSE decodeFrame(String stringFrame) {

            RESPONSE response = RESPONSE.CMD_UNKOWN;

            if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(DeBusProtocol.RESPONSE.GET_READER_INFO.HEADER)))
            {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length >= DeBusProtocol.RESPONSE.GET_READER_INFO.TOTAL_FRAME_LENGTH) {
                    response = RESPONSE.GET_READER_INFO;
                }

            }
            else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(DeBusProtocol.RESPONSE.TAG_RECEIVED.HEADER)))
            {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == ((byteFrame[7]+6)/8)+11)
                {

                    response = RESPONSE.TAG_RECEIVED;
                }
            }
            else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(DeBusProtocol.RESPONSE.MODE_CHANGED.HEADER)))
            {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == DeBusProtocol.RESPONSE.MODE_CHANGED.TOTAL_FRAME_LENGTH)
                {

                    if (byteFrame[DeBusProtocol.RESPONSE.MODE_CHANGED.MODE_INDEX] == DeBusProtocol.RESPONSE.MODE_CHANGED.MODE_AUTO_READING)
                    {

                        response = RESPONSE.MODE_CHANGED_TO_AUTO_READING;

                    }
                    else if (byteFrame[DeBusProtocol.RESPONSE.MODE_CHANGED.MODE_INDEX] == DeBusProtocol.RESPONSE.MODE_CHANGED.MODE_COMMAND)
                    {

                        response = RESPONSE.MODE_CHANGED_TO_COMMAND;

                    } else {
                        response = RESPONSE.CMD_UNKOWN;
                    }
                }
            } else {

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

        private void decodeFrameAndRaiseEvent(List<String> receivedData) {

            List<byte[]> validFrames = new List<byte[]>();

            String flattedData = concatReceivedData(receivedData);
            managedBuffer.AddRange(ConversionTool.stringToByteArray(flattedData));

            validFrames = splitBufferToValidFrames(ref managedBuffer);

            checkDecodingAttemptNumber(validFrames);

            foreach (byte[] byteFrame in validFrames) {

                String stringFrame = ConversionTool.byteArrayToString(byteFrame);

                switch (decodeFrame(stringFrame)) {
                    case RESPONSE.GET_READER_INFO:

                        Dictionary<String,String> infos = getInfoFromBuffer(stringFrame);

                        raiseInfoDecodedEvent(infos);

                        break;
                    case RESPONSE.TAG_RECEIVED:

                        byte[] snr = getSnrFromBuffer(stringFrame);

                        raiseTagDecodedEvent(toolbox.ConversionTool.byteArrayToString(snr));

                        break;
                    case RESPONSE.MODE_CHANGED_TO_COMMAND:

                        raiseModeChangedToCmdEvent();

                        break;
                    case RESPONSE.MODE_CHANGED_TO_AUTO_READING:

                        raiseModeChangedToAutoReadingEvent();

                        break;
                    case RESPONSE.CMD_UNKOWN:
                        break;
                    default:
                        break;
                }
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

        private static Dictionary<String, String> getInfoFromBuffer(String buffer) {

            Dictionary<String,String> infos = new Dictionary<string, string>();

            byte[] softVersion = new byte[DeBusProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_LENGTH];

            if (!String.IsNullOrEmpty(buffer)) {

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);

                System.Buffer.BlockCopy(frame, DeBusProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_INDEX,
                                        softVersion, 0, DeBusProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_LENGTH);

                infos.Add("SOFT_VERSION", toolbox.ConversionTool.byteArrayToString(softVersion));

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

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);
                /*tag = new byte[frame[7] / 8];
                System.Buffer.BlockCopy(frame, DeBusProtocol.RESPONSE.TAG_RECEIVED.SNR_INDEX+1,
                                       tag, 0, frame[7]/8);
                 * */
                tag = new byte[8];
                System.Buffer.BlockCopy(frame, DeBusProtocol.RESPONSE.TAG_RECEIVED.SNR_INDEX + 1,
                                       tag, 8 - ((frame[7]+6) / 8), (frame[7]+6) / 8);

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
        MODE_CHANGED_TO_COMMAND,
        MODE_CHANGED_TO_AUTO_READING,
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
    public struct DeBusProtocol {

        public const int HEAD_SIZE    = 2; // BYTE
        public const int SOC_SIZE     = 1; // BYTE (Start of Command)
        public const int DST_SIZE     = 1; // BYTE (Destination address)
        public const int SRC_SIZE     = 1; // BYTE (Source address)

        public const int DUMMY = 0xFF;
        public const int SOC = 0xFD;
        public const int SOM = 0xFB;
        public const int STOP = 0xFE;
        public const int SHIFT = 0xF8;
        public const int READER_ADDRESS = 0x31;// prdm/5: 0x3F;
        public const int SERVER_ADDRESS = 0x11;

        // requests
        public struct CMD {

            public struct CMD_Reset
            {
                public const byte RESET = 0x01;
            }

            public struct CMD_Version
            {
                public const byte VERSION = 0x02;
            }

            public struct CMD_Polling
            {
                public const byte POLLING = 0x0B;
            }

            public struct CMD_MODE
            {
                public const byte MODE = 0x0A;
                public static byte[] COMMAND_MODE = { 0x00 };
                public static byte[] POLLING_MODE = { 0x01 };
                public static byte[] AUTOMATIC_READING = { 0x02 };
            }

            public struct CMD_GetStatus
            {
                public const byte STATUS = 0x0D;
            }

            public struct CMD_RF
            {
                public const byte MODE = 0x81;
                public static byte[] RF_OFF = { 0x00 };
                public static byte[] RF_ON = { 0xFF };
            }

        }


        public struct RESPONSE {

            public struct GET_READER_INFO {

                public const int SOFT_VERSION_INDEX = 8;
                public const int SOFT_VERSION_LENGTH = 2;
                public const int SERIAL_INDEX = 2;
                public const int SERIAL_LENGTH = 4;

                public const int TOTAL_FRAME_LENGTH = 17;

                public static byte[] HEADER = { 0x02 }; 
            }

            public struct TAG_RECEIVED {

                public const int SNR_INDEX = 7;

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER = { 0x40 };
            }

            public struct MODE_CHANGED {

                public const int TOTAL_FRAME_LENGTH = 5;
                public const int MODE_INDEX = 1;

                public const byte MODE_COMMAND = 0x00;
                public const byte MODE_POLLING = 0x01;
                public const int MODE_AUTO_READING = 0x10;

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER = { 0x0A };
            }

        }
    }
    #endregion
}
