using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fr.nexess.toolbox.log;
using System.Windows.Threading;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox;
using fr.nexess.toolbox.comm;
using fr.nexess.toolbox.comm.serial;
using fr.nexess.toolbox.comm.eventHandler;

namespace fr.nexess.hao.rfid.device.elatec {
    public class TWN4ComHandler : SerialComHandler {

        private LogProducer logProducer = new LogProducer(typeof(TWN4ComHandler));

        public TWN4ComHandler(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) :
            base(portName, baudRate, parity, dataBits, stopBits) {

            mySerialPort.NewLine = "\r";
        }

        public override void sendData(byte[] msg) {

            if (mySerialPort == null || mySerialPort.IsOpen == false) {
                logProducer.Logger.Debug("Can't send data : [" + ConversionTool.byteArrayToString(msg) + "], the port \"" + mySerialPort.PortName + "\" is closed");
                return;
            }

            try {
                logProducer.Logger.Debug(ConversionTool.byteArrayToString(msg));
                mySerialPort.WriteLine(getStringfromByteArray(msg));

            } catch (Exception ex) {

                // fire event
                notifyError(ex.Message);
                // notify all listeners
                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
            }
        }

        public string sendDataAndWaitResponse(byte[] msg) {

            if (mySerialPort == null || mySerialPort.IsOpen == false) {
                logProducer.Logger.Debug("Can't send data : [" + ConversionTool.byteArrayToString(msg) + "], the port \"" + mySerialPort.PortName + "\" is closed");
                return "";
            }

            try {
                //logProducer.Logger.Debug(ConversionTool.byteArrayToString(msg));
                mySerialPort.WriteLine(getStringfromByteArray(msg));
                string response = mySerialPort.ReadLine();
                //logProducer.Logger.Debug(response);
                return response;
            }
            catch (Exception ex) {

                // fire event
                notifyError(ex.Message);
                // notify all listeners
                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
            }
            return "";
        }

        protected override void onDataReceived(Object sender, SerialDataReceivedEventArgs e) {

            // Nothing to do as 
            return;
            /*
            SerialPort aSerialPort = (SerialPort)sender;
            if (aSerialPort == null || aSerialPort.IsOpen == false) {
                return;
            }

            try {

                // copy received data into the buffer
                string line = aSerialPort.ReadLine();
                byte[] buffer = new byte[line.Length / 2];
                // following proces is for synchronous needs...
                lock (locker) {// enter into the critical section
                    
                    for (int i = 0; i < (buffer.Length); i++) {
                        // Convert PRS Chars to byte array buffer
                        buffer[i] = byte.Parse(line.Substring((i * 2), 2), NumberStyles.HexNumber);
                    }
                    // copy buffer to the shared one
                    sharedResponse = new Byte[buffer.Length];
                    Buffer.BlockCopy(buffer, 0, sharedResponse, 0, buffer.Length);
                }

                // Signal response is available
                manualEvent.Set();

                // notify all listeners (switch to the main thread)
                fireResponseReceived(buffer);

            }
            catch (Exception ex) {

                raiseResponseError(ex.Message);
            }
            */
        }

        protected override void fireResponseReceived(byte[] response) {
            // too many logs
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(response));
            notifyDataReceived(response);
            foreach (ComListener listener in this.listeners) {
                listener.onResponse(response);
            }
        }

