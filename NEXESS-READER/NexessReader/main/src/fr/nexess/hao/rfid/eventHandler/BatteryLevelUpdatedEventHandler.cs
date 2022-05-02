using System;
using System.Collections.Generic;

namespace fr.nexess.hao.rfid.eventHandler {

    public delegate void BatteryLevelUpdatedEventHandler(object sender, BatteryLevelUpdatedEventArgs e);

    public class BatteryLevelUpdatedEventArgs : EventArgs {
        private int level;

        public BatteryLevelUpdatedEventArgs(int level) {
            this.level = level;
        }

        public int Level {
            get {
                return this.level;
            }
        }

    }

}
