using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using fr.nexess.toolbox;

namespace NexessToolboxTest.main.src.fr.nexess.toolbox {
    [TestClass]
    public class CallerTest {
        [TestMethod]
        public void should_get_correct_type() {

            // arrange
            Caller caller = new Caller(/*this*/"rien");

            // act
            String className = caller.getCallerClassName();

            // assert
            Assert.IsTrue(String.Compare(className,"CallerTest",true)==0);
        }
    }
}
