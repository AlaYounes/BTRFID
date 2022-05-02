using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace fr.nexess.toolbox {

    public class Caller {

        public int Level { get; set; }
        public String classAccessorName;

        public Caller(Object classAccessor) {

            classAccessorName = classAccessor.GetType().Name;
        }
        
        public String getCallerClassName() {

            String callerClassName = "";

            System.Diagnostics.StackTrace stackTrace =  new System.Diagnostics.StackTrace(true);

            if (stackTrace.FrameCount >= 0) {

                //StackFrame frame in stackTrace.GetFrames()
                for (int i = 1; i < stackTrace.FrameCount; i++ ) {

                    String aClassName = stackTrace.GetFrame(i).GetFileName();

                    if (!String.IsNullOrEmpty(aClassName)) {

                        aClassName = Path.GetFileNameWithoutExtension(aClassName);

                        if (!String.IsNullOrEmpty(aClassName) 
                            && String.Compare(aClassName, classAccessorName) != 0) {

                            callerClassName = aClassName;

                            break;
                        }
                    }
                }
            }

            return callerClassName;
        }
    }
}
