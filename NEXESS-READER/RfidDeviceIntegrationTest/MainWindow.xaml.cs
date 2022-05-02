

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
using System.Windows.Navigation;
using System.Windows.Shapes;
using fr.nexess.hao.rfid.device.feig;
using fr.nexess.hao.rfid.device.axesstmc;
using RfidDeviceIntegrationTest.main.src.fr.nexess.hao.rfid.device.impinj;
using RfidDeviceIntegrationTest.main.src.fr.nexess.hao.rfid.device.mti;
using RfidDeviceIntegrationTest.main.src.fr.nexess.hao.weight.reader.pcb12;
using RfidDeviceIntegrationTest.main.src.fr.nexess.hao.optic.device.opticon;

namespace RfidDeviceIntegrationTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {

            InitializeComponent();

            try
            {

                this.Hide();

                //Cpr02_10IntegrationTest cpr02_10IntegrationTest = new Cpr02_10IntegrationTest();
                //cpr02_10IntegrationTest.Show();

                //Cpr02_10_bIntegrationTest cpr02_10_bIntegrationTest = new Cpr02_10_bIntegrationTest();
                //cpr02_10_bIntegrationTest.Show();

                //Lru3500IntegrationTest lru3500IntegrationTest = new Lru3500IntegrationTest();
                //lru3500IntegrationTest.Show();


                /* Commented Out : SB 17/06/2015 : Complete Driver Developpment   */

                //Legic904Rfid2IntegrationTest legic904Rfid2IntegrationTest = new Legic904Rfid2IntegrationTest();
                //legic904Rfid2IntegrationTest.Show();


                // Complete Windows Forms RFID Driver
                //RFIDDriver rfidDriver = new RFIDDriver();
                //rfidDriver.RFIDIntegration();
                //rfidDriver.Show();

                //SpeedwayRevolutionIntegrationTest speedwayRevolutionIntegrationTest = new SpeedwayRevolutionIntegrationTest();
                //speedwayRevolutionIntegrationTest.Show();

                //Ru_865Integrationtest ru_865Integrationtest = new Ru_865Integrationtest();
                //ru_865Integrationtest.Show();

                //Pcb12IntegrationTest pcb12IntegrationTest = new Pcb12IntegrationTest();
                //pcb12IntegrationTest.Show();


                NLV3101Integrationtest integrationtest = new NLV3101Integrationtest();
                integrationtest.Show();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
    }
}
