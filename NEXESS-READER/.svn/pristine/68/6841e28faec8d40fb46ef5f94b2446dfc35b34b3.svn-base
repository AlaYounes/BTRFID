using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using fr.nexess.toolbox.log;
using fr.nexess.toolbox.comm.eventHandler;
using System.Net.NetworkInformation;
//using WebEye.Controls.WinForms.StreamPlayerControl; // RTSP management from Code project - License GPL
using CortexDecoderDotNet;
using Accord.Video.FFMPEG; // The framework  is licensed under GNU Lesser Public License v2.1 (accord-framework.net)

namespace fr.nexess.hao.reader.video {

    public class StahlCamera : Reader {

        private LogProducer log = new LogProducer(typeof(StahlCamera));
        static int nbEventOccurences = 0; // should never exceed 1
        // critical section object locker
        private static readonly Object locker = new Object();
        // singleton instance
        protected static StahlCamera instance = null;

        ReaderState state = ReaderState.DISCONNECTED;
        // video
        string cameraIp = "";
        string rtspUri = "rtsp://192.168.62.57/nce";
        bool isAccessing = false;
        // codebar treatment
        private System.Timers.Timer scanTimer;
        private string barcode;

        // events
        public event EventHandler tagFoundEventHandler;
        public event OnDataReceivedEventHandler refreshEventHandler;
        public event HasReportedAnErrorEventHandler onErrorEventHandler;
        public event OnDataReceivedEventHandler onReading;

        #region CONSTRUCTOR
        public static StahlCamera getInstance() {

            // critical section
            lock (locker) {
                if (instance == null) {
                    throw new NotImplementedException(); // Object must be initialized with new
                }
            }

            return instance;
        }

        public StahlCamera(Double scanInterval, Panel container) {

            // initialize timer
            scanTimer = new System.Timers.Timer(scanInterval);
            scanTimer.Elapsed += onTakePicture;
            scanTimer.Enabled = false;
        }

        ~StahlCamera() {
            state = ReaderState.DISCONNECTED;
            scanTimer.Enabled = false;
        }
        #endregion

        #region GETTER SETTER
        public string Barcode {
            get {
                return barcode;
            }

            set {
                barcode = value;
            }
        }


        public string RtspUri {
            get {
                return rtspUri;
            }

            set {
                rtspUri = value;
            }
        }

        public string CameraIp {
            get {
                return cameraIp;
            }

            set {
                cameraIp = value;
            }
        }
        #endregion

        #region READER IMPL
        public void connect() {

            state = ReaderState.CONNECTED;
        }

        public void disconnect() {
            state = ReaderState.DISCONNECTED;
        }

        public void startScan() {

            if (state != ReaderState.READING) {
                state = ReaderState.READING;
                // activate timer
                scanTimer.Enabled = true;
            }
        }

        public void stopScan() {

            if (state == ReaderState.READING) {
                // disable timer
                scanTimer.Enabled = false;
                state = ReaderState.CONNECTED;
                isAccessing = false;
            }
        }

        public ReaderState getReaderState() {
            return state;
        }
        #endregion

        #region PRIVATE
        private bool pingStatus() {
            // Ping's the camera
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(cameraIp);

            if (reply.Status == IPStatus.Success) {

                log.Logger.Debug("Ping OK");
                return true;
                /*Console.WriteLine("Address: {0}", reply.Address.ToString());
                Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);*/
            } else {

                log.Logger.Error("Ping failed. Camera not reached " + cameraIp + " , status: " + reply.Status);
                return false;
            }
        }

        #endregion

        #region EVENT HANDLER
        private void openStream(VideoFileReader reader) {
            try {
                log.Logger.Debug("Opening stream");
                reader.Open(rtspUri);
            } catch (Exception ex) {
                log.Logger.Error("Exception when opening stream, " + ex.Message + ", " + ex.StackTrace);
                isAccessing = false;
                notifyError();
            }
        }

