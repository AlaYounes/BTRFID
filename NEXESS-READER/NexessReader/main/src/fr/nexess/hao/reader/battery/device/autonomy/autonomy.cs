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
using fr.nexess.hao.reader.battery.eventhandler;

namespace fr.nexess.hao.reader.battery.device.autonomy
{
    public class Autonomy : Reader, ComListener
    {
        private LogProducer logProducer = new LogProducer(typeof(Autonomy));

        // critical section object locker
        private static readonly Object locker = new Object();

        // singleton instance
        protected static Autonomy instance = null;

        private static Thread batteryReadingThreadHandler = null;

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

        private string batteryInfos;

        #region EVENT_HANDLER_DECLARATION
        protected event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        protected event EventHandler connectedEventHandler;
        protected event EventHandler disconnectedEventHandler;
        protected event EventHandler startReadingEventHandler;
        protected event EventHandler stopReadingEventHandler;
        protected event ReportAfterBatteryReadingEventHandler reportAfterReadingEventHandler;
        #endregion


        public static Autonomy getInstance()
        {

            // critical section
            lock (locker)
            {
                if (instance == null)
                {
                    instance = new Autonomy();
                }
            }

            return instance;
        }

        protected Autonomy()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {

                // force thread apartment to be a Single Thread Apartment (as the current)
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }

            // Multiple threads management
            Dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            initializeComHandler();
        }

        ~Autonomy()
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

