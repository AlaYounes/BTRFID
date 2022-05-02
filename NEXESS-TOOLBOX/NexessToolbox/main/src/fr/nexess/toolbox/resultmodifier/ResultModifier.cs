using System.Linq;
using System.Collections.Generic;
using System;
namespace fr.nexess.toolbox.resultmodifier {

    /// <summary>
    /// This classe agregate all objects in charge of modifiing result of a request
    /// </summary>
    /// <version>$Revision: 340 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2014-04-14 11:27:57 +0200 (lun., 14 avr. 2014) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public class ResultModifier {
        public int     start = 0;
        public int     limit = 0;
        public Filter  filter = null;
        public Sort    sort = null;

        public ResultModifier(int start = 0, int limit = 0, Filter filter = null, Sort sort = null) {
            this.start = start;
            this.limit = limit;
            this.filter = filter;
            this.sort = sort;
        }

        public static List<T> applyPaging<T>(List<T> item, int start = 0, int limit = 0) {

            List<T> modifiedResult = new List<T>();

            if (item == null
                || item.Count < 1) {
                return modifiedResult;
            }

            IEnumerable<T> modified = null;

            modified = item.Skip(start);

            if (limit > 0) {
                modified = modified.Take(limit);
            }

            modifiedResult = modified.ToList();

            return modifiedResult;
        }

        public static List<T> applySorting<T, U>(List<T> item, SortDirection sortDirection, Func<T, U> keySelector) {

            List<T> orderedList = item;

            if (sortDirection != null) {

                if (sortDirection == SortDirection.ASC) {

                    orderedList = item.OrderBy(keySelector).ToList();

                } else {

                    orderedList = item.OrderByDescending(keySelector).ToList();
                }
            }
            return orderedList;
        }
    }
}