        private string getStringfromByteArray(byte[] bytes) {

            if (bytes.Length < 1) {
                return null;
            }
            string buffer = null;
            for (int i = 0; i < bytes.Length; i++) {
                // convert byte to characters
                buffer = buffer + bytes[i].ToString("X2");
            }
            return buffer;
        }
    }

    public class Twn4 : RfidDevice, SustainableRfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(Twn4));

        // singleton instance.
        private static Twn4 instance = null;
        protected static readonly Object locker = new Object();
        private static Dispatcher dispatcher = null;
        private ManualResetEvent manualEvent;

        // reader
        protected static RfidDeviceState state = RfidDeviceState.DISCONNECTED;
        private string comPort = "";
        private String type = "TWN4";
        private static TWN4ComHandler comHandler = null;
        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();
        private const int BAUDRATE = 115200;
        private const Parity PARITY = Parity.None;
        private const int DATABITS = 8;
        private const StopBits STOPBITS = StopBits.One;
        private static Thread comPortSeekingThreadHandler = null;

        private string VENDOR_ID = "VID_09D8";
        private string PRODUCT_ID = "PID_0420";
        private string MANUFACTURER_NAME = "USB";

        // requests
        private String TWN4_COM_PORT = "COM20";
        private int COM_SEEKING_TIMEOUT = 10000;
        private int REQUEST_TIMEOUT = 800;
        private static Dictionary<String, String> readerInfo = new Dictionary<string, string>();
        private Twn4FrameRebuilder frameBuilder = null;

        // scan
        private bool IsListening { get; set; }
        private string tagDetected = "";
        string lastBuffer = "INIT";

        public enum TAG_TYPE {
            ALL,
            HF_ISO15693,
            HF_ISO14443,
            HF_LEGIC,
            HF_HIDICLASS,
            LF_EM4, // EM4 102 150 026 and 305
            LF_HITAG // HITAG2 HITAG1S
        };

        #region EVENT_HANDLER_DECLARATION
        protected static event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        protected static event EventHandler connectedEventHandler;
        protected static event EventHandler disconnectedEventHandler;
        protected static event EventHandler startReadingEventHandler;
        protected static event EventHandler stopReadingEventHandler;
        protected static event TagFoundEventHandler tagFoundEventHandler;
        #endregion

        #region CONSTRUCTOR
        public static Twn4 getInstance() {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new Twn4();
                }
            }

            return instance;
        }

        public Twn4() {
            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            frameBuilder = new Twn4FrameRebuilder();
            frameBuilder.TagDecoded += new TagDecodedEventHandler(onTagDecoded);
            frameBuilder.InfoDecoded += new InfoDecodedEventHandler(onInfoDecoded);

            if (String.IsNullOrEmpty(ComPort) && ComHandler == null) {
                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
            }
            logProducer.Logger.Debug("class loaded");
        }
        #endregion

        #region GETTER SETTER

        protected string ComPort {
            get {
                return comPort;
            }

            set {
                comPort = value;
            }
        }

        protected static TWN4ComHandler ComHandler {
            get {
                return comHandler;
            }

            set {
                comHandler = value;
            }
        }

        protected static Dispatcher Dispatcher {
            get {
                return dispatcher;
            }

            set {
                dispatcher = value;
            }
        }

        public static Dictionary<string, string> ReaderInfo {
            get {
                return readerInfo;
            }

            set {
                readerInfo = value;
            }
        }

        public string TagDetected {
            get {
                return tagDetected;
            }
            set {
                lock (locker) {
                    tagDetected = value;
                }
            }
        }
        #endregion

        #region PRIVATE
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

        protected void comPortSeekingThread() {

            seekComPort();
        }

        private void initializeComHandler() {

            String portName = TWN4_COM_PORT;

            if (!String.IsNullOrEmpty(ComPort)) { // com port is found automatically
                portName = ComPort;
                logProducer.Logger.Info("TWN4 Serial communication port found : " + portName);
            } else {
                logProducer.Logger.Warn("Unable to seek TWN4 serial communication port, let's use config or default value : " + portName);
                ComHandler = new TWN4ComHandler(portName, BAUDRATE, PARITY, DATABITS, STOPBITS);
                ComHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);
            }

            ComPort = portName;
            if (comHandler != null) {
                sendSetTagType(Twn4Protocol.CMD.SetTagType);
                //sendInitLeds();
                //
                ComHandler.Connected += new EventHandler(aComHandler_Connected);
                ComHandler.Disconnected += new EventHandler(aComHandler_Disconnected);
                ComHandler.HasReportedAComError += new HasReportedAComErrorEventHandler(aComHandler_HasReportedAComError);
                ComHandler.StartListening += new EventHandler(aComHandler_StartListening);
                ComHandler.UnexpectedDisconnection += new EventHandler(aComHandler_UnexpectedDisconnection);
                ComHandler.StoppedListening += new EventHandler(aComHandler_StoppedListening);
                aComHandler_Connected(this, null);
            }
        }

        private void sendSetTagType(byte[] typeFrame) {
            // set tag type
            //Select ISO1569, ISO14443A Protocol
            byte[] frameSetTagType = Twn4FrameRebuilder.buildFrame(typeFrame, new byte[0]);
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frameSetTagType));
            lock (locker) {
                Thread.Sleep(200);
                comHandler.sendData(frameSetTagType);
            }
        }

        private void sendInitLeds() {

            byte[] frameLedInit = Twn4FrameRebuilder.buildFrame(Twn4Protocol.CMD.LEDInit, new byte[0]);
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frameLedInit));

            byte[] frameLedOn = Twn4FrameRebuilder.buildFrame(Twn4Protocol.CMD.LEDOnGreen, new byte[0]);
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frameLedOn));

            lock (locker) {
                Thread.Sleep(200);
                comHandler.sendDataAndWaitResponse(frameLedInit);
                Thread.Sleep(200);
                comHandler.sendDataAndWaitResponse(frameLedOn);
            }
        }

        private void sendBeep() {
            byte[] frameBeep = Twn4FrameRebuilder.buildFrame(Twn4Protocol.CMD.Beep, new byte[0]);
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frameBeep));

            lock (locker) {
                Thread.Sleep(200);
                string result = comHandler.sendDataAndWaitResponse(frameBeep);
            }
        }

        private void sendGetReaderInfo(ComHandler aComHandler = null) {

            byte[] frame = Twn4FrameRebuilder.buildFrame(Twn4Protocol.CMD.GetVersionString, new byte[0]);

            if (aComHandler == null) {
                aComHandler = ComHandler;
            }
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
            lock (locker) {
                Thread.Sleep(200);
                string result = ((TWN4ComHandler)aComHandler).sendDataAndWaitResponse(frame);
                frameBuilder.rebuildFrames(result);
            }
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

        protected void raiseEvent(EventHandler eventhandler) {

            if (eventhandler != null) {
                Dispatcher.Invoke((Action)(() => eventhandler(this, EventArgs.Empty)));
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

        protected void seekComPort() {

            TWN4ComHandler tempSerialHandler = null;
            List<String> portNames = new List<String>();

            try {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts) {
                    if (entry.Value.Contains(MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(PRODUCT_ID))
                        portNames.Add(entry.Key);
                }
            } catch (Exception ex) {

                logProducer.Logger.Error("Unable to seek Com Port, because : " + ex.Message);
                return;
            }


            // extract com ids
            foreach (String portname in portNames) {

                try {

                    // retrieve serial com handler and associated event...
                    tempSerialHandler = new TWN4ComHandler(portname, BAUDRATE, PARITY, DATABITS, STOPBITS);
                    tempSerialHandler.Dispatcher = null;
                    //tempSerialHandler.OnDataReceived += new OnDataReceivedEventHandler(aComHandler_OnDatareceived);

                    if (tempSerialHandler.getState() == ComState.CONNECTED) {

                        logProducer.Logger.Debug(portname + " is already opened, skip it and switch to the following");
                        continue;
                    }

                    // let's connect
                    tempSerialHandler.connect();
                    tempSerialHandler.startlistening();

                    // check if serial is opened 
                    if (tempSerialHandler.getState() != ComState.CONNECTED) {
                        logProducer.Logger.Debug("unable to open " + portname + ", skip it and switch to the following");
                        continue;
                    }

                    // send HARD_VERSIONS command over serial com and wait response
                    logProducer.Logger.Debug("Ok, " + portname + " is opened, let's send get_reader_info command over serial com and wait response");

                    // reset manual event
                    manualEvent.Reset();
                    sendGetReaderInfo(tempSerialHandler);
                    // Wait for response (re-sync)
                    manualEvent.WaitOne(REQUEST_TIMEOUT);

                    // check if version retrieved
                    if (ReaderInfo.Count > 0) {
                        logProducer.Logger.Debug("Reader info received");
                        // ok 
                        ComPort = portname;

                        // stop listening and unplug listener
                        if (tempSerialHandler != null) {
                            ComHandler = tempSerialHandler;
                        }
                        break;

                    } else {
                        // stop listening and unplug listener
                        if (tempSerialHandler != null) {
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
                } catch (Exception ex) {
                    //rtz com port
                    ComPort = "";

                    // stop listening and unplug listener
                    if (tempSerialHandler != null) {
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

        #endregion

        #region PUBLIC
        public void startScan() {
            TagDetected = "";
            if (comHandler != null) {
                ComHandler.startlistening();
            }
            Thread th = new Thread(new ThreadStart(startInventory));
            IsListening = true;
            th.Start();
        }

        private void startInventory() {

            while (IsListening) {
                try {
                    if (comHandler == null || comHandler.UnexpectedDisconnectionFlag == true) {
                        Thread.Sleep(500);
                        logProducer.Logger.Error("The card reader TWN4 is disconnected. Lets's try to reconnect");
                        //try to reconnect
                        disconnect();
                        initializeComPortSeekingThread();
                        startAndWaitComPortSeekingThread();
                        connect();
                        continue;
                    }
                    byte[] frameSearch = Twn4FrameRebuilder.buildFrame(Twn4Protocol.CMD.SearchTag, new byte[0]);
                    Thread.Sleep(200);
                    lock (locker) {
                        string buffer = comHandler.sendDataAndWaitResponse(frameSearch);
                        if (buffer != lastBuffer && buffer != "0000") {
                            frameBuilder.rebuildFrames(buffer);
                        }
                        lastBuffer = buffer;
                    }
                }
                catch (Exception ex) {
                    logProducer.Logger.Error("Exception while reading tag, " + ex.Message);
                }
            }
        }
    

        public void stopScan() {
            //lock (locker) {
            IsListening = false;
            try {
                Thread.Sleep(200);
                ComHandler.stopListening();
                }
            catch (Exception e) { }
            //}
        }

        public RfidDeviceState getReaderState() {
            return state;
        }

        public void connect() {
            Thread.Sleep(200);
            initializeComHandler();
            if (ComHandler != null) {
                ComHandler.connect();
            }
        }

        public void disconnect() {
            if (ComHandler != null) {
                ComHandler.disconnect();
                ComHandler = null;
            }
        }

        public void changeTagType(TAG_TYPE type) {
            switch(type) {
                case TAG_TYPE.ALL:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType);
                    break;
                case TAG_TYPE.HF_ISO15693:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_ISO15693);
                    break;
                case TAG_TYPE.HF_ISO14443:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_14443);
                    break;
                case TAG_TYPE.HF_LEGIC:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_LEGIC);
                    break;
                case TAG_TYPE.HF_HIDICLASS:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_HIDICLASS);
                    break;
                case TAG_TYPE.LF_EM4:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_LF_EM4);
                    break;
                case TAG_TYPE.LF_HITAG:
                    sendSetTagType(Twn4Protocol.CMD.SetTagType_LF_HITAG);
                    break;
            }
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, ComPort, getReaderState());
            return readerInfo.InfoList;
        }

        public string getReaderComPort() {
            return comPort;
        }

        public string getReaderUUID() {
            throw new NotImplementedException();
        }

        public String getReaderSwVersion() {
            throw new NotImplementedException();
        }

        public bool transferCfgFile(string fileName) {
            throw new NotImplementedException();
        }

        public void switchOffAntenna() {
            throw new NotImplementedException();
        }

        public void switchOnAntenna() {
            startScan();
        }
        #endregion

        #region EVENT HANDLER
        private void onTagDecoded(object sender, TagDecodedEventArgs e) {

            List<String> snrs = new List<string>() { e.Snr };
            // send beep sound
            /*stopScan();
            sendBeep();
            Thread.Sleep(4000);
            startScan();*/
            if (snrs != null && snrs.Count>=1) {
                tagDetected = snrs[0];            
                raiseTagFoundEvent(snrs);
            }
        }

        private void onInfoDecoded(object sender, InfoDecodedEventArgs e) {

            readerInfo = e.Info;

            if (manualEvent != null) {
                Thread.Sleep(200); // wait for the wait of manualevent
                logProducer.Logger.Info(e.Info.Keys.First() + ": " + e.Info.Values.First());
                manualEvent.Set();
            }
        }

        private void aComHandler_OnDatareceived(object sender, OnDataReceivedEventArgs e) {

            frameBuilder.rebuildFrames(e.DataAsString);
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

            componentHealthStates.Add("Reader type", "TWN4");

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
    }
}
