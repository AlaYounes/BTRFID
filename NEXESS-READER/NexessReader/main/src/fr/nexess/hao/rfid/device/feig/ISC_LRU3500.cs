using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Windows.Threading;

using OBID;

using fr.nexess.toolbox.log;
using fr.nexess.hao.rfid;
using fr.nexess.hao.rfid.eventHandler;
using System.IO.Ports;
using System.Globalization;

namespace fr.nexess.hao.rfid.device.feig {

    /// <summary>
    /// This class is used to abstract i-scan reader (LRU 3500) communication services.
    /// </summary>
    /// <version>$Revision: 163 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2016-08-10 11:19:14 +0200 (mer., 10 août 2016) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class ISC_LRU3500 : RfidDevice, FeUsbListener, ConfigurableRfidDevice, ILru3x00, SustainableRfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(ISC_LRU3500));

        private const   String DEVICE_ID                = "Device-ID";
        private String  READER_NAME                     = "ID ISC.LRU3500";
        private  int    NB_TAG_FOR_EACH_NOTIFICATION    = 1000; // max value
        private  int    SCAN_DURATION                   = 10000; // 10 sec, default value
        public  string LRU_CONNECTION_TYPE = "USB";

        private long connectedDeviceIdentifier = 0L;

        // STA Threads management
        private static Dispatcher  dispatcher = null;

        // several useful OBID reader driving commands
        const int FEISC_CLEAR_BUFFER_CMD    = 0x32;
        const int FEISC_INIT_BUFFER_CMD     = 0x33;
        const int FEISC_READ_BUFFER_CMD     = 0x22; // BUFFERED READ MODE
        const int FEISC_RF_ON_OFF_CMD       = 0x6A;

        const int FEISC_CHECK_ANTENNAS_CMD  = 0x76;

        const int MIN_POWER = 0x11;
        const int MAX_POWER = 0x37;

        private bool muxEnable = false;

        // singleton instance.
        protected static ISC_LRU3500 instance = null;

        // critical section object locker
        protected static readonly Object locker = new Object();
        protected static readonly Object readerLocker = new Object();

        // ISC_LRU3500's reader instance.
        private static FedmIscReader reader = null;

        // current communication port of the reader (serial com only)
        protected int comPort = -1;
        private String type = "LRU3500";

        // reader state.
        private RfidDeviceState state = RfidDeviceState.DISCONNECTED;

        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startReadingEventHandler;
        private event EventHandler                      stopReadingEventHandler;
        private event TagFoundEventHandler              tagFoundEventHandler;
        public event TagFoundObidEventHandler           tagFoundDetailedEventHandler;

        List<FedmBrmTableItem> tagReportList = new List<FedmBrmTableItem>();

        private object objectLock = new Object();
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// ISC_LRU3500 get instance (singleton).
        /// </summary>
        /// <returns>ISC_LRU3500 instance</returns>
        public static ISC_LRU3500 getInstance() {
            // critical section
            lock (locker) {
                if (instance == null) {
                    instance = new ISC_LRU3500();
                }
            }

            return instance;
        }