                if (batteryReadingThreadHandler != null && !batteryReadingThreadHandler.IsAlive)
                {

                IsReading = true;
                raiseEvent(startReadingEventHandler);

                 batteryReadingThreadHandler.Start();
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
        public event ReportAfterBatteryReadingEventHandler ReportAfterReading
        {
            add
            {
                lock (locker)
                {
                    reportAfterReadingEventHandler += value;
                }
            }
            remove
            {
                lock (locker)
                {
                    reportAfterReadingEventHandler -= value;
                }
            }
        }

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

                port = getBatteryComPortName(port);

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

        private string getBatteryComPortName(string port)
        {

            foreach (KeyValuePair<string, string> entry in SerialComHandler.friendlyPorts)
            {
                if (entry.Value.Contains(autonomyProtocol.DEVICE_PROVIDER))
                    port = entry.Key;
            }
            return port;
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
            batteryInfos += System.Text.Encoding.ASCII.GetString(response);
            if (batteryInfos.Split('\n').ToList().Count()==31)
                raiseReportAfterReadingEvent(new List<string> (batteryInfos.Split('\n')));
            else
                raiseReportAfterReadingEvent(new List<string>(0));
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

        
        protected void raiseReportAfterReadingEvent(List<String> captions)
        {

            if (reportAfterReadingEventHandler != null)
            {

                Dispatcher.BeginInvoke((Action)(() => reportAfterReadingEventHandler(this, new ReportAfterBatteryReadingEventArgs(captions))));
            }
        }
        

      
        //#endregion

        #region THREAD
        
    protected void initializeBatteryReadingThread()
    {

        if (batteryReadingThreadHandler != null)
        {

                batteryReadingThreadHandler.Abort();
                batteryReadingThreadHandler = null;
        }

            batteryReadingThreadHandler = new Thread(new ThreadStart(batteryReadingThread));

            batteryReadingThreadHandler.IsBackground = true;
            System.Threading.Thread.Sleep(1000);
            
        }

        protected void batteryReadingThread()
        {

            batteryInfos = "";
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.STATE_OF_CHARGE.SOC).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.TIME_TO_EMPTY.TTE).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.TIME_TO_FULL.TTF).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.TEMPERATURE.TEMP).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.CYCLE_NB.CYCLE).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE.VOLT).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.CURRENT.CURR).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL1).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL2).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL3).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL4).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL5).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL6).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL7).Concat(autonomyProtocol.FIXED.END).ToArray());
            sendCmdData(autonomyProtocol.FIXED.HEAD.Concat(autonomyProtocol.COMMAND.VOLTAGE_CELLS.CELL8).Concat(autonomyProtocol.FIXED.END).ToArray());
            //System.Threading.Thread.Sleep(1000);

        }

        #endregion

        class autonomyProtocol
        {
            //internal const int MAX_FRAME_LENGTH = 35;
            internal const string DEVICE_PROVIDER = "VID_04D8";

            internal struct FIXED
            {
                internal static byte[] HEAD = Encoding.ASCII.GetBytes("\r\nBMS+");
                internal static byte[] END = Encoding.ASCII.GetBytes("?\r\n");
            }
            internal struct SIZE
            {
                internal const int HEAD_SIZE = 6;
                internal const int END_SIZE = 3;
            }

            internal struct COMMAND
            {

                internal struct STATE_OF_CHARGE
                {
                    // CMD [1 BYTE]
                    internal static byte[] SOC = Encoding.ASCII.GetBytes("SOC");
                }
                internal struct TIME_TO_FULL
                {
                    // CMD [1 BYTE]
                    internal static byte[] TTF = Encoding.ASCII.GetBytes("TTF");
                }
                internal struct TIME_TO_EMPTY
                {
                    // CMD [1 BYTE]
                    internal static byte[] TTE = Encoding.ASCII.GetBytes("TTE");
                }
                internal struct TEMPERATURE
                {
                    // CMD [1 BYTE]
                    internal static byte[] TEMP = Encoding.ASCII.GetBytes("TEMP");
                }
                internal struct VOLTAGE
                {
                    // CMD [1 BYTE]
                    internal static byte[] VOLT = Encoding.ASCII.GetBytes("VOLT");
                }
                internal struct CURRENT
                {
                    // CMD [1 BYTE]
                    internal static byte[] CURR = Encoding.ASCII.GetBytes("CURR");
                }
                internal struct CYCLE_NB
                {
                    // CMD [1 BYTE]
                    internal static byte[] CYCLE = Encoding.ASCII.GetBytes("CYCLE");
                }

                internal struct VOLTAGE_CELLS
                {
                    // CMD [1 BYTE]
                    internal static byte[] CELL1 = Encoding.ASCII.GetBytes("CELL1");
                    internal static byte[] CELL2 = Encoding.ASCII.GetBytes("CELL2");
                    internal static byte[] CELL3 = Encoding.ASCII.GetBytes("CELL3");
                    internal static byte[] CELL4 = Encoding.ASCII.GetBytes("CELL4");
                    internal static byte[] CELL5 = Encoding.ASCII.GetBytes("CELL5");
                    internal static byte[] CELL6 = Encoding.ASCII.GetBytes("CELL6");
                    internal static byte[] CELL7 = Encoding.ASCII.GetBytes("CELL7");
                    internal static byte[] CELL8 = Encoding.ASCII.GetBytes("CELL8");
                }

            }
            internal struct ERROR
            {
                internal struct CMD_ERROR
                {
                    // CMD [1 BYTE]
                    internal static byte[] CHECK_COUNT = Encoding.ASCII.GetBytes("SOC");
                }
                internal struct BMS_ERROR
                {
                    // CMD [1 BYTE]
                    internal static byte[] CHECK_COUNT = Encoding.ASCII.GetBytes("SOC");
                }
            }

        }
    }
}

namespace fr.nexess.hao.reader.battery.eventhandler
{

    /// <summary>
    /// report after reading Event handler  : thrown when optical reader ends its reading
    /// </summary>
    public delegate void ReportAfterBatteryReadingEventHandler(object sender, ReportAfterBatteryReadingEventArgs e);

    /// <summary>
    /// report after reading Event args
    /// </summary>
    public class ReportAfterBatteryReadingEventArgs : EventArgs
    {

        private List<String> captions;

        public ReportAfterBatteryReadingEventArgs(List<String> captions)
        {
            this.captions = captions;
        }

        public List<String> Captions
        {
            get
            {
                return this.captions;
            }
        }
    }
}