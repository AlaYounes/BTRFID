using System;
using System.Collections.Generic;

namespace fr.nexess.hao.rfid.eventHandler {

    public delegate void CodeFoundEventHandler(object sender, CodeFoundEventArgs e);

    public class CodeFoundEventArgs : EventArgs {
        private string code;

        public CodeFoundEventArgs(string code) {
            this.code = code;
        }

        public string Code {
            get {
                return this.code;
            }
        }

    }

}
