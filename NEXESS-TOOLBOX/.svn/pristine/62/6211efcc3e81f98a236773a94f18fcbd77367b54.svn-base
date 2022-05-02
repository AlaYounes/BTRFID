using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox.comm
{
    /**
     * Serial Com listener
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-25 16:00:56 +0200 (lun., 25 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface ComListener
    {
        /** This method will be called each time some communication event occurs.*/
        void onComEvent(ComEvent comEvent, string msg);

        /** This method will be called each time a message is received from the communication pipe.*/
        void onResponse(byte[] response);

        /** This method will be called each time a message is received from the communication pipe.*/
        void onResponse(string response);
    }

    public enum ComEvent
    {
        UNEXPECTED_DISCONNECTION,
        HAS_REPORTED_AN_ERROR,
        CONNECTED,
        DISCONNECTED,
        START_LISTENING,
        STOPPED_LISTENING
    }
}
