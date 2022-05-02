using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using stid.sdk;
using System.Windows.Threading;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.log;
using System.Threading;
using System.Diagnostics;


namespace fr.nexess.hao.rfid.device.stid {

    public class SscpReader : RfidDevice, ConfigurableRfidDevice {

        private const String DEFAULT_COM_PORT = "COM10";
        private const int DEFAULT_SCAN_DURATION = 3000;

        private LogProducer logProducer = new LogProducer(typeof(SscpReader));

        private UInt16 cultureIdentifier = (ushort)System.Globalization.CultureInfo.CurrentCulture.LCID;

        // singleton instance.
        private static SscpReader instance = null;

        private String type = "STID";

        // critical section object locker
        private static readonly Object locker = new Object();

        // STA Threads management
        private static Dispatcher  dispatcher = null;

        private static Thread tagReadingThreadHandler = null;

        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();

        private bool IsConnected { get; set; }
        private bool IsReading { get; set; }
        private bool IsInitialized { get; set; }

        private String comPort = null;

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler    hasReportedAnErrorEventHandler;
        private event EventHandler                      connectedEventHandler;
        private event EventHandler                      disconnectedEventHandler;
        private event EventHandler                      startReadingEventHandler;
        private event EventHandler                      stopReadingEventHandler;
        private event TagFoundEventHandler              tagFoundEventHandler;
        #endregion

        #region CONSTRUCTOR
        protected SscpReader() {

            // Multiple threads management
            Dispatcher = Dispatcher.CurrentDispatcher;

            setDefaultConfigurationValues();

            initializeSSCPLibEPC();

            //initializeReaderCom(); not here, at Connect

            logProducer.Logger.Debug("class loaded");
        }

        private void setDefaultConfigurationValues() {

            setParameter("COM_TYPE", "USB");
            setParameter("COM_PORT", DEFAULT_COM_PORT);

            setParameter("SCAN_MODE", SCAN_MODE.CONTINUOUS_SCAN.Value);

            setParameter("SCAN_DURATION", DEFAULT_SCAN_DURATION);
        }

        public static SscpReader getInstance() {

            // critical section
            lock (locker) {

                if (instance == null) {
                    instance = new SscpReader();
                }
            }

            return instance;
        }

