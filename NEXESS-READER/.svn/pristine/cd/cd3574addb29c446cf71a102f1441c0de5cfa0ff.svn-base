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
using fr.nexess.toolbox.comm;
using fr.nexess.hao.rfid.eventHandler;
using System.Configuration;

namespace fr.nexess.hao.rfid.device.stid
{
    /// <summary>
    /// Interaction logic for StidReaderIntegrationTest.xaml
    /// </summary>
    public partial class StidReaderIntegrationTest : Window
    {

        private RfidDevice reader = null;

        public StidReaderIntegrationTest()
        {

            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(StidReaderIntegrationTest_Closing);

            this.listBox1.Items.Clear();

            // tag reader handler instanciation
            SscpReader sscpReader = SscpReader.getInstance();

            String comPort = "COM60";

            try
            {
                comPort = ConfigurationManager.AppSettings["STID_COM_PORT"];
            }
            catch (Exception ex)
            {
                addToListBox("Unable to retrieve STID_COM_PORT value from configuration");
            }

            reader = sscpReader;
            ((ConfigurableRfidDevice)reader).setParameter("COM_PORT", comPort);
            ((ConfigurableRfidDevice)reader).setParameter("SCAN_MODE", SscpReader.SCAN_MODE.CONTINUOUS_SCAN.Value);
            ((ConfigurableRfidDevice)reader).setParameter("COM_TYPE", "USB");

            // listener binding...
            reader.TagFound += new TagFoundEventHandler(onBadgeFound);

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new nexess.hao.rfid.eventHandler.HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);
        }

        #region CROSS_THREADING_METHOD
        private void addToListBox(string msg)
        {
            this.listBox1.Items.Add(msg);
        }
        private void changeControlState(RfidDeviceEvent readerEvent)
        {
            if (readerEvent == RfidDeviceEvent.DISCONNECTED)
            {
                this.button1.Content = "connect";
                this.button2.IsEnabled = false;
                this.button3.IsEnabled = false;
            }
            if (readerEvent == RfidDeviceEvent.CONNECTED)
            {
                this.button1.Content = "disconnect";
                this.button2.IsEnabled = true;
                this.button3.IsEnabled = true;
            }
            if (readerEvent == RfidDeviceEvent.START_READING)
            {
                this.button2.Content = "scanning";
            }
            if (readerEvent == RfidDeviceEvent.STOPPED_READING)
            {
                this.button2.Content = "start scan";
            }
        }
        #endregion

        #region CLICK_EVENT_HANDLER
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if ((String)this.button1.Content == "connect")
            {

                reader.connect();

            }
            else
            {

                reader.disconnect();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            reader.startScan();
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            reader.stopScan();
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null
                || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {

                return;
            }

            //foreach (KeyValuePair<String, String> pair in ((Legic904Rfid2)reader).ReaderInfo) {

            //    addToListBox(pair.Key + " - " + pair.Value);
            //}
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null
                || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {


                return;
            }

            //((ConfigurableRfidDevice)reader).setParameter("OPERATING_MODE", SscpReader.OPERATING_MODE.AUTONOMOUS_MODE);
        }
        #endregion

        #region READER_DRIVING_EVENT_HANDLER
        private void reader_StopReading(object sender, EventArgs e)
        {

            string msg = "onReaderEvent : " + RfidDeviceEvent.STOPPED_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.STOPPED_READING);
        }

        private void reader_StartReading(object sender, EventArgs e)
        {

            string msg = "onReaderEvent : " + RfidDeviceEvent.START_READING.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.START_READING);
        }

        private void reader_HasReportedAnError(object sender, nexess.hao.rfid.eventHandler.HasReportedAnErrorEventArgs e)
        {

            string msg = "onReaderEvent : " + RfidDeviceEvent.HAS_REPORTED_AN_ERROR.ToString() + " - " + e.Message;
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.HAS_REPORTED_AN_ERROR);
        }

        private void reader_Disconnected(object sender, EventArgs e)
        {

            string msg = "onReaderEvent : " + RfidDeviceEvent.DISCONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.DISCONNECTED);
        }

        private void reader_Connected(object sender, EventArgs e)
        {

            string msg = "onReaderEvent : " + RfidDeviceEvent.CONNECTED.ToString();
            Console.WriteLine(msg);

            // Update UI
            addToListBox(msg);
            changeControlState(RfidDeviceEvent.CONNECTED);
        }

        private void onBadgeFound(object sender, TagFoundEventArgs e)
        {
            foreach (string snr in e.Snrs)
            {
                string msg = "SNR : " + snr;

                addToListBox(msg);
            }
        }
        #endregion

        private void StidReaderIntegrationTest_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            Environment.Exit(Environment.ExitCode);
        }
    }
}
