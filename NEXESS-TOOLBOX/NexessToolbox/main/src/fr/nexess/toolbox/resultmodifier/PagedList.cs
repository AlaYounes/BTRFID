using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox.resultmodifier {

    /// <summary>
    /// Class that helps for manipulating paged list. 
    /// Will provides list of whatever type of items and the total number of the entire list.
    /// </summary>
    /// <version>$Revision: 1239 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2015-10-09 14:57:19 +0200 (ven., 09 oct. 2015) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class PagedList<T> {

        #region MEMBERS
        private readonly T items;

        private readonly int total = 0;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="items">paged list of item</param>
        /// <param name="total">total number from the entire list</param>
        public PagedList(T items, int total) {

            this.items = items;
            this.total = total;
        }

        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// get the paged data list
        /// </summary>
        public T Items {

            get {

                return items;
            }
        }

        /// <summary>
        /// get the total number of item from the entire (and not retrieved) list
        /// </summary>
        public int Total {
            get {

                return total;
            }
        }

        #endregion
    }
}
