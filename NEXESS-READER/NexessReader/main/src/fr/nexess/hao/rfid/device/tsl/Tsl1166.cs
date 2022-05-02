using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.log;
using TechnologySolutions.Rfid.AsciiOperations;
using TechnologySolutions.Rfid.AsciiProtocol;
using TechnologySolutions.Rfid.AsciiProtocol.Transports;
using TechnologySolutions.Rfid.AsciiProtocol.Extensions;
using TechnologySolutions.Rfid;

namespace fr.nexess.hao.rfid.device.tsl {
    public class Tsl1166 : RfidDevice, ConfigurableRfidDevice {
        // We try 1 minute to communicate with reader if it is unavailable
        // 1 minute = 60 000 ms
        private const int READER_UNAVAILABLE_TIMEOUT = 300;
        private const int READER_UNAVAILABLE_RETRY_COUNT = 200;

        // basic
        private LogProducer logProducer = new LogProducer(typeof(Tsl1166));
        protected static readonly Object locker = new Object();
        protected static Tsl1166 instance = null;
        protected static readonly Object lockerActiveTransport = new Object();

        // reader basic
        private RfidDeviceState state = RfidDeviceState.DISCONNECTED;
        private string version = "";
        private string comPort = "";
        private String type = "TSL1166";

        // configuration basic
        private Dictionary<String, Object> parameters = new Dictionary<String, Object>();
        private float power = 29; // default value

        // tsl data
        AsciiReader activeReader;
        IAsciiTransport activeTransport;
        IAsciiTransportsManager transportsManager;
        private string SERIAL_NUMBER = "1166-001685";
        HashSet<IAsciiTransport> bluetoothTransports;
        private IProgress<IReaderOperationBatteryStatus> batteryLevelUpdate;
        private IReaderOperationInventory OperationInventory { get; set; }
        private IReaderOperationBarcode OperationBarcode { get; set; }
        private IReaderOperationBatteryStatus batteryStatus;
        private string firmwareVersion = "";
        private ConnectionState lastConnectionState = ConnectionState.NotAvailable;

        // public data
        public static string ERROR_CONNECTING = "ERROR_CONNECTING";
        private bool isDisconnectionRequested = false;

        #region EVENT_HANDLER_DECLARATION
        private event HasReportedAnErrorEventHandler hasReportedAnErrorEventHandler;
        private event EventHandler connectedEventHandler;
        private event EventHandler disconnectedEventHandler;
        private event TagFoundEventHandler tagFoundEventHandler;
        private event CodeFoundEventHandler codeFoundEventHandler;
        private event BatteryLevelUpdatedEventHandler batteryLevelUpdateEventHandler;
        #endregion

        #region CONSTRUCTOR
        public static Tsl1166 getInstance() {
            // critical section
            lock (locker) {
                if (instance == null) {
                    instance = new Tsl1166();
                }
            }
            return instance;
        }

        public Tsl1166() {
            transportsManager = new AsciiTransportsManager();
            bluetoothTransports = new HashSet<IAsciiTransport>();

            this.transportsManager.TransportChanged += onTransportChanged;
            foreach (var enumerator in this.transportsManager.Enumerators) {
                if (enumerator.State == EnumerationState.Created) {
                    enumerator.Start();
                }
            }
        }
        #endregion

        #region TSL functions
        protected async void onTransportChanged(object sender, TransportStateChangedEventArgs e) {
            try {
                IAsciiTransport transport = e.Transport;
                if (e.Transport.Physical == PhysicalTransport.Bluetooth) {
                    if (e.Transport.State == ConnectionState.Available) {
                        logProducer.Logger.Info("BLUETOOTH : " + transport.DisplayName + " found");
                        try {
                            bluetoothTransports.Add(transport);
                        }
                        catch (Exception ex) {
                            logProducer.Logger.Error("Error adding transport, " + ex.Message);
                        }
                    }
                    else {
                        logProducer.Logger.Info("BLUETOOTH : " + transport.DisplayName + " " + e.Transport.State.ToString());
                        if (e.Transport.State == ConnectionState.Connected) {
                            await connectTsl(transport);
                        }
                    }
                    // check if Gun is off and need to retry connection
                    if (e.Transport.State == ConnectionState.Disconnected && lastConnectionState == ConnectionState.Connecting) {
                        await connectTsl(transport);
                    }
                    lastConnectionState = e.Transport.State;
                 }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception on transport state change, " + ex.Message);
            }
        }

