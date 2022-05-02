using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.reader.optic.eventhandler {

    /// <summary>
    /// report after reading Event handler  : thrown when optical reader ends its reading
    /// </summary>
    public delegate void ReportAfterOpticalReadingEventHandler(object sender, ReportAfterOpticalReadingEventArgs e);

    /// <summary>
    /// report after reading Event args
    /// </summary>
    public class ReportAfterOpticalReadingEventArgs : EventArgs {

        private List<String> captions;

        public ReportAfterOpticalReadingEventArgs(List<String> captions) {
            this.captions = captions;
        }

        public List<String> Captions {
            get {
                return this.captions;
            }
        }
    }
}
