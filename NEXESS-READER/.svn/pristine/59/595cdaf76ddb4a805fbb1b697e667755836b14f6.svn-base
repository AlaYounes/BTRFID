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
using fr.nexess.toolbox;
using fr.nexess.hao.reader;

namespace fr.nexess.hao.reader.wireless.device.microchip.rn4871
{
    public class rn4871 : Reader, ComListener
    {
        private LogProducer logProducer = new LogProducer(typeof(rn4871));

        // critical section object locker
        private static readonly Object locker = new Object();

        // singleton instance
        protected static rn4871 instance = null;

        private static Thread rn4871ReadingThreadHandler = null;

        // STA Threads management
        public Dispatcher Dispatcher { get; set; }

        // default value
        protected static String comPort = "COM1";
        protected int scanDelay = 200;

        // fixed value
        protected const int BAUDRATE = 115200;
        protected const Parity PARITY = Parity.None;
        protected const int DATABITS = 8;
        protected const StopBits STOPBITS = StopBits.One;

        protected static ComHandler comHandler = null;

        protected int MAX_READING_DURATION = 3000;

        private bool IsListenning { get; set; }
        private bool IsReading { get; set; }

        private string rn4871Infos;

        private int REQUEST_TIMEOUT = 800;

        private ManualResetEvent manualEvent;
        private static Thread comPortSeekingThreadHandler = null;
        private int COM_SEEKING_TIMEOUT = 10000;

        #region EVENT_HANDLER_DECLARATION
        protected event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        protected event EventHandler connectedEventHandler;
        protected event EventHandler disconnectedEventHandler;
        protected event EventHandler startReadingEventHandler;
        protected event EventHandler stopReadingEventHandler;
        #endregion

        public static rn4871 getInstance()
        {

            // critical section
            lock (locker)
            {
                if (instance == null)
                {
                    instance = new rn4871();
                }
            }

            return instance;
        }

        protected rn4871()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {

                // force thread apartment to be a Single Thread Apartment (as the current)
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }

            // Multiple threads management
            Dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            if (String.IsNullOrEmpty(ComPort) && ComHandler == null)
            {
                initializeComPortSeekingThread();
                startAndWaitComPortSeekingThread();
            }

            logProducer.Logger.Debug("class loaded");
        }

        ~rn4871()
        {

            if (ComHandler != null)
            {
                // communication disconnection
                ComHandler.disconnect();
                ComHandler.stopListening();

                instance = null;
            }
        }

        #region PUBLIC_METHODS
        public static ComHandler ComHandler
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

        public void connect()
        {

            if (ComHandler == null)
            {

                initializeComHandler();
            }

            if (ComHandler != null)
            {

                ComHandler.connect();
            }

            ComHandler.startlistening();
        }

        public void disconnect()
        {

            if (ComHandler != null)
            {

                ComHandler.stopListening();
                IsListenning = false;

                ComHandler.disconnect();
            }
        }

        public void startScan()
        {

            if (IsConnected && !IsReading)
            {

                //initializeOpticalReadingThread();
                initializeBatteryReadingThread();

                if (rn4871ReadingThreadHandler != null && !rn4871ReadingThreadHandler.IsAlive)
                {

                    IsReading = true;
                    raiseEvent(startReadingEventHandler);

                    rn4871ReadingThreadHandler.Start();
                }
            }
        }

        public void stopScan()
        {

            if (IsConnected && IsReading)
            {

                IsReading = false;

                raiseEvent(stopReadingEventHandler);

            }
        }

