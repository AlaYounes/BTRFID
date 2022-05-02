using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Threading;
using System.Windows.Threading;

using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.log;

using Impinj.OctaneSdk;
using System.Globalization;

using System.Net;

namespace fr.nexess.hao.rfid.device.impinj {

    public class SpeedwayRevolution : RfidDevice, ConfigurableRfidDevice, SustainableRfidDevice {

        private LogProducer logProducer = new LogProducer(typeof(SpeedwayRevolution));

        bool muxEnable = false;

        private String  READER_NAME     = "speedwayr-10-3b-05.local";
        private int     SCAN_DURATION   = 10000; // 10 sec, default value
        private Boolean RSSI_ENABLED    = false;
        private String SETTINGS_FILE = @"C:\nexcapImpinj\settings.xml";

        protected static SpeedwayRevolution instance = null;
        protected static readonly Object locker = new Object();

        private String type = "IMPINJ";

        // STA Threads management
        private static Dispatcher  dispatcher = null;

        // impinj speedway revolution's reader instance
        private ImpinjReader reader = null;

        private RfidDeviceState state = RfidDeviceState.DISCONNECTED;

        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();

        List<string> tagList = new List<string>();
        List<Impinj.OctaneSdk.Tag> tagReportList = new List<Impinj.OctaneSdk.Tag>();

        // manual event to break off inventory
        ManualResetEvent signalInterrupt; 
        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startReadingEventHandler;
        private event EventHandler                      stopReadingEventHandler;
        private event TagFoundEventHandler              tagFoundEventHandler;
        public event TagFoundOctaneEventHandler        tagFoundDetailedEventHandler;
        #endregion

        #region CONSTRUCTORS
        /** RFIDTagReader get instance (singleton).*/
        public static SpeedwayRevolution getInstance() {
            // critical section
            lock (locker) {

                if (instance == null) {

                    instance = new SpeedwayRevolution();
                }
            }

            return instance;
        }

        /// <summary>
        /// protected default constructor
        /// </summary>
        public SpeedwayRevolution() {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            setDefaultParameters();
            signalInterrupt = new ManualResetEvent(false);
            logProducer.Logger.Debug("class loaded");
        }

        ~SpeedwayRevolution() {

            if (reader != null) {

                // communication disconnection
                try
                { if (reader.IsConnected) {
                        reader.Disconnect();
                    }
                }
                catch (Exception) { }
                reader = null;

                instance = null;
            }
        }
        #endregion

