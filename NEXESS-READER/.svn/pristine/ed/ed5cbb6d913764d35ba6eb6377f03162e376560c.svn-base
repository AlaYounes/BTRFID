using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.Collections;
using fr.nexess.toolbox;

namespace fr.nexess.hao.rfid.device.stm
{
    public class CR95FrameRebuilder
    {
        // logger
        private LogProducer logProducer = new LogProducer(typeof(CR95FrameRebuilder));
        protected static readonly Object locker = new Object();

        private event TagDecodedEventHandler tagDecodedEventhandler = null;
        private event InfoDecodedEventHandler infoDecodedEventHandler = null;
        private event EventHandler modeChangedToAutoReadingEventHandler = null;
        private event EventHandler modeChangedToCmdEventHandler = null;

        protected ArrayList managedBuffer = new ArrayList();

        private const int MAX_FRAME_NUMBER = 10;
        private int decodingAttempts = 0;
        private string PartSnr14443 = "";
        private bool isfullSnr = false;

        #region CONSTRUCTOR
        public CR95FrameRebuilder() { }
        #endregion

        #region EVENT_HANDLERS
        public event TagDecodedEventHandler TagDecoded
        {
            add
            {
                lock (locker)
                {
                    tagDecodedEventhandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    tagDecodedEventhandler -= value;
                }
            }
        }
        public event InfoDecodedEventHandler InfoDecoded
        {
            add
            {
                lock (locker)
                {
                    infoDecodedEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    infoDecodedEventHandler -= value;
                }
            }
        }
        public event EventHandler ModeChangedToCommand
        {
            add
            {
                lock (locker)
                {
                    modeChangedToCmdEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    modeChangedToCmdEventHandler -= value;
                }
            }
        }
        public event EventHandler ModeChangedToAutoReading
        {
            add
            {
                lock (locker)
                {
                    modeChangedToAutoReadingEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    modeChangedToAutoReadingEventHandler -= value;
                }
            }
        }
        #endregion

        #region PUBLIC_METHODS

