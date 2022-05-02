using System;

namespace fr.nexess.toolbox.comm.eventHandler
{
    /**
     *  Has Reported A communication Error Event handler : thrown when a rfid reader "HasReportedAnError" Event occurs.
     * 
     * @version $Revision: 5 $
     * @author J.FARGEON
     * @since $Date: 2014-09-12 13:46:35 +0200 (ven., 12 sept. 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public delegate void HasReportedAComErrorEventHandler(object sender, HasReportedAComErrorEventArgs e);

    /**
     *  Has Reported A communication Error Event Arguments
     * 
     * @version $Revision: 5 $
     * @author J.FARGEON
     * @since $Date: 2014-09-12 13:46:35 +0200 (ven., 12 sept. 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public class HasReportedAComErrorEventArgs : EventArgs
    {
        private String message;

        public HasReportedAComErrorEventArgs(String message)
        {
            this.message = message;
        }

        public String Message
        {
            get
            {
                return this.message;
            }
        }
    }
}
