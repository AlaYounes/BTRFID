using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using fr.nexess.toolbox;
using fr.nexess.toolbox.log;
using System.Threading;
using System.Windows.Threading;
using System.Configuration;
using fr.nexess.toolbox.comm.eventHandler;
using Microsoft.Win32;

namespace fr.nexess.toolbox.comm.serial {

    /// <summary>
    /// Serial Com handler
    /// </summary>
    /// <version>$Revision: 40 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2016-03-25 09:29:43 +0100 (ven., 25 mars 2016) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class SerialComHandler : ComHandler, ComListenerDependent/*, ComEventProvider*/ {
        // log producer
        protected LogProducer logProducer = new LogProducer(typeof(SerialComHandler));

        // dot net serialPort handler
        protected SerialPort mySerialPort = null;

        // serialPort event handler
        private SerialDataReceivedEventHandler dataReceivedEventHandler = null;
        private SerialErrorReceivedEventHandler errorReceivedEventHandler = null;

        protected List<ComListener> listeners = new List<ComListener>();

        // listening flag
        private bool bListening = false;
        private bool unexpectedDisconnectionFlag = true;

        // STA Threads management
        private Dispatcher  dispatcher = null;

        // serial request default timeout
        public static int   REQUEST_TIMEOUT    = 1000;

        // re-sync request response mechanism class members
        protected ManualResetEvent    manualEvent;
        protected Byte[] sharedResponse       = null;

        // critical section protector
        protected static readonly Object locker = new Object();

        #region EVENT_HANDLER_DECLARATION
        private event EventHandler                      unexpectedDisconnectionEventHandler;
        protected event HasReportedAComErrorEventHandler  hasReportedAComErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startListeningEventHandler;
        private event EventHandler                      stoppedListeningEventHandler;
        private event OnDataReceivedEventHandler        onDataReceivedEventHandler;
        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// default constructor. 
        /// </summary>
        /// <param name="portName">the port name, ex : "COM1"</param>
        /// <param name="baudRate">the baudrate, ex : 9600</param>
        /// <param name="parity">Parity object, ex : Parity.Even</param>
        /// <param name="dataBits">data bits number</param>
        /// <param name="stopBits">stop bits number</param>
        /// <exception cref="IoComException" />
        public SerialComHandler(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits) {

            // check if the calling thread is a STA thread (not a MTA one)
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) {

                logProducer.Logger.Debug("SerialComHandler current thread ApartmentState : " + Thread.CurrentThread.GetApartmentState());
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }

            Console.WriteLine("[ SerialComHandler ] Thread id : " + Thread.CurrentThread.ManagedThreadId.ToString());

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            getDefaultValuesFromConfiguration();

            // Serial port instanciation and configuration
            try {

                mySerialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);