        #region RFID_DEVICE_EVENT_PROVIDER_IMPL
        event HasReportedAnErrorEventHandler RfidDeviceEventProvider.HasReportedAnError {
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
        event EventHandler RfidDeviceEventProvider.Connected {
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
        event EventHandler RfidDeviceEventProvider.Disconnected {
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
        event EventHandler RfidDeviceEventProvider.StartReading {
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
        event EventHandler RfidDeviceEventProvider.StopReading {
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
        event TagFoundEventHandler RfidDeviceEventProvider.TagFound {
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
        event TagFoundOctaneEventHandler TagFoundDetailed {
            add {
                lock (locker) {
                    tagFoundDetailedEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    tagFoundDetailedEventHandler -= value;
                }
            }
        }
        #endregion

        #region RFID_DEVICE_IMPL

        public void startScan() {

            this.startInventory();
        }

        public void stopScan() {
            try {
                if (reader != null) {
                    reader.Stop();
                }
            }
            catch (Exception) {
                // do nothing
            }
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            try {
                if (reader != null
                    && reader.IsConnected) {
                    ReaderInfo readerInfo = new ReaderInfo(type, "Ethernet", getReaderState());
                    string version = getReaderSwVersion();
                    readerInfo.InfoList.Add(EnumReaderType.VERSION, version);
                    readerInfo.Version = version;
                    return readerInfo.InfoList;
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception getting reader version, " + ex.Message);
            }
            return new Dictionary<EnumReaderType, string>();
        }

        public RfidDeviceState getReaderState() {
            return this.state;
        }

        public void connect() {
            initializeReader();
            if (reader != null
                && !reader.IsConnected) {
                // let's open com to the reader througth usb
                    this.openEthReader((String)parameters[ConfigurableReaderParameter.READER_NAME.ToString()]);
            }
        }

        public void disconnect() {
            if (reader != null
                && reader.IsConnected) {
                try {
                    // disconnection
                    reader.Disconnect();
                    reader = null;
                    if (disconnectedEventHandler != null) {
                        disconnectedEventHandler(this, EventArgs.Empty);
                    }

                    state = RfidDeviceState.DISCONNECTED;

                } catch (Exception ex) {

                    if (hasReportedAnErrorEventHandler != null) {
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                    }
                }
            }
        }

        public bool isConnected() {
            if (reader != null && reader.IsConnected) {
                return true;
            }
            return false;
        }

        public string getReaderComPort() {
            return "";
        }

        public String getReaderUUID() {
            String str = "";
            if (reader != null
                && reader.IsConnected) {
                str = reader.Name;
            }

            return str;
        }

        public String getReaderSwVersion()
        {
            FeatureSet features = reader.QueryFeatureSet();
            return features.FirmwareVersion;
        }

        public bool transferCfgFile(string fileName)
        {
            bool status = false;
            if (reader != null
                || this.state != RfidDeviceState.READING)
            {
                try
                {
                    logProducer.Logger.Info("Reloading Impinj configuration : "+ fileName);
                    Settings toto = Settings.Load(fileName);
                    reader.ApplySettings(Settings.Load(fileName));
                    reader.SaveSettings();
                    status = true;
                }
                catch (Exception ex) {
                    logProducer.Logger.Error("Error configuring reader with file : " + fileName + ", " + ex.Message);
                }
            }
            if (!status)
                hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("Transfer failure : "));
            return status;
        }

        #endregion

        #region CONFIGURABLE_RFID_READER_IMPL

        public void setParameter(String key, Object value) {

            if (parameters.ContainsKey(key)) {

                parameters[key] = value;

            } else {

                if (!String.IsNullOrEmpty(key)) {
                    parameters.Add(key, value);
                }
            }


        }

        public Dictionary<String, Object> getParameters() {

            Dictionary<String, Object> maptoReturn = new Dictionary<string, object>();

            // copy
            foreach (KeyValuePair<String,Object> kv in parameters) {
                maptoReturn.Add(kv.Key, kv.Value);
            }

            return maptoReturn;
        }

        public float getPower(int antenna) {
            Settings settings = reader.QuerySettings();
            if (settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower)
            {
                return (float)1.4;
            }
            return (float)Math.Pow(10, (settings.Antennas.GetAntenna((ushort)antenna).TxPowerInDbm / 10)) / 1000;
        }

        public void setPower(int antenna, float power) {
            Settings settings = reader.QuerySettings();
            double powerToSet = Math.Round(40 * Math.Log10(power * 1000), 0) / 4;
            if (powerToSet < 10)
                powerToSet = 10;
            //MDF FR Manage max transmit power
            if (powerToSet > 31.5)
                powerToSet = 31.5;
            // Exception with SDK 3.6.0
            //if (powerToSet == 31.5)
            //    settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = true;
            //else
            //{
                settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = false;
                settings.Antennas.GetAntenna((ushort)antenna).TxPowerInDbm = powerToSet;
            //}
            //FR
            reader.ApplySettings(settings);
            reader.SaveSettings();
        }

        public void setPowerDynamically(int antenna, float power) {
            Settings settings = reader.QuerySettings();
            double powerToSet = Math.Round(40 * Math.Log10(power * 1000), 0) / 4;
            if (powerToSet < 10)
                powerToSet = 10;
            //MDF FR Manage max transmit power
            if (powerToSet > 31.5)
                powerToSet = 31.5;
            // Exception with SDK 3.6.0
            //if (powerToSet == 31.5)
            //    settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = true;
            //else
            //{
                settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = false;
                settings.Antennas.GetAntenna((ushort)antenna).TxPowerInDbm = powerToSet;
            //}
            reader.ApplySettings(settings);
        }

        public void setPowerinDbm(int antenna, float power)
        {
            Settings settings = reader.QuerySettings();
            double powerToSet = power;
            if (powerToSet < 10)
                powerToSet = 10;
            //MDF FR Manage max transmit power
            if (powerToSet > 31.5)
                powerToSet = 31.5;
            // Exception with SDK 3.6.0
            //if (powerToSet == 31.5)
            //    settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = true;
            //else
            //{
                settings.Antennas.GetAntenna((ushort)antenna).MaxTxPower = false;
                settings.Antennas.GetAntenna((ushort)antenna).TxPowerInDbm = powerToSet;
            //}
            //FR
            reader.ApplySettings(settings);
            reader.SaveSettings();
        }

        public byte getAntennaConfiguration() {
            if (reader != null
                && reader.IsConnected)
            {
                Status status = reader.QueryStatus();
                Settings settings = reader.QuerySettings();
                int cfgAntenna = 0;
                foreach (AntennaStatus antStatus in status.Antennas)
                {
                    if (settings.Antennas.GetAntenna(antStatus.PortNumber).IsEnabled && antStatus.IsConnected)
                    {
                        cfgAntenna += (1 << (antStatus.PortNumber - 1));
                    }
                }
                return (byte)cfgAntenna;
            }
            return 0;
        }

        public void setAntennaConfiguration(byte configAntenna) {
            throw new NotImplementedException();
        }

        public byte addAntenna(int antenna) {
            Settings settings = reader.QuerySettings();
            settings.Antennas.GetAntenna((ushort)antenna).IsEnabled = true;
            reader.ApplySettings(settings);
            reader.SaveSettings();
            return getAntennaConfiguration();
        }

        public byte removeAntenna(int antenna) {
            Settings settings = reader.QuerySettings();
            settings.Antennas.GetAntenna((ushort)antenna).IsEnabled = false;
            reader.ApplySettings(settings);
            reader.SaveSettings();
            return getAntennaConfiguration();
        }

        public bool isMuxEnabled(int port) {
            switch (port) {
                case 1:
                    return muxEnable;
                default:
                    return false;
            }
        }

        public void detectMux(int port) {
            throw new NotImplementedException();
        }

        public byte getMuxConfiguration(int port) {
            throw new NotImplementedException();
        }

        public void setMuxConfiguration(byte configMux, int port) {
            throw new NotImplementedException();
        }

        public byte addMuxAntenna(int readerPort, int muxPort) {
            throw new NotImplementedException();
        }

        public byte removeMuxAntenna(int readerPort, int muxPort) {
            throw new NotImplementedException();
        }

        public byte removeMux(int antenna) {
            throw new NotImplementedException();
        }

        public void switchOffAntenna()
        {
            reader.Stop();
            //throw new NotImplementedException();
        }

        public void switchOnAntenna()
        {
            reader.Start();
            //throw new NotImplementedException();
        }
        #endregion

        #region SUSTAINABLE_RFID_DEVICE

        public Dictionary<String, String> getBuiltInComponentHealthStates() {

            Dictionary<String, String> componentHealthStates = new Dictionary<String, String>();

            componentHealthStates.Add("Reader type", "IMPINJ Speedway Revolution");

            if (reader != null
                || reader.IsConnected) {

                try {
                    componentHealthStates.Add("State", getReaderState().ToString());
                    componentHealthStates.Add("Serial Communication Type", "Ethernet");

                    Byte antennaConfiguration = getAntennaConfiguration();
                    componentHealthStates.Add("functionnal Antennas", returnActivatedAntennaNamesFromByte(antennaConfiguration));

                    // get power for each antenna
                    if ((antennaConfiguration & (Byte)0x01) > 0) {
                        componentHealthStates.Add("Antenna #1 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(1)));
                    }
                    if ((antennaConfiguration & (Byte)0x02) > 0) {
                        componentHealthStates.Add("Antenna #2 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(2)));
                    }
                    if ((antennaConfiguration & (Byte)0x04) > 0) {
                        componentHealthStates.Add("Antenna #3 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(3)));
                    }
                    if ((antennaConfiguration & (Byte)0x08) > 0) {
                        componentHealthStates.Add("Antenna #4 power", String.Format(CultureInfo.CurrentCulture, "{0} Watt", getPower(4)));
                    }

                } catch (Exception ex) {
                    logProducer.Logger.Error("unable to get component health states", ex);
                }

            } else {
                componentHealthStates.Add("State", "disconnected");
            }

            return componentHealthStates;
        }

        #endregion

        #region PRIVATE_METHODS
        private String returnActivatedAntennaNamesFromByte(Byte activatedAntennas) {

            String activatedAntennaNames = "";

            if ((activatedAntennas & (Byte)0x01) > 0) {

                activatedAntennaNames += "Antenna #1";
            }
            if ((activatedAntennas & (Byte)0x02) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }

                activatedAntennaNames += "Antenna #2";
            }
            if ((activatedAntennas & (Byte)0x04) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }

                activatedAntennaNames += "Antenna #3";
            }
            if ((activatedAntennas & (Byte)0x08) > 0) {

                if (!String.IsNullOrEmpty(activatedAntennaNames)) {

                    activatedAntennaNames += " - ";
                }
                activatedAntennaNames += "Antenna #4";
            }

            return activatedAntennaNames;
        }

        /// <summary>
        /// thread Dispatcher getter
        /// </summary>
        private static Dispatcher Dispatcher {
            get {
                return dispatcher;
            }
            set {
                dispatcher = value;
            }
        }

        private void initializeReader() {
            try {

                if (reader == null) {

                    // instanciate reader
                    reader = new Impinj.OctaneSdk.ImpinjReader();
                }

            } catch (Exception ex) {

                if (hasReportedAnErrorEventHandler != null) {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }
            }
        }

        // TODO Configuration must be handled by the manager, not the API
        private void setDefaultParameters() {


            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["IMPINJ_HOST_NAME"])) {
                READER_NAME = ConfigurationManager.AppSettings["IMPINJ_HOST_NAME"];
            }

            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["TAG_SCAN_DURATION"])) {
                try {
                    SCAN_DURATION = int.Parse(ConfigurationManager.AppSettings["TAG_SCAN_DURATION"]);
                } catch (Exception) { }
            }

            if (ConfigurationManager.AppSettings["RSSI_ENABLED"] != null) {
                if (ConfigurationManager.AppSettings["RSSI_ENABLED"] == "true") {
                    RSSI_ENABLED = true;
                }
                if (ConfigurationManager.AppSettings["RSSI_ENABLED"] == "false") {
                    RSSI_ENABLED = false;
                }
            }

            parameters.Add(ConfigurableReaderParameter.READER_NAME.ToString(), READER_NAME);
            parameters.Add(ConfigurableReaderParameter.TAG_SCAN_DURATION.ToString(), SCAN_DURATION);
            parameters.Add(ConfigurableReaderParameter.RSSI_ENABLED.ToString(), RSSI_ENABLED);
            parameters.Add(ConfigurableReaderParameter.TIMEOUT_CONNECT.ToString(), 5000);
            parameters.Add(ConfigurableReaderParameter.NO_DISPATCHER_QUEUE.ToString(), false);
            parameters.Add(ConfigurableReaderParameter.SETTINGS_FILE.ToString(), SETTINGS_FILE);
        }

        private bool openEthReader(string hostname) {
            try {


                //MDF FR Connect function is unstable when used with hostname. First resolve the hostname to pass the IP adress
                //reader.Connect(hostname);
                IPHostEntry hostInfo = Dns.Resolve(hostname);
                IPAddress[] address = hostInfo.AddressList;
                reader.ConnectTimeout = (int)parameters[ConfigurableReaderParameter.TIMEOUT_CONNECT.ToString()];
                for (int i = 1; i <= 3; i++) //Try to connect 3 times
                {
                    logProducer.Logger.Info("Try connecting to Impinj");
                    if (i <= 2)
                    {
                        try
                        {
                            reader.Connect(address[0].ToString());
                            break;
                        }
                        catch (Exception ex) {
                            logProducer.Logger.Error("Failure connecting to Impinj, " + ex.Message);
                        }
                    }
                    else
                        reader.Connect(address[0].ToString());
                }
                //FR

                reader.TagsReported += OnTagsReported;
                reader.ConnectionLost += OnConnectionLost;
                reader.ReaderStopped += OnReaderStopped;
                //MDF FR Search internal current setting instead of default settings to keep the initial XML configuration
                Settings settings;
                try
                {
                    // reload file every time App starts, because of the power variation evolution
                    transferCfgFile((String)parameters[ConfigurableReaderParameter.SETTINGS_FILE.ToString()]);
                    settings = reader.QuerySettings();
                }
                catch (Exception ex) //No file or bad configuration file
                {
                    logProducer.Logger.Error("Failure querying settings, " + ex.Message);
                    settings = reader.QueryDefaultSettings();
                }
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeLastSeenTime = true;
                reader.ApplySettings(settings);
                reader.SaveSettings();

                //FR

                // notify all listeners
                if (connectedEventHandler != null) {

                    connectedEventHandler(this, EventArgs.Empty);
                }
                logProducer.Logger.Info("Impinj Connected");
                state = RfidDeviceState.CONNECTED;
                return true;
            } catch (Exception ex) {
                if (hasReportedAnErrorEventHandler != null) {
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs("unable to establish a connection to the rfid reader due to : " + ex.Message));
                }

                logProducer.Logger.Info("unable to establish a connection to the rfid reader due to : " + ex.Message);
            }
            return false;
        }

        private List<string> getInventorywithRSSI(int scanDuration, out bool isInterrupted) {
            lock(locker) {
                tagList.Clear();
                tagReportList.Clear();
            }
            signalInterrupt.Reset();
            reader.Start();
            isInterrupted = signalInterrupt.WaitOne(scanDuration);
            //System.Threading.Thread.Sleep(scanDuration);
            reader.Stop();
            List<string> newTagList = null;
            lock (locker) {
                newTagList = tagList.Distinct().ToList();
            }
            return newTagList;
        }

        private void OnTagsReported(Impinj.OctaneSdk.ImpinjReader sender, TagReport report) {
            if (state == RfidDeviceState.READING) {
                foreach (Tag tag in report) {
                    if ((bool)parameters[ConfigurableReaderParameter.RSSI_ENABLED.ToString()] == true) {
                        bool tagFound = false;
                        foreach (string registeredTag in tagList) {
                            if (registeredTag.Contains(tag.Epc.ToString().Replace(" ", "")) && registeredTag.Split('|')[2] == tag.AntennaPortNumber.ToString()) {
                                tagFound = true;
                                break;
                            }
                        }
                        if (!tagFound)
                            lock (locker) {
                                tagList.Add(tag.Epc.ToString().Replace(" ", "") + "|" + (int)tag.PeakRssiInDbm + "|" + tag.AntennaPortNumber);
                            }
                    } else {
                        lock (locker) {
                            tagList.Add(tag.Epc.ToString().Replace(" ", ""));
                        }
                    }
                    tagReportList.Add(tag);
                }
            }
        }

        private void OnConnectionLost(Impinj.OctaneSdk.ImpinjReader sender) {

            // notify all listeners
            if (disconnectedEventHandler != null) {

                disconnectedEventHandler(this, EventArgs.Empty);
            }

            state = RfidDeviceState.DISCONNECTED;
        }

        protected void OnReaderStopped(Impinj.OctaneSdk.ImpinjReader sender, ReaderStoppedEvent e) {
            logProducer.Logger.Info("Stopped Speedway reader");
            this.state = RfidDeviceState.CONNECTED;
            signalInterrupt.Set();
        }

        private void startInventory() {
            // check context
            if (reader != null
                && reader.IsConnected &&
                this.state != RfidDeviceState.READING) {

                // start tag scan physically
                Thread th = new Thread(new ParameterizedThreadStart(threadStartInventory));
                try {
                    SCAN_DURATION = int.Parse((string)parameters[ConfigurableReaderParameter.TAG_SCAN_DURATION.ToString()]);
                } catch (Exception) {
                    //do nothing
                } 
                th.Start(SCAN_DURATION);

            } else {

                // a problem occurs, let's notify listeners
                String EncounteredIssue = "Unable to start inventory, invalid context of execution";
                if (hasReportedAnErrorEventHandler != null) {

                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(EncounteredIssue));
                }

                return;
            }
        }

        private void threadStartInventory(object data) {

            int duration = (int)data;
            List<string> snrs = null;

            // change state and notify all listeners
            this.state = RfidDeviceState.READING;

            if (startReadingEventHandler != null) {
                if ((bool)parameters[ConfigurableReaderParameter.NO_DISPATCHER_QUEUE.ToString()] == false)
                    Dispatcher.BeginInvoke((Action)(() => startReadingEventHandler(this, EventArgs.Empty)));
                else
                    startReadingEventHandler(this, EventArgs.Empty);
            }

            try {
                logProducer.Logger.Info("Start Speedway reader inventory.");
                bool isInterrupted = false;
                snrs = getInventorywithRSSI(duration, out isInterrupted);
                if(isInterrupted == true) {
                    logProducer.Logger.Info("Interrupted Speedway reader inventory");
                    return;
                }
                logProducer.Logger.Info("Finished Speedway reader inventory : " + snrs.Count());

                this.state = RfidDeviceState.CONNECTED;
                if (stopReadingEventHandler != null) {
                    if ((bool)parameters[ConfigurableReaderParameter.NO_DISPATCHER_QUEUE.ToString()] == false)
                        Dispatcher.BeginInvoke((Action)(() => stopReadingEventHandler(this, EventArgs.Empty)));
                    else
                        stopReadingEventHandler(this, EventArgs.Empty);
                }
                // If closest tag is requested, filter
                if (parameters.ContainsKey(ConfigurableReaderParameter.CLOSEST_SCAN_ENABLED.ToString())
                    && (bool)parameters[ConfigurableReaderParameter.CLOSEST_SCAN_ENABLED.ToString()] == true) {
                    snrs = getClosestSnr(snrs);
                }

            } catch (Exception ex) {
                logProducer.Logger.Info("Speedway reader exception, " + ex.Message);
                this.state = RfidDeviceState.CONNECTED;
                if (hasReportedAnErrorEventHandler != null) {
                    if ((bool)parameters[ConfigurableReaderParameter.NO_DISPATCHER_QUEUE.ToString()] == false)
                        Dispatcher.BeginInvoke((Action)(() => hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message))));
                    else
                        hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ex.Message));
                }

