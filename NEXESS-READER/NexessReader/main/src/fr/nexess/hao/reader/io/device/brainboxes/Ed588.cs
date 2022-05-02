using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyModbus;
using fr.nexess.toolbox.log;
using System.Threading;
using System.Windows.Threading;

namespace fr.nexess.hao.reader.io.device.brainboxes
{
    
    public class Ed588
    {
        private LogProducer logProducer = new LogProducer(typeof(Ed588));

        // critical section object locker
        private static readonly Object locker = new Object();

        // singleton instance
        protected static Ed588 instance = null;

        // STA Threads management
        public Dispatcher Dispatcher { get; set; }

        protected EasyModbus.ModbusClient client = null;
        const int DEFAUT_TCP_PORT = 502;
        const string DEFAULT_IP_ADRESS = "127.0.0.1";

        public static Ed588 getInstance()
        {

            // critical section
            lock (locker)
            {
                if (instance == null)
                {
                    instance = new Ed588();
                }
            }

            return instance;
        }

        public static Ed588 getMultipleInstance()
        {
            return new Ed588();
        }

        public static Ed588 getMultipleInstance( string ipadd)
        {
            return new Ed588(ipadd);
        }

        protected Ed588()
        {
            /*
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {

                // force thread apartment to be a Single Thread Apartment (as the current)
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }
            */
            // Multiple threads management
            Dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            client = new ModbusClient();
            client.Port = DEFAUT_TCP_PORT;
            client.IPAddress = DEFAULT_IP_ADRESS;

            //client.receiveDataChanged += new ModbusClient.ReceiveDataChanged(onReceiveDataChanged);
            //client.sendDataChanged += new ModbusClient.SendDataChanged(onSendDataChanged);

        }

        protected Ed588(string ipadd)
        {
            /*
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {

                // force thread apartment to be a Single Thread Apartment (as the current)
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            }
            */
            // Multiple threads management
            Dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            client = new ModbusClient();
            client.Port = DEFAUT_TCP_PORT;
            client.IPAddress = ipadd;
            //client.receiveDataChanged += new ModbusClient.ReceiveDataChanged(onReceiveDataChanged);
            //client.sendDataChanged += new ModbusClient.SendDataChanged(onSendDataChanged);

        }

        ~Ed588()
        {
                instance = null;
            if (client != null)
            {
                disconnect();
            }

        }

        public void connect()
        {
            client.Connect();
        }

        public void disconnect()
        {
            client.Disconnect();
        }

        private void onReceiveDataChanged(object sender)
        {
                
        }

        private void onSendDataChanged(object sender)
        {

        }

        public string getIP()
        {
            return client.IPAddress;
        }

        public bool[] readAllInputs()
        {
            return client.ReadDiscreteInputs(0x0, 8);
        }

        public bool readInput(int noInput)
        {
            return client.ReadDiscreteInputs(0x0 + noInput, 1)[0];
        }

        public int readInputs()
        {
            return client.ReadInputRegisters(0x20, 1)[0];
        }

        public void setOutput(int noOutput, bool state)
        {
            client.WriteSingleCoil(0x0 + noOutput, state);
        }

        public void setOutputs(int value)
        {
            client.WriteSingleRegister(0x20, value);
        }

        public bool readOutput(int noOutput)
        {
            return client.ReadCoils(0x0 + noOutput, 1)[0];
        }

        public int readOutputs()
        {
            return client.ReadHoldingRegisters(0x20 , 1)[0];
        }

        public bool[] readAllOutputs()
        {
            return client.ReadCoils(0x0, 8);
        }

        protected void raiseEvent(EventHandler eventhandler)
        {

            if (eventhandler != null)
            {
                Dispatcher.BeginInvoke((Action)(() => eventhandler(this, EventArgs.Empty)));
            }
        }

        public bool isConnected()
        {
            return client.Connected;
        }

    }
}
