using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.log;

using OBID;
using System.Management;
using fr.nexess.toolbox.comm.serial;

namespace fr.nexess.hao.rfid.device.feig {

    /// <summary>
    /// This class is used to abstract classic-pro reader (CPR.02.XX) communication services.
    /// </summary>
    /// <version>$Revision: 144 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2016-07-13 10:44:53 +0200 (mer., 13 juil. 2016) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class Cpr02_10 : RfidDevice, SustainableRfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(Cpr02_10));

        // singleton instance.
        private static Cpr02_10 instance = null;

        // critical section object locker
        protected static readonly Object locker = new Object();

        // STA Threads management
        protected static Dispatcher  dispatcher = null;

        // Cpr02_10_ad's reader instance.
        protected static FedmIscReader reader = null;

        // reader state.
        protected static RfidDeviceState state = RfidDeviceState.DISCONNECTED;

        // current communication port of the reader
        protected static int comPort = -1;
        private String type = "CPR02";

        // flags.
        protected static Boolean startNotificationFlag = false;

        // default tag scan tempo
        protected static int BADGE_READER_SCAN_TEMPO = 250;

        // iso Table Reader thread Handler
        protected static Thread isoTableReaderHandler = null;

        private string VENDOR_ID = "VID_0403";
        private string PRODUCT_ID = "PID_6001";
        private string MANUFACTURER_NAME = "FTDI";
        private string HUB_PRODUCT_ID = "PID_6015";
        private string HUB_MANUFACTURER_NAME = "FTDIBUS";

