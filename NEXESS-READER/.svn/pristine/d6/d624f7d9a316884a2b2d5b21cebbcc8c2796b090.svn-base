using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;
using System.IO.Ports;
using fr.nexess.toolbox.comm;
using System.Threading;
using System.Windows.Threading;
using fr.nexess.toolbox.comm.serial;
using fr.nexess.toolbox.comm.eventHandler;
using fr.nexess.hao.reader.optic.eventhandler;
using fr.nexess.toolbox;

namespace fr.nexess.hao.reader.optic.device.opticon {

    public class NLV3101 : OpticReader, ComListener {

        private LogProducer logProducer = new LogProducer(typeof(NLV3101));

        // critical section object locker
        private static readonly Object locker = new Object();

        // singleton instance
        protected static NLV3101 instance = null;

        // default value
        protected static String comPort = "COM2";
        protected int scanDelay = 200;

        // fixed value
        protected const int BAUDRATE = 9600;
        protected const Parity PARITY = Parity.None;
        protected const int DATABITS = 8;
        protected const StopBits STOPBITS = StopBits.One;

        protected static ComHandler comHandler = null;

        protected int MAX_READING_DURATION = 3000;

        // re-sync request response mechanism class members
        protected ManualResetEvent manualEvent;
        protected Byte[] sharedResponse = null;
        private bool IsListenning { get; set; }
        private bool IsReading { get; set; }

        private static Thread opticalReadingThreadHandler = null;

        #region EVENT_HANDLER_DECLARATION
        protected event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        protected event EventHandler                      connectedEventHandler;
        protected event EventHandler                      disconnectedEventHandler;
        protected event EventHandler                      startReadingEventHandler;
        protected event EventHandler                      stopReadingEventHandler;
        protected event ReportAfterOpticalReadingEventHandler    reportAfterReadingEventHandler;
        #endregion

        // STA Threads management
        public Dispatcher Dispatcher { get; set; }

        byte[] fullFrame = new byte[128];
        int fullFrameLength = 0;

        #region CONSTRUCTOR_DESTRUCTOR

        public static NLV3101 getInstance() {

            // critical section
            lock (locker) {
                if (instance == null) {
                    instance = new NLV3101();
                }
            }

            return instance;
        }

        protected NLV3101() {

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {

                // force thread apartment to be a Single Thread Apartment (as the current)
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }

            // Multiple threads management
            Dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            // internal event notification
            manualEvent = new ManualResetEvent(false);

            initializeComHandler();
        }

        ~NLV3101() {

            if (ComHandler != null) {
                // communication disconnection
                ComHandler.disconnect();
                ComHandler.stopListening();

                instance = null;
            }
        }
        #endregion

        #region PUBLIC_METHODS
        public static ComHandler ComHandler {
            get {
                return comHandler;
            }
            set {
                comHandler = value;
            }
        }

        public void connect() {

            if (ComHandler == null) {

                initializeComHandler();
            }

            if (ComHandler != null) {

                ComHandler.connect();
            }

            ComHandler.startlistening();
        }

        public void disconnect() {

            if (ComHandler != null) {

                ComHandler.stopListening();
                IsListenning = false;

                ComHandler.disconnect();
            }
        }

        public void startScan() {

            if (IsConnected && !IsReading) {

                initializeOpticalReadingThread();

                if (opticalReadingThreadHandler != null && !opticalReadingThreadHandler.IsAlive) {

                    IsReading = true;
                    raiseEvent(startReadingEventHandler);

                    opticalReadingThreadHandler.Start();
                }
            }
        }

        public void stopScan() {

            if (IsConnected && IsReading) {

                IsReading = false;

                sendCmdData(NLV3101Protocol.COMMAND.DETRIGGER_READER_0x59.DETRIGGER_READER);

                raiseEvent(stopReadingEventHandler);

                if (opticalReadingThreadHandler != null) {

                    opticalReadingThreadHandler.Abort();
                    opticalReadingThreadHandler = null;
                }
            }
        }

        public ReaderState getReaderState() {

            ReaderState state = ReaderState.DISCONNECTED;

            if (ComHandler != null) {

                switch (ComHandler.getState()) {

                    case ComState.CONNECTED:
                        state = ReaderState.CONNECTED;

                        if (IsReading) {
                            state = ReaderState.READING;
                        }
                        break;

                    case ComState.DISCONNECTED:
                        state = ReaderState.DISCONNECTED;
                        break;

                    default:
                        break;
                }
            }

            return state;
        }

