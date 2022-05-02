using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Threading;
using System.Windows.Threading;

using fr.nexess.hao.rfid.eventHandler;
using fr.nexess.toolbox.log;

using OBID;
using System.Timers;


namespace fr.nexess.hao.rfid.device.feig {

    /// <summary>
    /// This class is used to abstract classic-pro reader (CPR.02.10 B) communication services.
    /// </summary>
    /// <version>$Revision: 13 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-09-11 13:38:08 +0200 (jeu., 11 sept. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class Cpr02_10_b : Cpr02_10, ICpr02_10_b {

        private LogProducer logProducer = new LogProducer(typeof(Cpr02_10_b));

        // singleton instance.
        private static Cpr02_10_b instance = null;

        private System.Timers.Timer timer = new System.Timers.Timer();

        private Boolean relayOn = false;

        #region CONSTRUCTORS
        /// <summary>
        /// Cpr02_10_b get instance (singleton).
        /// </summary>
        public new static Cpr02_10_b getInstance() {

            if (instance == null) {
                instance = new Cpr02_10_b();
            }

            return instance;
        }

        /// <summary>
        /// protected default constructor
        /// </summary>
        protected Cpr02_10_b()
            : base() {

            initTimer();

            logProducer.Logger.Debug("class loaded");
        }


        /// <summary>
        /// destructor
        /// </summary>
        ~Cpr02_10_b() {
        }
        #endregion

        #region PUBLIC_METHODS
        public Boolean isRelayOn() {

            return this.relayOn;
        }

        /// <summary>
        /// activate Relay min 1*100ms=100ms, max 65534*100ms = 1:49:13
        /// </summary>
        public void activateRelay(uint nb100ms) {

            if (nb100ms < 1 || nb100ms > (uint)65634) {
                return;
            }

            if (reader != null
                && reader.Connected) {

                try {

                    lock (locker) {

                        reader.SetData(OBID.ReaderCommand._0x71.Req.OUT_TIME, nb100ms);
                        reader.SendProtocol(0x71); // set ouput cmd
                    }

                    countdownRelay(nb100ms * 100);

                } catch (Exception ex) {

                    raisedHasReportedAnErrorEvent("Unable to activate relay, because : " + ex.Message);
                }
            }
        }

        public void activateRelayContinuously() {

            if (reader != null
                && reader.Connected) {

                try {

                    lock (locker) {

                        // 65635 = The relay is activated continuously
                        reader.SetData(OBID.ReaderCommand._0x71.Req.OUT_TIME, (uint)65635);
                        reader.SendProtocol(0x71); // set ouput cmd
                    }

                    relayOn = true;

                } catch (Exception ex) {

                    raisedHasReportedAnErrorEvent("Unable to activate relay constinously, because : " + ex.Message);
                }
            }
        }

        public void deactivateRelay() {
            // OUT_TIME = 1 has to be sent to the Reader, 
            // which effects a change to the idle status after 100 ms.
            activateRelay(1);
        }

        public int getInputs() {

            int readerStatus = -1;

            if (reader != null
               && reader.Connected) {

                try {
                    lock (locker) {

                        //reader.GetReaderInfo();

                        readerStatus = reader.SendProtocol(0x74); // get input cmd
                    }

                } catch (Exception ex) {

                    raisedHasReportedAnErrorEvent("Unable to get inputs, because : " + ex.Message);
                }
            }
            return readerStatus;
        }


        public Boolean readIn1() {

            bool input1= false;

            if (reader != null
                && reader.Connected) {

                try {
                    lock (locker) {
                        reader.GetData(OBID.ReaderCommand._0x74.Rsp.Inputs.IN1, out input1);
                    }
                } catch (Exception ex) {

                    raisedHasReportedAnErrorEvent("Unable to read in1, because : " + ex.Message);
                }
            }

            return input1;
        }

        public Boolean readIn2() {

            bool input2= false;

            if (reader != null
                && reader.Connected) {

                try {
                    lock (locker) {
                        reader.GetData(OBID.ReaderCommand._0x74.Rsp.Inputs.IN2, out input2);
                    }
                } catch (Exception ex) {

                    raisedHasReportedAnErrorEvent("Unable to read in2, because : " + ex.Message);
                }

            }

            return input2;
        }
        #endregion

        #region PRIVATE_METHODS

        private void initTimer() {

            timer.Enabled = false;
            timer.AutoReset = false;
            timer.Elapsed += new ElapsedEventHandler(onElapsed);
        }

        private void countdownRelay(uint nbMs) {

            relayOn = true;

            timer.Interval = nbMs;
            timer.Enabled = true;
            timer.Start();
        }
        #endregion

        #region EVENT_HANDLERS

        private void onElapsed(object sender, ElapsedEventArgs e) {

            relayOn = false;
        }
        #endregion
    }
}
