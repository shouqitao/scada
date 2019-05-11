/*
 * Copyright 2018 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Localization mechanism
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2014
 * Modified : 2018
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Scada {
    /// <summary>
    /// Localization mechanism
    /// <para>Localization mechanism</para>
    /// </summary>
    public static class Localization {
        /// <summary>
        /// Dictionary
        /// </summary>
        public class Dict {
            /// <summary>
            /// Constructor
            /// </summary>
            private Dict() { }

            /// <summary>
            /// Constructor
            /// </summary>
            public Dict(string key) {
                Key = key;
                Phrases = new Dictionary<string, string>();
            }

            /// <summary>
            /// Get dictionary key
            /// </summary>
            public string Key { get; private set; }

            /// <summary>
            /// Get the phrases contained in the dictionary by their keys
            /// </summary>
            public Dictionary<string, string> Phrases { get; private set; }

            /// <summary>
            /// Get the name of the dictionary file for a given culture.
            /// </summary>
            public static string GetFileName(string directory, string fileNamePrefix, string cultureName) {
                return ScadaUtils.NormalDir(directory) +
                       fileNamePrefix + (string.IsNullOrEmpty(cultureName) ? "" : "." + cultureName) + ".xml";
            }

            /// <summary>
            /// Get a phrase from the dictionary by key or an empty phrase if it is missing
            /// </summary>
            public string GetPhrase(string key) {
                return Phrases.ContainsKey(key) ? Phrases[key] : GetEmptyPhrase(key);
            }

            /// <summary>
            /// Get a phrase from the dictionary by key or default value if it is missing
            /// </summary>
            public string GetPhrase(string key, string defaultVal) {
                return Phrases.ContainsKey(key) ? Phrases[key] : defaultVal;
            }

            /// <summary>
            /// Get an empty phrase for a given key
            /// </summary>
            public static string GetEmptyPhrase(string key) {
                return "[" + key + "]";
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        static Localization() {
            InitDefaultCulture();
            SetCulture(ReadCulture());
            Dictionaries = new Dictionary<string, Dict>();
        }


        /// <summary>
        /// Get the default culture name
        /// </summary>
        public static string DefaultCultureName { get; private set; }

        /// <summary>
        /// Get default culture information
        /// </summary>
        public static CultureInfo DefaultCulture { get; private set; }

        /// <summary>
        /// Get information about the culture of all SCADA applications
        /// </summary>
        public static CultureInfo Culture { get; private set; }

        /// <summary>
        /// Get a sign of the use of Russian localization
        /// </summary>
        public static bool UseRussian { get; private set; }

        /// <summary>
        /// Get downloaded localization dictionaries
        /// </summary>
        public static Dictionary<string, Dict> Dictionaries { get; private set; }

        /// <summary>
        /// Get a sign that the day's record should be placed after the month's record
        /// </summary>
        public static bool DayAfterMonth {
            get {
                string pattern = Culture.DateTimeFormat.ShortDatePattern.ToLowerInvariant();
                return pattern.IndexOf('m') < pattern.IndexOf('d');
            }
        }


        /// <summary>
        /// Initialize name and default culture
        /// </summary>
        private static void InitDefaultCulture() {
            try {
                DefaultCultureName = CultureIsRussian(CultureInfo.CurrentCulture) ? "ru-RU" : "en-GB";
                DefaultCulture = CultureInfo.GetCultureInfo(DefaultCultureName);
            } catch {
                DefaultCultureName = "";
                DefaultCulture = CultureInfo.CurrentCulture;
            }
        }

        /// <summary>
        /// Read the name of the culture from the registry
        /// </summary>
        private static string ReadCulture() {
            try {
#if NETSTANDARD2_0
                return "";
#else
                using (var key =
                    Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                            Microsoft.Win32.RegistryView.Registry64)
                        .OpenSubKey("Software\\SCADA", false)) {
                    return key.GetValue("Culture").ToString();
                }
#endif
            } catch {
                return "";
            }
        }

        /// <summary>
        /// Set culture
        /// </summary>
        public static void SetCulture(string cultureName) {
            try {
                Culture = string.IsNullOrEmpty(cultureName) ? DefaultCulture : CultureInfo.GetCultureInfo(cultureName);
            } catch {
                Culture = DefaultCulture;
            } finally {
                UseRussian = CultureIsRussian(Culture);
            }
        }

        /// <summary>
        /// Check that the name of the culture corresponds to the Russian culture
        /// </summary>
        private static bool CultureIsRussian(CultureInfo cultureInfo) {
            return cultureInfo.Name == "ru" || cultureInfo.Name.StartsWith("ru-", StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Change culture
        /// </summary>
        public static void ChangeCulture(string cultureName) {
            if (string.IsNullOrEmpty(cultureName))
                cultureName = ReadCulture();
            SetCulture(cultureName);
        }

        /// <summary>
        /// Write the name of the culture in the registry
        /// </summary>
        public static bool WriteCulture(string cultureName, out string errMsg) {
            try {
#if !NETSTANDARD2_0
                using (var key =
                    Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,
                        Microsoft.Win32.RegistryView.Registry64).CreateSubKey("Software\\SCADA")) {
                    key.SetValue("Culture", cultureName);
                }
#endif
                errMsg = "";
                return true;
            } catch (Exception ex) {
                errMsg = ("Error writing culture info to the registry: ") + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Get the file name of the dictionary depending on the SCADA culture
        /// </summary>
        public static string GetDictionaryFileName(string directory, string fileNamePrefix) {
            return Dict.GetFileName(directory, fileNamePrefix, Culture.Name);
        }

        /// <summary>
        /// Download SCADA culture dictionaries
        /// </summary>
        public static bool LoadDictionaries(string directory, string fileNamePrefix, out string errMsg) {
            string fileName = GetDictionaryFileName(directory, fileNamePrefix);
            return LoadDictionaries(fileName, out errMsg);
        }

        /// <summary>
        /// Download SCADA culture dictionaries with the ability to load default dictionaries in case of error
        /// </summary>
        public static bool LoadDictionaries(string directory, string fileNamePrefix, bool defaultOnError,
            out string errMsg) {
            string fileName = GetDictionaryFileName(directory, fileNamePrefix);

            if (LoadDictionaries(fileName, out errMsg)) {
                return true;
            }

            if (!defaultOnError) return false;

            fileName = Dict.GetFileName(directory, fileNamePrefix, DefaultCultureName);
            LoadDictionaries(fileName, out string errMsg2);
            return false;
        }

        /// <summary>
        /// Download SCADA culture dictionaries
        /// </summary>
        /// <remarks>If the key of the loaded dictionary coincides with the key already loaded, the dictionaries merge.
        /// If the phrase keys match, the new phrase value is written over the old one.</remarks>
        public static bool LoadDictionaries(string fileName, out string errMsg) {
            if (File.Exists(fileName)) {
                try {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(fileName);

                    var dictNodeList = xmlDoc.DocumentElement.SelectNodes("Dictionary");
                    foreach (XmlElement dictElem in dictNodeList) {
                        string dictKey = dictElem.GetAttribute("key");

                        if (!Dictionaries.TryGetValue(dictKey, out var dict)) {
                            dict = new Dict(dictKey);
                            Dictionaries.Add(dictKey, dict);
                        }

                        var phraseNodeList = dictElem.SelectNodes("Phrase");
                        foreach (XmlElement phraseElem in phraseNodeList) {
                            string phraseKey = phraseElem.GetAttribute("key");
                            dict.Phrases[phraseKey] = phraseElem.InnerText;
                        }
                    }

                    errMsg = "";
                    return true;
                } catch (Exception ex) {
                    errMsg = $"Error loading dictionaries from file {fileName}: {ex.Message}";
                    return false;
                }
            }

            errMsg = ("Dictionary file not found: ") + fileName;
            return false;
        }

        /// <summary>
        /// Get a dictionary by key or empty dictionary when it is missing
        /// </summary>
        public static Dict GetDictionary(string key) {
            return Dictionaries.TryGetValue(key, out var dict) ? dict : new Dict(key);
        }


        /// <summary>
        /// Convert date and time to a string according to SCADA culture
        /// </summary>
        public static string ToLocalizedString(this DateTime dateTime) {
            return dateTime.ToString("d", Culture) + " " + dateTime.ToString("T", Culture);
        }

        /// <summary>
        /// Convert date to string according to SCADA culture
        /// </summary>
        public static string ToLocalizedDateString(this DateTime dateTime) {
            return dateTime.ToString("d", Culture);
        }

        /// <summary>
        /// Convert time to string according to SCADA culture
        /// </summary>
        public static string ToLocalizedTimeString(this DateTime dateTime) {
            return dateTime.ToString("T", Culture);
        }
    }
}