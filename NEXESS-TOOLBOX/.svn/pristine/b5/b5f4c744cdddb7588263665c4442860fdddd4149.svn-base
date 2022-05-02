using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace fr.nexess.toolbox.resultmodifier {
    /// <summary>
    /// This class is used to populate filter object usefull for requesting server.
    /// </summary>
    /// <version>$Revision: 1137 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2015-04-20 16:32:53 +0200 (lun., 20 avr. 2015) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence> 
    public class FilterElement {
        #region MEMBERS
        public String              property;    /// property name
        public ComparisonOperator  comparison;  /// comparison operator (enum gt,lt,eq etc.)
        public Object              value;       /// Value used for comparison
        public PropertyType        type;        /// Value type (enum string, numeric,date etc.)
        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// copy contructor
        /// </summary>
        /// <param name="aFilter">Filter for copiing</param>
        public FilterElement(FilterElement aFilter) {
            this.property = aFilter.getProperty();
            this.comparison = aFilter.getComparisonOperator();
            this.type = aFilter.getType();
            this.value = aFilter.getValue();
        }


        /// <summary>
        /// Filter default constructor
        /// </summary>
        public FilterElement() { }


        /// <summary>
        /// Filter complete constructor
        /// </summary>
        /// <param name="aProperty">property name</param>
        /// <param name="aComparisonOperator">comparison operator</param>
        /// <param name="aValue">property Value for comparison</param>
        /// <param name="aPropertyType">property type</param>
        public FilterElement(String aProperty,
                                ComparisonOperator aComparisonOperator,
                                Object aValue,
                                PropertyType aPropertyType) {
            this.property = aProperty;
            this.comparison = aComparisonOperator;
            this.type = aPropertyType;
            this.value = aValue;
        }

        #endregion

        #region PUBLIC_METHODS


        /// <summary>
        /// Get the Value of type
        /// </summary>
        /// <returns>return the Value of type</returns>
        public PropertyType getType() {
            return this.type;
        }

        /// <summary>
        /// Set the Value of type
        /// </summary>
        /// <param name="type">type</param>
        public void setType(PropertyType type) {
            this.type = type;
        }

        /// <summary>
        /// get Value
        /// </summary>
        /// <returns>generic object (could be a list)</returns>
        public Object getValue() {
            return this.value;
        }

        /// <summary>
        /// set Value
        /// </summary>
        /// <param name="Value">generic object Value (could be a list)</param>
        public void setValue(Object value) {
            this.value = value;
        }

        /// <summary>
        /// get comparison operator
        /// </summary>
        /// <returns>comparison operator</returns>
        public ComparisonOperator getComparisonOperator() {
            return this.comparison;
        }

        /// <summary>
        /// set comparison operator
        /// </summary>
        /// <param name="comparisonOperator">comparison operator</param>
        public void setComparison(ComparisonOperator comparisonOperator) {
            this.comparison = comparisonOperator;
        }

        /// <summary>
        /// get property name
        /// </summary>
        /// <returns>the property name : refers to a object's field</returns>
        public String getProperty() {
            return this.property;
        }

        /// <summary>
        /// set property name 
        /// </summary>
        /// <param name="property">the property name : refers to a object's field</param>
        public void setProperty(String property) {
            this.property = property;
        }
        #endregion
    }

    /// <summary>
    /// enumeration that refers to a list of DBMS valid operators.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComparisonOperator {
        GT,/// Greater than
        LT,/// Less than
        EQ,/// Equal to
        NE,/// Not equal
        IN,/// IN operator allows you to specify multiple values in a WHERE clause
        // The 'LIKE' operators is used to search for a specified pattern in a column
        CONTAINS,   /// ex: LIKE %Value%
        STARTSWITH,/// ex: LIKE Value%
        ENDSWITH,  /// ex: LIKE %Value
        IS_NULL, /// Property dont exists
        IS_NOT_NULL /// Property exists an not null !
    }

    /// <summary>
    /// enumeration that refers to Value types where filter will applied on.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PropertyType {
        STRING, /// string
        DATE,   /// date
        NUMERIC,/// numerical type
        LONG,   /// identifier object
        LIST,   /// list of parameter, will be used as a string
        BOOLEAN,/// boolean Value
        OBJECT,  /// generic object
        DATETIME,
        ENUM
    }
}