        private async Task connectTsl(IAsciiTransport transport) {
            try {
                if (transport.State == TechnologySolutions.Rfid.AsciiProtocol.Transports.ConnectionState.Available
                        || transport.State == TechnologySolutions.Rfid.AsciiProtocol.Transports.ConnectionState.Disconnected
                        || transport.State == TechnologySolutions.Rfid.AsciiProtocol.Transports.ConnectionState.Lost) {
                    logProducer.Logger.Info("Try to connect to BLUETOOTH");
                    await ConnectTransport(transport);
                }
                else {
                    if (transport.State == ConnectionState.Connected) {

                        await Task.Run(async () => {
                            await IdentifyReaderAsync(transport);
                            //await sendCommandGetVersion(transport);
                        });
                    }
                }
            } catch (Exception ex) {
                logProducer.Logger.Info("Exception connecting to BLUETOOTH device, " + ex.Message);
            }
        }


        protected void onConnectionLost(object sender, ConnectionLostEventArgs e) {
        }

        private static async Task ConnectTransport(IAsciiTransport transport) {
            try {
                lock (lockerActiveTransport) {
                    transport.ConnectAsync();
                }
            } catch(Exception ex) {
                //logProducer.Logger.Info("Exception connecting transport, " + ex.Message);
            }
        }

