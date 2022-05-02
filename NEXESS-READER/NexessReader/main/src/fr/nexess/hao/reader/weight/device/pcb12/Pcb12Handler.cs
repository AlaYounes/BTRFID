using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;
using fr.nexess.hao.weight.eventhandler;
using fr.nexess.toolbox;
using fr.nexess.toolbox.comm;
using fr.nexess.toolbox.comm.eventHandler;
using fr.nexess.toolbox.comm.serial;
using fr.nexess.toolbox.log;
using fr.nexess.hao.reader.weight;
using fr.nexess.hao.reader;
using System.Management;

namespace fr.nexess.hao.weight.device.pcb12 {

    public class Pcb12Handler : WeightReader, ComListener {

        private LogProducer logProducer = new LogProducer(typeof(Pcb12Handler));

        // default value
        protected static String weightComPort = "COM2";
        protected int requestWeightTimeout = 200;

        // fixed value
        protected const int BAUDRATE = 9600;
        protected const Parity PARITY = Parity.None;
        protected const int DATABITS = 8;
        protected const StopBits STOPBITS = StopBits.One;

        protected int MAX_READING_DURATION = 3000;

        // singleton instance
        protected static Pcb12Handler instance = null;

        // critical section object locker
        private static readonly Object locker = new Object();
        private static readonly Object lockerBuffer = new Object();

        protected static ComHandler comHandler = null;        

        // re-sync request response mechanism class members
        protected ManualResetEvent manualEvent;
        protected Byte[] sharedResponse = null;
        private bool IsListenning { get; set; }
        private bool IsReading { get; set; }

        private static Thread weightReadingThreadHandler = null;

        private SCAN_MODE scanMode = SCAN_MODE.INVENTORY_SCAN;
        private RAW_WEIGHT_SHOWING_LEVEL rawWeightShowingLevel = RAW_WEIGHT_SHOWING_LEVEL.ALL_WEIGHTS;

        // STA Threads management
        public Dispatcher Dispatcher { get; set; }

        protected ArrayList managedBuffer = new ArrayList();
        protected int decodingAttempts = 0;

        private List<int> pcbAddresses = new List<int>();

        private string VENDOR_ID = "VID_0403";
        private string HUB_PRODUCT_ID = "PID_6015";
        private string HUB_MANUFACTURER_NAME = "FTDIBUS";

        #region EVENT_HANDLER_DECLARATION
        protected event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        protected event EventHandler                      connectedEventHandler;
        protected event EventHandler                      disconnectedEventHandler;
        protected event EventHandler                      startReadingEventHandler;
        protected event EventHandler                      stopReadingEventHandler;
        protected event ReportAfterReadingEventHandler    reportAfterReadingEventHandler;
        #endregion

        #region CONSTRUCTOR_DESTRUCTOR

        public static Pcb12Handler getInstance() {

            // critical section
            lock (locker) {
                if (instance == null) {
                    instance = new Pcb12Handler();
                }
            }

            return instance;
        }

