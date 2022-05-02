using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox;

namespace fr.nexess.hao.weight.device.pcb12 {

    class Pcb12Protocol {

        internal const int NUMBER_OF_PAD_BY_CHANNEL = 12;
        internal struct FIXED {
            internal const int HEAD = 0xF2;
            internal const int END = 0xF3;
            internal const int SHARP = 0x23;
            internal const int UU = 0x7575;
        }

        internal struct SIZE {
            internal const int HEAD_SIZE = 1;
            internal const int END_SIZE = 1;
            internal const int CRC_SIZE = 1;
            internal const int LENGTH_SIZE = 1;
            internal const int CMD_SIZE = 1;

            internal const int CHANNEL_SIZE = 4;
            internal const int PAD_NUMBER_SIZE = 1;
            internal const int MODEL_NUMBER_SIZE = 6;
            internal const int RESOLUTION_SIZE = 5;
            internal const int LOAD_SIZE = 5;

            internal const int WEIGHT_ACQ_SIZE = 10;

            internal const int SHARP_SIZE = 1;
            internal const int UU_SIZE = 2;
        }

        internal struct COMMAND {
            internal struct CONFIGURATION {
                internal const int SET_PREDEFINED_MODEL_NUMBER = 0x4D;
                internal const int REQUEST_PREDEFINED_MODEL_NUMBER = 0x51;
                internal const int SET_CALIBRATION_WT_PREDEFINED_MODEL = 0x42;
                internal const int REQUEST_CALIBRATION_WT_PREDEFINED_MODEL = 0x4F;
                internal const int SET_PAD_MODEL_NUMBER = 0x4D;
                internal const int REQUEST_PAD_MODEL_NUMBER = 0x51;
                internal const int SET_CALIBRATION_WT_PAD_MODEL = 0x42;
                internal const int REQUEST_CALIBRATION_WT_PAD_MODEL = 0x4F;
                internal const int REQUEST_FIRMWARE_VERSION = 0x56;
                internal const int REQUEST_SERIAL8NUMBER = 0x31;
                internal const int SET_SCALE_ALIAS_NAME = 0x31;
                internal const int REQUEST_SCALE_ALIAS_NAME = 0x31;
            }
            internal struct ADDRESSING {
                internal const int RETRIEVE_SCALE_ID = 0x41;
                internal const int SET_SCALE_ID = 0x53;
                internal const int CHANGE_SCALE_ID = 0x49;
                internal const int RETRIEVE_CHANNEL_COUNTS = 0x31;

            }
            internal struct OPERATIONAL {
                internal const int REQUEST_WEIGHT = 0x57;
                internal const int REQUEST_ALL_WEIGHT = 0x54;
                internal const int REQUEST_CHANNELS_WEIGHT = 0x54;
                internal const int REQUEST_VALID_CHANNELS_WEIGHT = 0x54;
                internal const int RESET_SCALE = 0x52;
                internal const int ZERO_SCALE = 0x5A;

            }
            internal struct CALIBRATION {
                internal const int START_CALIBRATION = 0x43;
                internal const int SAMPLE_DEADLOAD = 0x45;
                internal const int SAMPLE_LOAD = 0x46;
            }
        }
        public enum ERRORS {
            LOAD_CELL_ERROR = 1,
            CALIBRATE_DATA_EMPTY,
            IN_MOTION,
            SCALE_MODEl_NOT_SET,
            SCALE_CHANNEL_NUMBER_ERROR,
            COMMAND_ERROR,
            EEPROM_RD_WR_ERROR,
            ERROR_CALIBRATION_WEIGHT,
            PAD_DISABLED_CANNOT_WEIGHT,
            SHELF_MODE_CANNOT_RUN_COMMAND_OF_PAD,
            PAD_MODE_CANNOT_RUN_COMMAND_OF_SHELF,
            SCALE_IN_POWER_UP = 0x5057
        }
        internal struct WEIGHT_STATUS {
            internal const char HAS_ERROR = 'E';
            internal const char UNSTABLE = 'M';
            internal const char OVERLOAD = 'C';
            internal const char NOT_VALID_WEIGHT = 'I';
            internal const char OK = ' ';
        }

