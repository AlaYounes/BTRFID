using System;
using fr.nexess.toolbox.comm.eventHandler;

namespace fr.nexess.toolbox.comm
{
    /**
    * interface for retrieving events from any Communication provider
    * 
    * @version $Revision: 5 $
    * @author J.FARGEON
    * @since $Date: 2014-09-12 13:46:35 +0200 (ven., 12 sept. 2014) $
    * 
    * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
    * Licence: Property of Nexess
    */
    public interface ComEventProvider
    {
        /** Event fired when an unexpected disconnection occured.*/
        event EventHandler UnexpectedDisconnection;

        /** Event fired when the communication provider has reported a communication error.*/
        event HasReportedAComErrorEventHandler HasReportedAComError;

        /** Event fired when the communication provider is connected.*/
        event EventHandler Connected;

        /** Event fired when the communication provider is disconnected on a classical way.*/
        event EventHandler Disconnected;

        /** Event fired when the communication provider start listening on communication port/pipe.*/
        event EventHandler StartListening;

        /** Event fired when the communication provider stop listening on communication port/pipe (nominal way).*/
        event EventHandler StoppedListening;

        event OnDataReceivedEventHandler OnDataReceived;
    }
}
