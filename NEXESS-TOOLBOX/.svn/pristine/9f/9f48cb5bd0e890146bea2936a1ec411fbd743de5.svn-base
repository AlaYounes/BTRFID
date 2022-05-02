using System.Collections.Generic;
using System;

namespace fr.nexess.toolbox.resultmodifier
{
    /// <summary>
    ///  used to modificate the retrieving results by paging and sorting.
    /// </summary>
    /// <version>$Revision: 340 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-04-14 11:27:57 +0200 (lun., 14 avr. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence> 
    public class Sort
    {
        private List<SortElement> sortList = new List<SortElement>();

        #region CONSTRUCTORS
        // default constructor
        public Sort() { }

        public Sort( List<SortElement> sortList)
        {
            SortList    = sortList;
        }
        // copy constructor
        public Sort(Sort anResultModificator)
        {
            SortList    = anResultModificator.SortList;
        }
        #endregion

        #region PUBLIC_METHODS
        /// <summary>
        /// getter / setter
        /// </summary>
        /// <exception cref="Exception" />
        public List<SortElement> SortList
        {
            get
            {
                return this.sortList;
            }
            set
            {
                if (value != null)
                {
                    // clear current sort element list
                    this.sortList.Clear();

                    // copy
                    foreach (var item in value)
                    {
                        sortList.Add(item);
                    }
                }
                else
                {
                    throw new Exception("unable to set Sort List, invalid input parameter");
                }
            }
        }
        #endregion
    }
}
