using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.hao.weight.eventhandler;
using fr.nexess.hao.reader;

namespace fr.nexess.hao.weight.eventhandler {

    public interface WeightReaderEventProvider {

        /// <summary>
        /// Event fired when the rfid reader has reported an error
        /// </summary>
        event HasReportedAnErrorEventHandler HasReportedAnError;

        /// <summary>
        /// Event fired each time the reader establishes a connection with system
        /// </summary>
        event EventHandler  Connected;

        /// <summary>
        /// Event fired each time the reader breaks a connection with system
        /// </summary>
        event EventHandler  Disconnected;

        /// <summary>
        /// Event fired each time the reader starts reading tag over rfid
        /// </summary>
        event EventHandler  StartReading;

        /// <summary>
        /// Event fired each time the reader stop reading tag over rfid
        /// </summary>
        event EventHandler  StopReading;

        /// <summary>
        /// Event fired at the end of reading with result
        /// </summary>
        event ReportAfterReadingEventHandler ReportAfterReading;
    }

    public class WeightReaderError {

        public static WeightReaderError UNSTABLE = new WeightReaderError("A weight system is in motion");
        public static WeightReaderError OVERLOAD = new WeightReaderError("A weight system is overloaded");
        public static WeightReaderError NOT_VALID_WEIGHT = new WeightReaderError("A weight system has an invalid weight");
        
        protected String weightReaderError = "";

        protected WeightReaderError(String error) {

            weightReaderError = error;
        }

        public String Error {
            get {
                return weightReaderError;
            }
        }
    }
}