                Console.WriteLine(ex);
            }

            // fire event !
            if ((bool)parameters[ConfigurableReaderParameter.NO_DISPATCHER_QUEUE.ToString()] == false)
                Dispatcher.BeginInvoke((Action)(() => fireTagFound(snrs)));
            else
                fireTagFound(snrs);

        }

        private void fireTagFound(List<string> snrs) {
            // fire event
            if (tagFoundEventHandler != null) {
                tagFoundEventHandler(this, new TagFoundEventArgs(snrs));
            }
            if (tagFoundDetailedEventHandler != null) {
                tagFoundDetailedEventHandler(this, new TagFoundOctaneEventArgs(tagReportList));
            }
        }


        private List<string> getClosestSnr(List<string> listSnrRssi) {
            string snr;
            double maxValue;
            if (listSnrRssi != null && listSnrRssi.Count() > 0) {
                string[] tab = listSnrRssi.First().Split('|');
                maxValue = double.Parse(tab[1]);
                snr = tab[0];
            } else {
                return listSnrRssi;
            }
            foreach (string s in listSnrRssi) {
                string[] tab = s.Split('|');
                maxValue = Math.Max(maxValue, double.Parse(tab[1]));
                if (maxValue == double.Parse(tab[1])) {
                    snr = tab[0];
                }
            }
            return new List<string>{ snr};
        }

        public void setFrequencies(List<double> freqList)
        {
            Settings settings = reader.QuerySettings();
            settings.TxFrequenciesInMhz = freqList;
            reader.ApplySettings(settings);
            reader.SaveSettings();
        }

        #endregion
    }
}
