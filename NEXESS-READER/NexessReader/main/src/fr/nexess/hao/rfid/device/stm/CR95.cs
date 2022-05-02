using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox;
using fr.nexess.toolbox.comm;
using fr.nexess.toolbox.comm.eventHandler;
using fr.nexess.toolbox.comm.serial;
using fr.nexess.toolbox.log;
using System.Management;

namespace fr.nexess.hao.rfid.device.stm
{
    public class CR95ComHandler : SerialComHandler {

        private LogProducer logProducer = new LogProducer(typeof(CR95ComHandler));

        public CR95ComHandler(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)  : 
            base(portName, baudRate, parity, dataBits, stopBits) { }

        public override void sendData(byte[] msg) {

            if (mySerialPort == null || mySerialPort.IsOpen == false) {
                logProducer.Logger.Debug("Can't send data : [" + ConversionTool.byteArrayToString(msg) + "], the port \"" + mySerialPort.PortName + "\" is closed");
                return;
            }

            try {

                // too many logs
                // logProducer.Logger.Debug(ConversionTool.byteArrayToString(msg));
                mySerialPort.Write(msg, 0, msg.Length);

            } catch (Exception ex) {

                // fire event
                notifyError(ex.Message);
                // notify all listeners
                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
            }
        }

        protected override void fireResponseReceived(byte[] response) {
            // too many logs
            //logProducer.Logger.Debug(ConversionTool.byteArrayToString(response));

            notifyDataReceived(response);

            foreach (ComListener listener in this.listeners) {

                listener.onResponse(response);
            }
        }

    }

