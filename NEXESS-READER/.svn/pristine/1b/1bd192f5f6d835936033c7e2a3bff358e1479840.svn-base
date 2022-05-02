using System;


namespace fr.nexess.hao.rfid.eventHandler {

    /**
     * interface for retrieving events from the RFID reader
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface RfidDeviceEventProvider {
        /** Event fired when the rfid reader has reported an error.*/
        event HasReportedAnErrorEventHandler HasReportedAnError;

        /** Event fired each time the reader establishes a connection with system.*/
        event EventHandler  Connected;

        /** Event fired each time the reader breaks a connection with system.*/
        event EventHandler  Disconnected;

        /** Event fired each time the reader starts reading tag over rfid.*/
        event EventHandler  StartReading;

        /** Event fired each time the reader stop reading tag over rfid.*/
        event EventHandler  StopReading;

        /** Event fired each time some tags are detected.*/
        event TagFoundEventHandler TagFound;
    }
}
