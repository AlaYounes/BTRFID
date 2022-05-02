using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.Windows.Threading;
using System.Threading;
using fr.nexess.hao.rfid.eventHandler;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace fr.nexess.hao.rfid.device.mti {

    public class Ru_865 : RfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(Ru_865));

        // singleton instance.
        private static Ru_865 instance = null;

        private const String type = "RU-865";

        private SCAN_MODE scanMode = SCAN_MODE.INVENTORY_SCAN;

        // iso Table Reader thread Handler
        private static Thread tagReadingThreadHandler = null;

        // critical section object locker
        private static readonly Object locker = new Object();

        private int RU_865_TAG_READING_TIME_INTERVAL = 250;
        private int TAG_SCAN_DURATION = 3000;

        // STA Threads management
        private static Dispatcher  dispatcher = null;

        unsafe private static IntPtr ru865Handler;

        private bool IsConnected { get; set; }
        private bool IsReading { get; set; }

        private static Dictionary<String,String> readerInfo = new Dictionary<string, string>();

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startReadingEventHandler;
        private event EventHandler                      stopReadingEventHandler;
        private event TagFoundEventHandler              tagFoundEventHandler;
        #endregion

        #region DLL_IMPORTATION
        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "openReader", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint openReader(ref IntPtr hReader);

        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "closeReader", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint closeReader(IntPtr hreader);

        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "getPower", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint getPower(IntPtr hreader, ref int rfpower);

        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "setPower", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint setPower(IntPtr hreader, int rfpower);

        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "firstTag", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint firstTag(IntPtr hreader, byte* pepc, ref int pepclen, ref int prssi);

        [DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "nextTag", CallingConvention = CallingConvention.StdCall)]
        unsafe private static extern uint nextTag(IntPtr hreader, byte* pepc, ref int pepclen, ref int prssi);

        // not used...
        //[DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "selectTag", CallingConvention = CallingConvention.StdCall)]
        //unsafe private static extern uint selectTag(IntPtr hreader, byte* pepc, int epclen);

        //[DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "readTag", CallingConvention = CallingConvention.StdCall)]
        //unsafe private static extern uint readTag(IntPtr hreader, int bank, int address, byte* ppasscode, int passcodelen, ushort* pdata, int datalen);

        //[DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "writeTag", CallingConvention = CallingConvention.StdCall)]
        //unsafe private static extern uint writeTag(IntPtr hreader, int bank, int address, byte* ppasscode, int passcodelen, ushort* pdata, int datalen);

        //[DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "killTag", CallingConvention = CallingConvention.StdCall)]
        //unsafe private static extern uint killTag(IntPtr hreader, byte* ppasscode, int passcodelen);

        //[DllImport("TbUHFRFIDWrapper.dll", EntryPoint = "lockTag", CallingConvention = CallingConvention.StdCall)]
        //unsafe private static extern uint lockTag(IntPtr hreader, int action, int bank, byte* ppasscode, int passcodelen);
        #endregion

        #region CONSTRUCTOR
        protected Ru_865() {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            getDefaultValuesFromConfiguration();

            logProducer.Logger.Debug("class loaded");
        }

        public static Ru_865 getInstance() {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new Ru_865();
                }
            }

            return instance;
        }
        #endregion

        #region RFID_DEVICE_EVENT_PROVIDER_IMPL
        public event HasReportedAnErrorEventHandler HasReportedAnError {
            add {
                lock (locker) {
                    hasReportedAnErrorEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    hasReportedAnErrorEventHandler -= value;
                }
            }
        }
        public event EventHandler Connected {
            add {
                lock (locker) {
                    connectedEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    connectedEventHandler -= value;
                }
            }
        }
        public event EventHandler Disconnected {
            add {
                lock (locker) {
                    disconnectedEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    disconnectedEventHandler -= value;
                }
            }
        }
        public event EventHandler StartReading {
            add {
                lock (locker) {
                    startReadingEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    startReadingEventHandler -= value;
                }
            }
        }
        public event EventHandler StopReading {
            add {
                lock (locker) {
                    stopReadingEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    stopReadingEventHandler -= value;
                }
            }
        }
        public event TagFoundEventHandler TagFound {
            add {
                lock (locker) {
                    tagFoundEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    tagFoundEventHandler -= value;
                }
            }
        }
        #endregion

        #region PUBLIC_METHODS

        public SCAN_MODE ScanMode {
            get { return scanMode; }
            set { scanMode = value; }
        }

        public int ScanDuration {
            get { return TAG_SCAN_DURATION; }
            set { TAG_SCAN_DURATION = value; }
        }

        public IDictionary<String, String> ReaderInfo {
            get {
                return readerInfo;
            }
        }

        public Dispatcher Dispatcher {
            get {
                return dispatcher;
            }
            set {
                dispatcher = value;
            }
        }

        public void connect() {

            if (!IsConnected) {

                RU_RESULT res = Ru_865.unsafeOpenReader();

                if (res == RU_RESULT.UR_OK) {

                    IsConnected = true;

                    raiseEvent(connectedEventHandler);

                } else {
                    raiseHasReportedAnErrorEvent("unable to connect RU-865 : " + res.ToString());
                }
            }
        }

        public void disconnect() {

            if (IsConnected) {

                if (IsReading) {
                    stopScan();
                }

                RU_RESULT res = Ru_865.unsafeCloseReader();

                if (res == RU_RESULT.UR_OK) {

                    IsConnected = false;

                    raiseEvent(disconnectedEventHandler);

                } else {
                    raiseHasReportedAnErrorEvent("unable to disconnect RU-865 : " + res.ToString());
                }
            }
        }

        /// <summary>
        /// start Scan as continuous scanning. no event from this task unless when tags are read
        /// </summary>
        public void startScan() {

            if (IsConnected && !IsReading) {

                initializeTagReadingThread();

                if (tagReadingThreadHandler != null && !tagReadingThreadHandler.IsAlive) {

                    tagReadingThreadHandler.Start();

                    IsReading = true;
                    raiseEvent(startReadingEventHandler);
                }
            }
        }

        public void stopScan() {

            if (IsConnected && IsReading) {

                IsReading = false;
                raiseEvent(stopReadingEventHandler);

                if (tagReadingThreadHandler != null) {

                    tagReadingThreadHandler.Abort();
                    tagReadingThreadHandler = null;
                }
            }
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, "", getReaderState());
            return readerInfo.InfoList;
        }

        public RfidDeviceState getReaderState() {

            RfidDeviceState state = RfidDeviceState.DISCONNECTED;

            if (IsReading) {

                state = RfidDeviceState.READING;
            } else if (IsConnected) {

                state = RfidDeviceState.CONNECTED;
            } else {

                state = RfidDeviceState.DISCONNECTED;
            }

            return state;
        }

        public string getReaderComPort() {
            return "";
        }

        public string getReaderUUID() {
            throw new NotImplementedException();
        }

        public String getReaderSwVersion()
        {
            throw new NotImplementedException();
        }

        public bool transferCfgFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public float getPower(int antenna) {
            float value = 0;

            if (IsConnected) {
                int power;

                RU_RESULT res = unsafeGetPower(out power);

                if (res == RU_RESULT.UR_OK) {

                    value = (float)power;

                } else {

                    raiseHasReportedAnErrorEvent("Unable to get Power : " + res.ToString());
                }
            }

            return value;
        }

        public void setPower(int antenna, float power) {

            if (IsConnected) {

                RU_RESULT res = unsafeSetPower((int)power);

                if (res != RU_RESULT.UR_OK) {

                    raiseHasReportedAnErrorEvent("Unable to set Power : " + res.ToString() + " info " + power.ToString() + " range power has be between 10 and 27 dbm");
                }
            }
        }

        public void switchOffAntenna()
        {

            throw new NotImplementedException();
        }

        public void switchOnAntenna()
        {

            throw new NotImplementedException();
        }

        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates() {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "RU-865");

            // TODO

            componentHealthStates.Add("State", "disconnected");

            return componentHealthStates;
        }

        #endregion
        #endregion

        #region PRIVATE_METHODS

        private void initializeTagReadingThread() {

            if (tagReadingThreadHandler != null) {

                tagReadingThreadHandler.Abort();
                tagReadingThreadHandler = null;
            }

            tagReadingThreadHandler = new Thread(new ThreadStart(tagReadingThread));
            tagReadingThreadHandler.SetApartmentState(ApartmentState.STA);

            tagReadingThreadHandler.IsBackground = true;
        }

        private void getDefaultValuesFromConfiguration() {

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["RU_865_TAG_READING_TIME_INTERVAL"])) {

                String interval = ConfigurationManager.AppSettings["RU_865_TAG_READING_TIME_INTERVAL"];

                if (!String.IsNullOrEmpty(interval)) {
                    try {

                        int ms = int.Parse(interval);

                        if (ms > 0) {
                            RU_865_TAG_READING_TIME_INTERVAL = ms;
                        }

                    } catch (Exception) {
                        // continue
                    }
                }
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["TAG_SCAN_DURATION"])) {

                String duration = ConfigurationManager.AppSettings["TAG_SCAN_DURATION"];

                if (!String.IsNullOrEmpty(duration)) {
                    try {

                        int ms = int.Parse(duration);

                        if (ms > 0) {

                            ScanDuration = ms;
                        }

                    } catch (Exception) {
                        // continue
                    }
                }
            }
        }

        protected void raiseEvent(EventHandler eventhandler) {

            if (eventhandler != null) {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
            }
        }

        protected void raiseHasReportedAnErrorEvent(String issue) {

            if (hasReportedAnErrorEventHandler != null) {

                Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(issue))));
            }
        }
        protected void raiseTagFoundEvent(List<String> snrs) {

            if (tagFoundEventHandler != null) {

                Dispatcher.BeginInvoke((Action)(() => tagFoundEventHandler(this, new TagFoundEventArgs(snrs))));
            }
        }

        unsafe private static uint unsafeOpenReader() {

            uint res = openReader(ref ru865Handler);

            return res;
        }

        unsafe private static uint unsafeCloseReader() {

            uint res = closeReader(ru865Handler);

            return res;
        }

        unsafe private static uint unsafeGetPower(out int power) {

            power = -1;

            uint res = getPower(ru865Handler, ref power);

            return res;
        }

        unsafe static private uint unsafeSetPower(int power) {

            uint res = RU_RESULT.UR_ERR_INVALID_PARAMETER.Nbr;

            if (10 <= power && power <= 27) { // check range of values...

                res = setPower(ru865Handler, power);
            }

            return res;
        }

        unsafe static private uint unsafeStartTagInventory(out String snr) {

            snr = "";

            uint res = RU_RESULT.UR_ERR_INVALID_PARAMETER.Nbr;

            int pc = 2;
            int pepclen = 12 + pc; // PC (2bytes) + EPC
            int prssi = -1;

            byte [] buffer = new byte[pepclen];

            // The following fixed statement pins the location of the source and
            // target objects in memory so that they will not be moved by garbage
            // collection.
            fixed (byte* pepc = buffer) {

                res = firstTag(ru865Handler, pepc, ref pepclen, ref prssi);

                if (res == RU_RESULT.UR_OK.Nbr && pepclen > 0) {

                    snr = toolbox.ConversionTool.byteArrayToString(buffer);

                    // remove PC information from epc serial number
                    snr = snr.Remove(0, pc * 2);
                }
            }

            return res;
        }

        unsafe static private uint unsafeReadNextTag(out String snr) {

            snr = "";

            uint res = RU_RESULT.UR_ERR_INVALID_PARAMETER.Nbr;
            int pc = 2;
            int pepclen = 12 + pc; // PC (2bytes) + EPC
            int prssi = -1;

            byte [] buffer = new byte[pepclen];

            // The following fixed statement pins the location of the source and
            // target objects in memory so that they will not be moved by garbage
            // collection.
            fixed (byte* pepc = buffer) {

                res = nextTag(ru865Handler, pepc, ref pepclen, ref prssi);

                if (res == RU_RESULT.UR_OK.Nbr && pepclen > 0) {

                    snr = toolbox.ConversionTool.byteArrayToString(buffer);

                    // remove PC information from epc serial number
                    snr = snr.Remove(0, pc * 2);
                }
            }

            return res;
        }
        #endregion

        #region THREAD

        /// <summary>
        /// tag Reading Thread : start inventory and read tag from Ru-865.
        /// </summary>
        protected void tagReadingThread() {

            switch (ScanMode) {

                case SCAN_MODE.CONTINUOUS_SCAN:

                    performContinuousScan();
                    break;

                case SCAN_MODE.TRIGGERED_SCAN:

                    performTriggeredScan();
                    break;

                case SCAN_MODE.INVENTORY_SCAN:
                default:

                    performInventoryScan(ScanDuration);
                    break;
            }
        }

        private void performContinuousScan() {

            while (true) {

                List<String> snrs = new List<String>();

                String snr;
                RU_RESULT res = unsafeStartTagInventory(out snr);

                if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                    raiseHasReportedAnErrorEvent("Unable to start tag inventory : " + res.ToString());
                }

                if (!String.IsNullOrEmpty(snr)) {

                    snrs.Add(snr);
                    snr = "";
                }

                do {

                    res = unsafeReadNextTag(out snr);

                    if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                        raiseHasReportedAnErrorEvent("Unable to read next tag : " + res.ToString());
                    }

                    if (!String.IsNullOrEmpty(snr)) {

                        snrs.Add(snr);
                        snr = "";
                    }

                    if (snrs.Count > 0) {

                        raiseTagFoundEvent(snrs);
                    }

                } while (res == RU_RESULT.UR_OK);

                // rest for a moment...
                System.Threading.Thread.Sleep(RU_865_TAG_READING_TIME_INTERVAL);
            }
        }

        private void performTriggeredScan() {
            List<String> snrs = new List<String>();

            while (true) {

                Boolean gotTag = false;

                String snr;
                RU_RESULT res = unsafeStartTagInventory(out snr);

                if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                    raiseHasReportedAnErrorEvent("Unable to start tag inventory : " + res.ToString());
                }

                if (!String.IsNullOrEmpty(snr)) {

                    snrs.Add(snr);
                    snr = "";

                    gotTag = true;
                }

                do {

                    res = unsafeReadNextTag(out snr);

                    if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                        raiseHasReportedAnErrorEvent("Unable to read next tag : " + res.ToString());
                    }

                    if (!String.IsNullOrEmpty(snr)) {

                        snrs.Add(snr);
                        snr = "";

                        gotTag = true;
                    }
                } while (res == RU_RESULT.UR_OK && gotTag == false);

                if (gotTag) {

                    raiseTagFoundEvent(new List<String>() { snrs.First() });

                    stopScan();

                    break;
                }

                // rest for a moment...
                System.Threading.Thread.Sleep(RU_865_TAG_READING_TIME_INTERVAL);
            }

        }

        private void performInventoryScan(int duration) {

            List<String> snrs = new List<String>();

            TimeSpan interval = TimeSpan.FromMilliseconds(duration);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < interval) {

                String snr;
                RU_RESULT res = unsafeStartTagInventory(out snr);

                if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                    raiseHasReportedAnErrorEvent("Unable to start tag inventory : " + res.ToString());
                }

                if (!String.IsNullOrEmpty(snr)) {

                    snrs.Add(snr);
                    snr = "";
                }

                do {

                    res = unsafeReadNextTag(out snr);

                    if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

                        raiseHasReportedAnErrorEvent("Unable to read next tag : " + res.ToString());
                    }

                    if (!String.IsNullOrEmpty(snr)) {

                        snrs.Add(snr);
                        snr = "";
                    }
                } while (res == RU_RESULT.UR_OK);

                // rest for a moment...
                System.Threading.Thread.Sleep(RU_865_TAG_READING_TIME_INTERVAL);
            }

            stopwatch.Stop();

            raiseTagFoundEvent(snrs.Distinct().ToList());

            stopScan();
        }

        #endregion
    }

    public class RU_RESULT {
        protected readonly uint nbr;
        protected readonly String desc = "";

        public static readonly RU_RESULT UR_OK = new RU_RESULT(0x00000000, "UR_OK");
        public static readonly RU_RESULT UR_ERR_UNCLASSIFIED = new RU_RESULT(0x00000001, "UR_ERR_UNCLASSIFIED");
        public static readonly RU_RESULT UR_ERR_INVALID_HANDLE = new RU_RESULT(0x00000002, "UR_ERR_INVALID_HANDLE");
        public static readonly RU_RESULT UR_ERR_INVALID_PARAMETER = new RU_RESULT(0x00000003, "UR_ERR_INVALID_PARAMETER");
        public static readonly RU_RESULT UR_ERR_NO_TAGS = new RU_RESULT(0x00000004, "UR_ERR_NO_TAGS");
        public static readonly RU_RESULT UR_ERR_COMMAND_SEND = new RU_RESULT(0x00000005, "UR_ERR_COMMAND_SEND");
        public static readonly RU_RESULT UR_ERR_COMMAND_RECEIVE = new RU_RESULT(0x00000006, "UR_ERR_COMMAND_RECEIVE");
        public static readonly RU_RESULT UR_ERR_BUFFERTOOSMALL = new RU_RESULT(0x00000007, "UR_ERR_BUFFERTOOSMALL");
        public static readonly RU_RESULT UR_ERR_BUFFERTOOBIG = new RU_RESULT(0x00000008, "UR_ERR_BUFFERTOOBIG");
        public static readonly RU_RESULT UR_ERR_DEVICENOTFOUND = new RU_RESULT(0x00000009, "UR_ERR_DEVICENOTFOUND");
        public static readonly RU_RESULT UR_ERR_SYSTEM = new RU_RESULT(0x0000000A, "UR_ERR_SYSTEM");
        public static readonly RU_RESULT UR_ERR_REQ_NOT_ALLOWED = new RU_RESULT(0x00010001, "UR_ERR_REQ_NOT_ALLOWED");
        public static readonly RU_RESULT UR_ERR_ACCESS = new RU_RESULT(0x00010002, "UR_ERR_ACCESS");
        public static readonly RU_RESULT UR_ERR_KILL = new RU_RESULT(0x00010003, "UR_ERR_KILL");
        public static readonly RU_RESULT UR_ERR_NOREPLY = new RU_RESULT(0x00010004, "UR_ERR_NOREPLY");
        public static readonly RU_RESULT UR_ERR_LOCK = new RU_RESULT(0x00010005, "UR_ERR_LOCK");
        public static readonly RU_RESULT UR_ERR_WRITE = new RU_RESULT(0x00010006, "UR_ERR_WRITE");
        public static readonly RU_RESULT UR_ERR_ERASE = new RU_RESULT(0x00010007, "UR_ERR_ERASE");
        public static readonly RU_RESULT UR_ERR_READ = new RU_RESULT(0x00010008, "UR_ERR_READ");
        public static readonly RU_RESULT UR_ERR_SELECT = new RU_RESULT(0x00010009, "UR_ERR_SELECT");
        public static readonly RU_RESULT UR_ERR_CHANNEL_TIMEOUT = new RU_RESULT(0x0001000A, "UR_ERR_CHANNEL_TIMEOUT");
        public static readonly RU_RESULT UR_ERR_INVALID_PARAMETER2 = new RU_RESULT(0x0001000F, "UR_ERR_INVALID_PARAMETER2");
        public static readonly RU_RESULT UR_ERR_EASCODE = new RU_RESULT(0x00010020, "UR_ERR_EASCODE");
        public static readonly RU_RESULT UR_ERR_OTHER = new RU_RESULT(0x00010080, "UR_ERR_OTHER");
        public static readonly RU_RESULT UR_ERR_MEMORY_OVERRUN = new RU_RESULT(0x00010083, "UR_ERR_MEMORY_OVERRUN");
        public static readonly RU_RESULT UR_ERR_MEMORY_LOCKED = new RU_RESULT(0x00010084, "UR_ERR_MEMORY_LOCKED");
        public static readonly RU_RESULT UR_ERR_INSUFFICIENT_POWER = new RU_RESULT(0x0001008B, "UR_ERR_INSUFFICIENT_POWER");
        public static readonly RU_RESULT UR_ERR_NONSPECIFIC = new RU_RESULT(0x0001008F, "UR_ERR_NONSPECIFIC");
        public static readonly RU_RESULT UR_ERR_READONLY_ADDRESS = new RU_RESULT(0x000100A0, "UR_ERR_READONLY_ADDRESS");
        public static readonly RU_RESULT UR_ERR_PCIE_RADIO_DISABLED = new RU_RESULT(0x000100B0, "UR_ERR_PCIE_RADIO_DISABLED");
        public static readonly RU_RESULT UR_ERR_SECURITY_FAILURE = new RU_RESULT(0x000100FE, "UR_ERR_SECURITY_FAILURE");
        public static readonly RU_RESULT UR_ERR_HW_FAILURE = new RU_RESULT(0x000100FF, "UR_ERR_HW_FAILURE");

        protected RU_RESULT(uint nbr, String desc) {
            this.nbr = nbr;
            this.desc = desc;
        }

        public static implicit operator RU_RESULT(uint nbr) {
            RU_RESULT toReturn = UR_ERR_UNCLASSIFIED;

            foreach (RU_RESULT result in ToArray()) {
                if (result.nbr == nbr) {
                    toReturn = result;
                }
            }

            return toReturn;
        }

        public static Boolean operator ==(RU_RESULT resultA, RU_RESULT resultB) {
            if (resultA.nbr == resultB.nbr) {
                return true;
            } else {
                return false;
            }
        }

        public static Boolean operator !=(RU_RESULT resultA, RU_RESULT resultB) {
            if (resultA.nbr != resultB.nbr) {
                return true;
            } else {
                return false;
            }
        }

        public uint Nbr {
            get {
                return this.nbr;
            }
        }

        public String Desc {
            get {
                return this.desc;
            }
        }

        public static RU_RESULT[] ToArray() {
            return new RU_RESULT[] { UR_OK,
                                            UR_ERR_UNCLASSIFIED,
                                            UR_ERR_INVALID_HANDLE,
                                            UR_ERR_INVALID_PARAMETER,
                                            UR_ERR_NO_TAGS,
                                            UR_ERR_COMMAND_SEND,
                                            UR_ERR_COMMAND_RECEIVE,
                                            UR_ERR_BUFFERTOOSMALL,
                                            UR_ERR_BUFFERTOOBIG,
                                            UR_ERR_DEVICENOTFOUND,
                                            UR_ERR_SYSTEM,
                                            UR_ERR_REQ_NOT_ALLOWED,
                                            UR_ERR_ACCESS,
                                            UR_ERR_KILL,
                                            UR_ERR_NOREPLY,
                                            UR_ERR_LOCK,
                                            UR_ERR_WRITE,
                                            UR_ERR_ERASE,
                                            UR_ERR_READ,
                                            UR_ERR_SELECT,
                                            UR_ERR_CHANNEL_TIMEOUT,
                                            UR_ERR_INVALID_PARAMETER2,
                                            UR_ERR_EASCODE,
                                            UR_ERR_OTHER,
                                            UR_ERR_MEMORY_OVERRUN ,
                                            UR_ERR_MEMORY_LOCKED,
                                            UR_ERR_INSUFFICIENT_POWER,
                                            UR_ERR_NONSPECIFIC,
                                            UR_ERR_READONLY_ADDRESS,
                                            UR_ERR_PCIE_RADIO_DISABLED,
                                            UR_ERR_SECURITY_FAILURE,
                                            UR_ERR_HW_FAILURE};
        }

        public new String ToString() {
            return this.desc;
        }
    }

    public enum SCAN_MODE {
        CONTINUOUS_SCAN,
        TRIGGERED_SCAN,
        INVENTORY_SCAN
    }
}
