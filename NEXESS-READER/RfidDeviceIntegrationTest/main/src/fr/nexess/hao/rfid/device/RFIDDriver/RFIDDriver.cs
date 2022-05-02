


// |==================================================================================|
// |Program Name  : RFIDDriver.cs                                                     |
// |Actual Vers.  : 1.0                                                               |
// |Author        : Steve Begelman (Steve.Begelman@gmail.com)                         |
// |                                                                                  |
// |Company       : Nexess Sophia-Antipolis                                           |
// |Creation Date : June 2015                                                         |
// |                                                                                  |
// |%%SVN%% Rev.  : Untagged/Versioned                                                |
// |SVN  Keywords : utility service surveillance Query  RFID   dto                    |
// |                                                                                  |
// |Dependencies  :  Type : This a "stand-alone" Windows 7.x Executable Winform       |
// |                 Uses : Log4Net Application Loging service                        |
// |                                                                                  |
// |Stored Procs. :  none : Uses Proxies for Data Base Access                         |
// |              :                                                                   |
// |----------------------------------------------------------------------------------|
// |Revision Hist.:  dd/mm/yy   |               Modification                   |  By  |
// |----------------------------------------------------------------------------------|
// |   1          :  17/06/15   |   Version 1 Delivered: Working Prototype OK  |  SB  |
// |   1.1        :  20/06/05   |   XML Logic for Events                       |  SB  |
// |   1.2        :  26/06/05   |   Normalized "Look and Feel" with D-Grid     |  SB  |
// |   1.3        :  16/07/05   |   Heartbeat Added : running or dead          |  SB  |
// |----------------------------------------------------------------------------------|
// |              :                                                                   |
// |Role          :  Acces Data on Remote Application Server Linked to RFID           |
// |              :                                                                   |  
// |              :  Optionally : Update on Real-time External Events (Badge Read)    |
// |              :                                                                   |
// |              :  Protocols: http/https                                            |
// |              :  Stored Procedures: none                                          |
// |              :  Needs: 'states.xml' config file in //bin                         |
// |              :                                                                   |
// |Compiler      : Microsoft .NET 4.0x                                               |
// |Compiler Opts.: /safe                                                             |
// |                                                                                  |
// |Comments      :                                                                   |
// |              : "App.conf" XML file Must be Present on Startup   in ../bin/debug  |
// |              :                                                                   |
// +==================================================================================+

using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Net;
using System.IO.Ports;
using System.Data;
using System.Timers;

using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.comm;

using fr.nexess.nexcap.tooltracking.dao;
using fr.nexess.nexcap.tooltracking.dao.exception;
using fr.nexess.nexcap.tooltracking.operation.dao;
using fr.nexess.nexcap.tooltracking.operation.dto;
using fr.nexess.nexcap.tooltracking.core.dto;

using System.Configuration;

namespace fr.nexess.hao.rfid.device.axesstmc
{
    public partial class RFIDDriver : Form
    {
        private static int counter = 1;

        private static int errcounter = 1;

        private const string Alive = "READING";

        public DataTable dt = new DataTable();

        public DataRow dr;

        public enum States
        {
            Other = 0,
            Down_Hole = 3,
            Rig_Floor = 4,
            Pipe_Deck = 5,
            Ocean_Floor = 2 ,          // Added 2/7/15
            Rig_Floor_Returning = 6    // Added 2/7/15
        }

        // Prepare Move to API
        private class StatesClass
        {
            public long Other       { get; set; }
            public long Down_Hole   { get; set; }
            public long Rig_Floor   { get; set; }
            public long Pipe_Deck   { get; set; }
            public long Ocean_Floor { get; set; }
            public long Rig_Floor_Returning { get; set; }
        }

        private RfidDevice reader = null;

        private ComHandler comHandler = null;

        private string IP { get; set; }

        private List<engineStates> states = new List<engineStates>();

        private List<engineStates> KnownStates
        {
              get { return states; }
              set { states = value; }
        }