        public bool IsConnected {
            get {
                if (ComHandler.getState() == ComState.CONNECTED) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        /// <summary>
        /// serial com port (ex : "COM2")
        /// </summary>
        public static String ComPort {

            get { return comPort; }
            set { comPort = value; }
        }
        #endregion

        #region READER_EVENT_PROVIDER_IMPL
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
        public event ReportAfterOpticalReadingEventHandler ReportAfterReading {
            add {
                lock (locker) {
                    reportAfterReadingEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    reportAfterReadingEventHandler -= value;
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS

        /// <summary>
        /// Data to retrieve in synchronous commmunication mode
        /// </summary>
        protected Byte[] cutNCopySharedResponse() {

            Byte[] data = null;

            lock (locker) {// enter into the critical section

                if (sharedResponse != null) {

                    data = sharedResponse;

                    // rtz shared response
                    sharedResponse = null;
                }
            }
            return data;
        }

        /// <summary>
        /// Start to monitor the com handler
        /// </summary>
        protected void initializeComHandler() {

            if (ComHandler == null) {

                // retrieve serial com handler and associated event...
                ComHandler = getComHandler();

                if (ComHandler == null) {
                    throw new IoComException("getCabinetComHandler method calling failed. Unable getting access througth the cabinet handler");
                }
            }

            ComHandler.Connected += new EventHandler(aComHandler_Connected);
            ComHandler.Disconnected += new EventHandler(aComHandler_Disconnected);
            ComHandler.HasReportedAComError += new HasReportedAComErrorEventHandler(aComHandler_HasReportedAComError);
            ComHandler.UnexpectedDisconnection += new EventHandler(aComHandler_UnexpectedDisconnection);
        }

        /// <summary>
        /// select Com port associated to device
        /// </summary>
        protected SerialComHandler getComHandler() {

            logProducer.Logger.Debug("open port Com");

            SerialComHandler aSerialHandler = null;

            string port = "";

            try {

                port = getOpticalComPortName(port);
                if(string.IsNullOrEmpty(port)) {
                    logProducer.Logger.Error("The optical reader port com is not found");
                    throw (new Exception("The optical reader port com is not found"));
                }

                // retrieve serial com handler and associated event...
                aSerialHandler = new SerialComHandler(port, BAUDRATE, PARITY, DATABITS, STOPBITS);

                if (((ComHandler)aSerialHandler).getState() == ComState.CONNECTED) {

                    logProducer.Logger.Debug("port : " + port.ToString() + " is already opened, skip it and switch to the following");
                    throw new Exception();
                }
            } catch (Exception ex) {

                throw ex;
            }

            ((ComListenerDependent)aSerialHandler).addListener(this);

            return aSerialHandler;
        }

        private string getOpticalComPortName(string port) {

            foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
            {
                if (entry.Value.Contains(NLV3101Protocol.DEVICE_PROVIDER) || entry.Value.Contains(NLV3101Protocol.DEVICE_VID))
                    port=entry.Key;
            }
            return port;
        }

        /// <summary>
        /// send a command to the device
        /// </summary>
        protected void sendCmdData(byte[] cmdData) {

            if (ComHandler != null && ComHandler.getState() == ComState.CONNECTED) {
                logProducer.Logger.Debug(ConversionTool.byteArrayToString(cmdData));
                ComHandler.sendData(cmdData);
            }
        }

        #region SERIAL_COM_EVENT_CALLBACK_METHODS

        public void onComEvent(ComEvent comEvent, string msg) {
            // nothing todo
            logProducer.Logger.Debug("com Event : " + comEvent.ToString() + " - " + msg);
        }

        public void onResponse(byte[] response) {

            logProducer.Logger.Debug("On response!, let's extract Data Frame And Raise ManualEvent");
            logProducer.Logger.Debug(response);
            syncFrame(response);

            //foreach (byte[] syncResp in syncFrame(response)) {


            //    //raiseReportAfterReadingEvent
            //}
        }

        public void onResponse(string response) {
            // nothing todo
        }

        protected void aComHandler_UnexpectedDisconnection(object sender, EventArgs e) {
            raiseHasReportedAnErrorEvent("Unexpected Disconnection occurs");
        }

        protected void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e) {
            raiseHasReportedAnErrorEvent(e.Message);
        }

        protected void aComHandler_Disconnected(object sender, EventArgs e) {
            raiseEvent(disconnectedEventHandler);
        }

        protected void aComHandler_Connected(object sender, EventArgs e) {
            raiseEvent(connectedEventHandler);
        }
        #endregion

        protected void raiseEvent(EventHandler eventhandler) {

            if (eventhandler != null) {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
            }
        }

        protected void raiseHasReportedAnErrorEvent(String issue, Exception ex = null) {

            if (hasReportedAnErrorEventHandler != null) {

                Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(issue, ex))));
            }
        }

        protected void raiseReportAfterReadingEvent(List<String> captions) {

            if (reportAfterReadingEventHandler != null) {

                Dispatcher.BeginInvoke((Action)(() => reportAfterReadingEventHandler(this, new ReportAfterOpticalReadingEventArgs(captions))));
            }
        }

        List<byte[]> syncFrame(byte[] response) {

            System.Buffer.BlockCopy(response,
                                       0,
                                       fullFrame,
                                       fullFrameLength,
                                       response.Length);

            fullFrameLength += response.Length;
            List<byte[]> extractResponseList = new List<byte[]>();

            bool flagEnd = false;

            // while there are frame to process
            while (!flagEnd) {

                flagEnd = true;
                int endPosition=0;

                for (int i = 1; i < fullFrameLength; i++) {

                    if (fullFrame[i] == NLV3101Protocol.FIXED.END) {

                        flagEnd = false;
                        endPosition = i;
                    }
                }

                if (!flagEnd) {

                    //Seems to be a valid frame
                    byte[] extractResponse = new byte[endPosition + 1];
                    System.Buffer.BlockCopy(fullFrame,
                                           0,
                                           extractResponse,
                                           0,

                                           extractResponse.Length);
                    extractResponseList.Add(extractResponse);

                    for (int i = 0; i < fullFrame.Length; i++) {
                        if ((i) < fullFrameLength)
                            fullFrame[i] = fullFrame[i + endPosition + 1];
                        else
                            fullFrame[i] = 0;
                    }

                    fullFrameLength -= endPosition + 1;

                    setSharedResponse(extractResponse);

                    // Signal response is available
                    manualEvent.Set();

                } else if (endPosition > NLV3101Protocol.MAX_FRAME_LENGTH) { //not valid frame

                    for (int i = 0; i < fullFrame.Length; i++) {
                        fullFrame[i] = 0;
                    }

                    fullFrameLength = 0;
                }

            }

            return extractResponseList;
        }

        private void setSharedResponse(byte[] byteFrame) {

            lock (locker) {// enter into the critical section

                // copy buffer to the shared one
                sharedResponse = new Byte[byteFrame.Length];
                Buffer.BlockCopy(byteFrame, 0, sharedResponse, 0, byteFrame.Length);
            }
        }

        #region THREAD

        protected void initializeOpticalReadingThread() {

            if (opticalReadingThreadHandler != null) {

                opticalReadingThreadHandler.Abort();
                opticalReadingThreadHandler = null;
            }

            opticalReadingThreadHandler = new Thread(new ThreadStart(opticalReadingThread));
            //weightReadingThreadHandler.SetApartmentState(ApartmentState.STA);

            opticalReadingThreadHandler.IsBackground = true;
        }

        /// <summary>
        /// tag Reading Thread : start inventory and read tag from Ru-865.
        /// </summary>
        protected void opticalReadingThread() {

            //switch (ScanMode) {

            // case SCAN_MODE.CONTINUOUS_SCAN:
            // performContinuousScan();
            //break;
            //case SCAN_MODE.INVENTORY_SCAN:
            // default:
            performTriggeredScan();


            //}
        }
        #endregion

        private void performTriggeredScan() {

            List<String> catchedString = new List<String>();

            Boolean gotSomething = false;


            sendCmdData(NLV3101Protocol.COMMAND.TRIGGER_READER_0x5A.TRIGGER_READER);


            do {

                // Wait for response (re-sync)
                manualEvent.WaitOne(300);

                Byte[] readerResponse = cutNCopySharedResponse();

                if (readerResponse != null) {

                    String response = System.Text.Encoding.UTF8.GetString(readerResponse);

                    response = response.Replace("\n", "").Replace("\r", "");

                    if (!String.IsNullOrEmpty(response)) {

                        catchedString.Add(response);
                        response = "";

                        gotSomething = true;
                    }
                }

                // rest for a moment...
                System.Threading.Thread.Sleep(scanDelay);

            } while (gotSomething == false);

            raiseReportAfterReadingEvent(catchedString);

            stopScan();
        }
        #endregion
    }

    class NLV3101Protocol {
        internal const int MAX_FRAME_LENGTH = 35;
        internal const string DEVICE_PROVIDER = "Opticon";
        internal const string DEVICE_VID = "065A";
        internal struct FIXED {
            internal const int HEAD = 0x1B;
            internal const int END = 0x0D;
        }
        internal struct SIZE {
            internal const int HEAD_SIZE = 1;
            internal const int END_SIZE = 1;
        }
        internal struct COMMAND {
            internal struct CHECK_COUNT_0x59 {
                // CMD [1 BYTE]
                internal static byte[] CHECK_COUNT = { 0x59, 0x56 };
            }
            internal struct TRIGGER_READER_0x5A {
                internal static byte[] TRIGGER_READER = { 0x1B, 0x5A, 0x0D };
            }
            internal struct DETRIGGER_READER_0x59 {
                internal static byte[] DETRIGGER_READER = { 0x1B, 0x59, 0x0D };
            }
        }
    }
}