        #region EVENT_HANDLER_DECLARATION
        protected static event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        protected static event EventHandler                   connectedEventHandler;
        protected static event EventHandler                   disconnectedEventHandler;
        protected static event EventHandler                   startReadingEventHandler;
        protected static event EventHandler                   stopReadingEventHandler;
        protected static event TagFoundEventHandler           tagFoundEventHandler;
        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// RFIDTagReader get instance (singleton).
        /// </summary>
        /// <returns>Cpr02_10_ad instance</returns>
        public static Cpr02_10 getInstance() {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new Cpr02_10();
                }
            }

            return instance;
        }


        /// <summary>
        /// protected default constructor
        /// </summary>
        public Cpr02_10() {

            // check if the calling thread is a STA thread (not a MTA one)
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["BADGE_READER_SCAN_TEMPO"])) {

                String tempo = ConfigurationManager.AppSettings["BADGE_READER_SCAN_TEMPO"];

                if (!String.IsNullOrEmpty(tempo)) {

                    try {

                        int ms = int.Parse(tempo);

                        if (ms > 0) {

                            BADGE_READER_SCAN_TEMPO = ms;
                        }
                    } catch (Exception) {
                        // continue
                    }
                }
            }

            initializeReader();

            initializeIsoTableReaderThread();

            logProducer.Logger.Debug("class loaded");
        }

        /// <summary>
        /// destructor
        /// </summary>
        ~Cpr02_10() {

            if (reader != null) {

                reader.Dispose();
                reader = null;

                instance = null;
            }
        }

        public static Cpr02_10 removeInstance()
        {
            lock (locker)
            {
                reader = null;
                instance = null;
            }
            return null;
        }
        #endregion

        #region PUBLIC_METHODS
        public int ComPort {
            get {
                return comPort;
            }
            set {
                comPort = value;
            }
        }

        /// <summary>
        /// thread Dispatcher getter
        /// </summary>
        public static Dispatcher Dispatcher {
            get {
                return dispatcher;
            }
            set {
                dispatcher = value;
            }
        }

        #region RFID_READER_IMPL
        /// <summary>
        /// Start scanning : e.g. "wait" a reading of a tag
        /// </summary>
        public void startScan() {
            // start inventory for getting one tag. startScan stopped after getting a tag.            
            this.startInventory();
        }

        /// <summary>
        /// stop scanning
        /// </summary>
        public void stopScan() {
            this.stopInventory();
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
            return state;
        }

        /// <summary>
        /// reader connection
        /// </summary>
        public void connect() {

            try {
                if (reader != null
                && !reader.Connected) {
                    // com connection
                    searchComPort();
                    connectToComPort(ComPort);

                    // start thread that be in charge of reading iso table
                    if (isoTableReaderHandler != null && !isoTableReaderHandler.IsAlive) {

                        isoTableReaderHandler.Start();
                    }
                }
            } catch (Exception ex) {

                // FePortDriverException || FedmException || FeReaderDriverException
                if (hasReportedAnErrorEventHandler != null) {

                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to establish a connection to the rfid reader due to : " + ex.Message));
                }

                logProducer.Logger.Info("unable to establish a connection to the rfid reader due to : " + ex.Message);
            }
        }

        /// <summary>
        /// reader disconnection
        /// </summary>
        public void disconnect() {

            if (reader != null
                && reader.Connected) {

                try {
                    // stop inventory (through RFIDReader interface)
                    this.stopScan();

                    // abort iso table reader thread 
                    isoTableReaderHandler.Abort();
                    isoTableReaderHandler = null;

                    // demand reader disconnection
                    int iRes = reader.DisConnect();

                    // free all reader resources
                    reader.Dispose();
                    reader = null;

                    // notify all listeners
                    state = RfidDeviceState.DISCONNECTED;

                    if (disconnectedEventHandler != null) {
                        disconnectedEventHandler(this, EventArgs.Empty);
                    }

                    logProducer.Logger.Debug("badge reader DISCONNECTED");
                } catch (Exception ex) {
                    // FePortDriverException ||
                    // FedmException
                    if (hasReportedAnErrorEventHandler != null) {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to disconnect communication pipe from the rfid reader due to : " + ex.Message));
                    }

                    logProducer.Logger.Info("unable to disconnect communication pipe from the rfid reader due to : " + ex.Message);
                }
            }
        }

        public string getReaderComPort() {

            if (comPort != -1) {
                return "COM" + comPort;
            } else {
                return "";
            }
        }

        /// <summary>
        /// get the reader Unique universal identifier.
        /// </summary>
        /// <returns>UUID string</returns>
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

        public bool transferCfgFile(string fileName)
        {
            bool status = false;
            if (reader != null
                || reader.Connected)
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

        public void switchOffAntenna()
        {
            if (reader != null
               || reader.Connected)
            {
                try
                {
                    reader.SetData(OBID.ReaderCommand._0x6A.Req.RF_OUTPUT, 0x00);
                    reader.SendProtocol(0x6A);
                }
                catch (Exception ex)
                {
                    logProducer.Logger.Debug("can't switch Off Antenna because : " + ex.Message);
                }
            }
        }

        public void switchOnAntenna()
        {

            if (reader != null
               || reader.Connected)
            {
                try
                {
                    reader.SetData(OBID.ReaderCommand._0x6A.Req.RF_OUTPUT, 0x01);
                    reader.SendProtocol(0x6A);
                }
                catch (Exception ex)
                {
                    logProducer.Logger.Debug("can't switch Off Antenna because : " + ex.Message);
                }
            }
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

        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates() {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "FEIG CPR02");

            if (reader != null
                || reader.Connected) {

                try {
                    componentHealthStates.Add("State", "Connected");
                    componentHealthStates.Add("Serial Communication Port", ComPort.ToString());

                    String readerName = getReaderUUID();
                    if (!String.IsNullOrEmpty(readerName)) {

                        componentHealthStates.Add("Reader Name", readerName);
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

        #endregion

        #region PRIVATE_METHOD

        /// <summary>
        /// start inventory : means turn on notification when a transponder has been detected 
        /// </summary>
        protected void startInventory() {

            // is the reader ready to work?
            if (reader == null
                || !reader.Connected) {

                // no, the reader isn't ready to work
                logProducer.Logger.Info("Can't start inventory, the reader isn't ready to work");

                return;
            }

            // yeah, ready to start inventory!
            lock (locker) {

                // set 'inventory Interrupt Requested' flag to true
                startNotificationFlag = true;

                if (isoTableReaderHandler.IsAlive) {

                    // change state and notify
                    state = RfidDeviceState.READING;
                    if (startReadingEventHandler != null) {
                        startReadingEventHandler(this, EventArgs.Empty);
                    }

                    logProducer.Logger.Debug("badge reader START READING");

                } else {

                    if (hasReportedAnErrorEventHandler != null) {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Unable to start inventory"));
                    }

                    logProducer.Logger.Info("Unable to start inventory");
                }
            }
        }

        /// <summary>
        /// stop Inventory : means turn off notification when a transponder has been detected 
        /// </summary>
        private void stopInventory() {

            // is the reader ready to work?
            if (reader == null
                || !reader.Connected) {

                // no, the reader isn't ready to work
                logProducer.Logger.Info("Can't stop inventory, the reader isn't ready to work");
                return;
            }

            // yeah, ready to stop inventory!
            lock (locker) {

                // reset flag
                startNotificationFlag = false;

                // notify all listeners!
                if (stopReadingEventHandler != null) {

                    stopReadingEventHandler(this, EventArgs.Empty);
                }

                logProducer.Logger.Debug("badge reader STOPPED READING");

                // change state
                state = RfidDeviceState.CONNECTED;
            }
        }

        /// <summary>
        /// seek serial port com where the badge reader is connected
        /// </summary>
        /// <returns>com port number</returns>
        protected int seekComPort() {

            int iResult = -1;

            // let's retrieve the whole com port list
            List<int> portIds = new List<int>();
            logProducer.Logger.Debug("Start seeking ports");
            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(PRODUCT_ID))
                        portIds.Add(int.Parse(entry.Key.Substring(3)));
                }
            }
            catch (Exception ex) {

                logProducer.Logger.Error("Unable to seek Com Port for USB reader, because : " + ex.Message);
                return iResult;
            }

            //cas du CPR connecté directement sur la carte porte
            if (portIds.Count == 0)
            {
                try
                {
                    foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                    {
                        if (entry.Value.Contains(HUB_MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(HUB_PRODUCT_ID) && entry.Value.Contains("Port_#0004"))
                            portIds.Add(int.Parse(entry.Key.Substring(3)));
                    }
                }
                catch (Exception ex)
                {

                    logProducer.Logger.Error("Unable to seek Com Port for serial reader, because : " + ex.Message);
                    return iResult;
                }
            }

            //cas du guichet connecté directement sur port COM1 du PC et cas de la XS
            if (portIds.Count == 0)
            {
                portIds.Add(1);
                portIds.Add(2);
            }


            // try to connect to reader and retrieve information
            foreach (int portId in portIds) {

                try {

                    // try connection
                    logProducer.Logger.Debug("try connection with port id : " + portId.ToString());

                    
                    reader.ConnectCOMM(portId,  // port com
                                       true);  // TODO FINDBAUDRATE & Co


                    // GOT IT ! return...
                    iResult = portId;
                    break;

                } catch (Exception ex) {

                    if (reader.Connected) {
                        reader.DisConnect();
                    }

                    logProducer.Logger.Debug("ConnectCOMM exception : " + ex.Message);
                    continue;
                }
            }

            // close connection
            if (reader.Connected) {

                logProducer.Logger.Debug("DisConnect");
                reader.DisConnect();
            }

            return iResult;
        }

        /// <summary>
        /// otify all listeners of "onTagFound" event
        /// </summary>
        /// <param name="snrs">snr serial number (tag uuid)</param>
        protected void fireTagFound(List<string> snrs) {

            // fire event
            if (tagFoundEventHandler != null) {

                tagFoundEventHandler(this, new TagFoundEventArgs(snrs));
            }
        }

        /// <summary>
        /// get Transponder Type Name.
        /// </summary>
        /// <param name="trType">transponder type (byte)</param>
        /// <returns>the name of the scanned transponder</returns>
        protected String getTransponderTypeName(byte trType) {

            String transpondertypeName = "unknown transponder type";

            switch (trType) {
                case 0x00: // Philips I-CODE1
                    transpondertypeName = "Philips I-CODE1";
                    break;
                case 0x01: // Texas Instruments Tag-it HF
                    transpondertypeName = "Texas Instruments Tag-it HF";
                    break;
                case 0x03: // ISO15693
                    transpondertypeName = "ISO15693";
                    break;
                case 0x04: // ISO14443A
                    transpondertypeName = "ISO14443A";
                    break;
                case 0x05: // ISO14443B
                    transpondertypeName = "ISO14443B";
                    break;
                case 0x07: // I-Code UID
                    transpondertypeName = "I-Code UID";
                    break;
                case 0x08: // Innovision Jewel
                    transpondertypeName = "Innovision Jewel";
                    break;
                case 0x09: // ISO 18000-3M3
                    transpondertypeName = "ISO 18000-3M3";
                    break;
                case 0x81: // ISO18000-6-B
                    transpondertypeName = "ISO18000-6-B";
                    break;
                case 0x83: // EM4222
                    transpondertypeName = "EM4222";
                    break;
                case 0x84: // EPC Class1 Gen2
                    transpondertypeName = "EPC Class1 Gen2";
                    break;
                case 0x88: // EPC Class0/0+
                    transpondertypeName = "EPC Class0/0+";
                    break;
                case 0x89: // EPC Class1 Gen1
                    transpondertypeName = "EPC Class1 Gen1";
                    break;
                case 0x06: // EPC (Electronic Product Code)
                    transpondertypeName = "EPC (Electronic Product Code)";
                    break;
            }// end switch

            return transpondertypeName;
        }

        protected void checkAndRestoreConnection() {
            try {
                int res = reader.GetLastStatus();
                if (res == 0) { // comm lost
                    logProducer.Logger.Warn("Badge reader Connection lost");
                    if (reader.Connected) {
                        reader.DisConnect();
                    }
                    reader = null;
                    initializeReader();

                    connectToComPort(ComPort);
                    state = RfidDeviceState.READING;
                    Thread.Sleep(200);
                } else {
                    logProducer.Logger.Info("Badge reader status: " + res);
                }
            } catch (Exception ex) {
                logProducer.Logger.Error("unable to establish a connection to the rfid reader due to : " + ex.Message);
                Thread.Sleep(3000);
            }
        }

        /// <summary>
        /// let's read list of tag from the read field and notify all listeners.
        /// </summary>
        /// <returns>only last serial number extracted from iso table</returns>
        protected String readTagSnrFromReader() {

            byte trType = 0;
            String snr = "";
            int readerStatus = -1;

            if (reader != null && !reader.Connected) {
                logProducer.Logger.Info("Reader is not connected");
                checkAndRestoreConnection();
            }
            if (reader == null
                || !reader.Connected) {
                logProducer.Logger.Info("Reader is not re-connected");
                return snr; // empty
            }

            try {
                lock (locker) {
                    // setting for the next send protocol
                    reader.SetData(FedmIscReaderID.FEDM_ISC_TMP_B0_CMD, (byte)0x01); // sub command
                    reader.SetData(FedmIscReaderID.FEDM_ISC_TMP_B0_MODE, (byte)0x00);// no option flag

                    // clear internal iso table for the next inventory
                    reader.ResetTable(FedmIscReaderConst.ISO_TABLE);

                    try {
                        // execute inventory
                        readerStatus = reader.SendProtocol(0xB0);
                    } catch (Exception ex) { // FePortDriverException
                        logProducer.Logger.Error("Exception while reading transponder: " + ex.Message);
                        // check connection
                        checkAndRestoreConnection();
                        return snr; // empty
                    }

                    if (readerStatus == 0) {

                        // tag found, let's query table
                        for (int idx = 0; idx < reader.GetTableLength(FedmIscReaderConst.ISO_TABLE); idx++) {

                            // take transponder type
                            reader.GetTableData(idx, FedmIscReaderConst.ISO_TABLE, FedmIscReaderConst.DATA_TRTYPE, out trType);

                            // log transponder type name
                            logProducer.Logger.Debug(getTransponderTypeName(trType));

                            switch (trType) {
                                case 0x00: // Philips I-CODE1
                                case 0x01: // Texas Instruments Tag-it HF
                                case 0x03: // ISO15693
                                case 0x04: // ISO14443A
                                case 0x05: // ISO14443B
                                case 0x07: // I-Code UID
                                case 0x08: // Innovision Jewel
                                case 0x09: // ISO 18000-3M3
                                case 0x81: // ISO18000-6-B
                                case 0x83: // EM4222
                                case 0x84: // EPC Class1 Gen2
                                case 0x88: // EPC Class0/0+
                                case 0x89: // EPC Class1 Gen1
                                    // take serial number as string
                                    reader.GetTableData(idx, FedmIscReaderConst.ISO_TABLE, FedmIscReaderConst.DATA_SNR, out snr);
                                    break;
                                case 0x06: // EPC (Electronic Product Code)
                                    // take EPC-Field as complete string
                                    reader.GetTableData(idx, FedmIscReaderConst.ISO_TABLE, FedmIscReaderConst.DATA_EPC_SNR, out snr);
                                    break;
                                default:
                                    logProducer.Logger.Info("Unknown transponder type, trType : " + trType.ToString());
                                    break;
                            }
                        }
                    }
                }
            } catch (Exception ex) { // FedmException || FePortDriverException

                if (hasReportedAnErrorEventHandler != null) {
                    Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to extract the transponder type due to : " + ex.Message))));
                }

                logProducer.Logger.Error("unable to extract the transponder type due to : " + ex.Message);
            }

            // check extracted data from read field
            if (!String.IsNullOrEmpty(snr)) {

                // delete all entries from the reader's table
                reader.ResetTable(FedmIscReaderConst.ISO_TABLE);
            }

            return snr;
        }

        /// <summary>
        /// get serial number from iso table from reader dedicated field and notify snr to all listeners
        /// </summary>
        protected void getSnrAndNotify() {

            if (state == RfidDeviceState.READING)
            {
                // let's read list of tag from the read field and notify all listeners.
                String snr = readTagSnrFromReader();

                // empty serial number or strange snr?
                if (String.IsNullOrEmpty(snr) || String.Compare(snr, "0000000000000000") == 0)
                {

                    // yep, return...
                    return;
                }

                // no! snr filled, let's notify all listeners if scan is started
                lock (locker)
                {

                    if (startNotificationFlag)
                    {
                        // notify all listeners (switch to the main thread)
                        Dispatcher.BeginInvoke((Action)(() => this.fireTagFound(new List<string>() { snr })));

                        // log Serial Number with caution !
                        int numberOfCharToShow = 3;

                        if (snr.Length > numberOfCharToShow)
                        {

                            // retrieve end of the serial number
                            String snrForLogging = snr.Substring(snr.Length - numberOfCharToShow, numberOfCharToShow);

                            String hiddenChar = "";

                            for (int i = 0; i < snr.Length - numberOfCharToShow; i++)
                            {
                                hiddenChar += "*";
                            }

                            logProducer.Logger.Debug("Tag Found : " + hiddenChar + snrForLogging);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// reader Com Connection management
        /// </summary>
        protected void searchComPort() {

            try {
                if (reader != null
                && !reader.Connected) {

                    // seek Serial Com port for the first time
                    if (ComPort < 0) {

                        ComPort = seekComPort();
                    }
                }
            } catch (Exception ex) {
                logProducer.Logger.Error("Error searching com port, " + ex.Message);
                throw (ex);
            }

        }

        protected void connectToComPort(int readerComPort) {
            try {
                logProducer.Logger.Debug("Connect to COM port: " + readerComPort);
                // valid com port ?
                if (readerComPort > 0) {

                    // ... yes, let's open communication pipe
                    reader.ConnectCOMM(readerComPort, false);

                } else {

                    // ... no, notify all listeners!
                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to connect, no OBID Classic-pro CPR.02.10 connected to the host system"));
                    }

                    logProducer.Logger.Info("unable to connect, no OBID Classic-pro CPR.02.10 connected");
                }

                if (reader.Connected) {

                    // set the default baud rate
                    reader.SetPortPara("Baud", "38400");
                    reader.SetPortPara("Frame", "8E1");
                    reader.SetPortPara("TxTimeControl", "1");
                    reader.SetPortPara("TxDelayTime", "50");
                    reader.SetPortPara("Timeout", "5000");
                    state = RfidDeviceState.CONNECTED;

                    // notify all listeners
                    if (connectedEventHandler != null) {

                        connectedEventHandler(this, EventArgs.Empty);
                    }

                    logProducer.Logger.Debug("badge reader CONNECTED on port : " + ComPort.ToString());

                } else {

                    if (hasReportedAnErrorEventHandler != null) {

                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to connect"));
                    }

                    logProducer.Logger.Info("unable to connect");
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Error connecting to com port, " + ex.Message);
                throw (ex);
            }

        }

        private void initializeIsoTableReaderThread() {

            // thread instanciation
            if (isoTableReaderHandler == null) {

                isoTableReaderHandler = new Thread(new ThreadStart(isoTableReaderThread));

                // set the apartement state of a thread before it is started
                isoTableReaderHandler.SetApartmentState(ApartmentState.STA);
            }

            isoTableReaderHandler.IsBackground = true;
        }

        private void initializeReader() {

            try {

                if (reader == null) {

                    // instanciate reader
                    reader = new FedmIscReader();
                }

                // the size is selected equal to the maximum number of transponders located in the antenna field at the same time
                // NOTE - initializes the internal table for max. 1 tag per Inventory
                reader.SetTableSize(FedmIscReaderConst.ISO_TABLE,   // table Id
                                    8);                             // 8 tag with each notification (prevent multiple tag scan)

                // reset 'all' entries from the reader's table
                reader.ResetTable(FedmIscReaderConst.ISO_TABLE);

            } catch (Exception ex) { // FedmException

                if (hasReportedAnErrorEventHandler != null) {

                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to establish a connection to the rfid reader due to : " + ex.Message));
                }

                logProducer.Logger.Info("unable to establish a connection to the rfid reader due to : " + ex.Message);
            }
        }

        protected void raisedEvent(EventHandler eventhandler) {

            if (eventhandler != null) {
                eventhandler(this, EventArgs.Empty);
            }
        }

        protected void raisedHasReportedAnErrorEvent(String issue) {

            if (hasReportedAnErrorEventHandler != null) {

                hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(issue));
            }
        }

        #region THREAD
        /// <summary>
        /// iso Table Reader cyclic Thread
        /// </summary>
        protected void isoTableReaderThread() {
            while (true) {
                getSnrAndNotify();
                System.Threading.Thread.Sleep(BADGE_READER_SCAN_TEMPO);
            }
        }
        #endregion

        #endregion
    }
}
