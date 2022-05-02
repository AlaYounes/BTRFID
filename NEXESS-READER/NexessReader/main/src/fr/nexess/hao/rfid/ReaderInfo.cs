using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fr.nexess.hao.rfid {
    public class ReaderInfo {

        private string type;
        private string comPort;
        private RfidDeviceState state;
        private string version;
        Dictionary<EnumReaderType, string> infoList;

        #region CONSTRUCTOR
        public ReaderInfo(string type, string comPort, RfidDeviceState state) {
            this.Type = type;
            this.ComPort = comPort;
            this.State = state;
            InfoList = new Dictionary<EnumReaderType, string>();
            InfoList.Add(EnumReaderType.TYPE, type);
            if (!string.IsNullOrEmpty(comPort)) {
                InfoList.Add(EnumReaderType.COM_PORT, comPort);
            }
            InfoList.Add(EnumReaderType.STATE, state.ToString());
        }
        #endregion

        public string Type {
            get {
                return type;
            }

            set {
                type = value;
            }
        }

        public string ComPort {
            get {
                return comPort;
            }

            set {
                comPort = value;
            }
        }

        public RfidDeviceState State {
            get {
                return state;
            }

            set {
                state = value;
            }
        }

        public Dictionary<EnumReaderType, string> InfoList {
            get {
                return infoList;
            }

            set {
                infoList = value;
            }
        }

        public string Version {
            get {
                return version;
            }

            set {
                version = value;
            }
        }
    }
}
