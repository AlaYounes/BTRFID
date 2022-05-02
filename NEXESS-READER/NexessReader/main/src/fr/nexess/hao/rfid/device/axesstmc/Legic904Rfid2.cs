using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox;
using fr.nexess.toolbox.comm;
using fr.nexess.toolbox.comm.eventHandler;
using fr.nexess.toolbox.comm.serial;
using fr.nexess.toolbox.log;

namespace fr.nexess.hao.rfid.device.axesstmc {

    /// <summary>
    /// This class is used to abstract Legic 904.227.15 13.56MHZ Rfid2 Rfid badge reader
    /// </summary>
    /// <author>J.FARGEON</author>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class Legic904Rfid2 : RfidDevice, ConfigurableRfidDevice, SustainableRfidDevice
    {

        private LogProducer logProducer = new LogProducer(typeof(Legic904Rfid2));

        // singleton instance.
        private static Legic904Rfid2 instance = null;

        // critical section object locker
        private static readonly Object locker = new Object();

        // STA Threads management
        private static Dispatcher  dispatcher = null;

        private static String comPort = "";
        private static ComHandler comHandler = null;
        private String type = "LEGIC";

        private LegicFrameRebuilder frameBuilder = null;

        private bool IsListenning { get; set; }

        private String LEGIC_904_COM_PORT = "COM2";
        private int COM_SEEKING_TIMEOUT = 10000;
        private int REQUEST_TIMEOUT     = 800;

        private string VENDOR_ID = "VID_0403";
        private string PRODUCT_ID = "PID_6001";
        private string MANUFACTURER_NAME = "FTDI";

        private ManualResetEvent    manualEvent;

        private const int       LEGIC_BAUDRATE = 57600;
        private const Parity    LEGIC_PARITY = Parity.None;
        private const int       LEGIC_DATABITS = 8;
        private const StopBits  LEGIC_STOPBITS = StopBits.One;

        private static Dictionary<String,String> readerInfo = new Dictionary<string, string>();

        private static Thread comPortSeekingThreadHandler = null;
        // Connexion supervision
        private static System.Timers.Timer  connectionSupervisor;
        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startReadingEventHandler;
        private event EventHandler                      stopReadingEventHandler;
        private event TagFoundEventHandler              tagFoundEventHandler;

        private event EventHandler                      modeChangedToAutoReadingEventHandler;
        private event EventHandler                      modeChangedToCmdEventHandler;

        #endregion

        #region CONSTRUCTOR
        public Legic904Rfid2() {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            frameBuilder = new LegicFrameRebuilder();
            frameBuilder.TagDecoded += new TagDecodedEventHandler(frameBuilder_TagDecoded);
            frameBuilder.InfoDecoded += new InfoDecodedEventHandler(frameBuilder_InfoDecoded);
            frameBuilder.ModeChangedToAutoReading += new EventHandler(frameBuilder_ModeChangedToAutoReading);
            frameBuilder.ModeChangedToCommand += new EventHandler(frameBuilder_ModeChangedToCommand);


            getDefaultValuesFromConfiguration();

            if (String.IsNullOrEmpty(ComPort) && ComHandler == null) {

                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
            }
            // Launch a timer to poll connection events
            connectionSupervisor = new System.Timers.Timer(500);
            connectionSupervisor.Elapsed += new ElapsedEventHandler(onConnectionSupervisorElapsed);
            connectionSupervisor.Enabled = true;
            logProducer.Logger.Debug("class loaded");
        }

        public static Legic904Rfid2 getInstance() {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new Legic904Rfid2();
                }
            }

