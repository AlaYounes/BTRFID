using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.hao.rfid {
    public interface SustainableRfidDevice {

        Dictionary<String, String> getBuiltInComponentHealthStates();
    }
}
