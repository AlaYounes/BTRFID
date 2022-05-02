using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.hao.rfid.eventHandler;

namespace fr.nexess.hao.rfid {

    /**
     * basic driving interface that must implemented by all rfid readers <br/>
     * (OBID I-Scan reader and classic-pro reader etc.).
     * 
     * @version $Revision: 90 $
     * @author J.FARGEON
     * @since $Date: 2015-09-15 16:23:51 +0200 (mar., 15 sept. 2015) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface RfidDevice : RfidDeviceEventProvider {
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
        RfidDeviceState getReaderState();

        /// <summary>
        /// get the reader Unique universal identifier
        /// </summary>
        String getReaderUUID();

        String getReaderSwVersion();

        String getReaderComPort();
        Dictionary<EnumReaderType, string> getReaderDetail();

        bool transferCfgFile(string fileName);

        void switchOffAntenna();
        void switchOnAntenna();
    }

    public enum RfidDeviceState {
        CONNECTED,
        DISCONNECTED,
        READING
    }

    public enum RfidDeviceEvent {
        HAS_REPORTED_AN_ERROR,
        CONNECTED,
        DISCONNECTED,
        START_READING,
        STOPPED_READING,
        TAG_FOUND
    }

    public enum RFIDReaderConnectionType {
        TCP,
        SERIAL,
        USB
    }

    public enum EnumReaderType {
        TYPE,
        STATE,
        COM_PORT,
        VERSION
    }
}
