using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.weight.eventhandler {

    /// <summary>
    /// report after reading Event handler  : thrown when weight reader ends its reading
    /// </summary>
    public delegate void ReportAfterReadingEventHandler(object sender, ReportAfterReadingEventArgs e);

    /// <summary>
    /// report after reading Event args
    /// </summary>
    public class ReportAfterReadingEventArgs : EventArgs {

        private List<Tuple<String,String,String>> weights;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weights">
        /// pcb@ , pad#, weight
        /// </param>
        public ReportAfterReadingEventArgs(List<Tuple<String,String,String>> weights) {
            this.weights = weights;
        }

        public List<Tuple<String,String,String>> Weights {
            get {
                return this.weights;
            }
        }
    }
}
