using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using fr.nexess.toolbox.log;
using System.Windows.Threading;

namespace fr.nexess.toolbox {

    public delegate void NoArgDelegate();

    public class LongTaskExecutor {

        private LogProducer logProducer = new LogProducer(typeof(LongTaskExecutor));
        public Dispatcher Dispatcher { get; set; }

        private event EventHandler onSuccessEventHandler = null;
        private event EventHandler onFailureEventHandler = null;

        private int timeDelay = 0;

        #region CONTRUCTOR
        public LongTaskExecutor() {
            Dispatcher = Dispatcher.CurrentDispatcher;
        }
        #endregion

        #region PUBLIC_METHODS
        public event EventHandler onSuccess {
            add {

                onSuccessEventHandler += value;
            }
            remove {

                onSuccessEventHandler -= value;
            }
        }

        public event EventHandler onFailure {
            add {

                onFailureEventHandler += value;
            }
            remove {

                onFailureEventHandler -= value;
            }
        }

        public void execute(NoArgDelegate aDelegate, int aTimeDelay = 0) {

            timeDelay = aTimeDelay;

            createBackgroundWorkerAndDoWork(aDelegate);
        }

        #endregion

        #region PRIVATE_METHODS
        private void createBackgroundWorkerAndDoWork(NoArgDelegate aDelegate) {

            BackgroundWorker worker = new BackgroundWorker();

            worker.WorkerSupportsCancellation = false;
            worker.WorkerReportsProgress = false;

            worker.DoWork += new DoWorkEventHandler(doWorkLazyGuy);

            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted);

            worker.RunWorkerAsync(aDelegate);
        }

        private void doWorkLazyGuy(object sender, DoWorkEventArgs e) {

            BackgroundWorker bw = sender as BackgroundWorker;

            if (timeDelay > 0) {
                Thread.Sleep(timeDelay);
            }

            if (!bw.CancellationPending) { // check before invokation

                ((NoArgDelegate)e.Argument).Invoke();
            }

            if (bw.CancellationPending) {// check after invokation
                e.Cancel = true;
            }
        }

        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

            if (e.Cancelled) {

                // operation canceled by the user

            } else if (e.Error != null) {

                logProducer.Logger.Error("an error during the operation : " + e.Error.Message);

                if (onFailureEventHandler != null) {

                    if (Dispatcher != null) {

                        Dispatcher.BeginInvoke((Action)(() => onFailureEventHandler(this, EventArgs.Empty)));

                    } else {

                        onFailureEventHandler(this, EventArgs.Empty);
                    }
                }
            } else {

                // let's groove tonight !
                if (onSuccessEventHandler != null) {

                    if (Dispatcher != null) {

                        Dispatcher.BeginInvoke((Action)(() => onSuccessEventHandler(this, EventArgs.Empty)));

                    } else {

                        onSuccessEventHandler(this, EventArgs.Empty);
                    }
                }
            }
        }
        #endregion
    }
}
