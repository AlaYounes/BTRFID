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

namespace fr.nexess.hao.rfid.device.feig {
    /// <summary>
    /// Interaction logic for Lru3500IntegrationTest.xaml
    /// </summary>
    public partial class Lru3500IntegrationTest : Window {

        private RfidDevice    reader = null;

        public Lru3500IntegrationTest() {

            InitializeComponent();
            this.listBox1.Items.Clear();

            // tag reader handler instanciation
            //ISC_LRU3500.getInstance().ComPort = 2;
            reader = ISC_LRU3500.getInstance();

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new nexess.hao.rfid.eventHandler.HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);
            reader.TagFound += new nexess.hao.rfid.eventHandler.TagFoundEventHandler(reader_TagFound);
        }

        void reader_TagFound(object sender, nexess.hao.rfid.eventHandler.TagFoundEventArgs e) {

            List<String> snrs = e.Snrs;

            foreach (string snr in snrs) {
                string msg = "onTagFound : " + snr;
                Console.WriteLine(msg);
                // Update UI
                Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            }
        }

        void reader_StopReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.STOPPED_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            Dispatcher.BeginInvoke((Action)(() => changeControlState(RfidDeviceEvent.STOPPED_READING)));
        }

        void reader_StartReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.START_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            Dispatcher.BeginInvoke((Action)(() => changeControlState(RfidDeviceEvent.START_READING)));
        }

        void reader_HasReportedAnError(object sender, nexess.hao.rfid.eventHandler.HasReportedAnErrorEventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.HAS_REPORTED_AN_ERROR.ToString() + " - " + e.Message;
            Console.WriteLine(msg);

            // Update UI
            Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            Dispatcher.BeginInvoke((Action)(() => changeControlState(RfidDeviceEvent.HAS_REPORTED_AN_ERROR)));
        }

        void reader_Disconnected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.DISCONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            Dispatcher.BeginInvoke((Action)(() => changeControlState(RfidDeviceEvent.DISCONNECTED)));
        }

        void reader_Connected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + RfidDeviceEvent.CONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            Dispatcher.BeginInvoke((Action)(() => addToListBox(msg)));
            Dispatcher.BeginInvoke((Action)(() => changeControlState(RfidDeviceEvent.CONNECTED)));
        }

        #region CROSS_THREADING_METHOD
        private void addToListBox(string msg) {
            this.listBox1.Items.Add(msg);
        }
        private void changeControlState(RfidDeviceEvent readerEvent) {
            if (readerEvent == RfidDeviceEvent.DISCONNECTED) {
                this.button1.Content = "connect";
                this.button2.Content = "start scan";
                this.button2.IsEnabled = false;
            }
            if (readerEvent == RfidDeviceEvent.CONNECTED) {
                this.button1.Content = "disconnect";
                this.button2.IsEnabled = true;
            }
            if (readerEvent == RfidDeviceEvent.START_READING) {
                this.button2.Content = "stop scan";
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
            } else {
                reader.disconnect();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            if ((String)this.button2.Content == "start scan") {
                reader.startScan();
            } else {
                reader.stopScan();
            }
        }

        private void button3_Click(object sender, RoutedEventArgs e) {
            Boolean in1;

            int status = ((ISC_LRU3500)reader).getInputs();

            if (status == 0) { // means OK
                in1 = ((ISC_LRU3500)reader).readIn1();

                textBox1.Text = (in1) ? "ON" : "OFF";
            }
        }
        #endregion
    }
}