        protected Pcb12Handler() {

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

        ~Pcb12Handler() {

            if (ComHandler != null) {
                // communication disconnection
                ComHandler.disconnect();
                ComHandler.stopListening();

                instance = null;
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
        public event ReportAfterReadingEventHandler ReportAfterReading {
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

        #region PUBLIC_METHODS

        public static ComHandler ComHandler {
            get {
                return comHandler;
            }
            set {
                comHandler = value;
            }
        }

        #region WEIGHT_READER_IMPL

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

                initializeWeightReadingThread();

                if (weightReadingThreadHandler != null && !weightReadingThreadHandler.IsAlive) {

                    IsReading = true;
                    raiseEvent(startReadingEventHandler);

                    weightReadingThreadHandler.Start();
                }
            }
        }

        public void stopScan() {

            if (IsConnected && IsReading) {

                IsReading = false;
                raiseEvent(stopReadingEventHandler);

                if (weightReadingThreadHandler != null) {

                    weightReadingThreadHandler.Abort();
                    weightReadingThreadHandler = null;
                }
            }
        }

        public ReaderState getReaderState() {

            ReaderState state = ReaderState.DISCONNECTED;

            if (ComHandler != null) {

                switch (ComHandler.getState()) {

                    case ComState.CONNECTED:
                        state = ReaderState.CONNECTED;

                        if (IsListenning) {
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

        #endregion


        /// <summary>
        /// serial com port (ex : "COM2")
        /// </summary>
        public static String WeightComPort {

            get { return weightComPort; }
            set { weightComPort = value; }
        }

        /// <summary>
        /// request weight timeout in millisecond
        /// </summary>
        public int RequestWeightTimeout {

            get { return requestWeightTimeout; }
            set { requestWeightTimeout = value; }
        }

        public List<int> PcbAddresses {
            get { return pcbAddresses; }
        }

        public SCAN_MODE ScanMode {
            get { return scanMode; }
            set { scanMode = value; }
        }

        public RAW_WEIGHT_SHOWING_LEVEL RawWeightShowingLevel {
            get { return rawWeightShowingLevel; }
            set { rawWeightShowingLevel = value; }
        }

        /// <summary>
        /// get all weights of a selected pcb
        /// </summary>
        public List<String> getPcbWeightsSync(int pcbAddr) {

            logProducer.Logger.Debug("get Pcb Weights Synchronously " + pcbAddr.ToString());

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeRequestAllWeightCmd(pcbAddr);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] pcbResponse = cutNCopySharedResponse();

            List<String> response = new List<String>();

            if (pcbResponse != null) {

                logProducer.Logger.Debug(System.Text.Encoding.Default.GetString(pcbResponse));

                for (int padId = 0; padId < Pcb12Protocol.NUMBER_OF_PAD_BY_CHANNEL; padId++) {

                    String smth = System.Text.Encoding.Default.GetString(pcbResponse).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE + Pcb12Protocol.SIZE.PAD_NUMBER_SIZE + (padId * Pcb12Protocol.SIZE.WEIGHT_ACQ_SIZE), Pcb12Protocol.SIZE.WEIGHT_ACQ_SIZE);
                    response.Add(smth);
                }
                return response;
            }
            else {
                logProducer.Logger.Error("pcbResponse is null");
                return null;
            }
        }

        /// <summary>
        /// get weight of seleted pad
        /// </summary>
        public string getOnePcbWeightSync(int pcbAddr, int padId) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeRequestWeightCmd(pcbAddr, padId);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }
        }

        /// <summary>
        /// Send Reset at Zero for selected pad
        /// </summary>
        public string setZeroScale(int pcbAddr, int padId) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeSetZeroScaleCmd(pcbAddr, padId);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }

        }

