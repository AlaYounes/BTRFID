using System;
using System.Collections.Generic;

namespace fr.nexess.hao.rfid.device.impinj {

    /**
     *  tag found detailed Event handler  : thrown when a rfid reader "tagFoundDetailed" Event occurs.

     * Copyright © 2005-2016 Nexess (http://www.nexess-solutions.com)<br/>
     * Licence: Property of Nexess
     */
    public delegate void TagFoundOctaneEventHandler(object sender, TagFoundOctaneEventArgs e);

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
    public class TagFoundOctaneEventArgs : EventArgs {

        private List<Impinj.OctaneSdk.Tag> tags;

        public TagFoundOctaneEventArgs(List<Impinj.OctaneSdk.Tag> tags)
        {
            this.tags = tags;
        }

        public List<Impinj.OctaneSdk.Tag> Tags
        {
            get
            {
                return this.tags;
            }
        }

    }

}