        public void rebuildFrames(List<String> data)
        {

            if (data != null && data.Count > 0)
            {

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
        public static byte[] buildFrame(byte cmd, byte[] parameters)
        {

            List<byte> list = new List<byte>();

            byte head = (byte)(CR95Protocol.CMD_SIZE + parameters.Length);

            list.Add(cmd);

            if (parameters.Length > 0)
            {

                list.AddRange(parameters);
            }

            return list.ToArray();
        }
        #endregion

        #region PRIVATE_METHODS


        private List<byte[]> splitBufferToValidFrames(ref ArrayList byteList)
        {

            List<byte[]> validFrames = new List<byte[]>();

            if (byteList == null)
            {
                // nothing to do, return...
                return validFrames;
            }

            Byte[] rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));
            if (rawDataBuffer.Length == 0)
                return validFrames;

            if (rawDataBuffer[0] != 0x80 || (rawDataBuffer.Length>1 && (rawDataBuffer[0] == 0x80 && (rawDataBuffer[1]== rawDataBuffer.Length-2))))
            {
                if (rawDataBuffer[0] == 0x80 && rawDataBuffer[1] >= 0x05 && rawDataBuffer[1] <=0x07)
                {
                    if (rawDataBuffer[3] == 0)
                    {
                        isfullSnr = true;
                        PartSnr14443 = "";
                    }
                }
                if (rawDataBuffer[0]==0x00)
                {
                    isfullSnr = false;
                    PartSnr14443 = "";
                }
                if (PartSnr14443.Length > 0)
                {
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
            }
            else {
                if (rawDataBuffer.Length > 15 || (rawDataBuffer[0] != 0x80))
                {
                    byteList.Clear();
                    PartSnr14443 = "";
                }
            }
            
 
            return validFrames;
        }

        private RESPONSE decodeFrame(String stringFrame)
        {

            RESPONSE response = RESPONSE.CMD_UNKOWN;

            if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(CR95Protocol.RESPONSE.GET_READER_INFO.HEADER)))
            {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                if (byteFrame.Length == CR95Protocol.RESPONSE.GET_READER_INFO.TOTAL_FRAME_LENGTH)
                {
                    response = RESPONSE.GET_READER_INFO;
                }

            }
            else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(CR95Protocol.RESPONSE.TAG_RECEIVED.HEADER_15693)))
            {

                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                    response = RESPONSE.TAG_RECEIVED;
            }
            else if (stringFrame.StartsWith(toolbox.ConversionTool.byteArrayToString(CR95Protocol.RESPONSE.TAG_RECEIVED.HEADER_14443)))
            {

                
                Byte[] byteFrame = toolbox.ConversionTool.stringToByteArray(stringFrame);

                response = RESPONSE.TAG_14443_PART;
            }
            else if (stringFrame.CompareTo(toolbox.ConversionTool.byteArrayToString(CR95Protocol.RESPONSE.AWAKEN.HEADER))==0)
            {
                response = RESPONSE.AWAKEN;
            }

            else {

                response = RESPONSE.CMD_UNKOWN;
            }
            
            return response;
        }

        private static String concatReceivedData(List<String> data)
        {

            String flattedString = "";

            if (data != null)
            {

                foreach (String str in data)
                {

                    flattedString += str;
                }
            }

            return flattedString;
        }

        private void decodeFrameAndRaiseEvent(List<String> receivedData)
        {

            List<byte[]> validFrames = new List<byte[]>();

            String flattedData = concatReceivedData(receivedData);
            managedBuffer.AddRange(ConversionTool.stringToByteArray(flattedData));

            validFrames = splitBufferToValidFrames(ref managedBuffer);

            checkDecodingAttemptNumber(validFrames);

            foreach (byte[] byteFrame in validFrames)
            {

                String stringFrame = ConversionTool.byteArrayToString(byteFrame);
                
                switch (decodeFrame(stringFrame))
                {
                    case RESPONSE.GET_READER_INFO:

                        Dictionary<String, String> infos = getInfoFromBuffer(stringFrame);

                        raiseInfoDecodedEvent(infos);

                        break;
                    case RESPONSE.TAG_RECEIVED:

                        if (stringFrame.Substring(stringFrame.Length - 2, 2).CompareTo("00") == 0)
                        {
                            byte[] snr = getSnrFromBuffer(stringFrame);

                            raiseTagDecodedEvent(toolbox.ConversionTool.byteArrayToString(snr));
                        }
                        isfullSnr = false;
                        break;
                    case RESPONSE.AWAKEN:

                        break;
                    case RESPONSE.ACK:
                        isfullSnr = false;

                        break;
                    case RESPONSE.TAG_14443_PART:

                        if (stringFrame.Substring(stringFrame.Length - 6, 6).CompareTo("280000") == 0)
                        {
                            byte[] snrPart = getSnrPartFromBuffer(stringFrame, PartSnr14443.Length, isfullSnr);
                            if (PartSnr14443.Length>0)
                            {
                                int toto = 0;
                            }
                            PartSnr14443 = (toolbox.ConversionTool.byteArrayToString(snrPart)) + PartSnr14443;
                            if (PartSnr14443.Length == 14)
                            {
                                raiseTagDecodedEvent("00" + PartSnr14443);
                                PartSnr14443 = "";
                                isfullSnr = false;
                            }
                        }
                        break;
                    case RESPONSE.CMD_UNKOWN:
                        break;
                    default:
                        break;
                }
                
            }
        }

        private void checkDecodingAttemptNumber(List<byte[]> validFrames)
        {
            if (validFrames.Count == 0)
            {

                decodingAttempts++;

                if (decodingAttempts >= MAX_FRAME_NUMBER)
                {

                    managedBuffer.Clear();
                    decodingAttempts = 0;
                }
            }
            else {
                decodingAttempts = 0;
            }
        }

        private static Dictionary<String, String> getInfoFromBuffer(String buffer)
        {

            Dictionary<String, String> infos = new Dictionary<string, string>();

            infos.Add("VERSION", buffer);

            return infos;
        }

        private void raiseModeChangedToCmdEvent()
        {

            if (modeChangedToCmdEventHandler != null)
            {
                modeChangedToCmdEventHandler(this, EventArgs.Empty);
            }
        }

        private void raiseModeChangedToAutoReadingEvent()
        {

            if (modeChangedToAutoReadingEventHandler != null)
            {
                modeChangedToAutoReadingEventHandler(this, EventArgs.Empty);
            }
        }

        private void raiseInfoDecodedEvent(Dictionary<String, String> infos)
        {

            if (infoDecodedEventHandler != null)
            {
                infoDecodedEventHandler(this, new InfoDecodedEventArgs(infos));
            }
        }

        private static byte[] getSnrFromBuffer(String buffer)
        {

            byte[] tag= new byte[CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_LENGTH];
            
            if (!String.IsNullOrEmpty(buffer))
            {

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);
                byte[] tmp = new byte[CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_LENGTH];
                System.Buffer.BlockCopy(frame, CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_INDEX,
                                       tmp, 0, CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_LENGTH);

                
                for (int i = 0; i < tmp.Length; i++)
                {
                    tag[i] = tmp[tmp.Length-1 - i];
                }

            }
            

            return tag;
        }

        private static byte[] getSnrPartFromBuffer(String buffer, int part, bool fullSnr)
        {

            

            int length, index;
            if (part == 0 && !fullSnr)
            {
                index = CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_PART_INDEX;
                length = CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_PART_LENGTH;
            }
            else
            {
                index = CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_PART2_INDEX;
                length = CR95Protocol.RESPONSE.TAG_RECEIVED.SNR_PART2_LENGTH;
            }
            byte[] tag = new byte[length];
            if (!String.IsNullOrEmpty(buffer))
            {

                Byte[] frame = toolbox.ConversionTool.stringToByteArray(buffer);
                byte[] tmp = new byte[length];
                System.Buffer.BlockCopy(frame, index,
                                       tmp, 0, length);

                for (int i = 0; i < tmp.Length; i++)
                {
                    tag[i] = tmp[tmp.Length - 1 - i];
                }
            }
            if (fullSnr)
            {
                byte[] fullbyte = new byte[length+3];
                fullbyte[0] = 0;
                fullbyte[1] = 0;
                fullbyte[2] = 0;
                for (int i = 0; i < tag.Length; i++)
                {
                    fullbyte[i + 3] = tag[i];
                }
                return fullbyte;
            }

            return tag;
        }

        private void raiseTagDecodedEvent(String snr)
        {

            if (tagDecodedEventhandler != null)
            {
                tagDecodedEventhandler(this, new TagDecodedEventArgs(snr));
            }
        }

        private Byte[] unstackFrameFromBuffer(ref ArrayList buffer, int nbElementToRemove, int from)
        {

            buffer.RemoveRange(from, nbElementToRemove);

            return (byte[])buffer.ToArray(typeof(byte));
        }

        private void clearUndecodableBytesFromManagedBuffer(ref ArrayList buffer, int nbElementToRemove)
        {

            if (nbElementToRemove > 0 && nbElementToRemove <= buffer.Count)
            {
                buffer.RemoveRange(0, nbElementToRemove);
            }
        }
        #endregion
    }



    #region EVENT_HANDLER_DEFINITION
    public delegate void TagDecodedEventHandler(object sender, TagDecodedEventArgs e);

    public class TagDecodedEventArgs : EventArgs
    {
        private String snr = "";

        public TagDecodedEventArgs(String snr)
        {
            this.snr = snr;
        }

        public String Snr
        {
            get
            {
                return this.snr;
            }
        }
    }

    public delegate void InfoDecodedEventHandler(object sender, InfoDecodedEventArgs e);

    public class InfoDecodedEventArgs : EventArgs
    {
        private Dictionary<String, String> info = new Dictionary<string, string>();

        public InfoDecodedEventArgs(Dictionary<String, String> info)
        {
            this.info = info;
        }

        public Dictionary<String, String> Info
        {
            get
            {
                return this.info;
            }
        }
    }
