using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox {

    /// <summary>
    /// Abstract class used for low level communication toolbox (ex: data conversion)
    /// </summary>
    /// <version>$Revision: 26 $</version>
    /// <author>J.FARGEON</author>
    /// <since>$Date: 2015-11-30 17:29:19 +0100 (lun., 30 nov. 2015) $</since>
    /// <licence>Copyright © 2005-2014 Nexess (http://www.nexess.fr)</licence>
    public abstract class ConversionTool {

        /// <summary>
        /// convert an array of bytes to an hex formatted string (2F3c)
        /// </summary>
        /// <param name="ba">an array of bytes</param>
        /// <returns>hex formatted string</returns>
        public static string byteArrayToString(byte[] ba) {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba) {
                hex.AppendFormat("{0:X2}", b);
            }
            return hex.ToString();
        }

        /// <summary>
        /// convert hex formatted string (2F3c) to an array of bytes
        /// </summary>
        /// <param name="hex">hex formatted frame</param>
        /// <returns>array of bytes</returns>
        public static byte[] stringToByteArray(String hex) {

            byte[] bytes = null;

            if (hex != null && hex.Length % 2 == 0) {

                bytes = new byte[hex.Length / 2];

                try {
                    for (int i = 0; i < hex.Length; i += 2) {
                        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }
                } catch (Exception) {
                    // do nothing
                }
            }

            return bytes;
        }

        /// <summary>
        /// convert hex formatted string with (2F 3c 5E) to an array of bytes
        /// </summary>
        /// <param name="hex">hex formatted frame</param>
        /// <param name="separator">char sequence that must be excluded from the hex frame</param>
        /// <returns>array of bytes</returns>
        public static byte[] stringToByteArray(String hex, String separator) {

            String hexWithoutSep = "";

            try {

                hexWithoutSep = hex.Replace(separator, "");
            } catch (Exception) {
                // do nothing
            }

            return stringToByteArray(hexWithoutSep);
        }

        // convert Hexa to ascii
        public static string hexaToAscii(string hexString) {
            try {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2) {
                    string hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    ulong decval = Convert.ToUInt64(hs, 16);
                    long deccc = Convert.ToInt64(hs, 16);
                    char character = Convert.ToChar(deccc);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { 
            // do nothing
            }

            return string.Empty;
        }

        /// <summary>
        /// Remove char in a string and keep numeric
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int getNumeric(string input) {
            try {
                string numericString =  new string(input.Where(c => char.IsDigit(c)).ToArray());
                if(numericString.Length == 0 ) {
                    return 0;
                }
                return Int32.Parse(numericString);
            }
            catch(Exception ex){
                throw ex;
            }
        }

        public static String formatDateTimeToISO8601(DateTime operationDate) {

            return operationDate.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
        }

        public static String formatDateToISO8601(DateTime operationDate) {

            return operationDate.ToString("yyyy-MM-dd");
        }

        /**
        “d” : short date without time (“1/1/2013”)
        “D” : long date (“Mardi 11 décembre 2012”)
        “f” : “D” + short time (“Mardi 11 décembre 2012 3:25”)
        “F” : “f” + seconds (“Mardi 11 décembre 2012 16:12:30”)
        “g” : short dateTime (“1/1/2013 3:25”)
        “G” : short dateTime + seconds (“1/1/2013 3:25:30”)
        “M” : day + month (“15 Décembre”)
        “R” : short day, long date and long time (“Ma, 1er Jan 2013 22:15:20 GMT”)
     */

        /// <summary>
        /// Converts the Value of the DateTime object to its equivalent string representation
        /// using the specified format and culture-specific format information.
        /// Accepts the null DateTime.
        /// </summary>
        /// 
        /// <param name="aDateTime"></param>
        /// <param name="aFormat"></param>
        /// <param name="aFormatProvider"></param>
        /// <returns></returns>
        public static String DateTimeToString(
            Nullable<DateTime> aDateTime,
            String aFormat,
            IFormatProvider aFormatProvider) {

            if (aDateTime == null) {
                return "";
            }
            DateTime theDateTime = (DateTime)aDateTime;
            return theDateTime.ToString(aFormat, aFormatProvider);
        }
    }
}
