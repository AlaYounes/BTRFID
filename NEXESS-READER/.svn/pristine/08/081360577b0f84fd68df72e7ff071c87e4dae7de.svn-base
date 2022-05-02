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
using fr.nexess.hao.weight;
using fr.nexess.toolbox.comm;
using fr.nexess.hao.weight.device.pcb12;
using fr.nexess.hao.weight.eventhandler;
using System.Collections;
using System.Configuration;
using System.Threading;
using fr.nexess.hao.reader.weight;
using fr.nexess.hao.reader;

namespace RfidDeviceIntegrationTest.main.src.fr.nexess.hao.weight.reader.pcb12 {
    /// <summary>
    /// Interaction logic for Pcb12IntegrationTest.xaml
    /// </summary>
    public partial class Pcb12IntegrationTest : Window {

        private WeightReader reader = null;

        private static Thread weightReadingThreadHandler = null;

        public int PCB_ADDR { get; set; }
        public int PAD_NBR { get; set; }
        public String Weight { get; set; }

        public Pcb12IntegrationTest() {

            InitializeComponent();

            this.comboBox1.ItemsSource = new ArrayList() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            this.comboBox2.ItemsSource = new ArrayList() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            this.comboBox3.ItemsSource = new ArrayList() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            this.comboBox4.ItemsSource = new ArrayList() { "ALL_WEIGHTS", "ALL_WEIGHTS_FOR_CONNECTED_PAD", "VALID_WEIGHT_ONLY" };

            this.comboBox1.SelectedIndex = 0;
            this.comboBox2.SelectedIndex = 0;
            this.comboBox3.SelectedIndex = 0;
            this.comboBox4.SelectedIndex = 0;

            this.Closing += new System.ComponentModel.CancelEventHandler(Pcb12IntegrationTest_Closing);

            this.listBox1.Items.Clear();

            String comPortFromConfig = "";
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["WEIGHT_COM_PORT"])) {

                comPortFromConfig = ConfigurationManager.AppSettings["WEIGHT_COM_PORT"];
            }

            Pcb12Handler.WeightComPort = comPortFromConfig;//"COM63";
            Pcb12Handler pcb12Handler = Pcb12Handler.getInstance();
            pcb12Handler.RequestWeightTimeout = 200;

            pcb12Handler.PcbAddresses.Add(12);

            reader = pcb12Handler;

