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
    /// Interaction logic for Cpr02_10_bIntegrationTest.xaml
    /// </summary>
    public partial class Cpr02_10_bIntegrationTest : Window {

        private Cpr02_10_b reader = null;

        public Cpr02_10_bIntegrationTest() {
            InitializeComponent();

            this.listBox1.Items.Clear();

            reader = Cpr02_10_b.getInstance();

            // listener binding...
            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new nexess.hao.rfid.eventHandler.HasReportedAnErrorEventHandler(reader_HasReportedAnError);

        }

        #region CROSS_THREADING_METHOD
        private void addToListBox(string msg) {
            this.listBox1.Items.Add(msg);
        }
        private void changeControlState(RfidDeviceEvent readerEvent) {
            if (readerEvent == RfidDeviceEvent.DISCONNECTED) {
                this.button1.Content = "connect";
            }
            if (readerEvent == RfidDeviceEvent.CONNECTED) {
                this.button1.Content = "disconnect";
            }
        }
        #endregion

        #region CLICK_EVENT_HANDLER
        private void button1_Click(object sender, RoutedEventArgs e) {

            if ((String)this.button1.Content == "connect") {

                reader.ComPort = 2;
                reader.connect();

                this.listBox1.Items.Add(reader.getReaderUUID());

            } else {
                reader.disconnect();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) {

            Boolean in1, in2, relay;

            int status = reader.getInputs();

            if (status == 0) { // means OK
                in1 = reader.readIn1();
                in2 = reader.readIn2();

                relay = reader.isRelayOn();

                textBox1.Text = (in1) ? "ON" : "OFF";
                textBox2.Text = (in2) ? "ON" : "OFF";
                textBox3.Text = (relay) ? "ON" : "OFF";
            }

        }

        private void button3_Click(object sender, RoutedEventArgs e) {

            if ((String)this.button3.Content == "activate relay") {

                this.button3.Content = "deactivate relay";

                reader.activateRelayContinuously();

            } else {
                this.button3.Content = "activate relay";

                reader.deactivateRelay();
            }

        }
        #endregion

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




    }
}