                mySerialPort.WriteTimeout = REQUEST_TIMEOUT;
                mySerialPort.ReadTimeout = REQUEST_TIMEOUT;

            } catch (System.IO.IOException ioException) {

                throw new IoComException(ioException.Message);
            }

            // internal event notification
            manualEvent = new ManualResetEvent(false);

            // instanciate delegates which will called when something occurs on serial communication.
            dataReceivedEventHandler = new SerialDataReceivedEventHandler(onDataReceived);
            errorReceivedEventHandler = new SerialErrorReceivedEventHandler(onErrorReceived);
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new System.Timers.ElapsedEventHandler(onCheckDisconnection);
            aTimer.Interval = 1000;
            aTimer.Enabled = true;
        }
        #endregion

        private void onCheckDisconnection(object source, System.Timers.ElapsedEventArgs e)
        {
            ((System.Timers.Timer)source).Stop();

            if (!UnexpectedDisconnectionFlag && unexpectedDisconnectionEventHandler != null  && !mySerialPort.IsOpen)
            {
                try
                {
                    bool test = mySerialPort.DsrHolding;
                }
                catch (Exception ex)
                {
                    unexpectedDisconnectionEventHandler(this, EventArgs.Empty);
                    fireComEvent(ComEvent.UNEXPECTED_DISCONNECTION);
                    UnexpectedDisconnectionFlag = true;
                }

            }
            ((System.Timers.Timer)source).Start();
        }

        #region PUBLIC_METHODS
        /// <summary>
        /// get all serial port names
        /// </summary>
        public static string[] Ports {
            get {
                return SerialPort.GetPortNames();
            }
        }

        public static Dictionary<string, string> friendlyPorts
        {
            get
            {
                return BuildPortNameHash(SerialPort.GetPortNames());
            }
        }
        #endregion

        #region PRIVATE_METHODS
        /// <summary>
        /// thread Dispatcher getter
        /// </summary>
        public Dispatcher Dispatcher {
            get {
                return this.dispatcher;
            }
            set {
                this.dispatcher = value;
            }
        }

        public bool UnexpectedDisconnectionFlag {
            get {
                return unexpectedDisconnectionFlag;
            }

            set {
                unexpectedDisconnectionFlag = value;
            }
        }

        /// <summary>
        /// called when data is comming from serial port.
        /// The DataReceived event is raised on a secondary thread (MTA) when data is received from the SerialPort object.
        /// Because this event is raised on a secondary thread, and not the main thread, 
        /// attempting to modify some elements in the main thread, such as UI elements, could raise a threading exception. 
        /// If it is necessary to modify elements in the main Form or Control, post change requests back using Invoke, which will do the work on the proper thread.
        /// </summary>
        /// <param name="sender">The sender of the event, which is the SerialPort object.</param>
        /// <param name="e">A SerialDataReceivedEventArgs object that contains the event data.</param>
        protected virtual void onDataReceived(Object sender, SerialDataReceivedEventArgs e) {

            SerialPort aSerialPort = (SerialPort)sender;

            if (aSerialPort == null || aSerialPort.IsOpen == false) {
                return;
            }

            // retrieve nb bytes to read
            int nbBytes = aSerialPort.BytesToRead;

            // instanciate buffer
            Byte[] buffer = new Byte[nbBytes];

            try {

                // copy received data into the buffer
                aSerialPort.Read(buffer, 0, nbBytes);

                // following proces is for synchronous needs...
                lock (locker) {// enter into the critical section

                    // copy buffer to the shared one
                    sharedResponse = new Byte[nbBytes];
                    Buffer.BlockCopy(buffer, 0, sharedResponse, 0, nbBytes);
                }

                // Signal response is available
                manualEvent.Set();

                // notify all listeners (switch to the main thread)
                fireResponseReceived(buffer);

            } catch (Exception ex) {

                raiseResponseError(ex.Message);
            }
        }

        protected void raiseResponseError(string msg) {
            // fire event (switch to the main thread)
            if (hasReportedAComErrorEventHandler != null) {

                if (Dispatcher != null) {

                    Dispatcher.BeginInvoke((Action)(() => hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(msg))));

                } else {

                    hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(msg));
                }
            }

            // notify all listeners
            if (Dispatcher != null) {

                Dispatcher.BeginInvoke((Action)(() => fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, msg)));

            } else {

                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, msg);
            }
        }

        /// <summary>
        /// called when an error is comming from serial port.
        /// The errorReceived event is raised on a secondary thread (MTA) when data is received from the SerialPort object.
        /// Because this event is raised on a secondary thread, and not the main thread, 
        /// attempting to modify some elements in the main thread, such as UI elements, could raise a threading exception. 
        /// If it is necessary to modify elements in the main Form or Control, post change requests back using Invoke, which will do the work on the proper thread.
        /// </summary>
        /// <param name="sender">The sender of the event, which is the SerialPort object.</param>
        /// <param name="e">SerialErrorReceivedEventArgs object that contains the event data.</param>
        private void onErrorReceived(Object sender, SerialErrorReceivedEventArgs e) {

            SerialError aSerialError = e.EventType;

            Console.WriteLine("[ errorReceived ] Thread id : " + Thread.CurrentThread.ManagedThreadId.ToString());

            string msg = "";

            switch (aSerialError) {
                case SerialError.Frame:
                    msg = "The hardware detected a framing error.";
                    break;
                case SerialError.Overrun:
                    msg = "A character-buffer overrun has occurred. The next character is lost.";
                    break;
                case SerialError.RXOver:
                    msg = "An input buffer overflow has occurred. There is either no room in the input buffer, or a character was received after the end-of-file (EOF) character.";
                    break;
                case SerialError.RXParity:
                    msg = "The hardware detected a parity error.";
                    break;
                case SerialError.TXFull:
                    msg = "The application tried to transmit a character, but the output buffer was full.";
                    break;
                default:
                    msg = "an unkown error is occured";
                    break;
            }

            // fire event (switch to the main thread)
            if (hasReportedAComErrorEventHandler != null) {

                if (Dispatcher != null) {

                    Dispatcher.BeginInvoke((Action)(() => hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(msg))));

                } else {

                    hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(msg));
                }
            }

            if (Dispatcher != null) {

                // notify all listeners
                Dispatcher.BeginInvoke((Action)(() => fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, msg)));

            } else {

                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, msg);
            }
        }

        /// <summary>
        /// notify all listeners of "onDataReceived" event
        /// </summary>
        /// <param name="response">buffer of bytes as response</param>
        protected virtual void fireResponseReceived(byte[] response) {

            responseReceived(response);

            if (onDataReceivedEventHandler != null) {

                if (Dispatcher != null) {

                    Dispatcher.BeginInvoke((Action)(() => onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response))));

                } else {

                    onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response));
                }
            }

            foreach (ComListener listener in this.listeners) {

                listener.onResponse(response);
            }
        }

        protected virtual void responseReceived(byte[] response) {
            logProducer.Logger.Debug(ConversionTool.byteArrayToString(response));
        }

        protected virtual void eventReceived(string msg) {
            logProducer.Logger.Debug(msg);
        }

        protected virtual void openPort(string msg) {
            logProducer.Logger.Debug(msg);
        }

        protected virtual void send(string msg) {
            logProducer.Logger.Debug(msg);
        }

        /// <summary>
        /// notify all listeners of "onDataReceived" event
        /// </summary>
        /// <param name="response">buffer of bytes as response</param>
        private void fireResponseReceived(string response) {

            logProducer.Logger.Debug(response);

            if (onDataReceivedEventHandler != null) {

                if (Dispatcher != null) {

                    Dispatcher.BeginInvoke((Action)(() => onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response))));

                } else {

                    onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response));
                }
            }

            foreach (ComListener listener in this.listeners) {

                listener.onResponse(response);
            }
        }

        /// <summary>
        /// notify all listeners of "onSerialEvent" event
        /// </summary>
        /// <param name="comEvent">the com event</param>
        /// <param name="msg">message</param>
        protected void fireComEvent(ComEvent comEvent, string msg = "") {

            string trace = comEvent.ToString() + ((msg.Length > 0) ? " : " + msg : "");
            eventReceived(trace); 

            foreach (ComListener listener in this.listeners) {

                listener.onComEvent(comEvent, msg);
            }
        }

        protected void notifyError(string msg) {
            if (hasReportedAComErrorEventHandler != null) {

                hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(msg));
            }

        }

        protected void notifyDataReceived(byte[] response) {
            if (onDataReceivedEventHandler != null) {

                if (Dispatcher != null) {

                    Dispatcher.BeginInvoke((Action)(() => onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response))));

                } else {

                    onDataReceivedEventHandler(this, new OnDataReceivedEventArgs(response));
                }
            }
        }

        private static void getDefaultValuesFromConfiguration() {
            // get the REQUEST_TIMEOUT value from configuration
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["REQUEST_TIMEOUT"])) {

                String tempo = ConfigurationManager.AppSettings["REQUEST_TIMEOUT"];

                if (!String.IsNullOrEmpty(tempo)) {
                    try {

                        int ms = int.Parse(tempo);

                        if (ms > 0) {
                            REQUEST_TIMEOUT = ms;
                        }

                    } catch (Exception) {
                        // continue
                    }
                }
            }
        }

        private static Dictionary<string, string> BuildPortNameHash(string[] portsToMap)
        {
            Dictionary<string, string> oReturnTable = new Dictionary<string, string>();
            MineRegistryForPortName("SYSTEM\\CurrentControlSet\\Enum", oReturnTable, portsToMap);
            return oReturnTable;
        }

        private static void MineRegistryForPortName(string startKeyPath, Dictionary<string, string> targetMap, string[] portsToMap)
        {
            if (targetMap.Count >= portsToMap.Length)
                return;
            using (RegistryKey currentKey = Registry.LocalMachine)
            {
                try
                {
                    using (RegistryKey currentSubKey = currentKey.OpenSubKey(startKeyPath))
                    {
                        string[] currentSubkeys = currentSubKey.GetSubKeyNames();
                        if (currentSubkeys.Contains("Device Parameters") &&
                            startKeyPath != "SYSTEM\\CurrentControlSet\\Enum")
                        {
                            object portName = Registry.GetValue("HKEY_LOCAL_MACHINE\\" +
                                startKeyPath + "\\Device Parameters", "PortName", null);
                            if (portName == null ||
                                portsToMap.Contains(portName.ToString()) == false)
                                return;
                            object friendlyPortName = Registry.GetValue("HKEY_LOCAL_MACHINE\\" +
                                startKeyPath, "FriendlyName", null);
                            string friendlyName = "N/A";
                            if (friendlyPortName != null)
                                friendlyName = friendlyPortName.ToString();
                            if (friendlyName.Contains(portName.ToString()) == false)
                                friendlyName = string.Format("{0} ({1})", friendlyName, portName);
                            targetMap[portName.ToString()] = "FriendlyName="+friendlyName + "\nPath=" + startKeyPath;
                            string parentKeypath = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Enum\\USB\\" + startKeyPath.Split('\\')[4].Split('+')[0]+"&"+ startKeyPath.Split('\\')[4].Split('+')[1] + "\\" + startKeyPath.Split('\\')[4].Split('+')[2];
                            if (parentKeypath.EndsWith("A"))
                                parentKeypath = parentKeypath.Substring(0, parentKeypath.Length - 1);
                            object parent = Registry.GetValue(parentKeypath, "LocationInformation", null);
                            if (parent != null)
                                targetMap[portName.ToString()] += "\nLocation=" + parent.ToString();


                        }
                        else
                        {
                            foreach (string strSubKey in currentSubkeys)
                                MineRegistryForPortName(startKeyPath + "\\" + strSubKey, targetMap, portsToMap);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error accessing key '{0}'.. Skipping..", startKeyPath);
                }
            }
        }
        #endregion

        #region COM_EVENT_PROVIDER_IMPL
        public event EventHandler UnexpectedDisconnection {
            add {
                lock (locker) {
                    unexpectedDisconnectionEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    unexpectedDisconnectionEventHandler -= value;
                }
            }
        }
        public event HasReportedAComErrorEventHandler HasReportedAComError {
            add {
                lock (locker) {
                    hasReportedAComErrorEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    hasReportedAComErrorEventHandler -= value;
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
        public event EventHandler StartListening {
            add {
                lock (locker) {
                    startListeningEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    startListeningEventHandler -= value;
                }
            }
        }
        public event EventHandler StoppedListening {
            add {
                lock (locker) {
                    stoppedListeningEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    stoppedListeningEventHandler -= value;
                }
            }
        }
        public event OnDataReceivedEventHandler OnDataReceived {
            add {
                lock (locker) {
                    onDataReceivedEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    onDataReceivedEventHandler -= value;
                }
            }
        }
        #endregion

        #region COM_HANDLER_IMPL
        /// <summary>
        /// communication port connection
        /// </summary>
        public void connect() {

            if (!mySerialPort.IsOpen) {
                try {

                    openPort("try opening Port : " + mySerialPort.PortName);
                    // open com port
                    mySerialPort.Open();


                } catch (Exception ex) {

                    // fire event
                    if (hasReportedAComErrorEventHandler != null) {

                        hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(ex.Message));
                    }

                    // notify all listeners
                    fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                    return;
                }

                // fire event
                if (connectedEventHandler != null) {

                    connectedEventHandler(this, EventArgs.Empty);
                }

                // notify all listeners
                fireComEvent(ComEvent.CONNECTED);
                UnexpectedDisconnectionFlag = false;
            }
        }

        /// <summary>
        /// communication port disconnection
        /// </summary>
        public void disconnect() {

            if (mySerialPort.IsOpen) {
                try {

                    logProducer.Logger.Debug("serial port closing...");

                    // close com port
                    mySerialPort.Close();

                } catch (Exception ex) {

                    // fire event
                    if (hasReportedAComErrorEventHandler != null) {

                        hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(ex.Message));
                    }

                    // notify all listeners
                    fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                }

                // fire event
                if (disconnectedEventHandler != null) {

                    disconnectedEventHandler(this, EventArgs.Empty);
                }

                // notify all listeners
                fireComEvent(ComEvent.DISCONNECTED);
                UnexpectedDisconnectionFlag = true;
            }
        }

        /// <summary>
        /// Start listening through the communication port
        /// </summary>
        public void startlistening() {

            if (mySerialPort.IsOpen && !bListening) {

                // bind delegates which will called when something occurs on serial communication.
                mySerialPort.DataReceived += dataReceivedEventHandler;
                mySerialPort.ErrorReceived += errorReceivedEventHandler;

                bListening = true;

                // fire event
                if (startListeningEventHandler != null) {

                    startListeningEventHandler(this, EventArgs.Empty);
                }

                // notify all listeners
                fireComEvent(ComEvent.START_LISTENING);
            }
        }

        /// <summary>
        /// Stop listening through the communication port
        /// </summary>
        public void stopListening() {

            if (mySerialPort.IsOpen && bListening) {

                // unbind delegates which will called when something occurs on serial communication.
                mySerialPort.DataReceived -= dataReceivedEventHandler;
                mySerialPort.ErrorReceived -= errorReceivedEventHandler;

                bListening = false;

                // fire event
                if (stoppedListeningEventHandler != null) {

                    stoppedListeningEventHandler(this, EventArgs.Empty);
                }

                // notify all listeners
                fireComEvent(ComEvent.STOPPED_LISTENING);
            }
        }

        /// <summary>
        /// get current communication state
        /// </summary>
        /// <returns></returns>
        public ComState getState() {
            ComState returnState;

            if (mySerialPort.IsOpen) {

                returnState = ComState.CONNECTED;
            } else {

                returnState = ComState.DISCONNECTED;
            }

            return returnState;
        }

        /// <summary>
        /// send hex formatted string of data over serial
        /// </summary>
        /// <param name="msg">hex formatted string of data to send</param>
        public void sendData(string msg) {

            // convert hex formatted string (2F3c) to an array of bytes
            byte[] b = ConversionTool.stringToByteArray(msg);
            ((ComHandler)this).sendData(b);
        }

        /// <summary>
        /// send bytes array data over serial
        /// </summary>
        /// <param name="msg">array of bytes to send</param>
        public virtual void sendData(byte[] msg) {

            if (mySerialPort == null || mySerialPort.IsOpen == false) {
                logProducer.Logger.Debug("Can't send data : [" + ConversionTool.byteArrayToString(msg) + "], the port \"" + mySerialPort.PortName + "\" is closed");
                return;
            }

            try {

                send(ConversionTool.byteArrayToString(msg));
                lock (locker) {
                    mySerialPort.Write(msg, 0, msg.Length);
                }

            } catch (Exception ex) {

                // fire event
                if (hasReportedAComErrorEventHandler != null) {

                    hasReportedAComErrorEventHandler(this, new HasReportedAComErrorEventArgs(ex.Message));
                }

                // notify all listeners
                fireComEvent(ComEvent.HAS_REPORTED_AN_ERROR, ex.Message);
            }
        }

        /// <summary>
        /// Synchronous request of data through the serial port : means wait a response after sending data.
        /// </summary>
        /// <param name="buffer"> array of bytes to send over serial</param>
        /// <param name="timeout">optional timeout. If not setted, requestData will use default or configuration value</param>
        /// <returns>a buffer of bytes of received Data</returns>
        public virtual Byte[] requestData(Byte[] buffer, int timeout = -1) {
            Byte[] response = null;

            if (timeout < 0) {
                timeout = REQUEST_TIMEOUT;
            }

            // reset manual event
            manualEvent.Reset();

            // send request over serial
            ((ComHandler)this).sendData(buffer);

            // Wait for response (re-sync)
            manualEvent.WaitOne(timeout);

            lock (locker) {// enter into the critical section
                if (sharedResponse != null) {

                    // it's ready !! let's cook this
                    response = new Byte[sharedResponse.Length];
                    Buffer.BlockCopy(sharedResponse, 0, response, 0, sharedResponse.Length);

                    // rtz shared response
                    sharedResponse = null;
                }
            }

            // reset manual event
            manualEvent.Reset();

            return response;
        }
        #endregion

        #region SERIAL_COM_LISTENER_DEPEND_IMPL
        /// <summary>
        /// add a SerialComListener
        /// </summary>
        /// <param name="listener">comlistener</param>
        void ComListenerDependent.addListener(ComListener listener) {
            if (this.listeners != null) {
                this.listeners.Add(listener);
            }
        }

        /// <summary>
        /// remove a SerialComListener
        /// </summary>
        /// <param name="listener">com listener</param>
        void ComListenerDependent.removeListener(ComListener listener) {
            if (this.listeners != null) {
                this.listeners.Remove(listener);
            }
        }
        #endregion
    }
}
