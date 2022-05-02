using log4net;
using log4net.Appender;
using log4net.Filter;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace fr.nexess.toolbox.log {

    /**
     * log producer : log error for any exception
     */
    public class LogProducer : Log {
        
        public const string FILTER_REQUEST = "DECODED REQUEST";
        public const string FILTER_RESPONSE = "DECODED RESPONSES";
        public const string LEVEL = "LEVEL";

        public enum enumValidLevels {
            ALL,
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL,
            OFF
        }
        private ILog log = null;

        /**
         * Initialize the logger
         * @param the class that use the current logging system
         */
        public LogProducer(Type aCurrentClass) {

            try {
                log = LogManager.GetLogger(aCurrentClass);
                string folder = getEnvDataFolder();
                string filePath = ConfigurationManager.AppSettings["LOG4NET_CONF_FILE_PATH"];
                if (!string.IsNullOrEmpty(folder)) {
                    filePath = folder + "/" + filePath;
                }
                XmlConfigurator.Configure(new System.IO.FileInfo(filePath));
            } catch (Exception) {
                log = new FakeLogProducer();
            }
        }

        public LogProducer(Type aCurrentClass, string filePath, string appenderName = null) {

            try {
                XmlConfigurator.Configure(new System.IO.FileInfo(filePath));
                if (appenderName != null) {
                    log = LogManager.GetLogger(appenderName);
                } else {
                    log = LogManager.GetLogger(aCurrentClass);
                }
                
                
            } catch (Exception) {
                log = new FakeLogProducer();
            }
        }

        static public string getEnvDataFolder() {
            String environmentPath = System.Environment
                .GetEnvironmentVariable("NEXCAP_DATA_DIRECTORY", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(environmentPath)) {
                environmentPath = System.Environment
                    .GetEnvironmentVariable("NEXCAP_DATA_DIRECTORY", EnvironmentVariableTarget.Machine);
            }
            return environmentPath;
        }

        /// <summary>
        /// get logger
        /// </summary>
        public ILog Logger {
            get { return this.log; }
        }

        void Log.Debug(object message) {
            log.Debug(message);
        }

        void Log.Debug(object message, Exception exception) {
            log.Debug(message, exception);
        }

        void Log.Error(object message) {
            log.Error(message);
        }

        void Log.Error(object message, Exception exception) {
            log.Error(message, exception);
        }

        void Log.Fatal(object message) {
            log.Fatal(message);
        }

        void Log.Fatal(object message, Exception exception) {
            log.Fatal(message, exception);
        }

        void Log.Info(object message) {
            log.Info(message);
        }

        void Log.Info(object message, Exception exception) {
            log.Info(message, exception);
        }

        void Log.Warn(object message) {
            log.Warn(message);
        }

        void Log.Warn(object message, Exception exception) {
            log.Warn(message, exception);
        }

        // get log level
        public Level getLevel() {
            return ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level;
        }

        // change log level
        public void changeLevel(Level level) {
            ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = level;
            updateLevelInFile(level.ToString().ToUpper());        
        }

        // get filter value
        public Boolean getFilterValue(string stringToMatch) {
            RollingFileAppender appender = ((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).GetAppenders().OfType<RollingFileAppender>().FirstOrDefault();
            if (appender != null) {
                log4net.Filter.IFilter filterHead = appender.FilterHead;
                //traverse the filter chain
                var currentFilter = filterHead;
                while (currentFilter != null) {
                    if (currentFilter is log4net.Filter.StringMatchFilter) {
                        if(((log4net.Filter.StringMatchFilter)currentFilter).StringToMatch == stringToMatch) {
                            return ((log4net.Filter.StringMatchFilter)currentFilter).AcceptOnMatch;
                        }
                    }
                    currentFilter = currentFilter.Next;
                }
            }
            return false;
        }

        // set the level value in log4net.conf
        public void updateLevelInFile(string newLevel) {
            string filePath = ConfigurationManager.AppSettings["LOG4NET_CONF_FILE_PATH"];
            if (filePath.Length <= 0) {
                throw new Exception("filePath length = 0");
            }

            // Check if file exists
            if (File.Exists(filePath)) {
                XDocument file = XDocument.Load(filePath);
                XAttribute att = file.Descendants("log4net").Descendants("root").Descendants("level").Attributes().First();
                if (att != null) {
                    att.Value = newLevel;
                    file.Save(filePath);
                }      
            } 
        }

        // set the acceptOnMatch value n log4net.conf to "true" or "false" for the given filter 
        public void updateFilterInFile(string stringToMatch, string newValue) {
            string filePath = ConfigurationManager.AppSettings["LOG4NET_CONF_FILE_PATH"];
            if (filePath.Length <= 0) {
                throw new Exception("filePath length = 0");
            }

            // Check if file exists
            if (File.Exists(filePath)) {
                XDocument file = XDocument.Load(filePath);
                List<XElement> filters = file.Descendants("log4net").Descendants("appender").Descendants("filter").ToList();
                foreach(XElement ele in filters) {
                    XElement eleStringToMatch = ele.Descendants("stringToMatch").First();
                    if(eleStringToMatch != null && eleStringToMatch.Attribute("value").Value == stringToMatch) {
                        XElement eleAcceptOnMatch = ele.Descendants("acceptOnMatch").First();
                        if(eleAcceptOnMatch != null) {
                            eleAcceptOnMatch.Attribute("value").Value = newValue;
                            file.Save(filePath);
                            return;
                        }
                    }
                }
                file.Save(filePath);
            }
        }

        public void saveAllConfig(Dictionary<string, string> configs) {

            foreach (KeyValuePair<string, string> pair in configs) {
                switch(pair.Key) {
                    case LEVEL: updateLevelInFile(pair.Value);
                        break;
                    case FILTER_REQUEST:
                    case FILTER_RESPONSE:
                        updateFilterInFile(pair.Key, pair.Value);
                        break;
                    default: break;
                }
            }

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
