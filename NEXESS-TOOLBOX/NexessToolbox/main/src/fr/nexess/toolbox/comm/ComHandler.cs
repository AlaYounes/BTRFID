using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox.comm
{
    /**
     * Communication handler interface
     * 
     * @version $Revision: 5 $
     * @author J.FARGEON
     * @since $Date: 2014-09-12 13:46:35 +0200 (ven., 12 sept. 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public interface ComHandler : ComEventProvider
    {
        /** comm connection.*/
        void connect();

        /** comm disconnection.*/
        void disconnect();

        /** Start listening.*/
        void startlistening();

        /** stop listening.*/
        void stopListening();

        /** current communication state getter.*/
        ComState getState();

        /** send string.*/
        void sendData(string msg);

        /** send bytes array.*/
        void sendData(byte[] msg);

        /** send bytes array synchronously : means this method is waiting for a response*/
        byte[] requestData(byte[] msg, int timeout = -1);
    }

    public enum ComState
    {
        CONNECTED,
        DISCONNECTED
    }
}
