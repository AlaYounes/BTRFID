// -----------------------------------------------------------------------
// <copyright file="LanguageTranslator.cs" company="Nexess">
// Copyright © 2008-2014 Nexess (http://www.nexess.fr)
// </copyright>
// <author>K.GUYOMARD</author>
// <date>2014-01-27</date>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Resources;
using System.Globalization;
using fr.nexess.toolbox.log;

namespace fr.nexess.toolbox
{
    /// <summary>
    /// Multi-Language management class
    /// </summary>
    public class LanguageTranslator
    {
        // Singleton 
        private static LanguageTranslator instance = null;
        private static readonly Object locker = new Object();
        // Resource files manager 
        private ResourceManager resource;
        // Culture information 
        public static CultureInfo cultureInfo;
        // Logs
        protected LogProducer logProducer = new LogProducer(typeof(LanguageTranslator));

        #region CONSTRUCTORS

        /// <summary>
        /// LanguageManager constructor
        /// </summary>
        protected LanguageTranslator(String fileFolder = null) {

            try {
                // Get folder
                if (String.IsNullOrEmpty(fileFolder)) {
                    fileFolder = ConfigurationManager.AppSettings["LANGUAGE_FILE_FOLDER"];
                }

                // Search for resources files
                resource = ResourceManager.CreateFileBasedResourceManager("culture", fileFolder, null);

                // Define the culture
                cultureInfo = new CultureInfo(ConfigurationManager.AppSettings["LANGUAGE"]);  
            } catch (Exception ex) {
                logProducer.Logger.Error("Error creating culture, " + ex.Message);
                throw ex;
            }
        }

        public LanguageTranslator(string paramCulture, string paramFileFolder) {

            try {
                // Search for resources files
                resource = ResourceManager.CreateFileBasedResourceManager("culture", paramFileFolder, null);

                // Define the culture
                string culture = paramCulture;
                cultureInfo = new CultureInfo(culture);
            }
            catch (Exception ex) {
                logProducer.Logger.Error("Error creating culture, " + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// get LanguageManager instance
        /// </summary>
        /// <returns></returns>
        public static LanguageTranslator getInstance(String fileFolder = null) {
            // Multi-threading control
            lock (locker) {
                if (instance == null) {
                    instance = new LanguageTranslator(fileFolder);
                }
            }

            return instance;
        }

        public static LanguageTranslator getInstance(string paramCulture, string paramFileFolder) {
            // Multi-threading control
            lock (locker) {
                if (instance == null) {
                    instance = new LanguageTranslator(paramCulture, paramFileFolder);
                }
            }
            // Update the culture
            cultureInfo = new CultureInfo(paramCulture);

            return instance;
        }
        #endregion

        #region PUBLIC_METHODS
        public static void initLanguage(string paramCulture, string paramFileFolder) {
            try {
                // Define the culture
                string culture = paramCulture;
                cultureInfo = null;
                cultureInfo = new CultureInfo(culture);
            }
            catch (Exception ex) {
                LogProducer logProducer = new LogProducer(typeof(LanguageTranslator));
                logProducer.Logger.Error("Error creating culture, " + ex.Message);
                throw ex;
            }
        }

        /// <summary>
        /// Translates the Key into current culture
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public string translate(string key) {

            try {

                return resource.GetString(key, cultureInfo);

            } catch (Exception ex) {

                logProducer.Logger.Error("Error when translating " + key + ".[" + ex.Message + "]");
            }

            return key;
        }

        /// <summary>
        /// Get CultureInfo object
        /// </summary>
        /// <returns></returns>
        public CultureInfo getCultureInfo() {
            return cultureInfo;
        }

        /// <summary>
        /// Get the two last letters of the culture info
        /// </summary>
        /// <returns></returns>
        public String getCountryCode() {
            string cultureName = cultureInfo.Name;
            return cultureName.Substring(cultureName.LastIndexOf('-') + 1);
        }
        #endregion


        /// <summary>
        /// get the localisated separator
        /// </summary>
        /// <returns></returns>
        public String getLabelSeparator(){

            String lang = cultureInfo.Name;

           if(lang.Substring(3, 2) == "FR"){
               return " : ";
           }
           return ": ";
        }


        /// <summary>
        /// get current language
        /// </summary>
        /// <returns></returns>
        public String getLang() {

            String lang = cultureInfo.Name;
            string l = lang.Substring(3, 2);

            switch (l) {
                case "FR": return "fr-FR";
                case "US": return "en-US";
                case "ES": return "es-ES";
                case "DE": return "de-DE";
                default: return "en-US";
            }
        }


    }
}