        private void notifyError() {

            if (onErrorEventHandler != null) {
                    onErrorEventHandler(this, new HasReportedAnErrorEventArgs("Error accessing Video stream " + rtspUri
                        + ",Check the network or device"));
            }
        } 

        private void restoreTimer() {

            scanTimer.Enabled = true;
            nbEventOccurences = 0;
        }

        private void checkEventOccurences() {
            nbEventOccurences++;
            log.Logger.Debug("* Event occurences=" + nbEventOccurences + ")");
            if(nbEventOccurences > 1) {
                log.Logger.Fatal("* More than one event occurence, exiting program");
                // exit the program
                Application.Exit();
            }
        }

        private void onTakePicture(object sender, ElapsedEventArgs e) {
            if (state != ReaderState.READING) {
                return;
            }
            // Stop timer
            scanTimer.Enabled = false;
            Bitmap bmp = null;
            try {             
                checkEventOccurences();
                log.Logger.Debug("* Starting scan");
                if (refreshEventHandler != null) {
                    refreshEventHandler(this, new OnDataReceivedEventArgs(""));
                }
                if(!pingStatus()) {
                    isAccessing = false;
                    notifyError();
                    restoreTimer();
                    return;
                }
                log.Logger.Debug("* Getting picture from Stream");
                VideoFileReader reader = new VideoFileReader();
                var task = Task.Run(() => openStream(reader));
                if (!task.Wait(TimeSpan.FromSeconds(5))) {
                    log.Logger.Error("Timeout when opening stream: " + rtspUri);
                    isAccessing = false;
                    notifyError();
                    // wait a bit more before retrying
                    Thread.Sleep(2000);
                    restoreTimer();
                    return;
                }

                if (reader.IsOpen) {
                    log.Logger.Debug("Stream opened");
                    if(!isAccessing) {
                        // wait a bit more before treating image
                        Thread.Sleep(3000);
                        if (onReading != null) { // notify camera is reading
                            onReading(this, new OnDataReceivedEventArgs(ReaderState.READING.ToString()));
                        }
                    }

                    isAccessing = true;
                    bmp = reader.ReadVideoFrame();
                    reader.Close();
                    log.Logger.Debug("Picture captured");

                    if (bmp != null) {
#if !DEBUG
                        CortexDecoder.result.decodeData = "";
                        // save picture
                        log.Logger.Debug("* Saving picture");
                        bmp.Save("barcode.bmp");
                        // decode picture
                        log.Logger.Debug("* Decoding picture with CortexDecoder");
                        CortexDecoder cortexDecoder = new CortexDecoder();
                        if (cortexDecoder.Initialize() != 1) {
                            log.Logger.Error("Cortex decoder initialization error");
                            cortexDecoder.Close();
                            restoreTimer();
                            return;
                        }
                        try {
                            Thread.Sleep(200);
                            int crdResult = cortexDecoder.Decode(bmp);
                        }
                        catch(System.AccessViolationException ex) {
                            log.Logger.Error("Memory exception: " + ex.Message + ":: " + ex.StackTrace);
                            cortexDecoder.Close();
                            restoreTimer();
                            return;
                        }
                        barcode = CortexDecoder.result.decodeData;
                        cortexDecoder.Close();
                        log.Logger.Debug("Decoded picture");             
#endif
                        if (!string.IsNullOrEmpty(barcode)) {
                            if (refreshEventHandler != null) {
                                refreshEventHandler(this, new OnDataReceivedEventArgs(barcode));
                            }
                            if (tagFoundEventHandler != null) {
                                tagFoundEventHandler(this, new EventArgs());
                            }
                            //log.Logger.Debug("* Saving picture");
                            //bmp.Save("barcode.bmp");
                        }
                        bmp.Dispose();
                    }
                }
                log.Logger.Debug("Finished\r\n");
            } catch (Exception ex) {
                log.Logger.Error("Exception when decoding picture, " + ex.Message + ", " + ex.StackTrace);
                if (bmp != null) {
                    bmp.Dispose();
                }
            }
            restoreTimer();
        }
        #endregion
    }

}