        private struct engineStates
        {
             public string name     { get; set; }
             public string BindToIP { get; set; }
             public States From     { get; set; }
             public States To       { get; set; }
        }

        //Persistence:
        private MaterialDao dao = MaterialDao.getInstance();

        private LocationDao dao_location = LocationDao.getInstance();

        private OperationMoveDao mdao = OperationMoveDao.getInstance();

        private OperationMoveDto move = new OperationMoveDto();

        private object rigLocationCurrentId = null;

        private const string sep = "_";
        
        public void setIP_Local()
        {
            var portNames = SerialPort.GetPortNames();

            foreach (var port in portNames)
            {
                //Try for every portName and break on the first working
            }

            String strHostName = Dns.GetHostName();

            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);

            IPAddress[] addr = ipEntry.AddressList;

            for (int i = 0; i < addr.Length; i++)
            {
                if (i == 1) IP = addr[i].ToString();
            }
            return;
        }

        public int setXMLConfigurationParameters()
        {
            XElement xelement;

            /* Important : Config File : MUST be in "bin" Directory (same place as  "app.exe.config") */
            /* -------------------------------------------------------------------------------------- */
            try
            {
                xelement = XElement.Load("..\\bin\\states.xml");
            }
            catch (Exception i)
            {
                DialogResult dialogError = System.Windows.Forms.MessageBox.Show("'states.xml' XML Config File Not Found !",
                                           i.Message, MessageBoxButtons.OKCancel);
                // TODO Call Logger: 
                return -1;
            }

            IEnumerable<XElement> conf_params = xelement.Elements();

            foreach (var param in conf_params)
            {
                try
                {
                    string name = param.Attribute("name").Value.ToString();
                    string BindToIP = param.Attribute("BindToIP").Value.ToString();
                    string From = param.Attribute("From").Value.ToString();
                    string To = param.Attribute("To").Value.ToString();

                    engineStates state = new engineStates(); 

                    state.name = name;
                    state.BindToIP = BindToIP;

                    // XML string First Capital Word and Space  -> to ENUM  (example : "Pipe Deck")
                    if (From.ToLower().Equals("pipe deck")) state.From = States.Pipe_Deck;
                    if (From.ToLower().Equals("rig floor")) state.From = States.Rig_Floor;
                    if (From.ToLower().Equals("down hole")) state.From = States.Down_Hole;
                    if (From.ToLower().Equals("ocean floor")) state.From = States.Ocean_Floor;
                    if (From.ToLower().Equals("rig floor returning")) state.From = States.Rig_Floor_Returning;
                    
                    if (To.ToLower().Equals("pipe deck")) state.To = States.Pipe_Deck;
                    if (To.ToLower().Equals("rig floor")) state.To = States.Rig_Floor;
                    if (To.ToLower().Equals("down hole")) state.To = States.Down_Hole;
                    if (To.ToLower().Equals("ocean floor")) state.To = States.Ocean_Floor;
                    if (To.ToLower().Equals("rig floor returning")) state.To = States.Rig_Floor_Returning;

                    states.Add(state);
                }
                catch (Exception j)
                {
                    DialogResult dialogError = System.Windows.Forms.MessageBox.Show(" 'states.xml' XML Config File is Invalid!! ",
                                               j.Message, MessageBoxButtons.OKCancel);
                    // TODO Call Logger: 
                    return -1;
                }
            }
            return 0;
        }

