using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OBID;

namespace fr.nexess.hao.rfid.device.feig
{
    /**
     *  tag found detailed Event handler  : thrown when a rfid reader "tagFoundDetailed" Event occurs.

     * Copyright © 2005-2016 Nexess (http://www.nexess-solutions.com)<br/>
     * Licence: Property of Nexess
     */
    public delegate void TagFoundObidEventHandler(object sender, TagFoundObidEventArgs e);

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
    public class TagFoundObidEventArgs : EventArgs
    {

        private List<FedmBrmTableItem> tags;

        public TagFoundObidEventArgs(List<FedmBrmTableItem> tags)
        {
            this.tags = tags;
        }

        public List<FedmBrmTableItem> Tags
        {
            get
            {
                return this.tags;
            }
        }

    }

}
