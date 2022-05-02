using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using fr.nexess.toolbox;

namespace NexessToolboxTest.main.src.fr.nexess.toolbox {
    [TestClass]
    public class LongTaskExecutorTest {

        [TestMethod]
        public void should_execute_a_task_asynchronously() {

            // arrange
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            LongTaskExecutor taskExecuter = new LongTaskExecutor();
            taskExecuter.Dispatcher = null;

            Boolean flag = false;
            
            NoArgDelegate task = () => { flag = true; };

            // act 
            Assert.IsFalse(flag);
            taskExecuter.execute(task);
            
            // assert
            Assert.IsTrue(!manualResetEvent.WaitOne(1000));
            Assert.IsTrue(flag);
        }

        [TestMethod]
        public void should_execute_a_task_asynchronously_pasted_a_delay() {

            // arrange
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            LongTaskExecutor taskExecuter = new LongTaskExecutor();
            taskExecuter.Dispatcher = null;

            Boolean flag = false;

            NoArgDelegate task = () => { flag = true; };

            // act 
            Assert.IsFalse(flag);
            taskExecuter.execute(task,2000);

            // assert
            Assert.IsTrue(!manualResetEvent.WaitOne(1000));
            Assert.IsFalse(flag);

            manualResetEvent.Reset();

            Assert.IsTrue(!manualResetEvent.WaitOne(1005));
            Assert.IsTrue(flag);
        }

        [TestMethod]
        public void should_execute_a_task_asynchronously_with_success() {

            // arrange
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            LongTaskExecutor taskExecuter = new LongTaskExecutor();
            taskExecuter.Dispatcher = null;

            Boolean flag = false;
            Boolean success = false;

            NoArgDelegate task = () => { flag = true; };

            taskExecuter.onSuccess += (Object o, EventArgs e) => { success = true; };

            // act 
            Assert.IsFalse(flag);
            taskExecuter.execute(task);

            // assert
            Assert.IsTrue(!manualResetEvent.WaitOne(1000));
            Assert.IsTrue(flag);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void should_execute_a_task_asynchronously_with_failure() {

            // arrange
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);

            LongTaskExecutor taskExecuter = new LongTaskExecutor();
            taskExecuter.Dispatcher = null;

            Boolean failure = false;

            NoArgDelegate task = () => { throw new Exception(); };

            taskExecuter.onFailure += (Object o, EventArgs e) => { failure = true; };

            // act 
            taskExecuter.execute(task);

            // assert
            Assert.IsTrue(!manualResetEvent.WaitOne(1000));
            Assert.IsTrue(failure);
        }
    }
}