        public ReaderState getReaderState()
        {

            ReaderState state = ReaderState.DISCONNECTED;

            if (ComHandler != null)
            {

                switch (ComHandler.getState())
                {

                    case ComState.CONNECTED:
                        state = ReaderState.CONNECTED;

                        if (IsListenning)
                        {
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

        public bool IsConnected
        {
            get
            {
                if (ComHandler.getState() == ComState.CONNECTED)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// serial com port (ex : "COM2")
        /// </summary>
        public static String ComPort
        {

            get { return comPort; }
            set { comPort = value; }
        }
        #endregion

        #region READER_EVENT_PROVIDER_IMPL
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

        #endregion

        #region PRIVATE_METHODS


        /// <summary>
        /// Start to monitor the com handler
        /// </summary>
        protected void initializeComHandler()
        {

            if (ComHandler == null)
            {

                // retrieve serial com handler and associated event...
                ComHandler = getComHandler();

                if (ComHandler == null)
                {
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
        protected SerialComHandler getComHandler()
        {

            logProducer.Logger.Debug("open port Com");

            SerialComHandler aSerialHandler = null;

            string port = "";

            try
            {

                port = getRn4871ComPortName(port);

                // retrieve serial com handler and associated event...
                aSerialHandler = new SerialComHandler(port, BAUDRATE, PARITY, DATABITS, STOPBITS);

                if (((ComHandler)aSerialHandler).getState() == ComState.CONNECTED)
                {

                    logProducer.Logger.Debug("port : " + port.ToString() + " is already opened, skip it and switch to the following");
                    throw new Exception();
                }
            }
            catch (Exception)
            {

                throw new Exception();
            }

            ((ComListenerDependent)aSerialHandler).addListener(this);

            return aSerialHandler;
        }

        private string getRn4871ComPortName(string port)
        {

            foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
            {
                if (entry.Value.Contains(rn4871Protocol.DEVICE_PROVIDER))
                    port = entry.Key;
            }
            return port;
        }

        protected void seekRn4871ComPort()
        {

            SerialComHandler tempSerialHandler = null;
            List<String> portNames = new List<String>();

            try
            {
                foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
                {
                    if (entry.Value.Contains(rn4871Protocol.MANUFACTURER_NAME) && entry.Value.Contains(rn4871Protocol.DEVICE_PROVIDER) && entry.Value.Contains(rn4871Protocol.PRODUCT_ID))
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
                    tempSerialHandler = new SerialComHandler(portname, BAUDRATE, PARITY, DATABITS, STOPBITS);
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
                    getReaderInfo(tempSerialHandler);
                    // Wait for response (re-sync)
                    manualEvent.WaitOne(REQUEST_TIMEOUT);

                    // check if version retrieved
                    if (rn4871Infos.Length > 0)
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
                    else
                    {

                        //No version retrieved
                        //rtz com port
                        ComPort = "";

                        // stop listening and unplug listener
                        if (tempSerialHandler != null)
                        {
                            logProducer.Logger.Error("stopping listening, no version");
                            tempSerialHandler.stopListening();
                            tempSerialHandler.disconnect();
                            tempSerialHandler = null;
                        }
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
        /// <summary>
        /// send a command to the device
        /// </summary>
        protected void sendCmdData(byte[] cmdData)
        {

            if (ComHandler != null && ComHandler.getState() == ComState.CONNECTED)
            {
                logProducer.Logger.Debug(ConversionTool.byteArrayToString(cmdData));
                ComHandler.sendData(cmdData);
            }
        }

        private void getReaderInfo(ComHandler aComHandler = null)
        {

            byte[] frame = rn4871Protocol.COMMAND.ENTER_COMMAND_MODE.COMMAND_MODE;

            if (aComHandler == null)
            {

                aComHandler = ComHandler;
            }
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(frame));
            rn4871Infos = System.Text.Encoding.Default.GetString(aComHandler.requestData(frame, 200));
        }

        #endregion

        #region SERIAL_COM_EVENT_CALLBACK_METHODS

        public void onComEvent(ComEvent comEvent, string msg)
        {
            // nothing todo
            logProducer.Logger.Debug("com Event : " + comEvent.ToString() + " - " + msg);
        }

        public void onResponse(byte[] response)
        {

            logProducer.Logger.Debug("On response!, let's extract Data Frame And Raise ManualEvent");
            logProducer.Logger.Debug(response);
            rn4871Infos += System.Text.Encoding.ASCII.GetString(response);
            //if (batteryInfos.Split('\n').ToList().Count() == 31)
            //    raiseReportAfterReadingEvent(new List<string>(batteryInfos.Split('\n')));
            //else
            //    raiseReportAfterReadingEvent(new List<string>(0));
        }

        public void onResponse(string response)
        {
            // nothing todo
        }

        protected void aComHandler_UnexpectedDisconnection(object sender, EventArgs e)
        {
            raiseHasReportedAnErrorEvent("Unexpected Disconnection occurs");
        }

        protected void aComHandler_HasReportedAComError(object sender, HasReportedAComErrorEventArgs e)
        {
            raiseHasReportedAnErrorEvent(e.Message);
        }

        protected void aComHandler_Disconnected(object sender, EventArgs e)
        {
            raiseEvent(disconnectedEventHandler);
        }

        protected void aComHandler_Connected(object sender, EventArgs e)
        {
            raiseEvent(connectedEventHandler);
        }

        private void aComHandler_OnDatareceived(object sender, OnDataReceivedEventArgs e)
        {

///
        }
        #endregion

        protected void raiseEvent(EventHandler eventhandler)
        {

            if (eventhandler != null)
            {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
            }
        }

        protected void raiseHasReportedAnErrorEvent(String issue, Exception ex = null)
        {

            if (hasReportedAnErrorEventHandler != null)
            {

                Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(issue, ex))));
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
        #region THREAD

        protected void initializeBatteryReadingThread()
        {

            if (rn4871ReadingThreadHandler != null)
            {

                rn4871ReadingThreadHandler.Abort();
                rn4871ReadingThreadHandler = null;
            }

            rn4871ReadingThreadHandler = new Thread(new ThreadStart(rn4871ReadingThread));

            rn4871ReadingThreadHandler.IsBackground = true;
            System.Threading.Thread.Sleep(1000);

        }

        protected void rn4871ReadingThread()
        {

           /////

        }

        protected void comPortSeekingThread()
        {

            seekRn4871ComPort();
        }

        #endregion

        class rn4871Protocol
        {
            internal const string DEVICE_PROVIDER = "VID_04D8";
            internal const string PRODUCT_ID = "PID_6001";
            internal const string MANUFACTURER_NAME = "FTDI";


            internal struct COMMAND
            {

                internal struct ENTER_COMMAND_MODE
                {
                    internal static byte[] COMMAND_MODE = Encoding.ASCII.GetBytes("$$$");
                }

            }

        }

    }
}