        // TODO: Activate Check on IP for Multiple RFID Readers
        public States getNextStateTransition(string ip, States currentState)
        {
            States state = new States();

            for (int i = 0; i < states.Count; i++)
            {
                if ((states[i].From == States.Pipe_Deck) && (currentState == States.Pipe_Deck))
                {
                    // Check : Bind to Correct IP Reader or Manage COM_PORT_1 & COM_PORT_2
                    if (states[i].BindToIP.Equals(IP))
                    {
                        state = states[i].To;
                        return state;
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////

                if ((states[i].From == States.Rig_Floor) && (currentState == States.Rig_Floor))
                {
                    if (states[i].BindToIP.Equals(IP))
                    {
                        state = states[i].To;
                        return state;                        
                    }
                }
                
                //////////////////////////////////////////////////////////////////////////////////////

                if ((states[i].From == States.Down_Hole) && (currentState == States.Down_Hole))
                {
                    if (states[i].BindToIP.Equals(IP))
                    {
                        state = states[i].To;
                        return state;
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////

                if ((states[i].From == States.Ocean_Floor) && (currentState == States.Ocean_Floor))
                {
                    if (states[i].BindToIP.Equals(IP))
                    {
                        state = states[i].To;
                        return state;
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////

                if ((states[i].From == States.Rig_Floor_Returning) && (currentState == States.Rig_Floor_Returning))
                {
                    if (states[i].BindToIP.Equals(IP))
                    {
                        state = states[i].To;
                        return state;
                    }
                }

                //////////////////////////////////////////////////////////////////////////////////////
            }
            return States.Other;
        }

        public void Form1_Load(object sender, EventArgs e)
        {
            dt.Columns.Add("Event");
            dt.Columns.Add("MessageText");     
            dt.Columns.Add("Timestamp");       

            pictureBoxBad.Visible = true;
            pictureBoxGood.Visible = false;

            int status = setXMLConfigurationParameters();

            if (status != 0) {                
                reader.disconnect();
                Environment.Exit(Environment.ExitCode);
            }
            setIP_Local();

            System.Timers.Timer myTimer = new System.Timers.Timer();
            myTimer.Elapsed += new ElapsedEventHandler(heartBeatMonitor);
            myTimer.Interval = 5000;
            myTimer.Enabled = true;
            
        }

        private void heartBeatMonitor(object source, ElapsedEventArgs e) {

                string status  = reader.getReaderState().ToString();

                try {
                    if (status.ToString() == Alive)
                    {
                        pictureBoxBad.Visible = false;
                        pictureBoxGood.Visible = true;
                    }

                    if (status.ToString() != Alive)
                    {
                        pictureBoxBad.Visible = true;
                        pictureBoxGood.Visible = false;
                    }  
                }
                catch (Exception ex) {return;};              
        }

        public void RFIDIntegration()
        {
            InitializeComponent();

            this.Closing += new System.ComponentModel.CancelEventHandler(Driver_Closing);

            String comPort = "";
            try
            {
                comPort = ConfigurationManager.AppSettings["COM_PORT"];
            }
            catch (Exception) { }

            // tag reader handler instanciation for SSCP BEGIN
                    //SscpReader sscpReader = SscpReader.getInstance();
                    //sscpReader.setParameter("COM_PORT", comPort);
                    //reader = sscpReader;
            // tag reader handler instanciation for SSCP  END

            // tag reader handler instanciation for Legic 904 BEGIN
            Legic904Rfid2 legic904 = Legic904Rfid2.getInstance();
            reader = legic904;
            // tag reader handler instanciation for Legic 904 END

            reader.TagFound += new TagFoundEventHandler(onBadgeFound);

            reader.Connected += new EventHandler(reader_Connected);
            reader.Disconnected += new EventHandler(reader_Disconnected);
            reader.HasReportedAnError +=
                    new nexess.hao.rfid.eventHandler.HasReportedAnErrorEventHandler(reader_HasReportedAnError);
            reader.StartReading += new EventHandler(reader_StartReading);
            reader.StopReading += new EventHandler(reader_StopReading);  

        }
        #region CROSS_THREADING_METHOD

        public void addToDataGrid(string msg)
        {
            try {
                DataGridViewColumn column_sequence = dataGridView1.Columns["Event"];
                DataGridViewColumn column_messagetext = dataGridView1.Columns["MessageText"];
                DataGridViewColumn column_timestamp = dataGridView1.Columns["Timestamp"];

                dataGridView1.Columns["Timestamp"].DefaultCellStyle =
                                              new DataGridViewCellStyle { Format = "dd'/'MM'/'yyyy hh:mm:ss" };  

                column_sequence.Width = 75;
                column_messagetext.Width = 490;
                column_timestamp.Width = 170;               
            }

            catch(Exception e) {};

            DataRow dr = dt.NewRow();

            dr["Event"] = counter;
            dr["MessageText"] = msg;
            dr["Timestamp"] = DateTime.Now.ToString();

            dt.Rows.Add(dr);
            dataGridView1.DataSource = dt;
            dataGridView1.Show();
            dataGridView1.Refresh();

            if (dataGridView1.RowCount >=1)
                dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;

            ++counter;
        }
            
        
        private void changeControlState(RfidDeviceEvent readerEvent)
        {


        }
        #endregion

        #region CLICK_EVENT_HANDLER
        private void button1_Click(object sender, EventArgs e)
        {

            reader.connect();
            comHandler = Legic904Rfid2.ComHandler;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            reader.startScan();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            reader.stopScan();
            pictureBoxBad.Visible = true;
            pictureBoxGood.Visible = false;
        }


        #endregion

        void getReaderInfo_Click(object sender, EventArgs e)
        {
            if (reader == null || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {
                return;
            }

            foreach (KeyValuePair<String, String> pair in ((Legic904Rfid2)reader).ReaderInfo)
            {
                addToDataGrid(pair.Key + " - " + pair.Value);
            }
        }

        void reader_StopReading(object sender, EventArgs e)
        {
            string msg = "onReaderEvent : " + RfidDeviceEvent.STOPPED_READING.ToString();

            addToDataGrid(msg);
            changeControlState(RfidDeviceEvent.STOPPED_READING);
        }

        void reader_StartReading(object sender, EventArgs e)
        {
            string msg = "Scan/Read Event : " + RfidDeviceEvent.START_READING.ToString();

            addToDataGrid(msg);
            changeControlState(RfidDeviceEvent.START_READING);

            button2_Click(sender, e);

        }

        void reader_HasReportedAnError(object sender, nexess.hao.rfid.eventHandler.HasReportedAnErrorEventArgs e)
        {
            string msg = "onReaderEvent : " + RfidDeviceEvent.HAS_REPORTED_AN_ERROR.ToString() + " - " + e.Message;
            if (pictureBoxGood.Visible)
                addToDataGrid(msg);

            pictureBoxBad.Visible = true;
            pictureBoxGood.Visible = false;            

            changeControlState(RfidDeviceEvent.HAS_REPORTED_AN_ERROR);
        }

        void reader_Disconnected(object sender, EventArgs e)
        {
            pictureBoxBad.Visible = true;
            pictureBoxGood.Visible = false;

            string msg = "onReaderEvent : " + RfidDeviceEvent.DISCONNECTED.ToString();

            addToDataGrid(msg);
            changeControlState(RfidDeviceEvent.DISCONNECTED);
        }

        void reader_Connected(object sender, EventArgs e)
        {
            string msg = "Connection Event : " + RfidDeviceEvent.CONNECTED.ToString();

            addToDataGrid(msg);

            changeControlState(RfidDeviceEvent.CONNECTED);

            button1_Click(sender, e);
        }

        private List<MaterialDto> getMaterialsList()
        {
            List<MaterialDto> mList = new List<MaterialDto>();
            mList = dao.getList();
            return mList;
        }

        private List<LocationDto> getLocationsList()
        {
            List<LocationDto> lList = new List<LocationDto>();
            lList = dao_location.getList();
            return lList;
        }

        protected string persistLocaltionChange(string currentCode)
        {
            long rigId = 0;

            List<MaterialDto> materials = getMaterialsList();

            List<LocationDto> locations = getLocationsList();

            if (materials.Count == 0) return "No Materials Data";

            bool foundItem = false;
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].Code == currentCode)
                {
                    rigId = materials[i].Id;
                    rigLocationCurrentId = (States)materials[i].LocationId; 

                    foundItem = true;
                    break;
                }
            }

            if (!foundItem) return "Unable to Locate RFID Tag in Database";

            long numericId = 0;
            try
            {
                numericId = Convert.ToInt64(rigId);
            }
            catch (Exception ae)
            {
            }

            States state = new States();


            States newState = new States();

            newState = getNextStateTransition(IP, (States)rigLocationCurrentId);

            string tran = string.Format("{0} to → {1} ", (States)rigLocationCurrentId, (States)newState);

            if ((States)newState == States.Down_Hole)
            {
                state = States.Down_Hole;               
            }

            if ((States)newState == States.Pipe_Deck)
            {
                state = States.Pipe_Deck;
            }

            if ((States)newState == States.Rig_Floor)
            {
                state = States.Rig_Floor;                
            }

            if ((States)newState == States.Ocean_Floor)
            {
                state = States.Ocean_Floor;
            }

            if ((States)newState == States.Rig_Floor_Returning)
            {
                state = States.Rig_Floor_Returning;                
            }

            // Factorized
            move.date = new Nullable<DateTime>(DateTime.Now);
            move.userId = 2L;
            move.locationSourceId = (long)(int)rigLocationCurrentId;   // From:
            move.locationDestinationId = (long)(int)newState;          // To:
            move.materialId = (long)(int)numericId;
            //

            // No Valid States: Abandon Update
            if ((state != States.Down_Hole) && (state != States.Pipe_Deck) &&
                (state != States.Rig_Floor) && (state != States.Ocean_Floor) && (state != States.Rig_Floor_Returning))
            {
                return "No Valid State Transition";
            }

            // No Change in State : Abandon Update
            if (move.locationDestinationId == move.locationSourceId)
            {
                return "Illicit State Transition: No State Transition = Self";
            }

            DaoException daoException = null;

            try
            {
                if ((move.materialId >= 1L) && (move.locationSourceId >= 1L) && (move.locationDestinationId >= 1L))
                {
                    mdao.moveMaterial(move);
                }
            }
            catch (DaoException ex)
            {
                // TODO: Call Logger      
                string exeception = daoException.Message.ToString() + daoException.StackTrace.ToString();
                return "DAO Failure";
            }

            return string.Format("{0} to → {1} ", (States)rigLocationCurrentId, (States)newState);
        }

        protected void onBadgeFound(object sender, TagFoundEventArgs e)
        {

            // string myState = reader.getReaderState().ToString();

            foreach (string snr in e.Snrs)
            {
                string msg = "SNR Detected >> " + snr;

                addToDataGrid(msg);

                string materialId = snr.ToString();

                string tran = persistLocaltionChange(materialId);

                addToDataGrid(tran);

                pictureBoxBad.Visible = false;
                pictureBoxGood.Visible = true;

            }
        }

        private void legic904_ModeChangedToCommand(object sender, EventArgs e)
        {
            addToDataGrid("Change legic mode to command : [OK]");
            addToDataGrid("Please, switch off this app and disconnect the card reader");
        }

        private void legic904_ModeChangedToAutoReading(object sender, EventArgs e)
        {
            addToDataGrid("Change legic mode to automatic reading : [OK]");
            addToDataGrid("Please, switch off this app and disconnect the card reader");
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {

            
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {

                return;
            }
            ((Legic904Rfid2)reader).switchMode(MODE.COMMAND);
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {

                return;
            }
            ((Legic904Rfid2)reader).switchMode(MODE.AUTOMATIC_READING);
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            if (reader == null || reader.getReaderState() == RfidDeviceState.DISCONNECTED)
            {
                return;
            }

            foreach (KeyValuePair<String, String> pair in ((Legic904Rfid2)reader).ReaderInfo)
            {
                addToDataGrid(pair.Key + " - " + pair.Value);
            }
        }

        void Driver_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private void exitButton(object sender, EventArgs e)
        {
            reader.disconnect();
            Environment.Exit(Environment.ExitCode);
        }

    }
}