        private async Task sendCommandGetVersion(IAsciiTransport transport) {
            try {
                logProducer.Logger.Info("Send command version .vr to reader");
                lock (lockerActiveTransport) {
                    transport.Connection.WriteLine(".vr");
                }
                for (int i = 0; i < READER_UNAVAILABLE_RETRY_COUNT; i++) {
                    if(isDisconnectionRequested) {
                        logProducer.Logger.Info("Disconnection required");
                        return;
                    }
                    if (transport.Connection?.IsLineAvailable ?? false) {
                        break;
                    }
                    Thread.Sleep(READER_UNAVAILABLE_TIMEOUT);
                    //await Task.Delay(500);
                }
                string serialNumber = "";
                this.firmwareVersion = "";
                while (transport.Connection?.IsLineAvailable ?? false) {
                    string line = transport.Connection.ReadLine();
                    try {
                        string startWith = line.Substring(0, 3);
                        string[] lineArray = line.Split(':');
                        switch (startWith) {
                            case "US:":
                                serialNumber = lineArray[1].Trim();
                                break;
                            case "UF:":
                                firmwareVersion = lineArray[1].Trim();
                                logProducer.Logger.Info("Firmware version: " + firmwareVersion);
                                break;
                        }
                    }
                    catch (Exception ex) {
                        // ignore, it is ""
                    }
                    System.Diagnostics.Debug.WriteLine(line);
                }
                if (!String.IsNullOrEmpty(serialNumber)) {
                    logProducer.Logger.Info("Serial number detected: " + serialNumber);
                    await IdentifyReaderAsync(transport);
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception sending command to reader, " + ex.Message);
            }
        }

        private async Task IdentifyReaderAsync(IAsciiTransport transport) {

            try {
                if (activeReader == null) {
                    activeReader = new AsciiReader();
                }
                if (!activeReader.IsConnected) {
                    logProducer.Logger.Debug("Step1");
                    activeReader.AddTransport(transport);
                    Thread.Sleep(200);
                    await activeReader.RefreshAsync();
                    logProducer.Logger.Debug("Step2");
                    if (activeReader.IsConnected) {
                        logProducer.Logger.Debug("Step3");
                        if (String.IsNullOrEmpty(activeReader.SerialNumber)) {
                            logProducer.Logger.Debug("Device Busy");
                            Thread.Sleep(200);
                            await activeReader.RefreshAsync();
                        }
                        string serialNumber = (String)parameters[ConfigurableReaderParameter.READER_NAME.ToString()];
                        if (!String.IsNullOrEmpty(activeReader.SerialNumber) 
                            && activeReader.SerialNumber == serialNumber) {
                            logProducer.Logger.Debug("Step4");
                            // New reader has been identified
                            lock (lockerActiveTransport) {
                                this.activeTransport = transport;
                                logProducer.Logger.Debug("Step5");
                            }
                            this.state = RfidDeviceState.CONNECTED;
                            logProducer.Logger.Debug("Step6");
                            transport.Connection.ConnectionLost += onConnectionLost;
                            prepareRfid();
                            logProducer.Logger.Debug("Step7");
                            prepareBarcode();
                            logProducer.Logger.Debug("Step8");
                            prepareBatteryStatus();
                            raiseIsConnected();
                        }
                    }
                    else {
                        Thread.Sleep(200);
                        transport.Disconnect();
                        Thread.Sleep(200);
                        return;
                    }
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception identifying BLUETOOTH device, " + ex.Message);
            }
        }

        private async void prepareBatteryStatus() {
            try {
                batteryLevelUpdate = new Progress<IReaderOperationBatteryStatus>(onBatteryStatusUpdate);
                batteryStatus = activeReader.OperationOfType<IReaderOperationBatteryStatus>();
                this.batteryStatus.Updated += (sender, e) => this.batteryLevelUpdate.Report(this.batteryStatus);
                await this.batteryStatus.EnableAsync();
                await this.batteryStatus.StartAsync();
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception preparing battery, " + ex.Message);
            }
        }

        private void stopBattery() {
            try {
                this.batteryStatus.StopAsync();
                this.batteryStatus.DisableAsync();
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Exception stopping battery, " + ex.Message);
            }
        }
        
        private async void prepareRfid() {
            try {
                if (activeReader != null) {
                    var inventoryOperation = activeReader.OperationOfType<IReaderOperationInventory>();
                    if (this.OperationInventory != inventoryOperation) {
                        if (this.OperationInventory != null) {
                            // disable disconnect the previous operation
                            this.OperationInventory.TranspondersReceived -= this.onTagFound;
                            await this.OperationInventory.DisableAsync();
                        }

                        this.OperationInventory = inventoryOperation;

                        if (this.OperationInventory != null) {
                            // enable connect the new current operation
                            this.OperationInventory.TranspondersReceived += this.onTagFound;
                            await this.OperationInventory.EnableAsync();
                        }
                    }
                }

            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception preparing rfid, " + ex.Message);
            }
        }

        private void stopRfid() {
            try {
                if (this.OperationInventory != null) {
                    // disable disconnect the previous operation
                    this.OperationInventory.TranspondersReceived -= this.onTagFound;
                    this.OperationInventory.DisableAsync();
                }
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Exception stopping rfid, " + ex.Message);
            }
        }

        private async void prepareBarcode() {
            try {
                if (activeReader != null) {
                    var operationBarcode = activeReader.OperationOfType<IReaderOperationBarcode>();
                    if (operationBarcode != null) {
                        this.OperationBarcode = operationBarcode;
                        this.OperationBarcode.BarcodeScanned += this.onBarcodeScanned;
                        await OperationBarcode.DisableAsync();
                        OperationBarcode.TriggerIndex = 2;
                        await OperationBarcode.EnableAsync();

                    }
                }
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Exception preparing barcode, " + ex.Message);
            }
        }

        private void stopBarcode() {
            try {
                if (this.OperationBarcode != null) {
                    // disable disconnect the previous operation
                    this.OperationBarcode.BarcodeScanned -= this.onBarcodeScanned;
                    this.OperationBarcode.DisableAsync();
                }
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Exception stopping rfid, " + ex.Message);
            }
        }

        private void raiseIsConnected() {
            logProducer.Logger.Info("Tsl reader is Connected");
            if (connectedEventHandler != null) {
                connectedEventHandler(this, new EventArgs());
            }
        }

        private void raiseHasReportedError() {
            if (hasReportedAnErrorEventHandler != null) {
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(1000);
                    hasReportedAnErrorEventHandler(this, new HasReportedAnErrorEventArgs(ERROR_CONNECTING));
                })).Start();
            }
        }
        #endregion

        #region TSL events
        private void onTagFound(object sender, TranspondersEventArgs e) {
            HashSet<string> tags = new HashSet<string>();
            foreach (var transponder in e.Transponders) {
                tags.Add(transponder.Epc);
            }
            if (tagFoundEventHandler != null) {
                tagFoundEventHandler(this, new eventHandler.TagFoundEventArgs(tags.ToList()));
            }
        }

