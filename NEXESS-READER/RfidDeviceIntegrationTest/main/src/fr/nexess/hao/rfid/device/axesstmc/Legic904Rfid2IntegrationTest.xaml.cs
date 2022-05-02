using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.comm;

namespace fr.nexess.hao.rfid.device.axesstmc {
    /// <summary>
    /// Interaction logic for Legic904Rfid2IntegrationTest.xaml
    /// </summary>
    public partial class Legic904Rfid2IntegrationTest : Window {

        private RfidDevice  reader = null;
        private ComHandler  comHandler = null;

        public Legic904Rfid2IntegrationTest() {

            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(Legic904Rfid2IntegrationTest_Closing);

            this.listBox1.Items.Clear();

            // tag reader handler instanciation

            Legic904Rfid2 legic904 = Legic904Rfid2.getInstance();

            reader = legic904;

            // listener binding...
            reader.TagFound += new TagFoundEventHandler(onBadgeFound);

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new nexess.hao.rfid.eventHandler.HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);

            legic904.ModeChangedToAutoReading += new EventHandler(legic904_ModeChangedToAutoReading);
            legic904.ModeChangedToCommand += new EventHandler(legic904_ModeChangedToCommand);
        }

        #region CROSS_THREADING_METHOD
        private void addToListBox(string msg) {
            this.listBox1.Items.Add(msg);
        }
        private void changeControlState(RfidDeviceEvent readerEvent) {
            if (readerEvent == RfidDeviceEvent.DISCONNECTED) {
                this.button1.Content = "connect";
                this.button2.IsEnabled = false;
                this.button3.IsEnabled = false;
            }
            if (readerEvent == RfidDeviceEvent.CONNECTED) {
                this.button1.Content = "disconnect";
                this.button2.IsEnabled = true;
                this.button3.IsEnabled = true;
            }
            if (readerEvent == RfidDeviceEvent.START_READING) {
                this.button2.Content = "scanning";
            }
            if (readerEvent == RfidDeviceEvent.STOPPED_READING) {
                this.button2.Content = "start scan";
            }
        }
        #endregion

        #region CLICK_EVENT_HANDLER
        private void button1_Click(object sender, RoutedEventArgs e) {
            if ((String)this.button1.Content == "connect") {
                reader.connect();
                comHandler = Legic904Rfid2.ComHandler;
            } else {
                reader.disconnect();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            reader.startScan();
        }

        private void button3_Click(object sender, RoutedEventArgs e) {
            reader.stopScan();
        }
        #endregion

        void reader_StopReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.STOPPED_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.STOPPED_READING);
        }

        void reader_StartReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.START_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.START_READING);
        }

        void reader_HasReportedAnError(object sender, nexess.hao.rfid.eventHandler.HasReportedAnErrorEventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.HAS_REPORTED_AN_ERROR.ToString() + " - " + e.Message;
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.HAS_REPORTED_AN_ERROR);
        }

        void reader_Disconnected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.DISCONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.DISCONNECTED);
        }

        void reader_Connected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.CONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.CONNECTED);
        }

        protected void onBadgeFound(object sender, TagFoundEventArgs e) {
            foreach (string snr in e.Snrs) {
                string msg = "SNR : " + snr;

                addToListBox(msg);
            }
        }

        private void legic904_ModeChangedToCommand(object sender, EventArgs e) {

            // Update UI
            addToListBox("change legic mode to command : [OK]");
            addToListBox("please, switch off this app and disconnect the card reader");
        }

        private void legic904_ModeChangedToAutoReading(object sender, EventArgs e) {
            // Update UI
            addToListBox("change legic mode to automatic reading : [OK]");
            addToListBox("please, switch off this app and disconnect the card reader");
        }

        private void button4_Click(object sender, RoutedEventArgs e) {
            String fromTextBox1 = textBox1.Text;

            if (!String.IsNullOrEmpty(fromTextBox1)) {

                byte[] bytesToSend = toolbox.ConversionTool.stringToByteArray(fromTextBox1);

                if (comHandler != null) {

                    comHandler.sendData(bytesToSend);
                }
            }
        }

        private void button5_Click(object sender, RoutedEventArgs e) {

            if (reader == null
                || reader.getReaderState() == RfidDeviceState.DISCONNECTED) {

                return;
            }

            ((Legic904Rfid2)reader).switchMode(MODE.COMMAND);

        }

        private void button7_Click(object sender, RoutedEventArgs e) {
            if (reader == null
                || reader.getReaderState() == RfidDeviceState.DISCONNECTED) {

                return;
            }

            ((Legic904Rfid2)reader).switchMode(MODE.AUTOMATIC_READING);
        }

        private void button6_Click(object sender, RoutedEventArgs e) {
            if (reader == null
                || reader.getReaderState() == RfidDeviceState.DISCONNECTED) {

                return;
            }

            foreach (KeyValuePair<String, String> pair in ((Legic904Rfid2)reader).ReaderInfo) {

                addToListBox(pair.Key + " - " + pair.Value);
            }
        }


        void Legic904Rfid2IntegrationTest_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            Environment.Exit(Environment.ExitCode);
        }


    }
}
