using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox.comm.eventHandler {

    public delegate void OnDataReceivedEventHandler(object sender, OnDataReceivedEventArgs e);

    public class OnDataReceivedEventArgs : EventArgs {
        private String strData = null;
        private byte[] byteData = null;

        public OnDataReceivedEventArgs(String strData) {
            this.strData = strData;
        }

        public OnDataReceivedEventArgs(byte[] byteData) {
            this.byteData = byteData;
        }

        public byte[] DataAsBytes {
            get {
                if (byteData != null) {

                    return byteData;

                } else if (strData != null) {

                    return ConversionTool.stringToByteArray(strData);

                } else {

                    return null;
                }
            }
        }

        public String DataAsString {
            get {
                if (strData != null) {

                    return strData;

                } else if (byteData != null) {

                    return ConversionTool.byteArrayToString(byteData);

                } else {

                    return null;
                }
            }
        }
    }

}
