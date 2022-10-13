using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _Logger
{
    public class SysLogServer
    {
        private bool isAlive = true;

        public bool Enabled { get; private set; } = true;
        public int Port { get; private set; } = 514;



        public LogFile Logger { get; private set; } = null;



        public SysLogServer(string iniFile)
        {
            //read settings
            using (FicheiroINI ini = new FicheiroINI(iniFile))
            {
                this.Enabled = ini.RetornaTrueFalseDeStringFicheiroINI("SysLogServer", "Enabled", this.Enabled);
                this.Port = Convert.ToInt32(ini.RetornaINI("SysLogServer", "Port", this.Port.ToString()));

                this.Logger = new LogFile(ini.RetornaINI("SysLogServer", "Log", Application.StartupPath + @"\Logs\SysLog.txt"), true);
            }

            //start thread
            new Thread(SysLogServerThread).Start();
        }


        public void SysLogServerThread()
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

            using (UdpClient udpListener = new UdpClient(this.Port))
            {
                /* Main Loop */
                /* Listen for incoming data on udp port 514 (default for SysLog events) */
                while (this.isAlive)
                {
                    try
                    {
                        byte[] bReceive = udpListener.Receive(ref anyIP);
                        /* Convert incoming data from bytes to ASCII */
                        string sReceive = Encoding.ASCII.GetString(bReceive);
                        /* Get the IP of the device sending the syslog */
                        string sourceIP = anyIP.Address.ToString();

                        /* outputs received data */
                        Debug.WriteLine("SysLogServer - new message from " + sourceIP + ": '" + sReceive + "'");

                        //logs the message
                        this.Logger.WriteLine("From " + sourceIP + " : " + sReceive, DateTime.Now);
           
                    }
                    catch (Exception ex)
                    {
                        Forms.MainForm.AdicionaLog(ex);
                    }
                    finally
                    {
                        Thread.Sleep(1);
                    }
                }
            }




        }


        public void Dispose()
        {
            this.isAlive = false;
        }


    }
}
