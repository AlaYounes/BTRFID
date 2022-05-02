using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.toolbox.log;

namespace fr.nexess.toolbox.comm.eventHandler
{
    /**
     * Generic Io Communication Exception
     * 
     * @version $Revision: 2 $
     * @author J.FARGEON
     * @since $Date: 2014-08-25 16:00:56 +0200 (lun., 25 août 2014) $
     * 
     * Copyright © 2005-2014 Nexess (http://www.nexess.fr)<br/>
     * Licence: Property of Nexess
     */
    public class IoComException : Exception
    {
        
        public IoComException()
        {
            LogProducer logProducer = new LogProducer(this.GetType());
            logProducer.Logger.Error("an Io Communication Exception occurs");
        }

        public IoComException(String cause)
        {
            LogProducer logProducer = new LogProducer(this.GetType());
            logProducer.Logger.Error(cause);
        }
    }
}