    /// <summary>
    /// This class is used to abstract CR95 Rfid badge reader
    /// </summary>
    /// <author>J.FARGEON</author>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class CR95 : RfidDevice, SustainableRfidDevice
    {

        private LogProducer logProducer = new LogProducer(typeof(CR95));

        // singleton instance.
        private static CR95 instance = null;

        // critical section object locker
        private static readonly Object locker = new Object();

        // STA Threads management
        private static Dispatcher dispatcher = null;

        private static String comPort = "";
        private static CR95ComHandler comHandler = null;
        private String type = "CR95";

        private CR95FrameRebuilder frameBuilder = null;

        private bool IsListenning { get; set; }

        private String CR95_COM_PORT = "COM10";
        private int COM_SEEKING_TIMEOUT = 10000;
        private int REQUEST_TIMEOUT = 800;

        private string VENDOR_ID = "VID_0403";
        private string PRODUCT_ID = "PID_6001";
        private string MANUFACTURER_NAME = "FTDI";

        private ManualResetEvent manualEvent;

        private const int CR95_BAUDRATE = 57600;
        private const Parity CR95_PARITY = Parity.None;
        private const int CR95_DATABITS = 8;
        private const StopBits CR95_STOPBITS = StopBits.One;

        private static Dictionary<String, String> readerInfo = new Dictionary<string, string>();

        private static Thread comPortSeekingThreadHandler = null;
        // Connexion supervision
        private static System.Timers.Timer connectionSupervisor;
        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        private event EventHandler connectedEventHandler;
        private event EventHandler disconnectedEventHandler;
        private event EventHandler startReadingEventHandler;
        private event EventHandler stopReadingEventHandler;
        private event TagFoundEventHandler tagFoundEventHandler;

        //private event EventHandler modeChangedToAutoReadingEventHandler;
        //private event EventHandler modeChangedToCmdEventHandler;

        #endregion

        #region CONSTRUCTOR
        public CR95()
        {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            frameBuilder = new CR95FrameRebuilder();
            frameBuilder.TagDecoded += new TagDecodedEventHandler(frameBuilder_TagDecoded);
            frameBuilder.InfoDecoded += new InfoDecodedEventHandler(frameBuilder_InfoDecoded);

            getDefaultValuesFromConfiguration();

            if (String.IsNullOrEmpty(ComPort) && ComHandler == null)
            {
                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
            }

            logProducer.Logger.Debug("class loaded");
        }

        public static CR95 getInstance()
        {

            // critical section
            lock (locker)
            {

                if (instance == null)
                {
                    instance = new CR95();
                }
            }

            return instance;
        }


        public static CR95 removeInstance()
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

        #endregion

        #region PUBLIC_METHODS
        public static String ComPort
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

        public static CR95ComHandler ComHandler
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

        public void connect()
        {
            initializeComHandler();
            if (ComHandler != null) {
                ComHandler.connect();
            }
        }

        public void disconnect()
        {
            if (ComHandler != null)
            {
                ComHandler.disconnect();
                ComHandler = null;
            }
        }

        public void startScan()
        {
            lock(locker) {
                if (ComHandler != null) {
                    //Thread.Sleep(10000);
                    ComHandler.startlistening();
                }
                //ComHandler.sendData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SET_PROTOCOL_0x02.PROTOCOL, CR95Protocol.CMD.SET_PROTOCOL_0x02.ISO15693));
                Thread th = new Thread(new ThreadStart(startInventory));
                IsListenning = true;
                th.Start();
            }
        }

        private void startInventory()
        {
            byte[] gain = new byte[] { 0xD0, 0xD1, 0xD3, 0xD7, 0xDF };
            int nbgain = 0;

            while (IsListenning)
            {
                try
                {
                    if(comHandler == null || comHandler.UnexpectedDisconnectionFlag == true) {
                        Thread.Sleep(500);
                        logProducer.Logger.Error("The card reader CR95 is disconnect. Lets's try to reconnect");
                        //try to reconnect
                        disconnect();
                        initializeComPortSeekingThread();
                        startAndWaitComPortSeekingThread();
                        connect();
                        continue;
                    }
                    //Select ISO15693 protocol
                    //logProducer.Logger.Debug("cmd1");
                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SET_PROTOCOL_0x02.PROTOCOL, CR95Protocol.CMD.SET_PROTOCOL_0x02.ISO15693),100);
                    //logProducer.Logger.Debug("cmd2");
                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(0x09, new byte[] { 0x04, 0x68, 0x01, 0x01, 0xD7 }), 100);
                    //0xD1
                    //Launch ISO15693 inventory
                    //logProducer.Logger.Debug("cmd3");
                    ComHandler.sendData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, CR95Protocol.CMD.SendRecv_0x04.INVENTORY_ISO15693));
                    Thread.Sleep(50);

                    //Select ISO14443A Protocol
                    //logProducer.Logger.Debug("cmd4");
                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SET_PROTOCOL_0x02.PROTOCOL, CR95Protocol.CMD.SET_PROTOCOL_0x02.ISO14443),100);

                    //Adjust index and gain 
                    //logProducer.Logger.Debug("cmd5");
                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(0x09, new byte[] { 0x04, 0x3A, 0x00, 0x58, 0x04 }),100);
                    //logProducer.Logger.Debug("cmd6");
                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(0x09, new byte[] { 0x04, 0x68, 0x01, 0x01, 0xD7 }),100);//ComHandler.requestData(CR95FrameRebuilder.buildFrame(0x09, new byte[] { 0x04, 0x68, 0x01, 0x01, 0xD3 }),100);
                    //0xD7
                    //Check ISO14443A tag presence
                    //logProducer.Logger.Debug("cmd7");
                    byte[] present = ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, CR95Protocol.CMD.SendRecv_0x04.REQA_ISO14443),100);
                    if (present != null && present.Length == 7)
                    {
                        //Apply anti-collision
                        //logProducer.Logger.Debug("cmd8");
                        byte[] anticoll = ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, CR95Protocol.CMD.SendRecv_0x04.ANTICOLL_ISO14443),100);
                        if (anticoll.Length == 10)
                        {
                            byte[] frame = new byte[9];
                            for (int i = 1; i < 9; i++)
                                frame[i] = anticoll[i - 1];
                            frame[1] = 0x93;
                            frame[2] = 0x70;
                            frame[0] = 0x08;
                            //Select tag 
                            //logProducer.Logger.Debug("cmd9");
                            byte[] test = ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, frame),100);

                            //Check if other data are needed to read the complete UID
                            if (test.Length == 8 && present[3]!=0)
                            {
                                //Apply anti collision again
                                //logProducer.Logger.Debug("cmd10");
                                byte[] final = ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, CR95Protocol.CMD.SendRecv_0x04.ANTICOLL2_ISO14443),100);
                                Thread.Sleep(50);
                                if (final.Length == 10)
                                {
                                    for (int i = 1; i < 9; i++)
                                        frame[i] = anticoll[i - 1];
                                    frame[1] = 0x95;
                                    frame[2] = 0x70;
                                    frame[0] = 0x08;
                                    //Select second part of UID
                                    //logProducer.Logger.Debug("cmd11");
                                    ComHandler.requestData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, frame),100);
                                }
                            }
                        }
                    }
                    Thread.Sleep(100);
                    nbgain++;
                    if (nbgain == gain.Length)
                        nbgain = 0;
                }
                catch (Exception ex) {
                    logProducer.Logger.Error("Exception while reading tag, " + ex.Message);

                } 
            }

        }

        public void stopScan()
        {
            lock (locker) {
                IsListenning = false;
                Thread.Sleep(100); // wait for the wait of manualevent
                try
                {
                    ComHandler.sendData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SET_PROTOCOL_0x02.PROTOCOL, CR95Protocol.CMD.SET_PROTOCOL_0x02.OFF));
                    ComHandler.stopListening();
                }
                catch (Exception e)
                { }
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

        /*
        public void switchMode(MODE mode)
        {

            if (ComHandler != null
                && ComHandler.getState() == ComState.CONNECTED)
            {

                byte[] frame = null;

                switch (mode)
                {

                    case MODE.AUTOMATIC_READING:
                        frame = CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.MODE_0x14.MODE, CR95Protocol.CMD.MODE_0x14.AUTOMATIC_READING);
                        break;

                    case MODE.COMMAND:
                        frame = CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.MODE_0x14.MODE, CR95Protocol.CMD.MODE_0x14.COMMAND_MODE);
                        break;

                    default:
                        break;
                }

                if (frame != null)
                {
                    logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
                    ComHandler.sendData(frame);
                }
            }
        }
        */
        public void switchOffAntenna()
        {
            stopScan();
            //throw new NotImplementedException();
        }

        public void switchOnAntenna()
        {

            //throw new NotImplementedException();
            startScan();
        }

        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates()
        {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "CR95");

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
            else {
                componentHealthStates.Add("State", "disconnected");
            }
            return componentHealthStates;
        }

        #endregion
        #endregion

        #region CALLBACK_METHODS

        private void frameBuilder_TagDecoded(object sender, TagDecodedEventArgs e)
        {
            if (e.Snr.Length == 16)
            {
                List<String> snrs = new List<string>() { e.Snr };

                raiseTagFoundEvent(snrs);
            }
            else
            {
                /*
                Byte[] frame = toolbox.ConversionTool.stringToByteArray("9370"+e.Snr+"28");
                IsListenning = false;
                IsPartSnr = true;
                ComHandler.sendData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, frame));
                Thread.Sleep(50);
                ComHandler.sendData(CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.SendRecv_0x04.CMD, CR95Protocol.CMD.SendRecv_0x04.ANTICOLL2_ISO14443));
                Thread.Sleep(50);
                IsPartSnr = false;
                */

            }
        }

        private void frameBuilder_InfoDecoded(object sender, InfoDecodedEventArgs e)
        {

            readerInfo = e.Info;

            if (manualEvent != null)
            {
                Thread.Sleep(100); // it seeams if there is no tempo, the set is executed before the wait
                manualEvent.Set();
            }
        }
        /*
        void frameBuilder_ModeChangedToCommand(object sender, EventArgs e)
        {

            raiseEvent(modeChangedToCmdEventHandler);
        }

        void frameBuilder_ModeChangedToAutoReading(object sender, EventArgs e)
        {

            raiseEvent(modeChangedToAutoReadingEventHandler);
        }
        */
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
            raiseHasReportedAnErrorEvent("Unexpected Disconnection occurs");
        }

        private void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e)
        {
            raiseHasReportedAnErrorEvent(e.Message);
        }

        private void aComHandler_Disconnected(object sender, EventArgs e)
        {
            raiseEvent(disconnectedEventHandler);
        }

        private void aComHandler_Connected(object sender, EventArgs e)
        {
            raiseEvent(connectedEventHandler);
        }
        #endregion

        #region PRIVATE_METHODS

        private void getReaderInfo(ComHandler aComHandler = null)
        {

            byte[] frame = CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.WAKEUP_0x85.WAKEUP, CR95Protocol.CMD.WAKEUP_0x85.PARAMS);
            byte[] frame2 = CR95FrameRebuilder.buildFrame(CR95Protocol.CMD.GET_READER_INFO_0x01.GET_READER_INFO, CR95Protocol.CMD.GET_READER_INFO_0x01.PARAMS);

            if (aComHandler == null)
            {

                aComHandler = ComHandler;
            }
            //logProducer.Logger.Debug("Port " + ComPort + ": " + ConversionTool.byteArrayToString(frame));
            aComHandler.requestData(frame,100);
            aComHandler.requestData(frame2,100);
        }

        protected void seekLegicComPort()
        {

            CR95ComHandler tempSerialHandler = null;
            List<String> portNames = new List<String>();

            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(PRODUCT_ID))
                        portNames.Add(entry.Key);
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
                    tempSerialHandler = new CR95ComHandler(portname, CR95_BAUDRATE, CR95_PARITY, CR95_DATABITS, CR95_STOPBITS);
                    tempSerialHandler.Dispatcher = null;
                    tempSerialHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);

                    if (tempSerialHandler.getState() == ComState.CONNECTED)
                    {

                        logProducer.Logger.Debug(portname + " is already opened, skip it and switch to the following");
                        continue;
                    }

                    // let's connect
                    tempSerialHandler.connect();
                    tempSerialHandler.startlistening();

                    // check if serial is opened 
                    if (tempSerialHandler.getState() != ComState.CONNECTED)
                    {
                        logProducer.Logger.Debug("unable to open " + portname + ", skip it and switch to the following");
                        continue;
                    }

                    // send HARD_VERSIONS command over serial com and wait response
                    logProducer.Logger.Debug("Ok, " + portname + " is opened, let's send get_reader_info command over serial com and wait response");

                    // reset manual event
                    manualEvent.Reset();
                    Task t = Task.Factory.StartNew(() => {
                        getReaderInfo(tempSerialHandler);
                    });
                    // Wait for response (re-sync)
                    manualEvent.WaitOne(REQUEST_TIMEOUT);

                    // check if version retrieved

                    if (ReaderInfo.Count > 0)
                    {
                        logProducer.Logger.Debug("Reader info received");
                        // ok 
                        ComPort = portname;

                        // stop listening and unplug listener
                        if (tempSerialHandler != null)
                        {
                            tempSerialHandler.OnDataReceived -= new OnDataReceivedEventHandler(aComHandler_OnDatareceived);
                            ComHandler = tempSerialHandler;
                        }
                        break;

                    }
                    else {
                        // stop listening and unplug listener
                        if (tempSerialHandler != null)
                        {
                            logProducer.Logger.Error("stopping listening, no version on port " + ComPort);
                            tempSerialHandler.stopListening();
                            tempSerialHandler.disconnect();
                            tempSerialHandler = null;
                        }
                        //No version retrieved
                        //rtz com port
                        ComPort = "";
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    //rtz com port
                    ComPort = "";

                    // stop listening and unplug listener
                    if (tempSerialHandler != null)
                    {
                        logProducer.Logger.Error("stopping listening, exception: " + ex.Message);
                        tempSerialHandler.stopListening();
                        tempSerialHandler.disconnect();
                        tempSerialHandler = null;
                    }

                    // it isn't possible to connect to this com port, continue
                    continue;
                }
            }
        }

        private void getDefaultValuesFromConfiguration()
        {

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["CR95_COM_PORT"]))
            {

                String comPortFromConfig = ConfigurationManager.AppSettings["CR95_COM_PORT"];

                if (!String.IsNullOrEmpty(comPortFromConfig))
                {

                    CR95_COM_PORT = comPortFromConfig;
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

            parameters.Add(ConfigurableReaderParameter.COM_PORT.ToString(), CR95_COM_PORT);

        }

        private void initializeComHandler()
        {

            String portName = (String)parameters[ConfigurableReaderParameter.COM_PORT.ToString()]; // from config or by default

            if (!String.IsNullOrEmpty(ComPort))
            { // com port is found automatically
                portName = ComPort;
                logProducer.Logger.Info("CR95 Serial communication port found : " + portName);
            }
            else {
                logProducer.Logger.Warn("Unable to seek CR95 serial communication port, let's use config or default value : " + portName);
                ComHandler = new CR95ComHandler(portName, CR95_BAUDRATE, CR95_PARITY, CR95_DATABITS, CR95_STOPBITS);
                ComHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);
            }

            ComPort = portName;
            if (comHandler != null) {
                ComHandler.Connected += new EventHandler(aComHandler_Connected);
                ComHandler.Disconnected += new EventHandler(aComHandler_Disconnected);
                ComHandler.HasReportedAComError += new HasReportedAComErrorEventHandler(aComHandler_HasReportedAComError);
                ComHandler.StartListening += new EventHandler(aComHandler_StartListening);
                ComHandler.UnexpectedDisconnection += new EventHandler(aComHandler_UnexpectedDisconnection);
                ComHandler.StoppedListening += new EventHandler(aComHandler_StoppedListening);
                ComHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);
                aComHandler_Connected(this, null);
            }
        }

        protected void raiseEvent(EventHandler eventhandler)
        {

            if (eventhandler != null)
            {
                //eventhandler(this, EventArgs.Empty);
                Dispatcher.Invoke((Action)(() => eventhandler(this, EventArgs.Empty)));
                //Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
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
            if (comPortSeekingThreadHandler == null)
            {

                comPortSeekingThreadHandler = new Thread(new ThreadStart(comPortSeekingThread));

                // set the apartement state of a thread before it is started
                comPortSeekingThreadHandler.SetApartmentState(ApartmentState.STA);
            }

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
                comPortSeekingThreadHandler = null;
            }
        }

        #endregion

        #region THREAD
        protected void comPortSeekingThread()
        {

            seekLegicComPort();
        }
        #endregion

        /*
        #region EVENT HANDLERS

        public delegate void InvokeManageConnection();
        protected void onConnectionSupervisorElapsed(object sender, ElapsedEventArgs e)
        {
            connectionSupervisor.Enabled = false;
            Dispatcher.BeginInvoke(new InvokeManageConnection(checkAndRestoreConnection));
            connectionSupervisor.Enabled = true;
        }

        protected void checkAndRestoreConnection()
        {
            if (comHandler != null)
            {
                ComState st = ComHandler.getState();
                // Check if disconnection occured
                if (st == ComState.DISCONNECTED)
                {
                    Thread.Sleep(3000);
                    logProducer.Logger.Error("Badge reader Connection lost");

                    ComHandler.disconnect();
                    ComHandler.connect();
                }

            }
        }

        #endregion
    */

        public enum MODE
        {
            AUTOMATIC_READING,
            COMMAND
        }

    }


}