        private void onBarcodeScanned(object sender, BarcodeEventArgs e) {
            if (codeFoundEventHandler != null) {
                codeFoundEventHandler(this, new eventHandler.CodeFoundEventArgs(e.Barcode));
            }
        }

        private void onBatteryStatusUpdate(IReaderOperationBatteryStatus batteryStatus) {
            if(batteryLevelUpdateEventHandler != null) {
                batteryLevelUpdateEventHandler(this, new BatteryLevelUpdatedEventArgs(batteryStatus.BatteryLevel));
            }
        }
        #endregion

        #region PUBLIC
        public async void connect() {
            try {
                isDisconnectionRequested = false;
                logProducer.Logger.Info("Try to connect ...");
                foreach (IAsciiTransport t in bluetoothTransports) {
                    new Thread(new ThreadStart(() => {
                        connectTsl(t);
                    })).Start();
                }
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception connecting to TSL, " + ex.Message);
            }
        }

        public void disconnect() {

            try {
                isDisconnectionRequested = true;
                //logProducer.Logger.Info("OK1");
                if(bluetoothTransports != null) {
                    foreach(IAsciiTransport tr in bluetoothTransports) {
                        tr.Disconnect();
                    }
                }
                //logProducer.Logger.Info("OK3");
                if (activeReader != null) {
                    stopRfid();
                    stopBarcode();
                    stopBattery();
                    //logProducer.Logger.Info("OK4");
                    activeReader.DisconnectAsync();
                    //logProducer.Logger.Info("OK5");
                    activeReader.RemoveTransport(activeTransport);
                }
                //logProducer.Logger.Info("OK6");
                lock (lockerActiveTransport) {
                    activeTransport = null;
                }
                activeReader = null;
                if (disconnectedEventHandler != null) {
                   // logProducer.Logger.Info("OK7");
                    disconnectedEventHandler(this, new EventArgs());
                }
                //logProducer.Logger.Info("OK8");
                this.state = RfidDeviceState.DISCONNECTED;
                logProducer.Logger.Info("Tsl reader is Disconnected");
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Exception disconnecting from TSL, " + ex.Message);
            }
        }

        public void startScan() {

        }

        public void stopScan() {

        }

        public RfidDeviceState getReaderState() {
            return this.state;
        }

        public String getReaderUUID() {
            return activeReader?.SerialNumber;

        }

        public String getReaderSwVersion() {
            // Firmwer version
            return this.firmwareVersion;
        }

        public String getReaderComPort() {
            return activeReader?.ActiveTransport?.Id;
        }

        public Dictionary<EnumReaderType, string> getReaderDetail() {
            ReaderInfo readerInfo = new ReaderInfo(type, "Bluetooth", getReaderState());
            readerInfo.Version = getReaderSwVersion();
            return readerInfo.InfoList;
        }

        public bool transferCfgFile(string fileName) {
            throw new NotImplementedException();
        }

        public void switchOffAntenna() {
            throw new NotImplementedException();
        }
        public void switchOnAntenna() {
            throw new NotImplementedException();
        }
        #endregion

        #region CONFIGURATION
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
            return parameters;
        }

        public float getPower(int antenna) {
            return power;
        }

        public void setPower(int antenna, float power) {
            try {
                TagFields fields = TagFields.Epc;
                var filter = TagFilter.All().AtPower((int)power).Report(fields);
                this.OperationInventory.Filter = filter;
                this.power = power;
            }
            catch(Exception ex) {
                logProducer.Logger.Error("Exception changing power of the reader, " + ex.Message);
            }
        }

        public void setPowerDynamically(int antenna, float power) {
            throw new NotImplementedException();
        }

        public byte getAntennaConfiguration() {
            throw new NotImplementedException();
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
        public event CodeFoundEventHandler codeScanned {
            add {
                lock (locker) {
                    codeFoundEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    codeFoundEventHandler -= value;
                }
            }
        }
        public event BatteryLevelUpdatedEventHandler batteryLevelUpdated {
            add {
                lock (locker) {
                    batteryLevelUpdateEventHandler += value;
                }
            }
            remove {
                lock (locker) {
                    batteryLevelUpdateEventHandler -= value;
                }
            }
        }
        public event EventHandler StartReading {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }
        public event EventHandler StopReading {
            add {
                throw new NotImplementedException();
            }
            remove {
                throw new NotImplementedException();
            }
        }
        #endregion      
    }
}
