using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OBID;
using fr.nexess.hao.rfid.reader.eventHandler;


namespace fr.nexess.hao.rfid.reader.Brand
{
    public class FeigReader : RfidDevice, RFIDReaderListenerDependent, FeUsbListener, ConfigurableRfidDevice, RFIDReaderEventProvider
    {
        const int FEISC_CLEAR_BUFFER = 0x32;
        const int FEISC_INIT_BUFFER = 0x33;
        const int FEISC_ADV_READ_BUFFER = 0x22;
        const int FEISC_RF_ON_OFF = 0x6A;
        const int FEISC_WRITE_CONF_BLOCK = 0x81;
        const int FEISC_READ_CONF_BLOCK = 0x80;
        const int FEISC_RF_CONTROLLER_RESET = 0x63;
        const int FEISC_DETECT = 0x76;

        const int FEDM_ISC_BRM_TABLE = 1;

        const string deviceId = "Device-ID";

        const byte CFG_POWER = 3;
        const byte CFG_POWER_OTHER = 20;

        /** 
         * antennas selection.<br/>
         *           ___________________
         * bit      |  3 |  2 |  1 |  0 |
         *          |----|----|----|----|
         * antenna  |ant4|ant3|ant2|ant1|
         *          |____|____|____|____| 
         */
		 
        const byte CFG_ANTENNA = 15;

        const int MIN_POWER = 0x11;
        const int MAX_POWER = 0x37;

        bool muxEnable = false;

        /** singleton instance.*/
        protected static FeigReader instance = null;

        /** critical section object locker*/
        protected static readonly Object locker = new Object();

        /** RFIDTagReader's reader instance.*/
        private static FedmIscReader theReader = null;

        /** List of managed RFIDReaderListener listeners*/
        private List<RFIDReaderListener> listeners = new List<RFIDReaderListener>();

        /** reader state.*/
        private RfidDeviceState state = RfidDeviceState.DISCONNECTED;

        private Dictionary<String, Object> FeigParamList;

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
        public static FeigReader getInstance()
        {
            // critical section
            lock (locker)
            {
                if (instance == null)
                {
                    instance = new FeigReader();
                }
            }

            return instance;
        }

         /** protected default constructor*/
        protected FeigReader()
        {
            // INITIALIZE
            try
            {
                // instanciate reader
                theReader = new FedmIscReader();
                initialize();
            }
            catch (FedmException ex)
            {
                if (hasReportedAnErrorEventHandler != null)
                {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }
                this.fireReaderEvent(RFIDReaderEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                Console.WriteLine(ex);
                return;
            }

            // Registry from usb Events
            theReader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_CONNECT_EVENT);
            theReader.AddEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_DISCONNECT_EVENT);
        }

