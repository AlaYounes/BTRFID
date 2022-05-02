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
using fr.nexess.hao.rfid;
using fr.nexess.hao.rfid.device.mti;
using fr.nexess.hao.rfid.eventHandler;

namespace RfidDeviceIntegrationTest.main.src.fr.nexess.hao.rfid.device.mti {
    /// <summary>
    /// Interaction logic for Ru_865Integrationtest.xaml
    /// </summary>
    public partial class Ru_865Integrationtest : Window {

        private RfidDevice  reader = null;

        public Ru_865Integrationtest() {

            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(Ru_865Integrationtest_Closing);

            this.listBox1.Items.Clear();

            // tag redaer instanciation
            Ru_865 ru_865 = Ru_865.getInstance();

            reader = ru_865;

            // listener binding...
            reader.TagFound += new TagFoundEventHandler(onBadgeFound);

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);
        }

        #region CROSS_THREADING_METHOD
        private void addToListBox(string msg) {
            this.listBox1.Items.Add(msg);
            this.listBox1.SelectedIndex = listBox1.Items.Count - 1;
            listBox1.ScrollIntoView(this.listBox1.Items[this.listBox1.SelectedIndex]);
        }
        private void addToTextBox(String text) {
            this.textBox1.Text = text;
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

            try {
                if ((String)this.button1.Content == "connect") {

                    reader.connect();

                } else {

                    reader.disconnect();
                }
            } catch (Exception ex) {

                addToListBox(ex.Message + "-" + ex.StackTrace);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e) {
            reader.startScan();
        }

        private void button3_Click(object sender, RoutedEventArgs e) {
            reader.stopScan();
        }

        private void button4_Click(object sender, RoutedEventArgs e) {

            if (reader != null) {

                float value = ((Ru_865)reader).getPower(0);

                addToTextBox(value.ToString());
            }

        }

        private void button5_Click(object sender, RoutedEventArgs e) {
            if (reader != null) {

                float value;

                Boolean isGood = float.TryParse(this.textBox1.Text, out value);
                if (isGood) {
                    ((Ru_865)reader).setPower(0, value);
                    addToListBox("power set to " + value.ToString());
                }
            }
        }

        private void button6_Click(object sender, RoutedEventArgs e) {

            if (reader != null) {
                ((Ru_865)reader).ScanMode = SCAN_MODE.INVENTORY_SCAN;
            }
        }

        private void button7_Click(object sender, RoutedEventArgs e) {
            if (reader != null) {
                ((Ru_865)reader).ScanMode = SCAN_MODE.TRIGGERED_SCAN;
            }
        }

        private void button8_Click(object sender, RoutedEventArgs e) {
            if (reader != null) {
                ((Ru_865)reader).ScanMode = SCAN_MODE.CONTINUOUS_SCAN;
            }
        }

        private void button9_Click(object sender, RoutedEventArgs e) {
            this.listBox1.Items.Clear();
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

        void reader_HasReportedAnError(object sender, HasReportedAnErrorEventArgs e) {

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

                addToListBox(snr);
            }
        }

        private void Ru_865Integrationtest_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }
    }
}
