using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox {

    /// <summary>
    /// Notifiable abstract object
    /// </summary>
    /// <version>$Revision: 767 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-08-21 10:53:06 +0200 (jeu., 21 août 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public abstract class Notifiable {

        protected String message;
        protected String summary;

        public Notifiable(

            String message = "",
            String summary = "") {

            this.message = message;
            this.summary = summary;
        }

        #region PUBLIC_METHODS
        public String Message {
            get {
                return this.message;
            }
            set {
                this.message = value;
            }
        }

        public String Summary {
            get {
                return this.summary;
            }
            set {
                this.summary = value;
            }
        }
        #endregion
    }

}
