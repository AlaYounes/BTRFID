using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace fr.nexess.toolbox.resultmodifier {
    /// <summary>
    /// Sort condition for the requests
    /// </summary>
    /// <version>$Revision: 340 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-04-14 11:27:57 +0200 (lun., 14 avr. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence> 
    public class SortElement {

        public   String          property;
        public   SortDirection   direction;

        public SortElement() {
        }

        public SortElement(String aProperty, SortDirection aDirection) {
            property = aProperty;
            direction = aDirection;
        }

        /// <summary>
        /// return property to sort
        /// </summary>
        /// <returns></returns>
        public String getProperty() {
            return property;
        }

        /// <summary>
        /// set property to sort
        /// </summary>
        /// <param name="property"></param>
        public void setProperty(String property) {
            this.property = property;
        }

        /// <summary>
        /// return sort direction (asc-desc)
        /// </summary>
        /// <returns></returns>
        public SortDirection getDirection() {
            return direction;
        }

        /// <summary>
        /// set sort direction
        /// </summary>
        /// <param name="direction">SortDirection ASC or DESC</param>
        public void setDirection(SortDirection direction) {
            this.direction = direction;
        }
    }


    /// <summary>
    /// Enumeration type for the direction of sorts.
    /// </summary>
    /// <version>$Revision: 340 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-04-14 11:27:57 +0200 (lun., 14 avr. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence> 
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SortDirection {
        ASC,
        DESC
    }
}
