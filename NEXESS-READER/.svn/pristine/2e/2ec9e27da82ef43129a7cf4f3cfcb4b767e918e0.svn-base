using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.hao.weight.eventhandler;
using fr.nexess.hao.reader;
using fr.nexess.hao.reader.optic.eventhandler;

namespace fr.nexess.hao.optic.eventhandler {

    public interface OpticReaderEventProvider {

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
        event ReportAfterOpticalReadingEventHandler ReportAfterReading;
    }
}