         /** destructor*/
        ~FeigReader()
        {
                // Unregistry from usb Events
                theReader.RemoveEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_CONNECT_EVENT);
                theReader.RemoveEventListener((FeUsbListener)this, FeUsbListenerConst.FEUSB_DISCONNECT_EVENT);

        }

        /** initialize reader by loading a valid set of configuration values*/
        protected void initialize() // TODO
        {
            FeigParamList = new Dictionary<String, Object>() {
            {"READER_NAME", "ID ISC.LRU3500"},
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

        //Open reader with specified name in USB mode
        private void openUsbReader(string readerName)
        {
            //usb connection
            FeUsb myUsbConnection = new FeUsb();
            myUsbConnection.ClearScanList();

            //Scan all USB devices connected
            try
            {
                myUsbConnection.Scan(OBID.FeUsbScanSearch.SCAN_ALL, null);
            }
            catch (FePortDriverException ex)
            {
                if (hasReportedAnErrorEventHandler != null)
                {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }
                this.fireReaderEvent(RFIDReaderEvent.HAS_REPORTED_AN_ERROR, ex.Message);
                Console.WriteLine(ex);

                // break function
                return;
            }

            if (myUsbConnection.ScanListSize >= 1)
            {
                //Try to connect to each device
                for (int i = 0; i < myUsbConnection.ScanListSize; i++)
                {
                    string sDeviceID = myUsbConnection.GetScanListPara(i, deviceId);
                    if (sDeviceID.Length % 2 != 0)
                        sDeviceID = "0" + sDeviceID;
                    try
                    {
                        theReader.ConnectUSB((int)FeHexConvert.HexStringToLong(sDeviceID));

                        //get Device Info
                        theReader.ReadReaderInfo();

                        //Check readerName
                        if (theReader.GetReaderName().Contains(readerName))
                        {
                            theReader.ReadCompleteConfiguration(false);
                            if (connectedEventHandler != null)
                            {
                                connectedEventHandler(this, EventArgs.Empty);
                            }
                            this.fireReaderEvent(RFIDReaderEvent.CONNECTED);
                            state = RfidDeviceState.CONNECTED;
                            return;
                        }
                        else
                            theReader.DisConnect();
                    }
                    catch (Exception) { }//in use
                }
            }
            return;
        }

        //power on antenna
        private void switchOnAntenna()
        {
            theReader.SetData(FedmIscReaderID.FEDM_ISC_TMP_RF_ONOFF, 0x01);
            theReader.SendProtocol(FEISC_RF_ON_OFF);
        }

        //power off antenna
        private void switchOffAntenna()
        {
            theReader.SetData(FedmIscReaderID.FEDM_ISC_TMP_RF_ONOFF, 0x00);
            theReader.SendProtocol(FEISC_RF_ON_OFF);
        }

        //Launch inventory with specified duration
        //Requires reader is configured in Buffered Read Mode
        private List<string> getBRMInventory()
        {
            //initialize tag list
            List<string> tagList = new List<string>();
            int TableIndex, TableSize;
            int duration = (int)FeigParamList["SCAN_DURATION"];

            //Start scan
            theReader.SetTableSize(FEDM_ISC_BRM_TABLE, 256);
            theReader.SetData(FedmIscReaderID.FEDM_ISC_TMP_ADV_BRM_SETS, 255);
            theReader.SendProtocol(FEISC_CLEAR_BUFFER);
            theReader.SendProtocol(FEISC_INIT_BUFFER);

            //Wait for selected duration
            System.Threading.Thread.Sleep(duration);

            //Read results
            int status = theReader.SendProtocol(FEISC_ADV_READ_BUFFER);
            TableSize = -1;
            // while there are valid data to read
            while (TableSize != 0 && ((status == 0x00) || (status == 0x83) || (status == 0x84) || (status == 0x85) || (status == 0x93) || (status == 0x94)))
            {
                TableSize = theReader.GetTableLength(FEDM_ISC_BRM_TABLE);
                for (TableIndex = 0; TableIndex < TableSize; TableIndex++)
                {
                    //read table and update tag list
                    string str;
                    theReader.GetTableData(TableIndex, FEDM_ISC_BRM_TABLE, FedmIscReaderConst.DATA_SNR, out str);
                    tagList.Add(str);
                }
                theReader.SendProtocol(FEISC_CLEAR_BUFFER);
                status = theReader.SendProtocol(FEISC_ADV_READ_BUFFER);
            }
            switchOffAntenna();
            return tagList;
        }

        private List<string> getBRMInventorywithRSSI()
        {
            //initialize tag list
            List<string> tagList = new List<string>();
            int TableIndex, TableSize;
            int duration = (int)FeigParamList["SCAN_DURATION"];

            //Start scan
            theReader.SendProtocol(FEISC_CLEAR_BUFFER);
            theReader.SendProtocol(FEISC_INIT_BUFFER);

            //Wait for selected duration
            System.Threading.Thread.Sleep(duration);

            //Read results
            theReader.SetTableSize(FEDM_ISC_BRM_TABLE, 255);
            theReader.SetData(FedmIscReaderID.FEDM_ISC_TMP_ADV_BRM_SETS, 255);
            int status = theReader.SendProtocol(FEISC_ADV_READ_BUFFER);
            TableSize = -1;
            // while there are valid data to read
            while (TableSize != 0 && ((status == 0x00) || (status == 0x83) || (status == 0x84) || (status == 0x85) || (status == 0x93) || (status == 0x94)))
            {
                TableSize = theReader.GetTableLength(FEDM_ISC_BRM_TABLE);
                for (TableIndex = 0; TableIndex < TableSize; TableIndex++)
                {
                    string currentTag = theReader.GetTableItem(TableIndex, FEDM_ISC_BRM_TABLE).GetUid();
                    foreach (KeyValuePair<byte, FedmIscRssiItem> couple in theReader.GetTableItem(TableIndex, FEDM_ISC_BRM_TABLE).GetRSSI())
                    {
                        tagList.Add(currentTag
                            + "|-" + couple.Value.RSSI
                            + "|" + couple.Value.antennaNumber);
                    }
                    if (theReader.GetTableItem(TableIndex, FEDM_ISC_BRM_TABLE).GetRSSI().Count==0){
                        tagList.Add(currentTag
                             + "|-" + 0
                             + "|" + 0);
                    }
                }
                try
                {
                    theReader.SendProtocol(FEISC_CLEAR_BUFFER);
                    status = theReader.SendProtocol(FEISC_ADV_READ_BUFFER);
                }
                catch (Exception ex)
                {
                    tagList.Clear();
                    return tagList;
                }
            }
            switchOffAntenna();
            return tagList;
        }

        /**
        * execute Inventory for all tags with Mode=0 at first Antenna.
        */
        private void startInventory()
        {
            List<string> snrs = new List<string>();

            if (theReader != null
                && theReader.Connected
                && this.state != RfidDeviceState.READING)
            {

                int duration = (int)FeigParamList["SCAN_DURATION"];

                try
                {
                    this.state = RfidDeviceState.READING;
                    if (startReadingEventHandler != null)
                    {
                        startReadingEventHandler(this, EventArgs.Empty);
                    }
                    this.fireReaderEvent(RFIDReaderEvent.START_READING);

                    if ((bool)FeigParamList["RSSI_ENABLED"]==true)
                        snrs = getBRMInventorywithRSSI();
                    else
                        snrs = getBRMInventory();

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

                //if (snrs.Count > 0)
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
                && !theReader.Connected)
            {
                // let's open com to the reader througth usb
                this.openUsbReader((String)FeigParamList["READER_NAME"]);
            }
        }

        /** 
         * reader disconnection
         */
        void RfidDevice.disconnect()
        {
            if (theReader != null
                && theReader.Connected)
            {
                try
                {
                    // disconnection
                    int iRes = theReader.DisConnect();

                    if (iRes == Fedm.OK)
                    {
                        if (disconnectedEventHandler != null)
                        {
                            disconnectedEventHandler(this, EventArgs.Empty);
                        }
                        this.fireReaderEvent(RFIDReaderEvent.DISCONNECTED);
                        state = RfidDeviceState.DISCONNECTED;
                    }
                }
                catch (Exception ex)
                {
                    // FePortDriverException | FedmException
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
                && theReader.Connected)
            {
                str = theReader.GetReaderName();
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

        #region FEUSBLISTENER_IMPL
        // for USB readers
        void FeUsbListener.OnConnectReader(int deviceHandle, long deviceID)
        {
            if (connectedEventHandler != null)
            {
                connectedEventHandler(this, EventArgs.Empty);
            }
            this.fireReaderEvent(RFIDReaderEvent.CONNECTED);
            state = RfidDeviceState.CONNECTED;
        }

        void FeUsbListener.OnDisConnectReader(int deviceHandle, long deviceID)
        {
            if (disconnectedEventHandler != null)
            {
                disconnectedEventHandler(this, EventArgs.Empty);
            }
            this.fireReaderEvent(RFIDReaderEvent.DISCONNECTED);
            state = RfidDeviceState.DISCONNECTED;
        }
        #endregion

        #region CONFIGURABLE_RFID_READER_IMPL
        void ConfigurableRfidDevice.setParameter(String key, Object value)
        {
            if (FeigParamList.ContainsKey(key))
            {
                FeigParamList[key] = value;
            }

        }

        Dictionary<String, Object> ConfigurableRfidDevice.getParameters()
        {
            return FeigParamList;
        }

        //get power of specified antenna
        float ConfigurableRfidDevice.getPower(int antenna)
        {
            byte power = 0;

            switch (antenna)
            {
                case 1:
                    theReader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No1.OutputPower, out power, false);
                    break;
                case 2:
                    theReader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No2.OutputPower, out power, false);
                    break;
                case 3:
                    theReader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No3.OutputPower, out power, false);
                    break;
                case 4:
                    theReader.GetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No4.OutputPower, out power, false);
                    break;
            }

            if (((ConfigurableRfidDevice)this).isMuxEnabled(antenna)) //Manage MUX power loss
                return ((float)power - 15) / (float)13;
            else
                return ((float)power - 15) / 10;

        }

        //set input power on specified antenna
        void ConfigurableRfidDevice.setPower(int antenna, float power)
        {

            int convertedPower;


            if (((ConfigurableRfidDevice)this).isMuxEnabled(antenna)) //Manage MUX power loss
                convertedPower = (int)Math.Round(power * 13, 0) + 15;
            else
                convertedPower = (int)Math.Round(power * 10, 0) + 15;

            if (convertedPower < MIN_POWER || convertedPower > MAX_POWER)
                return;

            switch (antenna)
            {
                case 1:
                    theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No1.OutputPower, (byte)convertedPower, false);
                    break;
                case 2:
                    theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No2.OutputPower, (byte)convertedPower, false);
                    break;
                case 3:
                    theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No3.OutputPower, (byte)convertedPower, false);
                    break;
                case 4:
                    theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.No4.OutputPower, (byte)convertedPower, false);
                    break;
            }
            theReader.ApplyConfiguration(false);

        }

        byte ConfigurableRfidDevice.getAntennaConfiguration()
        {
            byte configAntenna = 0;
            theReader.GetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, out configAntenna, false);
            return configAntenna;
        }

        void ConfigurableRfidDevice.setAntennaConfiguration(byte configAntenna)
        {
            theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            theReader.ApplyConfiguration(false);
        }

        byte ConfigurableRfidDevice.addAntenna(int antenna)
        {
            int configAntenna = ((ConfigurableRfidDevice)this).getAntennaConfiguration() | (0x1 << (antenna - 1));
            theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            theReader.ApplyConfiguration(false);
            return (byte)configAntenna;
        }

        byte ConfigurableRfidDevice.removeAntenna(int antenna)
        {
            int configAntenna = ((ConfigurableRfidDevice)this).getAntennaConfiguration() & (0xF - (0x1 << (antenna - 1)));
            theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.UHF.Internal.SelectedAntennas, configAntenna, false);
            theReader.ApplyConfiguration(false);
            return (byte)configAntenna;
        }

        //Indicates if there is MUX on reader port 1
        bool ConfigurableRfidDevice.isMuxEnabled(int port)
        {
            switch (port)
            {
                case 1:
                    return muxEnable;
                default:
                   return false;
            }
        }

        //Detect a mux on reader port 1
        void ConfigurableRfidDevice.detectMux(int port)
        {
            switch (port)
            {
                case 1:
                    byte cfgAntenna = ((ConfigurableRfidDevice)this).getAntennaConfiguration();
                    byte cfgMux = ((ConfigurableRfidDevice)this).getMuxConfiguration(port);
                    theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Antenna.UHF.Miscellaneous.Enable_DCPower, 1, false);
                    theReader.SendProtocol(FEISC_DETECT);
                    System.Threading.Thread.Sleep(200);
                    switchOffAntenna();
                    theReader.ReadCompleteConfiguration(false);
                    muxEnable = ((ConfigurableRfidDevice)this).getMuxConfiguration(port) != 0;
                    ((ConfigurableRfidDevice)this).setAntennaConfiguration(cfgAntenna);
                    if (muxEnable)
                    {
                        ((ConfigurableRfidDevice)this).setMuxConfiguration(cfgMux, port);
                        theReader.SetConfigPara(OBID.ReaderConfig.AirInterface.Multiplexer.Enable, 1, false);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        string ConfigurableRfidDevice.getPortName(int port)
        {
            switch (port)
            {
                case 1:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No1.SelectedAntennas;
                case 2:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No2.SelectedAntennas;
                case 3:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No3.SelectedAntennas;
                case 4:
                    return OBID.ReaderConfig.AirInterface.Multiplexer.UHF.External.Output.No4.SelectedAntennas;
                default:
                    return "";
            }
        }

        //Get MUX config
        byte ConfigurableRfidDevice.getMuxConfiguration(int port)
        {
            byte configAntenna = 0;
            if (port < 1 || port > 4)
                return 0;
            theReader.GetConfigPara(((ConfigurableRfidDevice)this).getPortName(port), out configAntenna, false);
            return configAntenna;
        }

        //Set MUX config
        void ConfigurableRfidDevice.setMuxConfiguration(byte configMux, int port)
        {
            if (port < 1 || port > 4)
                return;
            theReader.SetConfigPara(((ConfigurableRfidDevice)this).getPortName(port), configMux, false);
            theReader.ApplyConfiguration(false);
        }

        byte ConfigurableRfidDevice.addMuxAntenna(int readerPort, int muxPort)
        {
            int configMux = ((ConfigurableRfidDevice)this).getMuxConfiguration(muxPort) | (0x1 << (muxPort - 1));
            ((ConfigurableRfidDevice)this).setMuxConfiguration((byte)configMux, readerPort);
            return (byte)configMux;
        }

        byte ConfigurableRfidDevice.removeMuxAntenna(int readerPort, int muxPort)
        {
            int configMux = ((ConfigurableRfidDevice)this).getMuxConfiguration(muxPort) & (0xFF - (0x1 << (muxPort - 1)));
            ((ConfigurableRfidDevice)this).setMuxConfiguration((byte)configMux, readerPort);
            return (byte)configMux;
        }

        byte ConfigurableRfidDevice.removeMux(int antenna)
        {
            theReader.SetConfigPara(((ConfigurableRfidDevice)this).getPortName(antenna), 0, false);
            theReader.ApplyConfiguration(false);
            return 0;
        }

        #endregion
    }
}
