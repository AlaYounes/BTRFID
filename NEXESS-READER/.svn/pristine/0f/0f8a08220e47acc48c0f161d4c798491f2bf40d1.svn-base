using System;
using System.Collections.Generic;

namespace fr.nexess.hao.rfid.eventHandler {

    /**
     *  tag found Event handler  : thrown when a rfid reader "tagFound" Event occurs.
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public delegate void TagFoundEventHandler(object sender, TagFoundEventArgs e);

    /**
     *  tag found Event Arguments
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-21 16:32:52 +0200 (jeu., 21 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public class TagFoundEventArgs : EventArgs {
        private List<string> snrs;

        public TagFoundEventArgs(List<string> snrs) {
            this.snrs = snrs;
        }

        public List<string> Snrs {
            get {
                return this.snrs;
            }
        }

    }

}
