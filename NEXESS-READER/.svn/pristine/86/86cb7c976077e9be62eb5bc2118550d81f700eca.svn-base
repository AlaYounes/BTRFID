using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.Collections;
using fr.nexess.toolbox;

namespace fr.nexess.hao.rfid.device.axesstmc {

    /// <summary>
    /// This class is used to manage frame sent by legic rfid card reader
    /// </summary>
    /// <version>$Revision: 32 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-11-21 13:46:24 +0100 (ven., 21 nov. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class LegicFrameRebuilder {

        // logger
        private LogProducer logProducer = new LogProducer(typeof(LegicFrameRebuilder));
        protected static readonly Object locker = new Object();

        private event TagDecodedEventHandler    tagDecodedEventhandler = null;
        private event InfoDecodedEventHandler   infoDecodedEventHandler = null;
        private event EventHandler              modeChangedToAutoReadingEventHandler = null;
        private event EventHandler              modeChangedToCmdEventHandler = null;

        protected ArrayList managedBuffer = new ArrayList();

        private const int MAX_FRAME_NUMBER = 10;
        private int decodingAttempts = 0;

        #region CONSTRUCTOR
        public LegicFrameRebuilder() { }
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
        /// NOTE - RESPONSE FRAME SCHEMA : 
        /// HEAD [1 BYTE] + CMD [1 BYTE] + PARAMS [N BYTES] + CRC [1 BYTE]
        /// </summary>
        public static byte[] buildFrame(byte cmd, byte[] parameters) {

            List<byte> list = new List<byte>();

            byte head = (byte)(LegicProtocol.CMD_SIZE + parameters.Length + LegicProtocol.CRC_SIZE);
            list.Add(head);

            list.Add(cmd);

            if (parameters.Length > 0) {

                list.AddRange(parameters);
            }

            byte crc = computeCrc(list);
            list.Add(crc);

            return list.ToArray();
        }
        #endregion

        #region PRIVATE_METHODS
        private static byte computeCrc(List<byte> list) {

            byte crc = 0x00;

            foreach (byte b in list) {
                crc ^= b;
            }

            return crc;
        }

        private static byte computeCrc(Byte[] list) {

            List<byte> byteList = new List<byte>();

            if (list.Length > 0) {
                byteList.AddRange(list);
            }

            return computeCrc(byteList);
        }

        private static Boolean isValidCrc(Byte[] byteFrame) {

            Boolean result = false;

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

            return result;
        }