        /// <summary>
        /// protected default constructor
        /// </summary>
        protected ISC_LRU3500() {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            setDefaultParameters();

            initializeReader();

            // Registry from usb Events
            if (reader != null) {
                reader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_CONNECT_EVENT);
                reader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_DISCONNECT_EVENT);
            }

            logProducer.Logger.Debug("class loaded");
        }

        public ISC_LRU3500(int nbTagsByNotification) {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            parameters.Add(ConfigurableReaderParameter.NB_MAX_TAGS.ToString(), nbTagsByNotification);

            initializeReader();

            // Registry from usb Events
            if (reader != null) {
                reader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_CONNECT_EVENT);
                reader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_DISCONNECT_EVENT);
            }

            logProducer.Logger.Debug("class loaded");
        }

        /** destructor*/
        ~ISC_LRU3500() {

            if (reader != null) {

                // Unregistry from usb Events
                reader.RemoveEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_CONNECT_EVENT);
                reader.RemoveEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_DISCONNECT_EVENT);

                // communication disconnection
                reader.Dispose();

                reader = null;

                instance = null;
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public int ComPort {
            get {
                return this.comPort;
            }
            set {
                this.comPort = value;
            }
        }
        #endregion

        #region RFID_READER_EVENT_PROVIDER_IMPL
        public event HasReportedAnErrorEventHandler HasReportedAnError {
            add {
                lock (objectLock) {
                    hasReportedAnErrorEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    hasReportedAnErrorEventHandler -= value;
                }
            }
        }
        public event EventHandler Connected {
            add {
                lock (objectLock) {
                    connectedEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    connectedEventHandler -= value;
                }
            }
        }
        public event EventHandler Disconnected {
            add {
                lock (objectLock) {
                    disconnectedEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    disconnectedEventHandler -= value;
                }
            }
        }
        public event EventHandler StartReading {
            add {
                lock (objectLock) {
                    startReadingEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    startReadingEventHandler -= value;
                }
            }
        }
        public event EventHandler StopReading {
            add {
                lock (objectLock) {
                    stopReadingEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    stopReadingEventHandler -= value;
                }
            }
        }
        public event TagFoundEventHandler TagFound {
            add {
                lock (objectLock) {
                    tagFoundEventHandler += value;
                }
            }
            remove {
                lock (objectLock) {
                    tagFoundEventHandler -= value;
                }
            }
        }
        event TagFoundObidEventHandler TagFoundDetailed
        {
            add
            {
                lock (locker)
                {
                    tagFoundDetailedEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    tagFoundDetailedEventHandler -= value;
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS
        /// <summary>
        /// thread Dispatcher getter
        /// </summary>
        private static Dispatcher Dispatcher {
            get {
                return dispatcher;
            }
            set {
                dispatcher = value;
            }
        }

        private void setDefaultParameters() {

            // extract data from configuration file
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["ISC_LRU_NAME"])) {
                READER_NAME = ConfigurationManager.AppSettings["ISC_LRU_NAME"];
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["NB_TAG_FOR_EACH_NOTIFICATION"])) {
                try {
                    int nbTagFromConfig = int.Parse(ConfigurationManager.AppSettings["NB_TAG_FOR_EACH_NOTIFICATION"]);
                    if (nbTagFromConfig < NB_TAG_FOR_EACH_NOTIFICATION) {
                        NB_TAG_FOR_EACH_NOTIFICATION = nbTagFromConfig;
                    }
                } catch (Exception) { }
            }

            // get duration from configuration each time we an inventory is required
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["TAG_SCAN_DURATION"])) {
                try {
                    SCAN_DURATION = int.Parse(ConfigurationManager.AppSettings["TAG_SCAN_DURATION"]);
                } catch (Exception) { }
            }

            // get duration from configuration each time we an inventory is required
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["LRU_CONNECTION_TYPE"])) {
                try {
                    String connectionType = ConfigurationManager.AppSettings["LRU_CONNECTION_TYPE"];
                    LRU_CONNECTION_TYPE = connectionType;

                } catch (Exception) { }
            }

            parameters.Add(ConfigurableReaderParameter.READER_NAME.ToString(), READER_NAME);
            parameters.Add(ConfigurableReaderParameter.NB_MAX_TAGS.ToString(), NB_TAG_FOR_EACH_NOTIFICATION);
            parameters.Add(ConfigurableReaderParameter.TAG_SCAN_DURATION.ToString(), SCAN_DURATION);
            parameters.Add(ConfigurableReaderParameter.CONNECTION_TYPE.ToString(), LRU_CONNECTION_TYPE);
        }

        private void initializeReader() {
            // let's initialize tag reader
            try {
                logProducer.Logger.Info("Initializing LRU reader");
                if (reader == null) {

                    // instanciate reader
                    reader = new FedmIscReader();
                }
                string key = ConfigurableReaderParameter.NB_MAX_TAGS.ToString();
                int nbTagsByNotif = (int)parameters[key];
                // the size is selected equal to the maximum number of transponders located in the antenna field at the same time
                reader.SetTableSize(FedmIscReaderConst.BRM_TABLE,
                                    nbTagsByNotif);// max table size = 1000

            } catch (Exception ex) {
                // FedmException || FeReaderDriverException ...
                // a problem occurs, let's notify listeners
                if (hasReportedAnErrorEventHandler != null) {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }
            }
        }

        private Boolean openSerialComAndNotify(String readerName) {
            Boolean connected = false;

            if (reader != null
                && !reader.Connected) {
                logProducer.Logger.Info("Opening Serial COm for LRU reader");
                try {

                    // seek Serial Com port for the first time
                    if (ComPort < 0) {

                        ComPort = seekSerialPortCom(readerName);
                        logProducer.Logger.Info("com port found, " + ComPort);
                    }

                    if (ComPort <= 0) {

                        // ... no, notify all listeners!
                        if (hasReportedAnErrorEventHandler != null) {

                            hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to connect, no OBID I-Scan LRU3500 connected to the host system"));
                        }

                        logProducer.Logger.Info("unable to connect, no OBID OBID I-Scan LRU3500 connected");
                    }

                    if (reader.Connected) {
                        lock (readerLocker) {
                            // set the default baud rate
                            reader.SetPortPara("Baud", "38400");
                            Thread.Sleep(300);
                            reader.SetPortPara("Frame", "8E1");
                            Thread.Sleep(300);
                            //reader.SetPortPara("TxTimeControl", "0");
                            reader.SetPortPara("TxTimeControl", "1");
                            Thread.Sleep(300);
                            reader.SetPortPara("TxDelayTime", "50");
                            Thread.Sleep(300);
                            reader.SetPortPara("Timeout", "5000");
                            Thread.Sleep(300);
                            logProducer.Logger.Info("LRU reader connected");
                            state = RfidDeviceState.CONNECTED;

                            connected = true;

                            // notify all listeners
                            if (connectedEventHandler != null) {

                                connectedEventHandler(this, EventArgs.Empty);
                            }

                            reader.ReadCompleteConfiguration(false);
                        }
                        logProducer.Logger.Debug("OBID I-Scan LRU3500 reader CONNECTED on port : " + ComPort.ToString());

                    } else {

                        if (hasReportedAnErrorEventHandler != null) {

                            hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to connect"));
                        }

                        logProducer.Logger.Info("unable to connect");
                    }
                } catch (Exception ex) {

                    // FePortDriverException || FedmException || FeReaderDriverException
                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to establish a connection to the rfid reader due to : " + ex.Message));
                    }

                    logProducer.Logger.Info("unable to establish a connection to the rfid reader due to : " + ex.Message);
                }
            }

            return connected;
        }

        /// <summary>
        /// seek serial port com where the badge reader is connected
        /// </summary>
        /// <returns>com port number</returns>
        protected int seekSerialPortCom(String readerName) {

            int iResult = -1;

            // let's retrieve the whole com port list
            List<int> portIds = new List<int>();

            try {
                // get all com port names
                String[] portNames = SerialPort.GetPortNames();

                // portNames = portNames.OrderBy(q => q).ToList(); // TODO turn on to blow com port

                // extract com ids
                foreach (String portname in portNames) {

                    String strPortId = portname.Remove(0, "COM".Length);

                    try {

                        portIds.Add(Int16.Parse(strPortId));

                    } catch (Exception) {

                        continue;
                    }
                }
            } catch (Exception ex) {

                logProducer.Logger.Error("Unable to seek Com Port, because : " + ex.Message);
                return iResult;
            }

            // try to connect to reader and retrieve information
            foreach (int portId in portIds) {

                try {

                    // try connection
                    logProducer.Logger.Debug("try connection with port id : " + portId.ToString());

                    reader.ConnectCOMM(portId,  // port com
                                       true);  // TODO FINDBAUDRATE & Co

                    // get Device Info
                    reader.ReadReaderInfo();

                    if (reader.GetReaderName().CompareTo(readerName) != 0) {

                        // not the expected device, disconnect !
                        logProducer.Logger.Info("not the expected device, skip and loop for another one : " + reader.GetReaderName());

                        if (reader.Connected) {
                            reader.DisConnect();
                        }

                        continue;
                    }

                    // GOT IT ! return...
                    iResult = portId;
                    break;

                } catch (Exception ex) {
                    logProducer.Logger.Debug("ConnectCOMM exception : " + ex.Message);
                    if (reader.Connected) {
                        reader.DisConnect();
                    }
                    continue;
                }
            }

            return iResult;
        }

        /// <summary>
        /// Open reader with specified name in USB mode.
        /// </summary>
        /// <param name="readerName">the reader name</param>
        /// <returns>true for usb com acknowledgment</returns>
        private Boolean openUsbComAndNotify(string readerName) {
            Boolean connected = false;
            logProducer.Logger.Info("Opening USB COM for LRU reader");
            // usb connection
            FeUsb usbhandler = new FeUsb();

            // reset usb devices list
            usbhandler.ClearScanList();

            //Scan all USB devices connected to host
            try {

                usbhandler.Scan(OBID.FeUsbScanSearch.SCAN_ALL, null);

            } catch (Exception ex) {

                // a problem occurs, let's notify listeners
                if (hasReportedAnErrorEventHandler != null) {

                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Exception when scanning ports, " + ex.Message));
                }

                // nop, let's get out from there!
                return connected;
            }


            // is there one or more usb devices?
            if (usbhandler.ScanListSize < 1) {

                // nop, let's get out from there!
                return connected;
            }

            // yep ! retrieve the "device id" for each usb device
            for (int i = 0; i < usbhandler.ScanListSize; i++) {

                try {

                    // get device id
                    string deviceId = usbhandler.GetScanListPara(i, DEVICE_ID);

                    // connect usb (deviceId do have an even number of characters for hex conversion)
                    if (deviceId.Length % 2 != 0) {
                        deviceId = "0" + deviceId;
                    }

                    // does the reader already connected?
                    if (reader.Connected) {

                        // get Device Info
                        reader.ReadReaderInfo();

                        if (reader.GetReaderName().CompareTo(readerName) != 0) {

                            // not the expected device, disconnect !
                            logProducer.Logger.Info("not the expected device, skip and loop for another one : " + reader.GetReaderName());

                            continue;

                        } else {

                            // yep, run away from there
                            break;
                        }
                    }

                    long  deviceIdentifier = FeHexConvert.HexStringToLong(deviceId);

                    for (int k = 1; k <= 3; k++) //Try to connect 3 times
                    {
                        logProducer.Logger.Info("Try connecting to LRU");
                        if (k <= 2) {
                            try {
                                reader.ConnectUSB((int)deviceIdentifier);
                                break;
                            } catch (Exception ex) {
                                logProducer.Logger.Error("Failure connecting to LRU, " + ex.Message);
                            }
                        } else {
                            reader.ConnectUSB((int)deviceIdentifier);
                        }
                    }

                    // get Device Info
                    reader.ReadReaderInfo();

                    // is sthe correct named reader?
                    if (reader.GetReaderName().CompareTo(readerName) != 0) {

                        // not the expected device, disconnect !
                        logProducer.Logger.Info("not the expected device, skip and loop for another one : " + reader.GetReaderName());

                        reader.DisConnect();

                        continue;

                    } else {

                        connectedDeviceIdentifier = deviceIdentifier;

                        // we got it !! let's fire event and notify all listeners
                        if (connectedEventHandler != null) {
                            connectedEventHandler(this, EventArgs.Empty);
                        }
                        logProducer.Logger.Info("LRU reader connected");
                        state = RfidDeviceState.CONNECTED;

                        connected = true;

                        reader.ReadCompleteConfiguration(false);

                        // quit loop
                        break;
                    }
                } catch (Exception ex) {

                    // a problem occurs, let's notify listeners
                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Exception when getting reader info, " + ex.Message));
                    }

                    // break function
                    break;
                }
            }// end for

            return connected;
        }

        /// <summary>
        /// power off antenna
        /// </summary>
        public void switchOffAntenna() {

            if (reader != null
                || reader.Connected
                || this.state != RfidDeviceState.READING) {

                try {
                    reader.SetData(OBID.ReaderCommand._0x6A.Req.RF_OUTPUT, 0x00);
                    reader.SendProtocol(FEISC_RF_ON_OFF_CMD);
                } catch (Exception ex) {
                    logProducer.Logger.Debug("can't switch Off Antenna because : " + ex.Message);
                }
            }
        }

        public void switchOnAntenna() {

            if (reader != null
                || reader.Connected
                || this.state != RfidDeviceState.READING) {

                try {
                    reader.SetData(OBID.ReaderCommand._0x6A.Req.RF_OUTPUT, 0x01);
                    reader.SendProtocol(FEISC_RF_ON_OFF_CMD);
                } catch (Exception ex) {
                    logProducer.Logger.Debug("can't switch Off Antenna because : " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Launch inventory using buffered read mode with a specified duration.
        /// </summary>
        /// <param name="duration">how many time scan applies</param>
        /// <returns>list of all scanned tag's serial numbers</returns>
        private void doBrmInventory(object data) {
            int duration = (int)data;

            // initialize the list of serial numbers
            List<String> snrs = new List<String>();
            tagReportList.Clear();

            // change state and notify all listeners
            this.state = RfidDeviceState.READING;

            if (startReadingEventHandler != null) {
                Dispatcher.BeginInvoke((Action)(() => startReadingEventHandler(this, EventArgs.Empty)));
            }

            try {
                lock (readerLocker) {
                    reader.SetData(FedmIscReaderID.FEDM_ISC_TMP_ADV_BRM_SETS, 255);

                    // clear and init buffer
                    reader.SendProtocol(FEISC_CLEAR_BUFFER_CMD);
                    reader.SendProtocol(FEISC_INIT_BUFFER_CMD); // this turn ON RF field !!
                }
                Thread.Sleep(duration);
                lock (readerLocker) {
                    // read buffer
                    string key = ConfigurableReaderParameter.NB_MAX_TAGS.ToString();
                    int nbTagsByNotif = (int)parameters[key];
                    reader.SetData(OBID.ReaderCommand._0x22.Req.DATA_SETS, nbTagsByNotif);
                    int status = reader.SendProtocol(FEISC_READ_BUFFER_CMD);

                    int TableSize = -1;

                    // are there valid data to read??
                    while (TableSize != 0 && ((status == 0x00) || (status == 0x83) || (status == 0x84) || (status == 0x85) || (status == 0x93) || (status == 0x94))) {

                        // yep ! get table size
                        TableSize = reader.GetTableLength(FedmIscReaderConst.BRM_TABLE);

                        // for each retrieved item
                        for (int index = 0; index < TableSize; index++) {

                            // read table and update tag list
                            string str;
                            reader.GetTableData(index, FedmIscReaderConst.BRM_TABLE, FedmIscReaderConst.DATA_SNR, out str);
                            snrs.Add(str);
                            tagReportList.Add((FedmBrmTableItem)reader.GetTableItem(index, FedmIscReaderConst.BRM_TABLE));
                        }
                        // clear buffer
                        reader.SendProtocol(FEISC_CLEAR_BUFFER_CMD);
                        status = reader.SendProtocol(FEISC_READ_BUFFER_CMD);
                    }
                }
            } catch (Exception ex) {

                // an issue occurs, let's notify listeners
                if (hasReportedAnErrorEventHandler != null) {

                    Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message))));
                }
            }

            endInventoryAndNotify();

            // notify the end of inventory
            List<String> snrList = new List<String>();
            if (snrs.Count > 0) {
                // no duplicata (to be sure!)
                var noDuplicata = new HashSet<string>(snrs);

                foreach (string st in noDuplicata) {
                    snrList.Add(st);
                }
            }

            // fire event !
            Dispatcher.BeginInvoke((Action)(() => fireTagFound(snrList)));
        }

        private void endInventoryAndNotify() {

            // turn OFF RF field
            switchOffAntenna();

            // change state, and notify listeners
            this.state = RfidDeviceState.CONNECTED;

            if (stopReadingEventHandler != null) {
                Dispatcher.BeginInvoke((Action)(() => stopReadingEventHandler(this, EventArgs.Empty)));
            }
        }

        /// <summary>
        /// start tag Inventory
        /// </summary>
        private void startInventory() {
            // check context
            if (reader != null
                && reader.Connected &&
                this.state != RfidDeviceState.READING) {
                logProducer.Logger.Info("Start LRU reader inventory");
                // start tag scan physically
                Thread th = new Thread(new ParameterizedThreadStart(doBrmInventory));
                try {
                    SCAN_DURATION = int.Parse((string)parameters[ConfigurableReaderParameter.TAG_SCAN_DURATION.ToString()]);
                } catch (Exception) {
                    //do nothing
                }
                th.Start(SCAN_DURATION);

            } else {

                // a problem occurs, let's notify listeners
                String EncounteredIssue = "Unable to start inventory, invalid context of execution";
                if (hasReportedAnErrorEventHandler != null) {

                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(EncounteredIssue));
                }

                return;
            }
        }

        /// <summary>
        /// notify all listeners of "onTagFound" event
        /// </summary>
        /// <param name="snrs">snr serial number (tag uuid)</param>
        private void fireTagFound(List<string> snrs) {
            logProducer.Logger.Info("Finished LRU reader inventory: " + snrs.Count);
            // fire event
            if (tagFoundEventHandler != null) {
                tagFoundEventHandler(this, new TagFoundEventArgs(snrs));
            }
            if (tagFoundDetailedEventHandler != null)
            {
                tagFoundDetailedEventHandler(this, new TagFoundObidEventArgs(tagReportList));
            }
        }

        private String getPortName(int port) {
            switch (port) {
                case 1:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No1.SelectedAntennas;
                case 2:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No2.SelectedAntennas;
                case 3:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No3.SelectedAntennas;
                case 4:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No4.SelectedAntennas;
                default:
                    return "";
            }
        }

        private String returnActivatedAntennaNamesFromByte(Byte activatedAntennas) {

            String activatedAntennaNames = "";

            if ((activatedAntennas & (Byte)0x01) > 0) {

                activatedAntennaNames += "Antenna #1";
            }
            if ((activatedAntennas & (Byte)0x02) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }

                activatedAntennaNames += "Antenna #2";
            }
            if ((activatedAntennas & (Byte)0x04) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }

                activatedAntennaNames += "Antenna #3";
            }
            if ((activatedAntennas & (Byte)0x08) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }
                activatedAntennaNames += "Antenna #4";
            }

            return activatedAntennaNames;
        }
        #endregion

        #region RFID_READER_IMPL
        /// <summary>
        /// Start scanning
        /// </summary>
        public void startScan() {

            this.startInventory();
        }

        /// <summary>
        /// stop scanning
        /// </summary>
        public void stopScan() {
            throw new NotImplementedException(); // TODO stopScan
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, ComPort.ToString(), getReaderState());
            return readerInfo.InfoList;
        }

        /// <summary>
        /// get reader current state
        /// </summary>
        /// <returns>RFID Reader State</returns>
        public RfidDeviceState getReaderState() {
            return this.state;
        }

        /// <summary>
        /// reader connection
        /// </summary>
        public void connect() {
            if (reader != null
                && !reader.Connected) {

                // let's open tag scan communication pipe through usb
                if (chooseAndOpenComAndNotify((String)parameters[ConfigurableReaderParameter.READER_NAME.ToString()])) {

                    // be sure scan isn't occurring
                    switchOffAntenna();
                }
            }
        }

        private bool chooseAndOpenComAndNotify(string readerName) {
            Boolean connected = false;
            String connectionType = (String)parameters[(String)ConfigurableReaderParameter.CONNECTION_TYPE.ToString()];
            if (connectionType == "SERIAL") {
                connected = openSerialComAndNotify(readerName);
            } else { // default USB 
                connected = openUsbComAndNotify(readerName);
            }

            return connected;
        }

        /// <summary>
        /// reader disconnection
        /// </summary>
        public void disconnect() {

            if (reader != null
                && reader.Connected) {

                try {

                    // disconnection
                    int iRes = reader.DisConnect();

                    if (iRes == Fedm.OK) {

                        if (disconnectedEventHandler != null) {
                            disconnectedEventHandler(this, EventArgs.Empty);
                        }

                        state = RfidDeviceState.DISCONNECTED;
                    }
                } catch (Exception ex) {

                    // FePortDriverException | FedmException
                    if (hasReportedAnErrorEventHandler != null) {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                    }
                }
            }
        }

        public string getReaderComPort() {

            if (comPort != -1) {
                return "COM" + comPort;
            }
            else {
                return "";
            }
        }

        /// <summary>
        /// get the reader Unique universal identifier.
        /// </summary>
        /// <returns>the reader UUID</returns>
        public String getReaderUUID() {
            String str = "";
            if (reader != null
                && reader.Connected) {
                str = reader.GetReaderName();
            }

            return str;
        }

        public String getReaderSwVersion()
        {
            String str = "";

            if (reader != null
                && reader.Connected)
            {
                str = string.Format("{0:x2}.", reader.GetReaderInfo().RfcSwVer[0]) + string.Format("{0:x2}", reader.GetReaderInfo().RfcSwVer[1]);
            }

            return str;
        }

        public int getInputs() {

            int readerStatus = -1;

            if (reader != null
               && reader.Connected) {

                try {

                    //reader.GetReaderInfo();
                    lock (readerLocker) {
                        readerStatus = reader.SendProtocol(0x74); // get input cmd
                    }
                } catch (Exception ex) {

                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Unable to get inputs, because : " + ex.Message));
                    }
                }
            }
            return readerStatus;

        }

        public Boolean readIn1() {

            bool input1= false;

            if (reader != null
                && reader.Connected) {

                try {
                    lock (readerLocker) {
                        reader.GetData(OBID.ReaderCommand._0x74.Rsp.Inputs.IN1, out input1);
                    }
                } catch (Exception ex) {

                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Unable to read in1, because : " + ex.Message));
                    }
                }
            }

            return input1;
        }

        public bool transferCfgFile(string fileName)
        {
            bool status = false;
            if (reader != null
                || reader.Connected
                || this.state != RfidDeviceState.READING)
            {
                try
                {
                    reader.ReadCompleteConfiguration(true);
                    status = reader.TransferXmlFileToReaderCfg(fileName) == 0;
                }
                catch (Exception) { }
            }
            if (!status)
                hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Transfer failure : "));
            return status;
        }

        #endregion

        #region FEUSBLISTENER_IMPL
        /// <summary>
        /// Event notification  on usb connection
        /// </summary>
        /// <param name="deviceHandle">device handle</param>
        /// <param name="deviceID">device id</param>
        void FeUsbListener.OnConnectReader(int deviceHandle, long deviceID) {

            if (connectedDeviceIdentifier != deviceID) {
                // not the right rfid device
                return;
            }

            if (connectedEventHandler != null) {
                connectedEventHandler(this, EventArgs.Empty);
            }

            state = RfidDeviceState.CONNECTED;
        }

        /// <summary>
        /// Event notification  on usb disconnection
        /// </summary>
        /// <param name="deviceHandle">device handle</param>
        /// <param name="deviceID">device id</param>
        void FeUsbListener.OnDisConnectReader(int deviceHandle, long deviceID) {

            if (connectedDeviceIdentifier != deviceID) {
                // not the right rfid device
                return;
            }

            if (disconnectedEventHandler != null) {
                disconnectedEventHandler(this, EventArgs.Empty);
            }

            state = RfidDeviceState.DISCONNECTED;
        }
        #endregion

        #region CONFIGURABLE_RFID_READER_IMPL

        /// <summary>
        /// Set a Map of key-value couples of parameter. This is highly device-dependent and what exactly will be 
        /// he available keys is left to the coder.
        /// </summary>
        public void setParameter(String key, Object value) {

            if (parameters.ContainsKey(key)) {

                parameters[key] = value;

            } else {

                if (!String.IsNullOrEmpty(key)) {
                    parameters.Add(key, value);
                }
            }
        }

        /// <summary>
        /// Get a Map of key-value couples of parameter. This is highly device-dependent and what exactly will be 
        /// the available keys is left to the coder.
        /// </summary>
        public Dictionary<String, Object> getParameters() {

            Dictionary<String, Object> maptoReturn = new Dictionary<string, object>();

            // copy
            foreach (KeyValuePair<String,Object> kv in parameters) {
                maptoReturn.Add(kv.Key, kv.Value);
            }

            return maptoReturn;
        }

        //get power of specified antenna
        public float getPower(int antenna) {
            byte power = 0;

            switch (antenna) {
                case 1:
                    reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No1.OutputPower, out power, false);
                    break;
                case 2:
                    reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No2.OutputPower, out power, false);
                    break;
                case 3:
                    reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No3.OutputPower, out power, false);
                    break;
                case 4:
                    reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No4.OutputPower, out power, false);
                    break;
            }

            if (isMuxEnabled(antenna)) //Manage MUX power loss
                return ((float)power - 15) / (float)13;
            else
                return ((float)power - 15) / 10;

        }

        //set input power on specified antenna
        public void setPower(int antenna, float power) {

            int convertedPower;


            if (isMuxEnabled(antenna)) //Manage MUX power loss
                convertedPower = (int)Math.Round(power * 13, 0) + 15;
            else
                convertedPower = (int)Math.Round(power * 10, 0) + 15;

            if (convertedPower < MIN_POWER || convertedPower > MAX_POWER)
                return;

            switch (antenna) {
                case 1:
                    reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No1.OutputPower, (byte)convertedPower, false);
                    break;
                case 2:
                    reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No2.OutputPower, (byte)convertedPower, false);
                    break;
                case 3:
                    reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No3.OutputPower, (byte)convertedPower, false);
                    break;
                case 4:
                    reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No4.OutputPower, (byte)convertedPower, false);
                    break;
            }
            reader.ApplyConfiguration(false);

        }

        public void setPowerDynamically(int antenna, float power) {
            throw new NotImplementedException();
        }
        public byte getAntennaConfiguration() {
            byte configAntenna = 0;
            reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, out configAntenna, false);
            return configAntenna;
        }

        public void setAntennaConfiguration(byte configAntenna) {
            reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            reader.ApplyConfiguration(false);
        }

        public byte addAntenna(int antenna) {
            int configAntenna = getAntennaConfiguration() | (0x1 << (antenna - 1));
            reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            reader.ApplyConfiguration(false);
            return (byte)configAntenna;
        }

        public byte removeAntenna(int antenna) {
            int configAntenna = getAntennaConfiguration() & (0xF - (0x1 << (antenna - 1)));
            reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            reader.ApplyConfiguration(false);
            return (byte)configAntenna;
        }

        //Indicates if there is MUX on reader port 1
        public bool isMuxEnabled(int port) {
            switch (port) {
                case 1:
                    return muxEnable;
                default:
                    return false;
            }
        }

        //Detect a mux on reader port 1
        public void detectMux(int port) {
            switch (port) {
                case 1:
                    byte cfgAntenna = getAntennaConfiguration();
                    byte cfgMux = getMuxConfiguration(port);
                    reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.Miscellaneous.Enable_DCPower, 1, false);
                    reader.SendProtocol(FEISC_CHECK_ANTENNAS_CMD);
                    System.Threading.Thread.Sleep(200);
                    switchOffAntenna();
                    reader.ReadCompleteConfiguration(false);
                    muxEnable = getMuxConfiguration(port) != 0;
                    setAntennaConfiguration(cfgAntenna);
                    if (muxEnable) {
                        setMuxConfiguration(cfgMux, port);
                        reader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.Enable, 1, false);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        //Get MUX config
        public byte getMuxConfiguration(int port) {
            byte configAntenna = 0;
            if (port < 1 || port > 4)
                return 0;
            reader.GetConfigPara(getPortName(port), out configAntenna, false);
            return configAntenna;
        }

        //Set MUX config
        public void setMuxConfiguration(byte configMux, int port) {
            if (port < 1 || port > 4)
                return;
            reader.SetConfigPara(getPortName(port), configMux, false);
            reader.ApplyConfiguration(false);
        }

        public byte addMuxAntenna(int readerPort, int muxPort) {
            int configMux = getMuxConfiguration(muxPort) | (0x1 << (muxPort - 1));
            setMuxConfiguration((byte)configMux, readerPort);
            return (byte)configMux;
        }

        public byte removeMuxAntenna(int readerPort, int muxPort) {
            int configMux = getMuxConfiguration(muxPort) & (0xFF - (0x1 << (muxPort - 1)));
            setMuxConfiguration((byte)configMux, readerPort);
            return (byte)configMux;
        }

        public byte removeMux(int antenna) {
            reader.SetConfigPara(getPortName(antenna), 0, false);
            reader.ApplyConfiguration(false);
            return 0;
        }

        #endregion

        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates() {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "FEIG ISC LRU3500");

            if (reader != null
                || reader.Connected) {

                try {
                    componentHealthStates.Add("State", "Connected");
                    componentHealthStates.Add("Serial Communication Port", ComPort.ToString());
                    componentHealthStates.Add("Serial Communication Type", (String)parameters[(String)ConfigurableReaderParameter.CONNECTION_TYPE.ToString()]);

                    String readerName = getReaderUUID();
                    if(!String.IsNullOrEmpty(readerName)){

                        componentHealthStates.Add("Reader Name", readerName);
                    }

                    reader.SendProtocol(FEISC_CHECK_ANTENNAS_CMD);
                    System.Threading.Thread.Sleep(200);
                    switchOffAntenna();
                    reader.ReadCompleteConfiguration(false);

                    Byte config;
                    reader.GetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.NoOfAntennas, out config, false);
                    componentHealthStates.Add("Number of Antennas", String.Format("{0:D}", config));

                    Byte antennaConfiguration = getAntennaConfiguration();
                    componentHealthStates.Add("functionnal Antennas", returnActivatedAntennaNamesFromByte(antennaConfiguration));

                    // get power for each antenna
                    if ((antennaConfiguration & (Byte)0x01) > 0) {
                        componentHealthStates.Add("Antenna #1 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(1)));
                    }
                    if ((antennaConfiguration & (Byte)0x02) > 0) {
                        componentHealthStates.Add("Antenna #2 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(2)));
                    }
                    if ((antennaConfiguration & (Byte)0x04) > 0) {
                        componentHealthStates.Add("Antenna #3 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(3)));
                    }
                    if ((antennaConfiguration & (Byte)0x08) > 0) {
                        componentHealthStates.Add("Antenna #4 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(4)));
                    }
                } catch (Exception ex) {
                    logProducer.Logger.Error("unable to get component health states", ex);
                }

            } else {
                componentHealthStates.Add("State", "disconnected");
            }
            return componentHealthStates;
        }

        #endregion
    }
}
