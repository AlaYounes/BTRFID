using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fr.nexess.hao.weight;
using fr.nexess.hao.optic.eventhandler;

namespace fr.nexess.hao.reader.optic {

    /// <summary>
    /// basic driving interface that must implemented by all optic readers
    /// </summary>
    public interface OpticReader : Reader, OpticReaderEventProvider {

    }
}
