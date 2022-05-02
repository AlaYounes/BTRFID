using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.comm;
using fr.nexess.toolbox.comm.serial;
using System.IO.Ports;
using System.Windows.Threading;
using fr.nexess.toolbox.comm.eventHandler;
using System.Configuration;
using System.Threading;
using fr.nexess.toolbox;
using System.Management;

namespace fr.nexess.hao.rfid.device.deister
{
    public class PrdiComHandler : SerialComHandler {
        public PrdiComHandler(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits) {
        }

        protected override void responseReceived(byte[] response) {
            logProducer.Logger.Debug(mySerialPort.PortName + " - " + ConversionTool.byteArrayToString(response));
        }

        protected override void eventReceived(string msg) {
            logProducer.Logger.Debug(mySerialPort.PortName + " - " + msg);
        }

        protected override void openPort(string msg) {
            logProducer.Logger.Debug(mySerialPort.PortName + " - " + msg);
        }

        protected override void send(string msg) {
            logProducer.Logger.Debug(mySerialPort.PortName + " - " + msg);
        }

    }

    public class prdi3 : RfidDevice, SustainableRfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(prdi3));

        // singleton instance.
        private static prdi3 instance = null;

        // critical section object locker
        private static readonly Object locker = new Object();

        // STA Threads management
        private static Dispatcher dispatcher = null;

        private String comPort = "";
        private ComHandler comHandler = null;
        private String type = "PRDI";

        private prdi3FrameRebuilder frameBuilder = null;

        private bool IsListenning { get; set; }

        private String DEISTER_RFID3_COM_PORT = "COM5";
        private int COM_SEEKING_TIMEOUT = 10000;
        private int REQUEST_TIMEOUT = 800;

        private string VENDOR_ID = "VID_0403";
        private string PRODUCT_ID = "PID_6001";
        private string MANUFACTURER_NAME = "FTDI";

        // RK 180620
        //Added for Nexport
        private string VENDOR_ID2 = "VID_0403";
        private string PRODUCT_ID2 = "PID_6015";
        private string MANUFACTURER_NAME2 = "FTDIBUS";

        private ManualResetEvent manualEvent;

        private const int DEISTER_BAUDRATE = 9600;
        private const Parity DEISTER_PARITY = Parity.None;
        private const int DEISTER_DATABITS = 8;
        private const StopBits DEISTER_STOPBITS = StopBits.One;

        private Dictionary<String, String> readerInfo = new Dictionary<string, string>();

        private static Thread comPortSeekingThreadHandler = null;
        protected byte readerAddress;
        private int nbRetryConnection = 0;
        private readonly int NB_MAX_RETRY_CONNECTION = 5;
        private bool noAutomaticReconnect = false;

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        private event EventHandler connectedEventHandler;
        private event EventHandler disconnectedEventHandler;
        private event EventHandler startReadingEventHandler;
        private event EventHandler stopReadingEventHandler;
        private event TagFoundEventHandler tagFoundEventHandler;

        private event EventHandler modeChangedToAutoReadingEventHandler;
        private event EventHandler modeChangedToCmdEventHandler;

        private event EventHandler maxConnectionsReached;

        #endregion

        #region CONSTRUCTOR
        public prdi3(byte readerAddress = 0x31)
        {
            this.readerAddress = readerAddress;
            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            frameBuilder = new prdi3FrameRebuilder();
            frameBuilder.TagDecoded += new TagDecodedEventHandler(frameBuilder_TagDecoded);
            frameBuilder.InfoDecoded += new InfoDecodedEventHandler(frameBuilder_InfoDecoded);
            frameBuilder.ModeChangedToAutoReading += new EventHandler(frameBuilder_ModeChangedToAutoReading);
            frameBuilder.ModeChangedToCommand += new EventHandler(frameBuilder_ModeChangedToCommand);


            getDefaultValuesFromConfiguration();

            if (String.IsNullOrEmpty(ComPort) && ComHandler == null) {

                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
            }

            logProducer.Logger.Debug("class loaded");
        }

        public static prdi3 getInstance()
        {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new prdi3();
                }
            }

            return instance;
        }

        public prdi3 removeInstance()
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
        public event HasReportedAnErrorEventHandler HasReportedAnError
        {
            add
            {
                lock (locker)
                {
                    hasReportedAnErrorEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    hasReportedAnErrorEventHandler -= value;
                }
            }
        }
        public event EventHandler Connected
        {
            add
            {
                lock (locker)
                {
                    connectedEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    connectedEventHandler -= value;
                }
            }
        }
        public event EventHandler Disconnected
        {
            add
            {
                lock (locker)
                {
                    disconnectedEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    disconnectedEventHandler -= value;
                }
            }
        }
        public event EventHandler StartReading
        {
            add
            {
                lock (locker)
                {
                    startReadingEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    startReadingEventHandler -= value;
                }
            }
        }
        public event EventHandler StopReading
        {
            add
            {
                lock (locker)
                {
                    stopReadingEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    stopReadingEventHandler -= value;
                }
            }
        }
        public event TagFoundEventHandler TagFound
        {
            add
            {
                lock (locker)
                {
                    tagFoundEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    tagFoundEventHandler -= value;
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
        public event EventHandler MaxConnectionsReached {
            add {
                lock (locker) {
                    maxConnectionsReached += value;
                }
            }
            remove {
                lock (locker) {
                    maxConnectionsReached -= value;
                }
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public String ComPort
        {
            get
            {
                return comPort;
            }
            set
            {
                comPort = value;
            }
        }

        public IDictionary<String, String> ReaderInfo
        {
            get
            {
                return readerInfo;
            }
        }

        public ComHandler ComHandler
        {
            get
            {
                return comHandler;
            }
            set
            {
                comHandler = value;
            }
        }

        public Dispatcher Dispatcher
        {
            get
            {
                return dispatcher;
            }
            set
            {
                dispatcher = value;
            }
        }

        public bool NoAutomaticReconnect {
            get {
                return noAutomaticReconnect;
            }

            set {
                noAutomaticReconnect = value;
            }
        }

        public void connect()
        {

            if (ComHandler == null && !string.IsNullOrEmpty(ComPort))
            {

                initializeComHandler();
            }

            if (ComHandler != null)
            {

                ComHandler.connect();
            }
        }

        public void disconnect()
        {
            if (ComHandler != null)
            {

                ComHandler.disconnect();
            }
        }

        public void startScan() {
            lock (locker) {
                Thread.Sleep(50);
                byte[] frame = prdi3FrameRebuilder.buildFrame(DeBusProtocol.CMD.CMD_MODE.MODE, DeBusProtocol.CMD.CMD_MODE.AUTOMATIC_READING, readerAddress);
                if (ComHandler != null) {

                    ComHandler.startlistening();
                    IsListenning = true;
                    ComHandler.sendData(frame);
                    Thread.Sleep(50);
                    frame = prdi3FrameRebuilder.buildFrame(DeBusProtocol.CMD.CMD_RF.MODE, DeBusProtocol.CMD.CMD_RF.RF_ON, readerAddress);
                    ComHandler.sendData(frame);
                }
            }

        }

        public void stopScan()
        {
            lock(locker) {
                if (IsListenning) {
                    byte[] frame = prdi3FrameRebuilder.buildFrame(DeBusProtocol.CMD.CMD_RF.MODE, DeBusProtocol.CMD.CMD_RF.RF_OFF, readerAddress);
                    ComHandler.sendData(frame);
                    ComHandler.stopListening();
                    IsListenning = false;
                }
            }
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, comPort, getReaderState());
            return readerInfo.InfoList;
        }

        public RfidDeviceState getReaderState()
        {

            RfidDeviceState state = RfidDeviceState.DISCONNECTED;

            if (ComHandler != null)
            {

                switch (ComHandler.getState())
                {

                    case ComState.CONNECTED:
                        state = RfidDeviceState.CONNECTED;

                        if (IsListenning)
                        {
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

        public string getReaderUUID()
        {
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

        public void switchOffAntenna()
        {
            stopScan();
            //throw new NotImplementedException();
        }

        public void switchOnAntenna()
        {
            startScan();
            //throw new NotImplementedException();
        }

        #endregion


        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates()
        {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "PRDI");

            if (ComHandler != null
                 && ComHandler.getState() == ComState.CONNECTED)
            {

                try
                {

                    componentHealthStates.Add("State", "Connected");
                    componentHealthStates.Add("Serial Communication Port", ComPort.ToString());
                }
                catch (Exception ex)
                {
                    logProducer.Logger.Error("unable to get component health states", ex);
                }

            }
            else
            {
                componentHealthStates.Add("State", "disconnected");
            }
            return componentHealthStates;
        }

        #endregion

        #region CALLBACK_METHODS

        private void frameBuilder_TagDecoded(object sender, TagDecodedEventArgs e)
        {

            List<String> snrs = new List<string>() { e.Snr };

            raiseTagFoundEvent(snrs);
        }

        private void frameBuilder_InfoDecoded(object sender, InfoDecodedEventArgs e)
        {

            readerInfo = e.Info;

            if (manualEvent != null)
            {
                Thread.Sleep(100); // wait for the wait of manualevent
                manualEvent.Set();
            }
        }

        void frameBuilder_ModeChangedToCommand(object sender, EventArgs e)
        {

            raiseEvent(modeChangedToCmdEventHandler);
        }

        void frameBuilder_ModeChangedToAutoReading(object sender, EventArgs e)
        {

            raiseEvent(modeChangedToAutoReadingEventHandler);
        }

        private void aComHandler_OnDatareceived(object sender, OnDataReceivedEventArgs e)
        {
            List<String> dataReceived = new List<String>() { e.DataAsString };

            frameBuilder.rebuildFrames(dataReceived);
        }

        private void aComHandler_StartListening(object sender, EventArgs e)
        {
            raiseEvent(startReadingEventHandler);
        }

        private void aComHandler_StoppedListening(object sender, EventArgs e)
        {
            raiseEvent(stopReadingEventHandler);
        }

        private void aComHandler_UnexpectedDisconnection(object sender, EventArgs e)
        {
            raiseEvent(disconnectedEventHandler);
            restoreCom();
        }

        private void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e) {
            raiseHasReportedAnErrorEvent(e.Message);
            restoreCom();
        }

        private void restoreCom() {
            if (!noAutomaticReconnect) {
                Thread thread = new Thread(threadRestoreCom);
                thread.Start();
            }
        }
        private void threadRestoreCom() {

            Thread.Sleep(3000);
            Dispatcher.BeginInvoke((Action)(() => checkAndRestoreConnection()));

        }

        private void aComHandler_Disconnected(object sender, EventArgs e)
        {
            raiseEvent(disconnectedEventHandler);
        }

        private void aComHandler_Connected(object sender, EventArgs e)
        {
            nbRetryConnection = 0;
            raiseEvent(connectedEventHandler);
        }
        #endregion

        #region PRIVATE_METHODS

        private void getReaderInfo(ComHandler aComHandler = null)
        {

            byte[] frame = prdi3FrameRebuilder.buildFrame(DeBusProtocol.CMD.CMD_Version.VERSION, new byte[0], readerAddress);

            if (aComHandler == null)
            {
                aComHandler = ComHandler;
            }
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
            aComHandler.sendData(frame);
        }

        protected void seekComPort()
        {

            PrdiComHandler aSerialHandler = null;

            List<String> portNames = new List<String>();
            
            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(PRODUCT_ID)) {
                        //logProducer.Logger.Info("Reader info: " + entry.ToString());
                        portNames.Add(entry.Key);
                    }
                    // RK 180620
                    //Added for Nexport
                    if (entry.Value.Contains(MANUFACTURER_NAME2) && entry.Value.Contains(VENDOR_ID2) && entry.Value.Contains(PRODUCT_ID2))
                    {
                        portNames.Add(entry.Key);
                    }

                }
            }
            catch (Exception ex)
            {

                logProducer.Logger.Error("Unable to seek Com Port, because : " + ex.Message);
                return;
            }
            
            // extract com ids
            foreach (String portname in portNames)
            {

                try
                {

                    // retrieve serial com handler and associated event...
                    aSerialHandler = new PrdiComHandler(portname, DEISTER_BAUDRATE, DEISTER_PARITY, DEISTER_DATABITS, DEISTER_STOPBITS);
                    aSerialHandler.Dispatcher = null;
                    aSerialHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);

                    if (aSerialHandler.getState() == ComState.CONNECTED)
                    {

                        logProducer.Logger.Debug(portname + " is already opened, skip it and switch to the following");

                        continue;
                    }

                    // let's connect
                    aSerialHandler.connect();
                    aSerialHandler.startlistening();

                    // check if serial is opened 
                    if (aSerialHandler.getState() != ComState.CONNECTED)
                    {

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
                    if (ReaderInfo.Count > 0)
                    {
                        logProducer.Logger.Debug("Reader info received");
                        // reset info received
                        readerInfo.Clear();
                        // ok 
                        ComPort = portname;

                        // stop listening and unplug listener
                        if (aSerialHandler != null)
                        {

                            aSerialHandler.stopListening();
                            aSerialHandler.disconnect();
                            aSerialHandler = null;
                        }

                        break;

                    }
                    else
                    {

                        //No version retrieved

                        //rtz com port
                        ComPort = "";

                        // stop listening and unplug listener
                        if (aSerialHandler != null)
                        {
                            logProducer.Logger.Error("stopping listening, no version");
                            aSerialHandler.stopListening();
                            aSerialHandler.disconnect();
                            aSerialHandler = null;
                        }

                        continue;
                    }
                }
                catch (Exception ex)
                {
                    //rtz com port
                    ComPort = "";

                    // stop listening and unplug listener
                    if (aSerialHandler != null)
                    {
                        logProducer.Logger.Error("stopping listening, exception: " + ex.Message);
                        aSerialHandler.stopListening();
                        aSerialHandler.disconnect();
                        aSerialHandler = null;
                    }

                    // it isn't possible to connect to this com port, continue
                    continue;
                }
            }
        }

        private void getDefaultValuesFromConfiguration()
        {

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["DEISTER_RFID3_COM_PORT"]))
            {

                String comPortFromConfig = ConfigurationManager.AppSettings["DEISTER_RFID3_COM_PORT"];

                if (!String.IsNullOrEmpty(comPortFromConfig))
                {

                    DEISTER_RFID3_COM_PORT = comPortFromConfig;
                }
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["COM_SEEKING_TIMEOUT"]))
            {

                String timeout = ConfigurationManager.AppSettings["COM_SEEKING_TIMEOUT"];

                if (!String.IsNullOrEmpty(timeout))
                {
                    try
                    {

                        int ms = int.Parse(timeout);

                        if (ms > 0)
                        {
                            COM_SEEKING_TIMEOUT = ms;
                        }

                    }
                    catch (Exception)
                    {
                        // continue
                    }
                }
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["REQUEST_TIMEOUT"]))
            {

                String timeout = ConfigurationManager.AppSettings["REQUEST_TIMEOUT"];

                if (!String.IsNullOrEmpty(timeout))
                {
                    try
                    {

                        int ms = int.Parse(timeout);

                        if (ms > 0)
                        {
                            REQUEST_TIMEOUT = ms;
                        }

                    }
                    catch (Exception)
                    {
                        // continue
                    }
                }
            }
        }

        private void initializeComHandler()
        {

            if (ComHandler == null)
            {

                String portName = DEISTER_RFID3_COM_PORT; // from config or by default

                if (!String.IsNullOrEmpty(ComPort))
                { // sought com port

                    portName = ComPort;

                    logProducer.Logger.Info("LEGIC Serial communication port found : " + portName);
                }
                else
                {
                    logProducer.Logger.Warn("Unable to seek LEGIC serial communication port, let's use config or default value : " + portName);
                }

                ComHandler = new PrdiComHandler(portName, DEISTER_BAUDRATE, DEISTER_PARITY, DEISTER_DATABITS, DEISTER_STOPBITS);

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

        protected void raiseEvent(EventHandler eventhandler)
        {

            if (eventhandler != null)
            {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
            }
        }

        protected void raiseHasReportedAnErrorEvent(String issue)
        {

            if (hasReportedAnErrorEventHandler != null)
            {

                Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(issue))));
            }
        }
        protected void raiseTagFoundEvent(List<String> snrs)
        {

            if (tagFoundEventHandler != null)
            {

                Dispatcher.BeginInvoke((Action)(() => tagFoundEventHandler(this, new TagFoundEventArgs(snrs))));
            }
        }

        private void initializeComPortSeekingThread()
        {

            // internal event notification
            manualEvent = new ManualResetEvent(false);

            // thread instanciation
            comPortSeekingThreadHandler = new Thread(new ThreadStart(comPortSeekingThread));

            // set the apartement state of a thread before it is started
            comPortSeekingThreadHandler.SetApartmentState(ApartmentState.STA);
            comPortSeekingThreadHandler.IsBackground = true;
        }

        private void startAndWaitComPortSeekingThread()
        {

            if (comPortSeekingThreadHandler != null && !comPortSeekingThreadHandler.IsAlive)
            {

                comPortSeekingThreadHandler.Start();

                bool finished = comPortSeekingThreadHandler.Join(COM_SEEKING_TIMEOUT);

                if (!finished)
                {
                    comPortSeekingThreadHandler.Abort();
                }
            }
        }

        protected void checkAndRestoreConnection()
        {
            try {
                logProducer.Logger.Error("Restore connection");
                if (nbRetryConnection == NB_MAX_RETRY_CONNECTION) {
                    nbRetryConnection = 0;
                    raiseEvent(maxConnectionsReached);
                }
                nbRetryConnection++;
                disconnect();
                ComPort = null;
                //this.removeInstance();
                //getInstance();
                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
                if (ComPort != null) {
                    ComHandler = null;

                    connect();
                    if(ComHandler == null) {
                        restoreCom();
                    }
                } else {
                    restoreCom();
                }
            } catch (Exception ex)
            {
                logProducer.Logger.Error("unable to establish a connection to the prdi due to : " + ex.Message);
                restoreCom();
            }
        }
        #endregion

        #region THREAD
        protected void comPortSeekingThread()
        {
            seekComPort();
        }
        
        #endregion
    }
}