        ~SscpReader() {

            terminateSSCPLibEPC();
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

        #region RFID_DEVICE_IMPL
        public void connect() {

            if (!IsConnected) {

                if (!IsInitialized) {

                    initializeReaderCom();

                    IsInitialized = true;
                }

                UInt16 status = StidReaderApi.SSCP_Connect();

                if ((byte)status == StidReaderApi.SSCP_OK) {

                    IsConnected = true;

                    logProducer.Logger.Debug("connected");

                    raiseEvent(connectedEventHandler);

                } else {

                    // oups! problem...
                    getErrorMsgAndRaiseEvent(status, "Unable to connect STid SSCP reader");
                }
            }
        }

        public void disconnect() {

            if (IsConnected) {

                if (IsReading) {
                    stopScan();
                }

                UInt16 status = StidReaderApi.SSCP_Disconnect();

                if ((byte)status == StidReaderApi.SSCP_OK) {

                    IsConnected = false;

                    logProducer.Logger.Debug("disconnected");

                    raiseEvent(disconnectedEventHandler);

                } else {

                    // oups! problem...
                    getErrorMsgAndRaiseEvent(status, "Unable to disconnect STid SSCP reader");
                }
            }
        }

        public void startScan() {

            if (IsConnected && !IsReading) {

                initializeTagReadingThread();

                if (tagReadingThreadHandler != null && !tagReadingThreadHandler.IsAlive) {

                    tagReadingThreadHandler.Start();

                    IsReading = true;
                    raiseEvent(startReadingEventHandler);
                }
            }
        }

        public void stopScan() {

            if (IsConnected && IsReading) {

                IsReading = false;
                raiseEvent(stopReadingEventHandler);

                if (tagReadingThreadHandler != null) {

                    tagReadingThreadHandler.Abort();
                    tagReadingThreadHandler = null;
                }
            }
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {

            ReaderInfo readerInfo = new ReaderInfo(type, comPort, getReaderState());
            return readerInfo.InfoList;
        }

        public RfidDeviceState getReaderState() {
            throw new NotImplementedException();
        }

        public string getReaderComPort() {
            return comPort;
        }

        public string getReaderUUID() {
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

        public void switchOffAntenna()
        {

            throw new NotImplementedException();
        }

        public void switchOnAntenna()
        {

            throw new NotImplementedException();
        }
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
            throw new NotImplementedException();
        }

        public void setPower(int antenna, float power) {
            throw new NotImplementedException();
        }
        public void setPowerDynamically(int antenna, float power) {
            throw new NotImplementedException();
        }
        #endregion

        public SCAN_MODE ScanMode {
            get {

                SCAN_MODE scanMode = SCAN_MODE.CONTINUOUS_SCAN;

                String parametizedScanMode = null;
                try {
                    parametizedScanMode = (String)getParameters()["SCAN_MODE"];
                } catch (Exception) { }

                if (parametizedScanMode == null) {

                    raiseHasReportedAnErrorEvent("Invalid parametized SCAN_MODE");
                    return scanMode;
                }

                if (SCAN_MODE.CONTINUOUS_SCAN.Value == parametizedScanMode) {

                    scanMode = SCAN_MODE.CONTINUOUS_SCAN;

                } else if (SCAN_MODE.TRIGGERED_SCAN.Value == parametizedScanMode) {

                    scanMode = SCAN_MODE.TRIGGERED_SCAN;

                } else if (SCAN_MODE.INVENTORY_SCAN.Value == parametizedScanMode) {

                    scanMode = SCAN_MODE.INVENTORY_SCAN;

                } else {

                    raiseHasReportedAnErrorEvent("Invalid parametized SCAN_MODE");
                }

                return scanMode;
            }
        }

        public int ScanDuration {

            get {

                int scanDuration = DEFAULT_SCAN_DURATION;

                try {

                    int parametizedScanDuration = (int)getParameters()["SCAN_DURATION"];

                    if (parametizedScanDuration == null) {

                        raiseHasReportedAnErrorEvent("Invalid parametized SCAN_MODE");

                        return scanDuration;
                    }

                    if (parametizedScanDuration > 0) {

                        scanDuration = parametizedScanDuration;

                    } else {

                        raiseHasReportedAnErrorEvent("Invalid parametized SCAN_DURATION");
                    }
                } catch (Exception) {

                    raiseHasReportedAnErrorEvent("Invalid parametized SCAN_DURATION");
                }

                return scanDuration;
            }
        }

        public Dispatcher Dispatcher {
            get {
                return dispatcher;
            }
            set {
                dispatcher = value;
            }
        }

        #region PRIVATE_METHODS

        private void initializeSSCPLibEPC() {

            UInt16 status = StidReaderApi.SSCP_Initialize();

            if ((byte)status == StidReaderApi.SSCP_OK) {

                logProducer.Logger.Debug("SSCP Library initialised");

            } else {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to initialize SSCP Lib EPC");
            }
        }

        private void initializeReaderCom() {

            initializeComType();

            initializeComPort();

            initializeAutoConnect();

            //initializeIpAddress();            
        }

        private void initializeComType() {

            String comType = null;

            try {
                comType = (String)getParameters()["COM_TYPE"];
            } catch (Exception) {

                raiseHasReportedAnErrorEvent("Unable to initialize Communication type, invalid parameter");
            }

            UInt16 status = StidReaderApi.SSCPLIBEPC_ERROR_EXCEPTION;

            if (comType != null && (comType == "USB" || comType == "SERIAL")) {

                status = StidReaderApi.SSCP_SetCOMType(StidReaderApi.ct_rs232);
            } else {

                //status = StidReaderApi.SSCP_SetCOMType(StidReaderApi.ct_tcp);
            }

            if (status == StidReaderApi.SSCP_OK) {

                logProducer.Logger.Debug("communication type initialised");

                StidReaderApi.SSCP_TCP_c_SetTimeOut(500);

            } else {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to initialize Communication type");
            }
        }

        private void initializeComPort() {

            comPort = null;

            try {
                comPort = (String)getParameters()["COM_PORT"];
            } catch (Exception) {

                raiseHasReportedAnErrorEvent("Unable to initialize communication port, invalid parameter");
            }

            UInt16 status = StidReaderApi.SSCPLIBEPC_ERROR_EXCEPTION;

            if (comPort != null) {

                status = StidReaderApi.SSCP_Serial_SetPort(comPort); 
            }

            if (status != StidReaderApi.SSCP_OK) {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to initialize communication port");

                return;
            }

            String comType = (String)getParameters()["COM_TYPE"];

            if (comType != null && (comType == "USB" || comType == "SERIAL")) {

                status = StidReaderApi.SSCP_Serial_SetBaudRate(4); // 115200

                StidReaderApi.SSCP_Serial_SetTimeout(500, 500, 500, 500);
            }

            if (status != StidReaderApi.SSCP_OK) {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to initialize Com Port");

                return;
            }

            logProducer.Logger.Debug("Com port initialised");
        }

        private void initializeAutoConnect() {

            UInt16 status = StidReaderApi.SSCP_SetAutoConnect(Convert.ToByte(true));

            if ((byte)status == StidReaderApi.SSCP_OK) {

                logProducer.Logger.Debug("auto connect initialised");

                StidReaderApi.SSCP_TCP_c_SetTimeOut(500);

            } else {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to initialize auto connect");
            }
        }

        //private void initializeIpAddress() {

        //    UInt16 status = StidReaderApi.SSCP_TCP_c_SetIPAdr("192.168.62.108"); // TODO getParameters()[IP_ADDR]

        //    if ((byte)status == StidReaderApi.SSCP_OK) {

        //        logProducer.Logger.Debug("auto connect initialised");

        //        StidReaderApi.SSCP_TCP_c_SetTimeOut(500);

        //    } else {

        //        // oups! problem...
        //        getErrorMsgAndRaiseEvent(status, "Unable to initialize ip address");
        //    }
        //}



        //private void setMODE(OPERATING_MODE mode) {

        //    UInt16 status = StidReaderApi.SSCPreader_AutonomousStart(Convert.ToUInt16("10")); // TODO getParameters()[COM_PORT]

        //    if ((byte)status == StidReaderApi.SSCP_OK) {

        //        logProducer.Logger.Debug("com port initialised");

        //        StidReaderApi.SSCP_TCP_c_SetTimeOut(500);

        //    } else {

        //        // oups! problem...
        //        getErrorMsgAndRaiseEvent(status, "Unable to initialize com port");
        //    }
        //}

        private void initializeTagReadingThread() {

            if (tagReadingThreadHandler != null) {

                tagReadingThreadHandler.Abort();
                tagReadingThreadHandler = null;
            }

            tagReadingThreadHandler = new Thread(new ThreadStart(tagReadingThread));
            tagReadingThreadHandler.SetApartmentState(ApartmentState.STA);

            tagReadingThreadHandler.IsBackground = true;
        }

        private void terminateSSCPLibEPC() {

            UInt16 status = StidReaderApi.SSCP_Terminate();

            if ((byte)status == StidReaderApi.SSCP_OK) {

                logProducer.Logger.Debug("SSCP Library closed");

            } else {

                // oups! problem...
                getErrorMsgAndRaiseEvent(status, "Unable to terminate SSCP Lib EPC");
            }
        }

        private void getErrorMsgAndRaiseEvent(UInt16 statusCode, String unperformedOperation) {

            String errorMsg = "";

            StidReaderApi.SSCPepc_GetErrorMsg(cultureIdentifier, statusCode, ref errorMsg);

            errorMsg = unperformedOperation + ", due to : " + errorMsg;

            logProducer.Logger.Error(errorMsg);

            raiseHasReportedAnErrorEvent(errorMsg);
        }

        protected void raiseEvent(EventHandler eventhandler) {

            if (eventhandler != null) {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
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

        /// <summary>
        /// tag Reading Thread : start inventory and read tag
        /// </summary>
        protected void tagReadingThread() {

            if (ScanMode == SCAN_MODE.CONTINUOUS_SCAN) {

                performContinuousScan();

            } else if (ScanMode == SCAN_MODE.TRIGGERED_SCAN) {

                //performTriggeredScan();

            } else if (ScanMode == SCAN_MODE.INVENTORY_SCAN) {

                performInventoryScan(DEFAULT_SCAN_DURATION);

            } else {

                performContinuousScan();
            }
        }

        private void performContinuousScan() {

            List<String> previousSnrs = new List<String>();

            while (true) {

                List<String> snrs = new List<String>();

                byte nbTags = 0;
                byte[] tags = new byte[1024];

                // detect all EPC tags in reader's antenna field 
                UInt16 status = StidReaderApi.SSCPepc_Inventory(ref nbTags, tags);

                if ((byte)status != StidReaderApi.SSCP_OK) {

                    getErrorMsgAndRaiseEvent(status, "Unable to perform an continuous scan");
                    break;// stop scanning
                }

                // is there something?
                if (nbTags > 0) {

                    // yep !
                    String tagEpc = "";
                    int i = 0;         // first tag @ in Tags array
                    int iterator = nbTags;

                    while (iterator > 0) {

                        tagEpc = "";
                        // Gather tagepc 
                        for (int j = 0; j < tags[i]; j++) {

                            tagEpc += (tags[i + j + 1]).ToString("X2");
                        }

                        i += (byte)(tags[i] + 2 + 2); // Shift to @ of next tag in Tags array = Len of EPC + Len of Antenna Nb + Len of NbRead
                        iterator--;

                        if (!String.IsNullOrEmpty(tagEpc)) {
                            snrs.Add(tagEpc);
                        }
                    }
                }

                // no dups
                snrs = snrs.Distinct().ToList();

                // tags maintained in field aren't kept
                List<String> newSnrs = snrs.Where(i => !previousSnrs.Any(e => i.Contains(e))).ToList();

                // raise new entry only (if there is something to raise)
                if (newSnrs != null && newSnrs.Count > 0) {
                
                    raiseTagFoundEvent(newSnrs.ToList<String>());
                }

                // update previously inventoried tags
                previousSnrs = snrs;

                // rest for a moment...
                System.Threading.Thread.Sleep(150);

            }// end of while(true)

            stopScan();
        }

        //private void performTriggeredScan() {
        //    List<String> snrs = new List<String>();

        //    while (true) {

        //        Boolean gotTag = false;

        //        String snr;
        //        RU_RESULT res = unsafeStartTagInventory(out snr);

        //        if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

        //            raiseHasReportedAnErrorEvent("Unable to start tag inventory : " + res.ToString());
        //        }

        //        if (!String.IsNullOrEmpty(snr)) {

        //            snrs.Add(snr);
        //            snr = "";

        //            gotTag = true;
        //        }

        //        do {

        //            res = unsafeReadNextTag(out snr);

        //            if (res != RU_RESULT.UR_OK && res != RU_RESULT.UR_ERR_NO_TAGS) {

        //                raiseHasReportedAnErrorEvent("Unable to read next tag : " + res.ToString());
        //            }

        //            if (!String.IsNullOrEmpty(snr)) {

        //                snrs.Add(snr);
        //                snr = "";

        //                gotTag = true;
        //            }
        //        } while (res == RU_RESULT.UR_OK && gotTag == false);

        //        if (gotTag) {

        //            raiseTagFoundEvent(new List<String>() { snrs.First() });

        //            stopScan();

        //            break;
        //        }

        //        // rest for a moment...
        //        System.Threading.Thread.Sleep(250);
        //    }

        //}

        private void performInventoryScan(int duration) {

            List<String> snrs = new List<String>();

            TimeSpan interval = TimeSpan.FromMilliseconds(duration);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed < interval) {

                byte nbTags = 0;
                byte[] tags = new byte[1024];

                // detect all EPC tags in reader's antenna field 
                UInt16 status = StidReaderApi.SSCPepc_Inventory(ref nbTags, tags);

                if ((byte)status != StidReaderApi.SSCP_OK) {

                    getErrorMsgAndRaiseEvent(status, "Unable to perform an inventory scan");
                    break;
                }

                // is there something?
                if (nbTags > 0) {

                    // yep !
                    String tagEpc = "";
                    int i = 0;         // first tag @ in Tags array
                    int iterator = nbTags;

                    while (iterator > 0) {

                        tagEpc = "";
                        // Gather tagepc 
                        for (int j = 0; j < tags[i]; j++) {

                            tagEpc += (tags[i + j + 1]).ToString("X2");
                        }

                        i += (byte)(tags[i] + 2 + 2); // Shift to @ of next tag in Tags array = Len of EPC + Len of Antenna Nb + Len of NbRead
                        iterator--;

                        if (!String.IsNullOrEmpty(tagEpc)) {
                            snrs.Add(tagEpc);
                        }
                    }
                }

                // rest for a moment...
                System.Threading.Thread.Sleep(250);
            }

            stopwatch.Stop();

            List<String> distinctSnrs = snrs.Distinct().ToList();

            raiseTagFoundEvent(distinctSnrs);

            stopScan();
        }

        #endregion

        public class SCAN_MODE {

            public String Value { get; internal set; }

            public static SCAN_MODE CONTINUOUS_SCAN = new SCAN_MODE("CONTINUOUS_SCAN");
            public static SCAN_MODE TRIGGERED_SCAN = new SCAN_MODE("TRIGGERED_SCAN");
            public static SCAN_MODE INVENTORY_SCAN = new SCAN_MODE("INVENTORY_SCAN");

            private SCAN_MODE(String name) {
                Value = name;
            }
        }

        public byte getAntennaConfiguration()
        {
            throw new NotImplementedException();
        }
        /*
        private void buttonResetRFSettings_Click(object sender, EventArgs e)
        {
             UInt16 status = SSCPlibEPC.SSCPLIBEPC_ERROR_EXCEPTION;
             string mesg = "";

             CommandInProgress();

             status = SSCPlibEPC.SSCPreader_ResetRFSettings();
             if ((byte)status == SSCPlibEPC.SSCP_OK)
                 mesg = "RFSettings reset to default values";
             else SSCPlibEPC.SSCPepc_GetErrorMsg(gLangID, status, ref mesg);

             Display("SSCPreader_GetRFSettings", mesg, status);
         }

         /// <summary>
         /// Get current RF settings and display then into listViewGetRFSettings
         /// </summary>
         /// <param name="sender">Unused</param>
         /// <param name="e">Unused</param>
        private void buttonGetRFSettings_Click(object sender, EventArgs e)
        {
            UInt16 status = SSCPlibEPC.SSCPLIBEPC_ERROR_EXCEPTION;
            string mesg = "";
            byte[] A0_15 = new byte[4 * 16];
            int i;


            CommandInProgress();

            status = SSCPlibEPC.SSCPreader_GetRFSettings(A0_15);
            if ((byte)status == SSCPlibEPC.SSCP_OK)
            {

                for (i = 0; i < 16; i++)
                {
                    listViewGetRFSettings.Items[i].SubItems[0].Text = i.ToString();
                    listViewGetRFSettings.Items[i].SubItems[1].Text = ((A0_15[4 * i] << 8) + A0_15[4 * i + 1]).ToString();
                    listViewGetRFSettings.Items[i].SubItems[2].Text = ((A0_15[4 * i + 2] << 4) + ((A0_15[4 * i + 3] & 0xF0) >> 4)).ToString();
                    listViewGetRFSettings.Items[i].SubItems[3].Text = (A0_15[4 * i + 3] & 0x0F).ToString();

                    mesg += listViewGetRFSettings.Items[i].SubItems[1].Text + ',';
                    mesg += listViewGetRFSettings.Items[i].SubItems[2].Text + ',';

                    if ((i + 1) % 3 == 0)
                        mesg += listViewGetRFSettings.Items[i].SubItems[3].Text + "\n";
                    else
                        mesg += listViewGetRFSettings.Items[i].SubItems[3].Text + ';';

                }
            }
            else SSCPlibEPC.SSCPepc_GetErrorMsg(gLangID, status, ref mesg);

            Display("SSCPreader_GetRFSettings", mesg, status);
        }

        /// <summary>
        /// Assigns reader's RF settings from listViewSetRFSettings
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
         private void buttonSetRFSettings_Click(object sender, EventArgs e)
         {
             UInt16 status = SSCPlibEPC.SSCPLIBEPC_ERROR_EXCEPTION;
             string mesg = "";
             byte[] A0_15 = new byte[4 * 16];
             UInt64 tmp, a, b, c;


             CommandInProgress();

             for (int i = 0; i < 16; i++)
             {
                 if (listViewSetRFSettings.Items[i].SubItems[1].Text == "") // "" (nothing) is forbidden, relaced by '0'
                     listViewSetRFSettings.Items[i].SubItems[1].Text = "0";
                 if (listViewSetRFSettings.Items[i].SubItems[2].Text == "")
                     listViewSetRFSettings.Items[i].SubItems[2].Text = "0";
                 if (listViewSetRFSettings.Items[i].SubItems[3].Text == "")
                     listViewSetRFSettings.Items[i].SubItems[3].Text = "0";

                 a = UInt64.Parse(listViewSetRFSettings.Items[i].SubItems[1].Text);
                 b = UInt64.Parse(listViewSetRFSettings.Items[i].SubItems[2].Text);
                 c = UInt64.Parse(listViewSetRFSettings.Items[i].SubItems[3].Text);

                 tmp = (a << (int)(16)) + ((b & 0x0FFF) << (int)(4)) + (c & 0x000F);

                 A0_15[4 * i]     = (byte)((tmp & 0xFF000000) >> (int)24);
                 A0_15[4 * i + 1] = (byte)((tmp & 0x00FF0000) >> (int)16);
                 A0_15[4 * i + 2] = (byte)((tmp & 0x0000FF00) >> (int)8);
                 A0_15[4 * i + 3] = (byte)( tmp & 0x000000FF);

             }

             if (checkBoxSetRFSave.Checked) status = SSCPlibEPC.SSCPreader_SetRFSettings_Saved(A0_15);
             else status = SSCPlibEPC.SSCPreader_SetRFSettings(A0_15);

             if ((byte)status == SSCPlibEPC.SSCP_OK)
                 mesg = "RFSettings set";
             else SSCPlibEPC.SSCPepc_GetErrorMsg(gLangID, status, ref mesg);

             Display("SSCPreader_SetRFSettings", mesg, status);

         }
         */
    }
}
