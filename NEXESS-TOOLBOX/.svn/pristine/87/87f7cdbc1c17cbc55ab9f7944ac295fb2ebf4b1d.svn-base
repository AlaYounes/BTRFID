using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using log4net;
using log4net.Config;
using fr.nexess.toolbox;
using log4net.Core;
using System.Reflection;

namespace fr.nexess.toolbox.log {

    

    /**
     * log producer : log error for any exception
     */
    public class LogProducer : Log {
        /** Log4net logger object.*/
        //private ILogger loggerForExternalAssembly = null;
        private ILog logger = null;

        /**
         * Initialize the logger
         * @param the class that use the current logging system
         */
        public LogProducer(Type aCurrentClass) {

            try {
                logger = LogManager.GetLogger(aCurrentClass);
                string filePath = "conf/log4Net.conf";
                //string filePath = ConfigurationManager.AppSettings["LOG4NET_CONF_FILE_PATH"];
                XmlConfigurator.Configure(new System.IO.FileInfo(filePath));
            } catch (Exception) {
                logger = new FakeLogProducer();
            }
        }

       /// <summary>
       /// get logger
       /// </summary>
        public ILog Logger {
            get { return this.logger; }
        }

        void Log.Debug(object message) {
            logger.Debug(message);
        }

        void Log.Debug(object message, Exception exception) {
            logger.Debug(message, exception);
        }

        void Log.Error(object message) {
            logger.Error(message);
        }

        void Log.Error(object message, Exception exception) {
            logger.Error(message, exception);
        }

        void Log.Fatal(object message) {
            logger.Fatal(message);
        }

        void Log.Fatal(object message, Exception exception) {
            logger.Fatal(message, exception);
        }

        void Log.Info(object message) {
            logger.Info(message);
        }

        void Log.Info(object message, Exception exception) {
            logger.Info(message, exception);
        }

        void Log.Warn(object message) {
            logger.Warn(message);
        }

        void Log.Warn(object message, Exception exception) {
            logger.Warn(message, exception);
        }
    }

    class FakeLogProducer : ILog {

        void ILog.Debug(object message, Exception exception) {}

        void ILog.Debug(object message) {}

        void ILog.DebugFormat(IFormatProvider provider, string format, params object[] args) {}

        void ILog.DebugFormat(string format, object arg0, object arg1, object arg2) {}

        void ILog.DebugFormat(string format, object arg0, object arg1) {}

        void ILog.DebugFormat(string format, object arg0) {}

        void ILog.DebugFormat(string format, params object[] args) {}

        void ILog.Error(object message, Exception exception) {}

        void ILog.Error(object message) {}

        void ILog.ErrorFormat(IFormatProvider provider, string format, params object[] args) {}

        void ILog.ErrorFormat(string format, object arg0, object arg1, object arg2) {}

        void ILog.ErrorFormat(string format, object arg0, object arg1) {}

        void ILog.ErrorFormat(string format, object arg0) {}

        void ILog.ErrorFormat(string format, params object[] args) {}

        void ILog.Fatal(object message, Exception exception) {}

        void ILog.Fatal(object message) {}

        void ILog.FatalFormat(IFormatProvider provider, string format, params object[] args) {}

        void ILog.FatalFormat(string format, object arg0, object arg1, object arg2) {}

        void ILog.FatalFormat(string format, object arg0, object arg1) {}

        void ILog.FatalFormat(string format, object arg0) {}

        void ILog.FatalFormat(string format, params object[] args) {}

        void ILog.Info(object message, Exception exception) {}

        void ILog.Info(object message) {}

        void ILog.InfoFormat(IFormatProvider provider, string format, params object[] args) {}

        void ILog.InfoFormat(string format, object arg0, object arg1, object arg2) {}

        void ILog.InfoFormat(string format, object arg0, object arg1) {}

        void ILog.InfoFormat(string format, object arg0) {}

        void ILog.InfoFormat(string format, params object[] args) {}

        bool ILog.IsDebugEnabled {
            get { return true; }
        }

        bool ILog.IsErrorEnabled {
            get { return true; }
        }

        bool ILog.IsFatalEnabled {
            get { return true; }
        }

        bool ILog.IsInfoEnabled {
            get { return true; }
        }

        bool ILog.IsWarnEnabled {
            get { return true; }
        }

        void ILog.Warn(object message, Exception exception) {}

        void ILog.Warn(object message) {}

        void ILog.WarnFormat(IFormatProvider provider, string format, params object[] args) {}

        void ILog.WarnFormat(string format, object arg0, object arg1, object arg2) {}

        void ILog.WarnFormat(string format, object arg0, object arg1) {}

        void ILog.WarnFormat(string format, object arg0) {}

        void ILog.WarnFormat(string format, params object[] args) {}

        ILogger ILoggerWrapper.Logger {
            get { throw new NotImplementedException(); }
        }
    }
}