        public static Byte[] computeRequestAllWeightCmd(int pcbAddr) {

            pcbAddr = convertPcbAddr(pcbAddr);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.OPERATIONAL.REQUEST_CHANNELS_WEIGHT;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);

            return buildFrame(cmdData);
        }

        public static Byte[] computeRequestWeightCmd(int pcbAddr, int padId) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.OPERATIONAL.REQUEST_WEIGHT;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)(padId);

            return buildFrame(cmdData);
        }

        public static Byte[] computeSetZeroScaleCmd(int pcbAddr, int padId) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.OPERATIONAL.ZERO_SCALE;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)(padId);

            return buildFrame(cmdData);
        }

        public static Byte[] computeSetPadModelCmd(int pcbAddr, int padId, int accuracy, int load) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            long myaccuracy = convertWeight(accuracy);
            long myload = convertWeight(load);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.SHARP_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE + Pcb12Protocol.SIZE.LOAD_SIZE + Pcb12Protocol.SIZE.LOAD_SIZE + Pcb12Protocol.SIZE.UU_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.CONFIGURATION.SET_PAD_MODEL_NUMBER;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)Pcb12Protocol.FIXED.SHARP;
            cmdData[6] = (byte)(padId);
            cmdData[11] = (byte)(myaccuracy);
            cmdData[10] = (byte)(myaccuracy >> 8);
            cmdData[9] = (byte)(myaccuracy >> 16);
            cmdData[8] = (byte)(myaccuracy >> 24);
            cmdData[7] = (byte)(myaccuracy >> 32);
            cmdData[16] = (byte)(myload);
            cmdData[15] = (byte)(myload >> 8);
            cmdData[14] = (byte)(myload >> 16);
            cmdData[13] = (byte)(myload >> 24);
            cmdData[12] = (byte)(myload >> 32);
            cmdData[18] = (byte)(Pcb12Protocol.FIXED.UU & 0xFF);
            cmdData[17] = (byte)(Pcb12Protocol.FIXED.UU >> 8);

            return buildFrame(cmdData);
        }

        public static Byte[] computeSetCalibrationWeightCmd(int pcbAddr, int padId, int weight) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);
            long myweight = convertCalibrationWeight(weight);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.SHARP_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE + Pcb12Protocol.SIZE.LOAD_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.CONFIGURATION.SET_CALIBRATION_WT_PAD_MODEL;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)Pcb12Protocol.FIXED.SHARP;
            cmdData[6] = (byte)(padId);
            cmdData[11] = (byte)(myweight);
            cmdData[10] = (byte)(myweight >> 8);
            cmdData[9] = (byte)(myweight >> 16);
            cmdData[8] = (byte)(myweight >> 24);
            cmdData[7] = (byte)(myweight >> 32);

            return buildFrame(cmdData);
        }

        public static Byte[] computeStartCalibrationCmd(int pcbAddr, int padId) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.CALIBRATION.START_CALIBRATION;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)(padId);

            return buildFrame(cmdData);
        }

        public static Byte[] computeEmptyCalibrationCmd(int pcbAddr, int padId) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.CALIBRATION.SAMPLE_DEADLOAD;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)(padId);

            return buildFrame(cmdData);
        }

        public static Byte[] computeSetLoadCalibrationCmd(int pcbAddr, int padId) {

            pcbAddr = convertPcbAddr(pcbAddr);
            padId = convertPadNumber(padId);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.CALIBRATION.SAMPLE_LOAD;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);
            cmdData[5] = (byte)(padId);

            return buildFrame(cmdData);
        }

        public static Byte[] computeSetAddressCmd(int pcbAddr) {

            pcbAddr = convertPcbAddr(pcbAddr);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.ADDRESSING.SET_SCALE_ID;
            cmdData[4] = (byte)(pcbAddr);
            cmdData[3] = (byte)(pcbAddr >> 8);
            cmdData[2] = (byte)(pcbAddr >> 16);
            cmdData[1] = (byte)(pcbAddr >> 24);

            return buildFrame(cmdData);
        }

        public static Byte[] computeChangeAddressCmd(int fromPcbAddr, int toPcbAddr) {

            fromPcbAddr = convertPcbAddr(fromPcbAddr);
            toPcbAddr = convertPcbAddr(toPcbAddr);

            Byte[] cmdData = new Byte[Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE + Pcb12Protocol.SIZE.CHANNEL_SIZE];

            cmdData[0] = Pcb12Protocol.COMMAND.ADDRESSING.CHANGE_SCALE_ID;
            cmdData[4] = (byte)(fromPcbAddr);
            cmdData[3] = (byte)(fromPcbAddr >> 8);
            cmdData[2] = (byte)(fromPcbAddr >> 16);
            cmdData[1] = (byte)(fromPcbAddr >> 24);
            cmdData[8] = (byte)(toPcbAddr);
            cmdData[7] = (byte)(toPcbAddr >> 8);
            cmdData[6] = (byte)(toPcbAddr >> 16);
            cmdData[5] = (byte)(toPcbAddr >> 24);

            return buildFrame(cmdData);
        }

        /// <summary>
        /// Protocol uses ascii characters for channel
        /// </summary>
        protected static int convertPcbAddr(int pcbAddr) {

            string channel = pcbAddr.ToString();

            while (channel.Length != 4) {
                channel = "0" + channel;
            }

            byte[] asciiBytes = Encoding.ASCII.GetBytes(channel);

            int convertedPcbAddr = asciiBytes[3] + (asciiBytes[2] << 8) + (asciiBytes[1] << 16) + (asciiBytes[0] << 24);

            return convertedPcbAddr;
        }

        /// <summary>
        /// Protocol uses ascii characters for pad ex pad 11 is identified as 'B'
        /// </summary>
        protected static int convertPadNumber(int padId) {

            string padNumber = ConversionTool.byteArrayToString(new byte[1] { (byte)padId });

            byte[] asciiBytes = Encoding.ASCII.GetBytes(padNumber);

            int convertedPadId = asciiBytes[1];

            return convertedPadId;
        }

        /// <summary>
        /// Protocol uses ascii characters for channel
        /// </summary>
        protected static long convertWeight(int currentWeight) {

            string weight = currentWeight.ToString();
            while (weight.Length != 5) {
                weight = "0" + weight;
            }
            byte[] asciiBytes = Encoding.ASCII.GetBytes(weight);
            long value1 = asciiBytes[4] + (asciiBytes[3] << 8) + (asciiBytes[2] << 16) + (asciiBytes[1] << 24) + ((long)asciiBytes[0] << 32);

            return value1;
        }

        /// <summary>
        /// Protocol uses ascii characters for channel
        /// </summary>
        protected static long convertCalibrationWeight(int currentWeight) {

            string weight = currentWeight.ToString();
            while (weight.Length != 4) {
                weight = "0" + weight;
            }
            byte[] asciiBytes = Encoding.ASCII.GetBytes(weight);
            long value1 = asciiBytes[3] + (asciiBytes[2] << 8) + (asciiBytes[1] << 16) + ('.' << 24) + ((long)asciiBytes[0] << 32);

            return value1;
        }

        /// <summary>
        /// build the frame according to the command
        /// </summary>
        protected static byte[] buildFrame(byte[] cmdData) {

            List<byte> list = new List<byte>();

            // frame start
            list.Add(Pcb12Protocol.FIXED.HEAD);

            // frame size
            int size = cmdData.Length + Pcb12Protocol.SIZE.CRC_SIZE + Pcb12Protocol.SIZE.END_SIZE;
            list.Add((byte)size);

            // cmd field + data field
            list.AddRange(cmdData);

            // add CRC
            byte crc = (byte)size;
            foreach (byte b in cmdData) {
                crc ^= b;
            }

            list.Add(crc);
            list.Add(Pcb12Protocol.FIXED.END);

            return list.ToArray();
        }
    }
}