        private List<byte[]> splitBufferToValidFrames(ref ArrayList byteList) {

            List<byte[]> validFrames = new List<byte[]>();

            if (byteList == null) {
                // nothing to do, return...
                return validFrames;
            }

            Byte[] rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));

            int pos = 0;

            // don't stepping out of bounds !
            while (pos < rawDataBuffer.Length) {

                // retrieve length information 
                int lenght = rawDataBuffer[pos];

                if (lenght+1 <= (rawDataBuffer.Length - pos)) {

                    // extract frame
                    byte[] frame = new byte[lenght + 1];
                    Buffer.BlockCopy(rawDataBuffer,     // src buffer
                                     pos,               // src offset
                                     frame,             // destination buffer
                                     0,                 // destination offset
                                     lenght + 1);       // count

                    if (frame.Length > 3 && isValidCrc(frame)) {

                        // fill frame list
                        validFrames.Add(frame);

                    } else {
                        // shift
                        pos++;
                        continue;
                    }

                    rawDataBuffer = unstackFrameFromBuffer(ref byteList, lenght + 1, pos);

                } else {

                    // shift
                    logProducer.Logger.Info("raw data buffer contains some incomplete frames : " + ConversionTool.byteArrayToString(rawDataBuffer));
                    pos++;
                    continue;
                }
            }// end while

            if (validFrames.Count > 0 && rawDataBuffer.Length > 0) {

                clearUndecodableBytesFromManagedBuffer(ref byteList, rawDataBuffer.Length);
            }

            return validFrames;
        }

        private RESPONSE decodeFrame(String stringFrame) {

            RESPONSE response = RESPONSE.CMD_UNKOWN;

            if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(LegicProtocol.RESPONSE.GET_READER_INFO.HEADER))) {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == LegicProtocol.RESPONSE.GET_READER_INFO.TOTAL_FRAME_LENGTH) {
                    response = RESPONSE.GET_READER_INFO;
                }

            } else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(LegicProtocol.RESPONSE.TAG_RECEIVED.HEADER))) {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == LegicProtocol.RESPONSE.TAG_RECEIVED.TOTAL_FRAME_LENGTH) {

                    response = RESPONSE.TAG_RECEIVED;
                }
            } else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(LegicProtocol.RESPONSE.MODE_CHANGED.HEADER))) {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == LegicProtocol.RESPONSE.MODE_CHANGED.TOTAL_FRAME_LENGTH) {

                    if (byteFrame[LegicProtocol.RESPONSE.MODE_CHANGED.MODE_INDEX] == LegicProtocol.RESPONSE.MODE_CHANGED.MODE_AUTO_READING) {

                        response = RESPONSE.MODE_CHANGED_TO_AUTO_READING;

                    } else if (byteFrame[LegicProtocol.RESPONSE.MODE_CHANGED.MODE_INDEX] == LegicProtocol.RESPONSE.MODE_CHANGED.MODE_COMMAND) {

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

            byte[] softVersion = new byte[LegicProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_LENGTH];
            byte[] hardVersion = new byte[LegicProtocol.RESPONSE.GET_READER_INFO.HARD_VERSION_LENGTH];

            if (!String.IsNullOrEmpty(buffer)) {

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);

                System.Buffer.BlockCopy(frame, LegicProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_INDEX,
                                        softVersion, 0, LegicProtocol.RESPONSE.GET_READER_INFO.SOFT_VERSION_LENGTH);

                System.Buffer.BlockCopy(frame, LegicProtocol.RESPONSE.GET_READER_INFO.HARD_VERSION_INDEX,
                                        hardVersion, 0, LegicProtocol.RESPONSE.GET_READER_INFO.HARD_VERSION_LENGTH);

                infos.Add("SOFT_VERSION", toolbox.ConversionTool.byteArrayToString(softVersion));
                infos.Add("HARD_VERSION", toolbox.ConversionTool.byteArrayToString(hardVersion));

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

            byte[] tag = new byte[LegicProtocol.RESPONSE.TAG_RECEIVED.SNR_LENGTH];

            if (!String.IsNullOrEmpty(buffer)) {

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);

                System.Buffer.BlockCopy(frame, LegicProtocol.RESPONSE.TAG_RECEIVED.SNR_INDEX,
                                       tag, 0, LegicProtocol.RESPONSE.TAG_RECEIVED.SNR_LENGTH);

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
    public struct LegicProtocol {

        public const int HEAD_SIZE    = 1; // BYTE
        public const int CMD_SIZE     = 1; // BYTE
        public const int CRC_SIZE     = 1; // BYTE
        public const int PARAM_INDEX  = 2;

        // requests
        public struct CMD {

            public struct MODE_0x14 {

                public const byte MODE  = 0x14;

                public static byte[] COMMAND_MODE = { 0x00 };
                public static byte[] AUTOMATIC_READING = { 0x01 };
            }

            public struct GET_READER_INFO_0xB6 {

                public const byte GET_READER_INFO = 0xB6;

                public static byte[] PARAMS = { 0x00, 0x01, 0x01 };
            }
        }


        public struct RESPONSE {

            public struct GET_READER_INFO {

                public const int SOFT_VERSION_INDEX = 12;
                public const int SOFT_VERSION_LENGTH = 4;
                public const int HARD_VERSION_INDEX = 16;
                public const int HARD_VERSION_LENGTH = 4;

                public const int TOTAL_FRAME_LENGTH = 32;

                public static byte[] HEADER = {   0x1F, // frame length, excluding this one
                                                    0xB6, 0x00, /*0x1B, 0xD5, 0x00, 0x00, 0x19, 0x54, 0x29, 0x01, 0x03 */}; // TODO attention : mise en commentaire d'un ensemble d'octet apparemment spécifiques à un legic
            }

            public struct TAG_RECEIVED {

                public const int SNR_INDEX = 5;
                public const int SNR_LENGTH = 8;

                public const int TOTAL_FRAME_LENGTH = 14;

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER = {   0x0D, // frame length, excluding this one
                                                    0x79, 0x00, 0x00, 0x08 };
            }

            public struct MODE_CHANGED {

                public const int TOTAL_FRAME_LENGTH = 4;
                public const int MODE_INDEX = 2;

                public const byte MODE_COMMAND = 0x00;
                public const int MODE_AUTO_READING = 0x01;

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER = {   0x03, // frame length, excluding this one
                                                    0x14/*0x00|0x01, 0x16|0x17*/};
            }

            public struct CMD_UNKOWN {

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER = {   0x09, // frame length, excluding this one
                                                    0x30, 0xF1, 0x00, 0xFF, 0xFF, 0x13, 0x00, 0x10, 0xCB };
            }
        }
    }
    #endregion

}
