using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symbol.RFID3;
using System.Net;
using fr.nexess.hao.rfid.reader.eventHandler;

namespace fr.nexess.hao.rfid.reader.Brand
{
    class MotorolaReader : RfidDevice, RFIDReaderListenerDependent, ConfigurableRfidDevice, RFIDReaderEventProvider
    {
        bool muxEnable = false;

        /** singleton instance.*/
        protected static MotorolaReader instance = null;

        /** critical section object locker*/
        protected static readonly Object locker = new Object();

        /** RFIDTagReader's reader instance.*/
        private static Symbol.RFID3.RFIDReader theReader = null;
        private ReaderManagement readerManager = new ReaderManagement();

        /** List of managed RFIDReaderListener listeners*/
        private List<RFIDReaderListener> listeners = new List<RFIDReaderListener>();

        /** reader state.*/
        private RfidDeviceState state = RfidDeviceState.DISCONNECTED;

        private Dictionary<String, Object> ParamList;

        List<string> tagList = new List<string>();

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        private event EventHandler connectedEventHandler;
        private event EventHandler disconnectedEventHandler;
        private event EventHandler startReadingEventHandler;
        private event EventHandler stopReadingEventHandler;
        private event TagFoundEventHandler tagFoundEventHandler;

        private object objectLock = new Object();
        #endregion

         #region CONSTRUCTORS
        /** RFIDTagReader get instance (singleton).*/
        public static MotorolaReader getInstance()
        {
            // critical section
            lock (locker)
            {
                if (instance == null)
                {
                    instance = new MotorolaReader();
                }
            }

            return instance;
        }

