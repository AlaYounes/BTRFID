using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox {

    /// <summary>
    /// Error as received from server
    /// </summary>
    /// <version>$Revision: 618 $</version>
    /// <author>J.FARGEON</author> 
    /// <since>$Date: 2014-07-03 17:23:53 +0200 (jeu., 03 juil. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class Error {

        private String  name        = "";
        private int     code        = 0;
        private String  message     = "";
        private String  level       = "";
        private String  logMessage  = "";

        public String Name {
            get {
                return this.name;
            }
            set {
                this.name = value;
            }
        }
        public int Code {
            get {
                return this.code;
            }
            set {
                this.code = value;
            }
        }
        public String Message {
            get {
                return this.message;
            }
            set {
                this.message = value;
            }
        }
        public String Level {
            get {
                return this.level;
            }
            set {
                this.level = value;
            }
        }
        public String LogMessage {
            get {
                return this.logMessage;
            }
            set {
                this.logMessage = value;
            }
        }
    }
}
