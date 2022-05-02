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
using fr.nexess.hao.reader;
using fr.nexess.hao.reader.optic.device.opticon;
using fr.nexess.hao.reader.optic;
using fr.nexess.hao.reader.optic.eventhandler;

namespace RfidDeviceIntegrationTest.main.src.fr.nexess.hao.optic.device.opticon {

    public partial class NLV3101Integrationtest : Window {

        private OpticReader  reader = null;

        public NLV3101Integrationtest() {

            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(NLV3101integrationtest_Closing);

            this.listBox1.Items.Clear();

            // tag redaer instanciation
            NLV3101 opticon = NLV3101.getInstance();

            reader = opticon;

            // listener binding...
            reader.ReportAfterReading += new ReportAfterOpticalReadingEventHandler(reader_ReportAfterReading);

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

        private void changeControlState(ReaderEvent readerEvent) {
            if (readerEvent == ReaderEvent.DISCONNECTED) {
                this.button1.Content = "connect";
                this.button2.IsEnabled = false;
                this.button3.IsEnabled = false;
            }
            if (readerEvent == ReaderEvent.CONNECTED) {
                this.button1.Content = "disconnect";
                this.button2.IsEnabled = true;
                this.button3.IsEnabled = true;
            }
            if (readerEvent == ReaderEvent.START_READING) {
                this.button2.Content = "scanning";
            }
            if (readerEvent == ReaderEvent.STOPPED_READING) {
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

        private void button9_Click(object sender, RoutedEventArgs e) {
            this.listBox1.Items.Clear();
        }
        #endregion

        void reader_StopReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + ReaderEvent.STOPPED_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(ReaderEvent.STOPPED_READING);
        }

        void reader_StartReading(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + ReaderEvent.START_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(ReaderEvent.START_READING);
        }

        void reader_HasReportedAnError(object sender, HasReportedAnErrorEventArgs e) {

            string msg = "onReaderEvent : " + ReaderEvent.HAS_REPORTED_AN_ERROR.ToString() + " - " + e.Message;
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(ReaderEvent.HAS_REPORTED_AN_ERROR);
        }

        void reader_Disconnected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + ReaderEvent.DISCONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(ReaderEvent.DISCONNECTED);
        }

        void reader_Connected(object sender, EventArgs e) {

            string msg = "onReaderEvent : " + ReaderEvent.CONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(ReaderEvent.CONNECTED);
        }

        void reader_ReportAfterReading(object sender, ReportAfterOpticalReadingEventArgs e) {

            foreach (string caption in e.Captions) {

                addToListBox(caption);
            }
        }

        private void NLV3101integrationtest_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            Environment.Exit(Environment.ExitCode);
        }
    }
}
