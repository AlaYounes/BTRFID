using System.Collections.Generic;
using System;

namespace fr.nexess.toolbox.resultmodifier {

    /// <summary>
    /// helps for manipulating filter field and serialize it to a particular export format.
    /// </summary>
    /// <version>$Revision: 340 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-04-14 11:27:57 +0200 (lun., 14 avr. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class Filter // : serializable ??
    {
        protected List<FilterElement> filterList = new List<FilterElement>();

        #region CONSTRUCTORS
        /// <summary>
        /// default constructor
        /// </summary>
        public Filter() { }

        /// <summary>
        /// second constructor
        /// </summary>
        public Filter(List<FilterElement> filterList) {
            FilterList = filterList;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aFilter">filter object from whch the current is created</param>
        public Filter(Filter aFilter) {

            FilterList = aFilter.filterList;
        }
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// getter / setter
        /// </summary>
        /// <exception cref="Exception" />
        public List<FilterElement> FilterList {
            get {
                return this.filterList;
            }
            set {
                if (value != null) {
                    // clear current filter element list
                    this.filterList.Clear();

                    // copy
                    foreach (var item in value) {
                        filterList.Add(item);
                    }
                } else {
                    throw new Exception("unable to set filter List, invalid input parameter");
                }
            }

        }
        #endregion
    }
}