            // listener binding...
            reader.ReportAfterReading += new ReportAfterReadingEventHandler(onReportAfterReading);

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError += new HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);

            initializeWeightReadingThread();
        }

        private void initializeWeightReadingThread() {

            // thread instanciation
            if (weightReadingThreadHandler == null) {

                weightReadingThreadHandler = new Thread(new ThreadStart(weightReadingThread));

                // set the apartement state of a thread before it is started
                weightReadingThreadHandler.SetApartmentState(ApartmentState.STA);
            }

            weightReadingThreadHandler.IsBackground = true;
        }        

        private void button1_Click(object sender, RoutedEventArgs e) {

            if ((String)this.button1.Content == "connect") {

                reader.connect();

            } else {

                reader.disconnect();
            }

        }

        private void button2_Click(object sender, RoutedEventArgs e) {

            String rawWeightShowingLevel = (String)comboBox4.SelectionBoxItem;

            if (rawWeightShowingLevel == "ALL_WEIGHTS") {

                ((Pcb12Handler)reader).RawWeightShowingLevel = RAW_WEIGHT_SHOWING_LEVEL.ALL_WEIGHTS;
            }

            if (rawWeightShowingLevel == "ALL_WEIGHTS_FOR_CONNECTED_PAD") {

                ((Pcb12Handler)reader).RawWeightShowingLevel = RAW_WEIGHT_SHOWING_LEVEL.ALL_WEIGHTS_FOR_CONNECTED_PAD;
            }

            if (rawWeightShowingLevel == "VALID_WEIGHT_ONLY") {

                ((Pcb12Handler)reader).RawWeightShowingLevel = RAW_WEIGHT_SHOWING_LEVEL.VALID_WEIGHTS_ONLY;
            }

            Dispatcher.BeginInvoke((Action)(() => reader.startScan()));
        }

        private void button3_Click(object sender, RoutedEventArgs e) {

            Dispatcher.BeginInvoke((Action)(() => reader.stopScan()));
        }

        private void button4_Click(object sender, RoutedEventArgs e) {

            int pcb12Addr = getPcbAddr();
            int padId = getPadNbr();

            Weight = ((Pcb12Handler)reader).getOnePcbWeightSync(pcb12Addr, padId);

            this.textBox1.Text = (Weight != null) ? Weight.Trim().Replace(" ", "") : "???";


            if (this.button4.Content.Equals("Stop getting Weight")) {
                this.button4.Content = "get Weight";

                if (weightReadingThreadHandler != null && weightReadingThreadHandler.IsAlive) {

                    weightReadingThreadHandler.Abort();
                }
            } else {

                Boolean? startAReadingtask = this.checkBox1.IsChecked;

                if (startAReadingtask != null && startAReadingtask == true) {

                    this.button4.Content = "Stop getting Weight";

                    if (weightReadingThreadHandler != null && !weightReadingThreadHandler.IsAlive) {

                        if(weightReadingThreadHandler.ThreadState == ThreadState.Aborted){

                            weightReadingThreadHandler = new Thread(new ThreadStart(weightReadingThread));

                            // set the apartement state of a thread before it is started
                            weightReadingThreadHandler.SetApartmentState(ApartmentState.STA);
                        }

                        weightReadingThreadHandler.Start();
                    }
                } else {

                    Weight = ((Pcb12Handler)reader).getOnePcbWeightSync(pcb12Addr, padId);

                    this.textBox1.Text = (Weight != null) ? Weight.Trim().Replace(" ", "") : "???";
                }

            }
           
        }        

        private void button5_Click(object sender, RoutedEventArgs e) {

            int pcb12Addr = (int)comboBox3.SelectionBoxItem;

            List<String> weights = ((Pcb12Handler)reader).getPcbWeightsSync(pcb12Addr);

            foreach (var weight in weights) {
                addToListBox(weight);
            }
        }

        private void button6_Click(object sender, RoutedEventArgs e) {

            int pcb12Addr = getPcbAddr();
            int padNbr = getPadNbr();

            String weight = ((Pcb12Handler)reader).setZeroScale(pcb12Addr, padNbr);
        }

        private void Pcb12IntegrationTest_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

            Environment.Exit(Environment.ExitCode);
        }

        protected void onReportAfterReading(object sender, ReportAfterReadingEventArgs e) {


            foreach (var weight in e.Weights) {

                string msg = "PCB board @ : " + weight.Item1 + " - PAD # : " + weight.Item2 + " - Weight : " + weight.Item3;

                addToListBox(msg);
            }
        }

        #region CROSS_THREADING_METHOD
        public int getPcbAddr() {

            Dispatcher.BeginInvoke((Action)(() => PCB_ADDR = (int)comboBox1.SelectionBoxItem));

            return PCB_ADDR;
        }

        public int getPadNbr() {

            Dispatcher.BeginInvoke((Action)(() => PAD_NBR = (int)comboBox2.SelectionBoxItem));

            return PAD_NBR;
        }

        public void WeightToTextBox() {

            Dispatcher.BeginInvoke((Action)(() => this.textBox1.Text = (Weight != null) ? Weight.Trim().Replace(" ", "") : "???"));
            //this.textBox1.Text = msg.Trim().Replace(" ","");
        }
        private void addToListBox(string msg) {

            this.listBox1.Items.Add(msg);
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

        #region THREAD
        protected void weightReadingThread() {

            while (true) {

                Dispatcher.BeginInvoke((Action)(() => Weight = ((Pcb12Handler)reader).getOnePcbWeightSync(PCB_ADDR, PAD_NBR)));

                WeightToTextBox();

                Thread.Sleep(250);
            }
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
    }
}
