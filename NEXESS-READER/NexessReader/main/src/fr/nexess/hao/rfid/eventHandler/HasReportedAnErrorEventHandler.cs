using System;

namespace fr.nexess.hao.rfid.eventHandler {
    /**
     *  Has Reported An Error Event handler : thrown when a rfid reader "HasReportedAnError" Event occurs.
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public delegate void HasReportedAnErrorEventHandler(object sender, HasReportedAnErrorEventArgs e);

    /**
     *  Has Reported An Error Event Arguments
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public class HasReportedAnErrorEventArgs : EventArgs {

        private String message;

        public HasReportedAnErrorEventArgs(String message) {
            this.message = message;
        }

        public String Message {
            get {
                return this.message;
            }
        }
    }
}
