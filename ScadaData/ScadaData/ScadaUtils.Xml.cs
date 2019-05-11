/*
 * Copyright 2017 Mikhail Shiryaev
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
 * Summary  : The class contains utility methods for the whole system. XML utilities
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2014
 * Modified : 2017
 */

using System;
using System.Globalization;
using System.Xml;

namespace Scada {
    partial class ScadaUtils {
        /// <summary>
        /// Create a FormatException exception for the XML node
        /// </summary>
        private static FormatException NewXmlNodeFormatException(string nodeName) {
            return new FormatException(string.Format(CommonPhrases.IncorrectXmlNodeVal, nodeName));
        }

        /// <summary>
        /// Create a FormatException exception for the XML attribute
        /// </summary>
        private static FormatException NewXmlAttrFormatException(string attrName) {
            return new FormatException(string.Format(CommonPhrases.IncorrectXmlAttrVal, attrName));
        }


        /// <summary>
        /// Convert the value to write to the XML file into a string
        /// </summary>
        public static string XmlValToStr(object value) {
            switch (value) {
                case null:
                    return "";
                case bool _:
                    return value.ToString().ToLowerInvariant();
                case double d:
                    return d.ToString(NumberFormatInfo.InvariantInfo);
                case DateTime time:
                    return time.ToString(DateTimeFormatInfo.InvariantInfo);
                case TimeSpan span:
                    return span.ToString("", DateTimeFormatInfo.InvariantInfo);
                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Convert a string read from an XML document to a real number
        /// </summary>
        public static double XmlParseDouble(string s) {
            return double.Parse(s, NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Convert the string read from the XML document to date and time.
        /// </summary>
        public static DateTime XmlParseDateTime(string s) {
            return DateTime.Parse(s, DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Convert a string read from an XML document to a date.
        /// </summary>
        public static DateTime XmlParseDate(string s) {
            return DateTime.Parse(s, DateTimeFormatInfo.InvariantInfo).Date;
        }

        /// <summary>
        /// Convert a string read from an XML document to a time interval.
        /// </summary>
        public static TimeSpan XmlParseTimeSpan(string s) {
            return TimeSpan.Parse(s, DateTimeFormatInfo.InvariantInfo);
        }

        /// <summary>
        /// Convert a string read from an XML document to an enum value.
        /// </summary>
        public static T XmlParseEnum<T>(string s) where T : struct {
            return (T) Enum.Parse(typeof(T), s, true);
        }


        /// <summary>
        /// Create and add an XML element
        /// </summary>
        public static XmlElement AppendElem(this XmlElement parentXmlElem, string elemName, object innerText = null) {
            var xmlElem = parentXmlElem.OwnerDocument.CreateElement(elemName);
            string val = XmlValToStr(innerText);
            if (!string.IsNullOrEmpty(val))
                xmlElem.InnerText = val;
            return (XmlElement) parentXmlElem.AppendChild(xmlElem);
        }

        /// <summary>
        /// Create and add XML parameter element
        /// </summary>
        public static XmlElement AppendParamElem(this XmlElement parentXmlElem, string paramName, object value,
            string descr = "") {
            var paramElem = parentXmlElem.OwnerDocument.CreateElement("Param");
            paramElem.SetAttribute("name", paramName);
            paramElem.SetAttribute("value", XmlValToStr(value));
            if (!string.IsNullOrEmpty(descr))
                paramElem.SetAttribute("descr", descr);
            return (XmlElement) parentXmlElem.AppendChild(paramElem);
        }

        /// <summary>
        /// Create and add an XML parameter element, automatically choosing a description language
        /// </summary>
        public static XmlElement AppendParamElem(this XmlElement parentXmlElem, string paramName, object value,
            string descrRu, string descrEn) {
            return parentXmlElem.AppendParamElem(paramName, value, Localization.UseRussian ? descrRu : descrEn);
        }

        /// <summary>
        /// Get XML parameter element
        /// </summary>
        public static XmlElement GetParamElem(this XmlElement parentXmlElem, string paramName) {
            var xmlNodes = parentXmlElem.SelectNodes($"Param[@name='{paramName}'][1]");
            return xmlNodes.Count > 0 ? xmlNodes[0] as XmlElement : null;
        }

        /// <summary>
        /// Get the string value of the child XML node
        /// </summary>
        /// <remarks>If the XML node does not exist, an InvalidOperationException is thrown.</remarks>
        public static string GetChildAsString(this XmlNode parentXmlNode, string childNodeName,
            string defaultVal = "") {
            var node = parentXmlNode.SelectSingleNode(childNodeName);
            return node == null ? defaultVal : node.InnerText;
        }

        /// <summary>
        /// Get the logical value of a child XML node
        /// </summary>
        public static bool GetChildAsBool(this XmlNode parentXmlNode, string childNodeName, bool defaultVal = false) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : bool.Parse(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the integer value of a child XML node
        /// </summary>
        public static int GetChildAsInt(this XmlNode parentXmlNode, string childNodeName, int defaultVal = 0) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : int.Parse(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the 64-bit integer value of the child XML node
        /// </summary>
        public static long GetChildAsLong(this XmlNode parentXmlNode, string childNodeName, long defaultVal = 0) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : long.Parse(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the real value of the child XML node
        /// </summary>
        public static double GetChildAsDouble(this XmlNode parentXmlNode, string childNodeName, double defaultVal = 0) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : XmlParseDouble(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the date and time value of the child XML node
        /// </summary>
        public static DateTime GetChildAsDateTime(this XmlNode parentXmlNode, string childNodeName,
            DateTime defaultVal) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : XmlParseDateTime(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the date and time value of the child XML node
        /// </summary>
        public static DateTime GetChildAsDateTime(this XmlNode parentXmlNode, string childNodeName) {
            return parentXmlNode.GetChildAsDateTime(childNodeName, DateTime.MinValue);
        }

        /// <summary>
        /// Get the value of the time interval of a child XML node
        /// </summary>
        public static TimeSpan GetChildAsTimeSpan(this XmlNode parentXmlNode, string childNodeName,
            TimeSpan defaultVal) {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : XmlParseTimeSpan(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }

        /// <summary>
        /// Get the value of the time interval of a child XML node
        /// </summary>
        public static TimeSpan GetChildAsTimeSpan(this XmlNode parentXmlNode, string childNodeName) {
            return parentXmlNode.GetChildAsTimeSpan(childNodeName, TimeSpan.Zero);
        }

        /// <summary>
        /// Get the enum value of a child XML node
        /// </summary>
        public static T GetChildAsEnum<T>(this XmlNode parentXmlNode, string childNodeName,
            T defaultVal = default(T)) where T : struct {
            try {
                var node = parentXmlNode.SelectSingleNode(childNodeName);
                return node == null ? defaultVal : XmlParseEnum<T>(node.InnerText);
            } catch (FormatException) {
                throw NewXmlNodeFormatException(childNodeName);
            }
        }


        /// <summary>
        /// Set XML attribute value
        /// </summary>
        public static void SetAttribute(this XmlElement xmlElem, string attrName, object value) {
            xmlElem.SetAttribute(attrName, XmlValToStr(value));
        }

        /// <summary>
        /// Get the string value of an attribute of an XML element
        /// </summary>
        public static string GetAttrAsString(this XmlElement xmlElem, string attrName, string defaultVal = "") {
            return xmlElem.HasAttribute(attrName) ? xmlElem.GetAttribute(attrName) : defaultVal;
        }

        /// <summary>
        /// Get Boolean attribute value of XML element
        /// </summary>
        public static bool GetAttrAsBool(this XmlElement xmlElem, string attrName, bool defaultVal = false) {
            try {
                return xmlElem.HasAttribute(attrName) ? bool.Parse(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the integer attribute value of an XML element
        /// </summary>
        public static int GetAttrAsInt(this XmlElement xmlElem, string attrName, int defaultVal = 0) {
            try {
                return xmlElem.HasAttribute(attrName) ? int.Parse(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the 64-bit integer value of the XML element attribute
        /// </summary>
        public static long GetAttrAsLong(this XmlElement xmlElem, string attrName, long defaultVal = 0) {
            try {
                return xmlElem.HasAttribute(attrName) ? long.Parse(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the real value of an attribute of an XML element
        /// </summary>
        public static double GetAttrAsDouble(this XmlElement xmlElem, string attrName, double defaultVal = 0) {
            try {
                return xmlElem.HasAttribute(attrName) ? XmlParseDouble(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the date and time value of an XML element attribute
        /// </summary>
        public static DateTime GetAttrAsDateTime(this XmlElement xmlElem, string attrName, DateTime defaultVal) {
            try {
                return xmlElem.HasAttribute(attrName) ? XmlParseDateTime(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the date and time value of an XML element attribute
        /// </summary>
        public static DateTime GetAttrAsDateTime(this XmlElement xmlElem, string attrName) {
            return xmlElem.GetAttrAsDateTime(attrName, DateTime.MinValue);
        }

        /// <summary>
        /// Get the value of the XML element attribute time interval
        /// </summary>
        public static TimeSpan GetAttrAsTimeSpan(this XmlElement xmlElem, string attrName, TimeSpan defaultVal) {
            try {
                return xmlElem.HasAttribute(attrName) ? XmlParseTimeSpan(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }

        /// <summary>
        /// Get the value of the XML element attribute time interval
        /// </summary>
        public static TimeSpan GetAttrAsTimeSpan(this XmlElement xmlElem, string attrName) {
            return xmlElem.GetAttrAsTimeSpan(attrName, TimeSpan.Zero);
        }

        /// <summary>
        /// Get the enumerated value of an XML element attribute
        /// </summary>
        public static T GetAttrAsEnum<T>(this XmlElement xmlElem, string attrName,
            T defaultVal = default(T)) where T : struct {
            try {
                return xmlElem.HasAttribute(attrName) ? XmlParseEnum<T>(xmlElem.GetAttribute(attrName)) : defaultVal;
            } catch (FormatException) {
                throw NewXmlAttrFormatException(attrName);
            }
        }
    }
}