using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Threading;

namespace fr.nexess.toolbox
{
    public class IPHelper
    {
        static string macAddr;
        static List<string> ipList = new List<string>();
        static CountdownEvent countdown = new CountdownEvent(1);
    

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, byte[] pMacAddr, ref uint PhyAddrLen);

        public static string getMAC(IPAddress address)
        {
            int intAddress = BitConverter.ToInt32(address.GetAddressBytes(), 0);

            byte[] macAddr = new byte[6];
            uint macAddrLen = (uint)macAddr.Length;
            if (SendARP(intAddress, 0, macAddr, ref macAddrLen) != 0)
                return "(NO ARP result)";

            string[] str = new string[(int)macAddrLen];
            for (int i = 0; i < macAddrLen; i++)
                str[i] = macAddr[i].ToString("x2");

            return string.Join(":", str);
        }

        public string getIP()
        {
            string Hostname = Dns.GetHostName();
            IPHostEntry thisHost = Dns.GetHostEntry(Hostname);
            string ipAddr = "";
            foreach (IPAddress ip in thisHost.AddressList)
            {
                if (!ip.ToString().Contains(':') && ip.ToString() != "127.0.0.1")
                {
                    ipAddr = ip.ToString();
                    break;
                }
            }
            return ipAddr;
        }

        public List<string> getIpList(string macAddress)
        {
            return ipList;
        }

        public List<string> findMac(string macAddress)
        {
            string ipAddr = getIP();
            string ipBase = "";
            macAddr = macAddress;

            ipList.Clear();

            try
            {
                ipBase = ipAddr.Split('.')[0] + "." + ipAddr.Split('.')[1] + "." + ipAddr.Split('.')[2] + ".";
            }
            catch (Exception){
                return ipList;
            }
            
            for (int i = 1; i < 255; i++)
            {
                string ip = ipBase + i.ToString();

                Ping p = new Ping();
                p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                countdown.AddCount();
                ThreadPool.QueueUserWorkItem((w) => p.SendAsync(ip, 100, ip));              
            }
            countdown.Signal();
            countdown.Wait();
            return ipList;

        }

        public List<string> findLocalMac(string macAddress, string ipAddr = "169.254.1.1") {
            //string ipAddr = "169.254.1.1";
            string ipBase = "";
            macAddr = macAddress;

            ipList.Clear();

            try {
                ipBase = ipAddr.Split('.')[0] + "." + ipAddr.Split('.')[1] + ".";
            } catch (Exception) {
                return ipList;
            }

            for (int i = 1; i < 255; i++) {
                for (int j = 1; j < 255; j++) {
                    string ip = ipBase + i.ToString() + "." + j.ToString();

                    Ping p = new Ping();
                    p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                    countdown.AddCount();
                    ThreadPool.QueueUserWorkItem((w) => {
                        try { p.SendAsync(ip, 100, ip);
                        }
                        catch(Exception) {

                        }
                    } );
            
                    //p.SendAsync(ip, 100, ip);
                }
                countdown.Signal();
                countdown.Wait();
                countdown.Reset();
                if (ipList.Count() > 0)
                    break;
            }

            return ipList;

        }

       static void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                string mac = getMAC(IPAddress.Parse(ip));
                if (mac.StartsWith(macAddr))
                    ipList.Add(ip);
            }
            try
            {
                countdown.Signal();
            }
            catch (Exception)
            { }
        }

       public static bool PingHost(string nameOrAddress)
       {
           bool pingable = false;
           Ping pinger = new Ping();
           try
           {
               PingReply reply = pinger.Send(nameOrAddress);
               pingable = reply.Status == IPStatus.Success;
           }
           catch (PingException)
           {
               // Discard PingExceptions and return false;
           }
           return pingable;
       }
    }
}
