using System;

namespace fr.nexess.hao.reader{

    /// <summary>
    /// Has Reported An Error Event handler : thrown when a reader "HasReportedAnError" Event occurs
    /// </summary>
    public delegate void HasReportedAnErrorEventHandler(object sender, HasReportedAnErrorEventArgs e);

    /// <summary>
    /// Has Reported An Error Event Arguments
    /// </summary>
    public class HasReportedAnErrorEventArgs : EventArgs {

        private String message;
        private Exception ex;

        public HasReportedAnErrorEventArgs(String message, Exception exception = null) {

            this.message = message;

            if (exception == null) {

                this.ex = new Exception(message);

            } else {

                this.ex = exception;
            }
        }

        public String Message {
            get {
                return this.message;
            }
        }

        public Exception Ex {
            get {
                return this.ex;
            }
        }
    }
}