            return instance;
        }

        public static Legic904Rfid2 removeInstance()
        {
            lock (locker)
            {
                instance = null;
                ComHandler = null;
            }
            return null;
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
        public static String ComPort {
            get {
                return comPort;
            }
            set {
                comPort = value;
            }
        }

        public IDictionary<String, String> ReaderInfo {
            get {
                return readerInfo;
            }
        }

        public static ComHandler ComHandler {
            get {
                return comHandler;
            }
            set {
                comHandler = value;
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

            if (ComHandler == null) {

                initializeComHandler();
            }

            if (ComHandler != null) {

                ComHandler.connect();
            }
        }

        public void disconnect() {
            if (ComHandler != null) {

                ComHandler.disconnect();
                connectionSupervisor.Enabled = false;
            }
        }

        public void startScan() {
            if (ComHandler != null) {

                ComHandler.startlistening();
                IsListenning = true;
            }
        }

        public void stopScan() {
            ComHandler.stopListening();
            IsListenning = false;
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, comPort, getReaderState());
            return readerInfo.InfoList; 
        }

        public RfidDeviceState getReaderState() {

            RfidDeviceState state = RfidDeviceState.DISCONNECTED;

            if (ComHandler != null) {

                switch (ComHandler.getState()) {

                    case ComState.CONNECTED:
                        state = RfidDeviceState.CONNECTED;

                        if (IsListenning) {
                            state = RfidDeviceState.READING;
                        }
                        break;

                    case ComState.DISCONNECTED:
                        state = RfidDeviceState.DISCONNECTED;
                        break;

                    default:
                        break;
                }
            }

            return state;
        }

        public string getReaderComPort() {
            return comPort;
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

        public void switchMode(MODE mode) {

            if (ComHandler != null
                && ComHandler.getState() == ComState.CONNECTED) {

                byte[] frame = null;

                switch (mode) {

                    case MODE.AUTOMATIC_READING:
                        frame = LegicFrameRebuilder.buildFrame(LegicProtocol.CMD.MODE_0x14.MODE, LegicProtocol.CMD.MODE_0x14.AUTOMATIC_READING);
                        break;

                    case MODE.COMMAND:
                        frame = LegicFrameRebuilder.buildFrame(LegicProtocol.CMD.MODE_0x14.MODE, LegicProtocol.CMD.MODE_0x14.COMMAND_MODE);
                        break;

                    default:
                        break;
                }

                if (frame != null) {
                    logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
                    ComHandler.sendData(frame);
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

            componentHealthStates.Add("Reader type", "LEGIC");

            if (ComHandler != null
                 && ComHandler.getState() == ComState.CONNECTED) {

                try {

                    componentHealthStates.Add("State", "Connected");
                    componentHealthStates.Add("Serial Communication Port", ComPort.ToString());
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

        #region CONFIGURABLE_RFID_READER_IMPL
        public void setParameter(String key, Object value)
        {

            if (parameters.ContainsKey(key))
            {

                parameters[key] = value;

            }
            else {

                if (!String.IsNullOrEmpty(key))
                {
                    parameters.Add(key, value);
                }
            }


        }

        public Dictionary<String, Object> getParameters()
        {

            Dictionary<String, Object> maptoReturn = new Dictionary<string, object>();

            // copy
            foreach (KeyValuePair<String, Object> kv in parameters)
            {
                maptoReturn.Add(kv.Key, kv.Value);
            }

            return maptoReturn;
        }

        public float getPower(int antenna)
        {
            throw new NotImplementedException();
        }

        public void setPower(int antenna, float power)
        {
            throw new NotImplementedException();
        }
        public void setPowerDynamically(int antenna, float power) {
            throw new NotImplementedException();
        }

        public byte getAntennaConfiguration()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region CALLBACK_METHODS

        private void frameBuilder_TagDecoded(object sender, TagDecodedEventArgs e) {

            List<String> snrs = new List<string>() { e.Snr };

            raiseTagFoundEvent(snrs);
        }

        private void frameBuilder_InfoDecoded(object sender, InfoDecodedEventArgs e) {

            readerInfo = e.Info;

            if (manualEvent != null) {
                manualEvent.Set();
            }
        }

        void frameBuilder_ModeChangedToCommand(object sender, EventArgs e) {

            raiseEvent(modeChangedToCmdEventHandler);
        }

        void frameBuilder_ModeChangedToAutoReading(object sender, EventArgs e) {

            raiseEvent(modeChangedToAutoReadingEventHandler);
        }

        private void aComHandler_OnDatareceived(object sender, OnDataReceivedEventArgs e) {

            List<String> dataReceived = new List<String>() { e.DataAsString };

            frameBuilder.rebuildFrames(dataReceived);
        }

        private void aComHandler_StartListening(object sender, EventArgs e) {
            raiseEvent(startReadingEventHandler);
        }

        private void aComHandler_StoppedListening(object sender, EventArgs e) {
            raiseEvent(stopReadingEventHandler);
        }

        private void aComHandler_UnexpectedDisconnection(object sender, EventArgs e) {
            raiseHasReportedAnErrorEvent("Unexpected Disconnection occurs");
        }

        private void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e) {
            raiseHasReportedAnErrorEvent(e.Message);
        }

        private void aComHandler_Disconnected(object sender, EventArgs e) {
            raiseEvent(disconnectedEventHandler);
        }

        private void aComHandler_Connected(object sender, EventArgs e) {
            raiseEvent(connectedEventHandler);
            connectionSupervisor.Enabled = true;
        }
        #endregion

        #region PRIVATE_METHODS

        private void getReaderInfo(ComHandler aComHandler = null) {

            byte[] frame = LegicFrameRebuilder.buildFrame(LegicProtocol.CMD.GET_READER_INFO_0xB6.GET_READER_INFO, LegicProtocol.CMD.GET_READER_INFO_0xB6.PARAMS);

            if (aComHandler == null) {

                aComHandler = ComHandler;
            }
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
            aComHandler.sendData(frame);
        }

        protected void seekLegicComPort() {

            SerialComHandler aSerialHandler  = null;

            List<String> portNames = new List<String>();

            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(PRODUCT_ID))
                        portNames.Add(entry.Key);
                }
            }           
            catch (Exception ex) {

                logProducer.Logger.Error("Unable to seek Com Port, because : " + ex.Message);
                return;
            }
            

            // extract com ids
            foreach (String portname in portNames) {

                try {

                    // retrieve serial com handler and associated event...
                    aSerialHandler = new SerialComHandler(portname, LEGIC_BAUDRATE, LEGIC_PARITY, LEGIC_DATABITS, LEGIC_STOPBITS);
                    aSerialHandler.Dispatcher = null;
                    aSerialHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);

                    if (aSerialHandler.getState() == ComState.CONNECTED) {

                        logProducer.Logger.Debug(portname + " is already opened, skip it and switch to the following");

                        continue;
                    }

                    // let's connect
                    aSerialHandler.connect();
                    aSerialHandler.startlistening();

                    // check if serial is opened 
                    if (aSerialHandler.getState() != ComState.CONNECTED) {

                        logProducer.Logger.Debug("unable to open " + portname + ", skip it and switch to the following");

                        continue;
                    }

                    // send HARD_VERSIONS command over serial com and wait response
                    logProducer.Logger.Debug("Ok, " + portname + " is opened, let's send get_reader_info command over serial com and wait response");

                    // reset manual event
                    manualEvent.Reset();

                    getReaderInfo(aSerialHandler);

                    // Wait for response (re-sync)
                    manualEvent.WaitOne(REQUEST_TIMEOUT);

                    // check if version retrieved
                    if (ReaderInfo.Count > 0) {

                        // ok 
                        ComPort = portname;

                        // stop listening and unplug listener
                        if (aSerialHandler != null) {

                            aSerialHandler.stopListening();
                            aSerialHandler.disconnect();
                            aSerialHandler = null;
                        }

                        break;

                    } else {

                        //No version retrieved

                        //rtz com port
                        ComPort = "";

                        // stop listening and unplug listener
                        if (aSerialHandler != null) {

                            aSerialHandler.stopListening();
                            aSerialHandler.disconnect();
                            aSerialHandler = null;
                        }

                        continue;
                    }
                } catch (Exception) {
                    //rtz com port
                    ComPort = "";

                    // stop listening and unplug listener
                    if (aSerialHandler != null) {

                        aSerialHandler.stopListening();
                        aSerialHandler.disconnect();
                        aSerialHandler = null;
                    }

                    // it isn't possible to connect to this com port, continue
                    continue;
                }
            }
        }

        private void getDefaultValuesFromConfiguration() {

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["LEGIC_904_COM_PORT"])) {

                String comPortFromConfig = ConfigurationManager.AppSettings["LEGIC_904_COM_PORT"];

                if (!String.IsNullOrEmpty(comPortFromConfig)) {

                    LEGIC_904_COM_PORT = comPortFromConfig;
                }
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["COM_SEEKING_TIMEOUT"])) {

                String timeout = ConfigurationManager.AppSettings["COM_SEEKING_TIMEOUT"];

                if (!String.IsNullOrEmpty(timeout)) {
                    try {

                        int ms = int.Parse(timeout);

                        if (ms > 0) {
                            COM_SEEKING_TIMEOUT = ms;
                        }

                    } catch (Exception) {
                        // continue
                    }
                }
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["REQUEST_TIMEOUT"])) {

                String timeout = ConfigurationManager.AppSettings["REQUEST_TIMEOUT"];

                if (!String.IsNullOrEmpty(timeout)) {
                    try {

                        int ms = int.Parse(timeout);

                        if (ms > 0) {
                            REQUEST_TIMEOUT = ms;
                        }

                    } catch (Exception) {
                        // continue
                    }
                }
            }

            parameters.Add(ConfigurableReaderParameter.COM_PORT.ToString(), LEGIC_904_COM_PORT);

        }

        private void initializeComHandler() {

            if (ComHandler == null) {

                String portName = (String)parameters[ConfigurableReaderParameter.COM_PORT.ToString()]; // from config or by default

                if (!String.IsNullOrEmpty(ComPort)) { // sought com port

                    portName = ComPort;

                    logProducer.Logger.Info("LEGIC Serial communication port found : " + portName);
                } else {
                    logProducer.Logger.Warn("Unable to seek LEGIC serial communication port, let's use config or default value : " + portName);
                }

                ComHandler = new SerialComHandler(portName, LEGIC_BAUDRATE, LEGIC_PARITY, LEGIC_DATABITS, LEGIC_STOPBITS);

                ComPort = portName;
            }

            ComHandler.Connected += new EventHandler(aComHandler_Connected);
            ComHandler.Disconnected += new EventHandler(aComHandler_Disconnected);
            ComHandler.HasReportedAComError += new HasReportedAComErrorEventHandler(aComHandler_HasReportedAComError);
            ComHandler.StartListening += new EventHandler(aComHandler_StartListening);
            ComHandler.UnexpectedDisconnection += new EventHandler(aComHandler_UnexpectedDisconnection);
            ComHandler.StoppedListening += new EventHandler(aComHandler_StoppedListening);
            ComHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);
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

        private void initializeComPortSeekingThread() {

            // internal event notification
            manualEvent = new ManualResetEvent(false);

            // thread instanciation
            if (comPortSeekingThreadHandler == null) {

                comPortSeekingThreadHandler = new Thread(new ThreadStart(comPortSeekingThread));

                // set the apartement state of a thread before it is started
                comPortSeekingThreadHandler.SetApartmentState(ApartmentState.STA);
            }

            comPortSeekingThreadHandler.IsBackground = true;
        }

        private void startAndWaitComPortSeekingThread() {

            if (comPortSeekingThreadHandler != null && !comPortSeekingThreadHandler.IsAlive) {

                comPortSeekingThreadHandler.Start();

                bool finished = comPortSeekingThreadHandler.Join(COM_SEEKING_TIMEOUT);

                if (!finished) {
                    comPortSeekingThreadHandler.Abort();
                }
                comPortSeekingThreadHandler = null;
            }
        }

        #endregion

        #region THREAD
        protected void comPortSeekingThread() {

            seekLegicComPort();
        }
        #endregion

        #region EVENT HANDLERS

        public delegate void InvokeManageConnection();
        protected void onConnectionSupervisorElapsed(object sender, ElapsedEventArgs e) {
            connectionSupervisor.Enabled = false;
            Dispatcher.BeginInvoke(new InvokeManageConnection(checkAndRestoreConnection));
            connectionSupervisor.Enabled = true;
        }

        protected void checkAndRestoreConnection() {
             if (comHandler != null) {
                ComState st = ComHandler.getState();
                // Check if disconnection occured
                if (st == ComState.DISCONNECTED) {
                    Thread.Sleep(3000);
                    logProducer.Logger.Error("Badge reader Connection lost");

                    ComHandler.disconnect();
                    ComHandler.connect();
                }

            } 
        }

        #endregion
    }

    public enum MODE {
        AUTOMATIC_READING,
        COMMAND
    }
}