#endregion

public enum RESPONSE
{
    GET_READER_INFO,
    TAG_RECEIVED,
    TAG_14443_PART,
    AWAKEN,
    ACK,
    CMD_UNKOWN
}

#region PROTOCOL
public struct CR95Protocol
    {

        public const int HEAD_SIZE = 1; // BYTE
        public const int CMD_SIZE = 1; // BYTE
        public const int CRC_SIZE = 1; // BYTE
        public const int PARAM_INDEX = 2;

        // requests
        public struct CMD
        {
            public struct WAKEUP_0x85
            {
                public const byte WAKEUP = 0x55;
                public static byte[] PARAMS = {};
            }

            public struct SET_PROTOCOL_0x02
            {

                public const byte PROTOCOL = 0x02;

                public static byte[] ISO14443 = { 0x02, 0x02, 0x00 };
                public static byte[] ISO15693 = { 0x02, 0x01, 0x01 };
                public static byte[] OFF = { 0x02, 0x00, 0x00 };
            }

            public struct GET_READER_INFO_0x01
            {

                public const byte GET_READER_INFO = 0x01;
                public static byte[] PARAMS = { 0x00 };
            }

            public struct SendRecv_0x04
            {
                public const byte CMD = 0x04;
                public static byte[] INVENTORY_ISO15693 = { 0x03, 0x26, 0x01, 0x00 };
                public static byte[] REQA_ISO14443 = { 0x02, 0x26, 0x07 };
                public static byte[] ANTICOLL_ISO14443 = { 0x03, 0x93, 0x20, 0x08 };
                //public static byte[] SELECT_ISO14443 = { 0x03, 0x93, 0x20, 0x08 };
                public static byte[] ANTICOLL2_ISO14443 = { 0x03, 0x95, 0x20, 0x08 };
            }

        }


        public struct RESPONSE
        {

            public struct GET_READER_INFO
            {

                public const int HARD_VERSION_INDEX = 2;
                public const int HARD_VERSION_LENGTH = 13;

                public const int TOTAL_FRAME_LENGTH = 17;

                public static byte[] HEADER = {   0x00, 0x0f};
            }

            public struct AWAKEN
            {
                public static byte[] HEADER = {0x55};
            }

            public struct TAG_RECEIVED
            {

                public const int SNR_INDEX = 4;
                public const int SNR_PART_INDEX = 3;
                public const int SNR_PART2_INDEX = 2;
                public const int SNR_LENGTH = 8;
                public const int SNR_PART_LENGTH = 3;
                public const int SNR_PART2_LENGTH = 4;

                // DATA (EVENT [1 BYTE])
                public static byte[] HEADER_15693 = { 0x80,0x0D };
                public static byte[] HEADER_14443 = { 0x80, 0x08 };
            }
        }
    }
    #endregion

}