        /// <summary>
        /// Set accuracy and max load
        /// </summary>       
        public string setPadModel(int pcbAddr, int padId, int accuracy, int load) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeSetPadModelCmd(pcbAddr, padId, accuracy, load);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }

        }

        /// <summary>
        /// Set  weight to use for calibration
        /// </summary>
        public string setCalibrationWeight(int pcbAddr, int padId, int weight) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeSetCalibrationWeightCmd(pcbAddr, padId, weight);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }

        }

        /// <summary>
        /// Begins calibration procedure
        /// </summary>
        public string startCalibration(int pcbAddr, int padId) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeStartCalibrationCmd(pcbAddr, padId);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }
        }

        /// <summary>
        /// Sample deadload
        /// </summary>
        public string setEmptyCalibration(int pcbAddr, int padId) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeEmptyCalibrationCmd(pcbAddr, padId);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }
        }

        /// <summary>
        /// Sample load
        /// </summary>
        public string setLoadCalibration(int pcbAddr, int padId) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeSetLoadCalibrationCmd(pcbAddr, padId);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }

        }

        /// <summary>
        /// Force a specific adress. There must be only one PCB in the line !!!
        /// </summary>
        public string setAddress(int pcbAddr) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeSetAddressCmd(pcbAddr);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
            }
        }

        /// <summary>
        /// change a specific adress. Beware to not have 2 PCB withe same address
        /// </summary>
        public string changeAddress(int fromPcbAddr, int toPcbAddr) {

            // reset manual event
            manualEvent.Reset();

            Byte[] cmdData = Pcb12Protocol.computeChangeAddressCmd(fromPcbAddr, toPcbAddr);

            sendCmdData(cmdData);

            // Wait for response (re-sync)
            manualEvent.WaitOne(RequestWeightTimeout);

            Byte[] data = cutNCopySharedResponse();

            if (data == null)
                return "";
            else {
                return System.Text.Encoding.Default.GetString(data).Substring(Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE + Pcb12Protocol.SIZE.CMD_SIZE, data[1] - Pcb12Protocol.SIZE.CRC_SIZE - Pcb12Protocol.SIZE.END_SIZE - 1);
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
                ComHandler = getPcb12ComHandler(WeightComPort);

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
        protected SerialComHandler getPcb12ComHandler(string port) {

            logProducer.Logger.Debug("open port Com");

            // get all com port port name
            string[] portNames = SerialComHandler.Ports;
            SerialComHandler aSerialHandler = null;

            logProducer.Logger.Debug("SerialComHandler current thread ApartmentState : " + Thread.CurrentThread.GetApartmentState());

            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(HUB_MANUFACTURER_NAME) && entry.Value.Contains(VENDOR_ID) && entry.Value.Contains(HUB_PRODUCT_ID) && entry.Value.Contains("Port_#0006"))
                        port = entry.Key;
                }
            }
            catch (Exception) { }

            try {
                // retrieve serial com handler and associated event...
                aSerialHandler = new SerialComHandler(port, BAUDRATE, PARITY, DATABITS, STOPBITS);

                if (((ComHandler)aSerialHandler).getState() == ComState.CONNECTED) {
                    logProducer.Logger.Debug("port : " + port.ToString() + " is already opened, skip it and switch to the following");
                    throw new Exception();
                }

                // let's connect
                ((ComHandler)aSerialHandler).connect();
                ((ComHandler)aSerialHandler).startlistening();

                // check if serial is opened 
                if (((ComHandler)aSerialHandler).getState() != ComState.CONNECTED) {
                    logProducer.Logger.Debug("unable to open port : " + port.ToString() + ", skip it and switch to the following");
                    throw new Exception();
                }

            } catch (Exception ex) {
                logProducer.Logger.Error("Exception while getting Pcb12Handler, " + ex.Message);
                throw new Exception();
                // it isn't possible to connect to this com port, continue

            } finally {

                // stop listening and unplug listener
                if (aSerialHandler != null) {

                    ((ComHandler)aSerialHandler).stopListening();

                }
            }

            if (aSerialHandler != null
                && ((ComHandler)aSerialHandler).getState() == ComState.CONNECTED) {
                logProducer.Logger.Debug(" active pcb12 communication found!");
                ((ComHandler)aSerialHandler).disconnect();

                ((ComListenerDependent)aSerialHandler).addListener(this);
            }

            return aSerialHandler;
        }

        /// <summary>
        /// send a command to the device
        /// </summary>
        protected void sendCmdData(byte[] cmdData) {

            if (ComHandler != null && ComHandler.getState() == ComState.CONNECTED) {

                //logProducer.Logger.Debug("send command...");
                //logProducer.Logger.Debug(ConversionTool.byteArrayToString(cmdData));
                ComHandler.sendData(cmdData);
            }
        }

        #region SERIAL_COM_EVENT_CALLBACK_METHODS
        //protected void aComHandler_OnDatareceived(object sender, OnDataReceivedEventArgs e) {

        //    //Dispatcher.BeginInvoke((Action)(()=> decodeFrameAndRaiseEvent(e.DataAsBytes)));         
        //    //decodeFrameAndRaiseEvent(e.DataAsBytes);         

        //    //syncFrame(e.DataAsBytes);
        //}

        public void onComEvent(ComEvent comEvent, string msg) {
            // nothing todo
            logProducer.Logger.Debug("com Event : " + comEvent.ToString() + " - " + msg);
        }

        public void onResponse(byte[] response) {

            //logProducer.Logger.Debug("On response!, let's extract Data Frame And Raise ManualEvent");
            //logProducer.Logger.Debug(response);
            extractDataFrameAndRaiseManualEvent(response);
        }

        public void onResponse(string response) {
            // nothing todo
        }

        protected void aComHandler_UnexpectedDisconnection(object sender, EventArgs e) {
            logProducer.Logger.Warn("Event - Unexpected disconnection occured");
            raiseHasReportedAnErrorEvent("Unexpected Disconnection occurs");
        }

        protected void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e) {
            logProducer.Logger.Warn("Event - A COM error occured");
            raiseHasReportedAnErrorEvent(e.Message);
        }

        protected void aComHandler_Disconnected(object sender, EventArgs e) {
            logProducer.Logger.Warn("Event - A disconnection occured");
            raiseEvent(disconnectedEventHandler);
        }

        protected void aComHandler_Connected(object sender, EventArgs e) {
            logProducer.Logger.Warn("Event - Connection restored");
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

        //protected void raiseHasReportedAnErrorEvent(WeightReaderError error) {

        //    if (hasReportedAnErrorEventHandler != null) {

        //        Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(error))));
        //    }
        //}

        protected void raiseReportAfterReadingEvent(List<Tuple<String, String, String>> weights) {

            if (reportAfterReadingEventHandler != null) {

                Dispatcher.BeginInvoke((Action)(() => reportAfterReadingEventHandler(this, new ReportAfterReadingEventArgs(weights))));
            }
        }

        /// <summary>
        /// synchronise the com buffer to output only valid frames
        /// </summary>
        protected void extractDataFrameAndRaiseManualEvent(byte[] response) {

            lock(lockerBuffer) {
                managedBuffer.AddRange(response);
                //***
                List<byte[]> validFrames = splitBufferToValidFrames(ref managedBuffer);

                checkDecodingAttemptNumber(validFrames);

                foreach (byte[] byteFrame in validFrames) {

                    setSharedResponse(byteFrame);

                    // Signal response is available
                    manualEvent.Set();
                }
            }
        }

        private void setSharedResponse(byte[] byteFrame) {

            lock (locker) {// enter into the critical section

                // copy buffer to the shared one
                sharedResponse = new Byte[byteFrame.Length];
                Buffer.BlockCopy(byteFrame, 0, sharedResponse, 0, byteFrame.Length);
                logProducer.Logger.Info("***** " + BitConverter.ToString(sharedResponse));
            }
        }

        protected List<byte[]> splitBufferToValidFrames(ref ArrayList byteList) {

            //logProducer.Logger.Debug("enter splitBuffer");

            List<byte[]> validFrames = new List<byte[]>();

            if (byteList == null) {
                // nothing to do, return...
                logProducer.Logger.Debug("Buffer empty");
                return validFrames;
            }

            Byte[] rawDataBuffer = (byte[])byteList.ToArray(typeof(byte));

            int pos = 0;

            // don't stepping out of bounds !
            while (pos < rawDataBuffer.Length && rawDataBuffer.Length > (pos + Pcb12Protocol.SIZE.HEAD_SIZE)) {

                // retrieve length information 
                int lenght = rawDataBuffer[pos + Pcb12Protocol.SIZE.HEAD_SIZE];

                if (rawDataBuffer[pos] == Pcb12Protocol.FIXED.HEAD // HEAD OK
                    && lenght + Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE <= (rawDataBuffer.Length - pos) // LENGTH OK
                    && rawDataBuffer[pos + lenght + Pcb12Protocol.SIZE.CRC_SIZE] == Pcb12Protocol.FIXED.END) { // END OK

                    // extract frame

                    byte[] frame = new byte[lenght + Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE];

                    Buffer.BlockCopy(rawDataBuffer,     // src buffer
                                     pos,               // src offset
                                     frame,             // destination buffer
                                     0,                 // destination offset
                                     lenght + Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE);// count

                    // fill frame list
                    validFrames.Add(frame);

                    rawDataBuffer = unstackValidFrameFromBuffer(ref byteList, lenght + Pcb12Protocol.SIZE.HEAD_SIZE + Pcb12Protocol.SIZE.LENGTH_SIZE, pos);

                } else {

                    // no enought data
                    //logProducer.Logger.Info("raw data buffer contains some incomplete frames : " + ConversionTool.byteArrayToString(rawDataBuffer));
                    break;
                }
            }// end while

            if (validFrames.Count > 0 && rawDataBuffer.Length > 0) {

                clearUndecodableBytesFromManagedBuffer(ref byteList, rawDataBuffer.Length);
            }

            //logProducer.Logger.Debug("exit splitBuffer");
            return validFrames;
        }

        protected Byte[] unstackValidFrameFromBuffer(ref ArrayList buffer, int nbElementToRemove, int from) {

            buffer.RemoveRange(from, nbElementToRemove);

            return (byte[])buffer.ToArray(typeof(byte));
        }

        protected void clearUndecodableBytesFromManagedBuffer(ref ArrayList buffer, int nbElementToRemove) {

            if (nbElementToRemove > 0 && nbElementToRemove <= buffer.Count) {
                buffer.RemoveRange(0, nbElementToRemove);
            }
        }

        protected void checkDecodingAttemptNumber(List<byte[]> validFrames) {

            if (validFrames.Count == 0) {

                decodingAttempts++;

                if (decodingAttempts >= 255) {
                    lock(locker) {
                        managedBuffer.Clear();
                    }
                    decodingAttempts = 0;
                }
            } else {
                decodingAttempts = 0;
            }
        }

        #region THREAD

        protected void initializeWeightReadingThread() {

            if (weightReadingThreadHandler != null) {

                weightReadingThreadHandler.Abort();
                weightReadingThreadHandler = null;
            }

            weightReadingThreadHandler = new Thread(new ThreadStart(weightReadingThread));
            //weightReadingThreadHandler.SetApartmentState(ApartmentState.STA);

            weightReadingThreadHandler.IsBackground = true;
        }

        /// <summary>
        /// tag Reading Thread : start inventory and read tag from Ru-865.
        /// </summary>
        protected void weightReadingThread() {

            switch (ScanMode) {

                case SCAN_MODE.CONTINUOUS_SCAN:
                // performContinuousScan();
                //break;
                case SCAN_MODE.INVENTORY_SCAN:
                default:
                    performInventoryScan();
                    break;
            }
        }

        //protected void performContinuousScan() {

        //    while (true) {

        //        List<String> snrs = new List<String>();

        //        String snr;
        //        RU_RESULT res = unsafeStartTagInventory(out snr);

        //        if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

        //            raiseHasReportedAnErrorEvent("Unable to start tag inventory : " + res.ToString());
        //        }

        //        if (!String.IsNullOrEmpty(snr)) {

        //            snrs.Add(snr);
        //            snr = "";
        //        }

        //        do {

        //            res = unsafeReadNextTag(out snr);

        //            if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

        //                raiseHasReportedAnErrorEvent("Unable to read next tag : " + res.ToString());
        //            }

        //            if (!String.IsNullOrEmpty(snr)) {

        //                snrs.Add(snr);
        //                snr = "";
        //            }

        //            if (snrs.Count > 0) {

        //                raiseTagFoundEvent(snrs);
        //            }

        //        } while (res == RU_RESULT.UR_OK);

        //        // rest for a moment...
        //        System.Threading.Thread.Sleep(RU_865_TAG_READING_TIME_INTERVAL);
        //    }
        //}       

        protected void performInventoryScan() {

            List<Tuple<String, String, String>> weights = new List<Tuple<string, string, string>>();
            bool error = false;
            try {
                foreach (var pcbAddr in PcbAddresses) {

                    List<String> rawWeightValues = getPcbWeightsSync(pcbAddr);
                    if (rawWeightValues == null) {
                        error = true;
                        break;
                    }
                    for (int i = 0; i < rawWeightValues.Count; i++) {

                        String rawWeight = rawWeightValues[i];

                        // according to the level

                        if (RawWeightShowingLevel == RAW_WEIGHT_SHOWING_LEVEL.ALL_WEIGHTS) {

                            weights.Add(new Tuple<string, string, string>(pcbAddr.ToString(), i.ToString(), rawWeightValues[i]));
                        }

                        if (RawWeightShowingLevel == RAW_WEIGHT_SHOWING_LEVEL.ALL_WEIGHTS_FOR_CONNECTED_PAD) {

                            if (isAConnectedPad(rawWeight)) {

                                weights.Add(new Tuple<string, string, string>(pcbAddr.ToString(), i.ToString(), rawWeightValues[i]));
                            }
                        }
                        if (RawWeightShowingLevel == RAW_WEIGHT_SHOWING_LEVEL.VALID_WEIGHTS_ONLY) {

                            if (isAConnectedPad(rawWeight)) {

                                if (hasAValidWeight(rawWeight)) {

                                    weights.Add(new Tuple<string, string, string>(pcbAddr.ToString(), i.ToString(), rawWeightValues[i].Trim()));

                                } else {

                                    raiseCorrectWeightReaderEvent(rawWeight);

                                    // need only one event raised
                                    break;
                                }
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            } catch (Exception ex) {
                logProducer.Logger.Error("Exception while getting pcb weights, " + ex.Message);
                raiseHasReportedAnErrorEvent("Exception while getting pcb weights, " + ex.Message);
            }

            if (error == false && weights.Count > 0) {
                raiseReportAfterReadingEvent(weights);
            } else {
                logProducer.Logger.Error("No Pcb weights found");
                raiseHasReportedAnErrorEvent("No Pcb weights found");
                // Try to reconnect
                Dispatcher.BeginInvoke((Action)(() => checkAndRestoreConnection()));
            }

            stopScan();
        }

        protected void checkAndRestoreConnection() {
            try {
                logProducer.Logger.Error("Restore connection");
                disconnect();
                comHandler = null;
                connect();
                // Clear the buffer
                lock(lockerBuffer) {
                    managedBuffer.Clear();
                }
            } catch (Exception ex) {
                logProducer.Logger.Error("unable to establish a connection to the weighing system due to : " + ex.Message);
                Thread.Sleep(3000);
            }
        }

        protected void raiseCorrectWeightReaderEvent(String rawWeight) {
            if (getWeightStatus(rawWeight) == Pcb12Protocol.WEIGHT_STATUS.NOT_VALID_WEIGHT) {

                logProducer.Logger.Error("not a valid weight : " + rawWeight);

                raiseHasReportedAnErrorEvent(WeightReaderError.NOT_VALID_WEIGHT.Error);

            } else if (getWeightStatus(rawWeight) == Pcb12Protocol.WEIGHT_STATUS.OVERLOAD) {

                logProducer.Logger.Error("Weight over load : " + rawWeight);

                raiseHasReportedAnErrorEvent(WeightReaderError.OVERLOAD.Error);

            } else if (getWeightStatus(rawWeight) == Pcb12Protocol.WEIGHT_STATUS.UNSTABLE) {

                logProducer.Logger.Error("Weight in motion : " + rawWeight);

                raiseHasReportedAnErrorEvent(WeightReaderError.UNSTABLE.Error);

            } else {

                logProducer.Logger.Error("not a valid weight : " + rawWeight);
            }
        }

        protected static char getWeightStatus(String rawWeight) {
            char status = rawWeight.ToCharArray()[9];
            return status;
        }

        protected static Boolean isAConnectedPad(String rawWeight) {

            Boolean result = true;
            char signOrError = rawWeight.ToCharArray()[0];

            if (signOrError == Pcb12Protocol.WEIGHT_STATUS.HAS_ERROR) {
                // no PAD 
                result = false;
            }
            return result;
        }

        protected static Boolean hasAValidWeight(String rawWeight) {

            Boolean result = true;
            char status = getWeightStatus(rawWeight);

            if (status != Pcb12Protocol.WEIGHT_STATUS.OK) {
                // invalid weight
                result = false;
            }
            return result;
        }

        #endregion
        #endregion
    }

    public enum SCAN_MODE {
        CONTINUOUS_SCAN,
        INVENTORY_SCAN
    }

    public enum RAW_WEIGHT_SHOWING_LEVEL {
        ALL_WEIGHTS = 0,
        ALL_WEIGHTS_FOR_CONNECTED_PAD,
        VALID_WEIGHTS_ONLY
    }
}