         /** protected default constructor*/
        protected MotorolaReader()
        {
            // INITIALIZE
            try
            {
                // instanciate reader
                theReader = new Symbol.RFID3.RFIDReader();
                initialize();
            }
            catch (Exception ex)
            {
                if (hasReportedAnErrorEventHandler != null)
                {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }
                this.fireReaderEvent(RFIDReaderEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                Console.WriteLine(ex);
                return;
            }

        }

         /** destructor*/
        ~MotorolaReader()
        {

        }

        /** initialize reader by loading a valid set of configuration values*/
        protected void initialize() // TODO
        {
            ParamList = new Dictionary<String, Object>() {
            {"READER_NAME", ""},
            {"SCAN_DURATION", 10000},
            {"RSSI_ENABLED", false} 
            };
        }

        #endregion


        #region RFID_READER_EVENT_PROVIDER_IMPL
        event HasReportedAnErrorEventHandler RFIDReaderEventProvider.HasReportedAnError
        {
            add
            {
                lock (objectLock)
                {
                    hasReportedAnErrorEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    hasReportedAnErrorEventHandler -= value;
                }
            }
        }
        event EventHandler RFIDReaderEventProvider.Connected
        {
            add
            {
                lock (objectLock)
                {
                    connectedEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    connectedEventHandler -= value;
                }
            }
        }
        event EventHandler RFIDReaderEventProvider.Disconnected
        {
            add
            {
                lock (objectLock)
                {
                    disconnectedEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    disconnectedEventHandler -= value;
                }
            }
        }
        event EventHandler RFIDReaderEventProvider.StartReading
        {
            add
            {
                lock (objectLock)
                {
                    startReadingEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    startReadingEventHandler -= value;
                }
            }
        }
        event EventHandler RFIDReaderEventProvider.StopReading
        {
            add
            {
                lock (objectLock)
                {
                    stopReadingEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    stopReadingEventHandler -= value;
                }
            }
        }
        event TagFoundEventHandler RFIDReaderEventProvider.TagFound
        {
            add
            {
                lock (objectLock)
                {
                    tagFoundEventHandler += value;
                }
            }
            remove
            {
                lock (objectLock)
                {
                    tagFoundEventHandler -= value;
                }
            }
        }
        #endregion

        #region PRIVATE_METHODS

        private bool openEthReader(string hostname)
        {
            try
            {
                IPHostEntry hostInfo = Dns.Resolve(hostname);
                IPAddress[] address = hostInfo.AddressList;
                theReader.HostName = address[0].ToString();
                theReader.Connect();
                theReader.Events.AttachTagDataWithReadEvent = true;
                theReader.Events.NotifyReaderDisconnectEvent = true;
                theReader.Events.ReadNotify += new Events.ReadNotifyHandler(OnTagsReported);
                theReader.Events.StatusNotify += new Events.StatusNotifyHandler(OnConnectionLost);

                LoginInfo info = new LoginInfo();
                info.HostName = address[0].ToString();
                info.UserName = "admin";
                info.Password = "change";
                info.SecureMode = SECURE_MODE.HTTP;
                info.ForceLogin = true;
                readerManager.Login(info, READER_TYPE.FX);

                this.fireReaderEvent(RFIDReaderEvent.CONNECTED);
                state = RfidDeviceState.CONNECTED;         
                return true;
            }
            catch (Exception e)
            {
            }
            return false;
        }

        private void disconnect()
        {
            theReader.Disconnect();
            this.fireReaderEvent(RFIDReaderEvent.DISCONNECTED);
            state = RfidDeviceState.DISCONNECTED;
        }

        private List<string> getInventorywithRSSI(int scanDuration)
        {
            tagList.Clear();
            theReader.Actions.Inventory.Perform();
            System.Threading.Thread.Sleep(scanDuration);
            theReader.Actions.Inventory.Stop();
            return tagList;
        }

        static Predicate<String> findRSSI(TagData tagData)
        {
            return delegate(String tag)
            {
                return tag.StartsWith(tagData.TagID) && tag.EndsWith("|" + tagData.AntennaID);
            };
        }

        private void OnTagsReported(object sender, Events.ReadEventArgs e)
        {
            Symbol.RFID3.TagData[] tagData = theReader.Actions.GetReadTags(1000);
            if ((tagData != null) && (tagData.Length > 0))
            {
                foreach (Symbol.RFID3.TagData tag in tagData)
                {
                    if ((bool)ParamList["RSSI_ENABLED"] == true)
                    {
                        if (!tagList.Exists(findRSSI(tag)))
                            tagList.Add(tag.TagID + "|" + tag.PeakRSSI + "|" + tag.AntennaID);
                    }
                    else
                        tagList.Add(tag.TagID);
                }
            }
        }

        private void OnConnectionLost(object sender, Events.StatusEventArgs e)
        {
            this.fireReaderEvent(RFIDReaderEvent.DISCONNECTED);
            state = RfidDeviceState.DISCONNECTED;
        }

        private void startInventory()
        {
            List<string> snrs = new List<string>();

            if (theReader != null
                && theReader.IsConnected
                && this.state != RfidDeviceState.READING)
            {

                int duration = (int)ParamList["SCAN_DURATION"];

                try
                {
                    this.state = RfidDeviceState.READING;
                    if (startReadingEventHandler != null)
                    {
                        startReadingEventHandler(this, EventArgs.Empty);
                    }
                    this.fireReaderEvent(RFIDReaderEvent.START_READING);

                    snrs = getInventorywithRSSI(duration);

                    this.state = RfidDeviceState.CONNECTED;
                    if (stopReadingEventHandler != null)
                    {
                        stopReadingEventHandler(this, EventArgs.Empty);
                    }
                    this.fireReaderEvent(RFIDReaderEvent.STOPPED_READING);
                }
                catch (Exception ex)
                {
                    this.state = RfidDeviceState.CONNECTED;
                    if (hasReportedAnErrorEventHandler != null)
                    {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                    }
                    this.fireReaderEvent(RFIDReaderEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                    Console.WriteLine(ex);
                }

                if (snrs.Count > 0)
                {
                    var noDuplicata = new HashSet<string>(snrs);

                    List<string> tagList = new List<string>();
                    foreach (string st in noDuplicata)
                    {
                        tagList.Add(st);
                    }

                    // fire event !
                    fireTagFound(tagList);
                }
            }
        }

        /**
 * notify all listeners of "onTagFound" event
 * 
 * @param snr serial number (tag uuid)
 */
        private void fireTagFound(List<string> snrs)
        {
            // fire event
            if (tagFoundEventHandler != null)
            {
                tagFoundEventHandler(this, new TagFoundEventArgs(snrs));
            }

            foreach (RFIDReaderListener listener in getListeners())
            {
                listener.onTagFound(snrs);
            }
        }

        /**
         * notify all listeners of "onReaderEvent" event
         * 
         * @param readerEvent 
         * @param msg 
         */
        private void fireReaderEvent(RFIDReaderEvent readerEvent, string msg = "")
        {
            foreach (RFIDReaderListener listener in getListeners())
            {
                listener.onReaderEvent(readerEvent, msg);
            }
        }

        /**
         * get listeners list
         * 
         * @return listeners list
         */
        private List<RFIDReaderListener> getListeners()
        {
            return this.listeners;
        }
        #endregion

        #region RFID_READER_IMPL
        /**
         * Start scanning : e.g. "wait" a reading of a tag
         */
        void RfidDevice.startScan()
        {
            // start inventory for getting one tag. startScan stopped after getting a tag.            
            this.startInventory();
        }

        /**
         * stop scanning
         */
        void RfidDevice.stopScan()
        {
            throw new NotImplementedException();
        }

        /** 
         * get reader current state
         */
        RfidDeviceState RfidDevice.getReaderState()
        {
            return this.state;
        }

        /** 
         * reader connection
         */
        void RfidDevice.connect()
        {
            if (theReader != null
                && !theReader.IsConnected)
            {
                // let's open com to the reader througth usb
                this.openEthReader((String)ParamList["READER_NAME"]);
            }
        }

        /** 
         * reader disconnection
         */
        void RfidDevice.disconnect()
        {
            if (theReader != null
                && theReader.IsConnected)
            {
                try
                {
                    // disconnection
                    theReader.Disconnect();

                    if (disconnectedEventHandler != null)
                    {
                        disconnectedEventHandler(this, EventArgs.Empty);
                    }
                    this.fireReaderEvent(RFIDReaderEvent.DISCONNECTED);
                    state = RfidDeviceState.DISCONNECTED;

                }
                catch (Exception ex)
                {
                    if (hasReportedAnErrorEventHandler != null)
                    {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                    }
                    this.fireReaderEvent(RFIDReaderEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                    Console.WriteLine(ex);
                }
            }
        }

        /** 
         * get the reader Unique universal identifier.
         * @return UUID
         */
        String RfidDevice.getReaderUUID()
        {
            String str = "";
            if (theReader != null
                && theReader.IsConnected)
            {
                str = theReader.HostName;
            }

            return str;
        }

        #endregion

        #region RFID_READER_LISTENER_DEPENDENT_IMPL
        /**
         * add a RFIDReaderListener
         */
        void RFIDReaderListenerDependent.addListener(RFIDReaderListener listener)
        {
            if (getListeners() != null)
            {
                getListeners().Add(listener);
            }
        }

        /**
         * remove a RFIDReaderListener
         */
        void RFIDReaderListenerDependent.removeListener(RFIDReaderListener listener)
        {
            if (getListeners() != null)
            {
                getListeners().Remove(listener);
            }
        }
        #endregion

        #region CONFIGURABLE_RFID_READER_IMPL

        void ConfigurableRfidDevice.setParameter(String key, Object value)
        {
            if (ParamList.ContainsKey(key))
            {
               ParamList[key] = value;
            }

        }

        Dictionary<String, Object> ConfigurableRfidDevice.getParameters()
        {
            return ParamList;
        }

        float ConfigurableRfidDevice.getPower(int antenna)
        {
            return (float)Math.Pow(10, ((theReader.Config.Antennas[antenna].GetConfig().TransmitPowerIndex+100) / 100)) / 1000;
        }

        void ConfigurableRfidDevice.setPower(int antenna, float power)
        {
            Antennas.Config cfg = theReader.Config.Antennas[antenna].GetConfig();
            if (power > 0.01 && power <1)
            {
                cfg.TransmitPowerIndex = (ushort)((Math.Log10(power * 1000) * 100) - 100);
                theReader.Config.Antennas[antenna].SetConfig(cfg);
            }
        }

        byte ConfigurableRfidDevice.getAntennaConfiguration()
        {
            byte cfg = 0;
            foreach (ushort antenna in theReader.Config.Antennas.AvailableAntennas)
            {
                if (readerManager.ReadPoint.GetReadPointStatus((ushort)antenna) == READPOINT_STATUS.ENABLE)
                    cfg |= (byte)(1 << (antenna-1));
            }
            return cfg;
        }

        void ConfigurableRfidDevice.setAntennaConfiguration(byte configAntenna)
        {
            throw new NotImplementedException();
        }

        byte ConfigurableRfidDevice.addAntenna(int antenna)
        {
            readerManager.ReadPoint.SetReadPointStatus((ushort)antenna, READPOINT_STATUS.ENABLE);
            return ((ConfigurableRfidDevice)this).getAntennaConfiguration();
        }

        byte ConfigurableRfidDevice.removeAntenna(int antenna)
        {
            readerManager.ReadPoint.SetReadPointStatus((ushort)antenna, READPOINT_STATUS.DISABLE);
            return ((ConfigurableRfidDevice)this).getAntennaConfiguration();
        }

        bool ConfigurableRfidDevice.isMuxEnabled(int port)
        {
            return false;
        }

        //Detect a mux on reader port 1
        void ConfigurableRfidDevice.detectMux(int port)
        {
        }

        string ConfigurableRfidDevice.getPortName(int port)
        {
            throw new NotImplementedException();
        }

        //Get MUX config
        byte ConfigurableRfidDevice.getMuxConfiguration(int port)
        {
            throw new NotImplementedException();
        }

        //Set MUX config
        void ConfigurableRfidDevice.setMuxConfiguration(byte configMux, int port)
        {
            throw new NotImplementedException();
        }

        byte ConfigurableRfidDevice.addMuxAntenna(int readerPort, int muxPort)
        {
            throw new NotImplementedException();
        }

        byte ConfigurableRfidDevice.removeMuxAntenna(int readerPort, int muxPort)
        {
            throw new NotImplementedException();
        }

        byte ConfigurableRfidDevice.removeMux(int antenna)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
