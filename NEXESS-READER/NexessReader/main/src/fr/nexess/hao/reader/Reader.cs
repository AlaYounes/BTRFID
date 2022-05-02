using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.reader {

    public interface Reader {

        /// <summary>
        /// get a reader connection.
        /// </summary>
        void connect();

        /// <summary>
        /// reader disconnection.
        /// </summary>
        void disconnect();

        /// <summary>
        /// start scanning mode.
        /// </summary>
        void startScan();

        /// <summary>
        /// stop scanning
        /// </summary>
        void stopScan();

        /// <summary>
        /// get the reader state
        /// </summary>
        ReaderState getReaderState();
    }

    public enum ReaderState {
        CONNECTED,
        DISCONNECTED,
        READING
    }

    public enum ReaderEvent {
        HAS_REPORTED_AN_ERROR,
        CONNECTED,
        DISCONNECTED,
        START_READING,
        STOPPED_READING,
        END_OF_READING
    }
}
