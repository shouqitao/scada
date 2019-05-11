/*
 * Copyright 2019 Mikhail Shiryaev
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
 * Summary  : The class contains utility methods for the whole system
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2019
 */

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Scada {
    /// <summary>
    /// The class contains utility methods for the whole system.
    /// <para>A class containing helper methods for the entire system.</para>
    /// </summary>
    public static partial class ScadaUtils {
        /// <summary>
        /// Version of this library
        /// </summary>
        internal const string LibVersion = "5.1.2.1";

        /// <summary>
        /// The format of real numbers with a dot separator
        /// </summary>
        private static readonly NumberFormatInfo NfiDot;

        /// <summary>
        /// The format of real numbers with a comma separator
        /// </summary>
        private static readonly NumberFormatInfo NfiComma;

        /// <summary>
        /// Flow delay to save resources, ms
        /// </summary>
        public const int ThreadDelay = 100;

        /// <summary>
        /// Begin Reporting Time Used by Rapid SCADA Applications
        /// </summary>
        /// <remarks>It coincides with the beginning of time in OLE Automation and Delphi</remarks>
        public static readonly DateTime ScadaEpoch = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Determines that the application is running on Windows.
        /// </summary>
        public static readonly bool IsRunningOnWin = IsWindows(Environment.OSVersion);

        /// <summary>
        /// Determines that the application is running on Mono Framework.
        /// </summary>
        public static readonly bool IsRunningOnMono = Type.GetType("Mono.Runtime") != null;


        /// <summary>
        /// Constructor
        /// </summary>
        static ScadaUtils() {
            NfiDot = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
            NfiComma = (NumberFormatInfo) CultureInfo.InvariantCulture.NumberFormat.Clone();
            NfiComma.NumberDecimalSeparator = ",";
        }


        /// <summary>
        /// Remove spaces from the string
        /// </summary>
        private static string RemoveWhiteSpace(string s) {
            var sb = new StringBuilder();

            if (s == null) return sb.ToString();

            foreach (char c in s) {
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check whether the application is running on Windows.
        /// </summary>
        private static bool IsWindows(OperatingSystem os) {
            // since .NET 4.7.1 change to RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            var pid = os.Platform;
            return pid == PlatformID.Win32NT || pid == PlatformID.Win32S ||
                   pid == PlatformID.Win32Windows || pid == PlatformID.WinCE;
        }


        /// <summary>
        /// Add a slash to the directory name, if necessary
        /// </summary>
        public static string NormalDir(string dir) {
            dir = dir == null ? "" : dir.Trim();
            int lastIndex = dir.Length - 1;

            if (lastIndex >= 0 && !(dir[lastIndex] == Path.DirectorySeparatorChar ||
                                    dir[lastIndex] == Path.AltDirectorySeparatorChar)) {
                dir += Path.DirectorySeparatorChar;
            }

            return dir;
        }

        /// <summary>
        /// Adjust directory delimiter
        /// </summary>
        public static string CorrectDirectorySeparator(string path) {
            // Path.AltDirectorySeparatorChar == '/' for Mono on Linux, which is incorrect 
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Encode date and time to real time
        /// </summary>
        /// <remarks>Compatible with the DateTime.ToOADate () method</remarks>
        public static double EncodeDateTime(DateTime dateTime) {
            return (dateTime - ScadaEpoch).TotalDays;
        }

        /// <summary>
        /// Decode a real time value by converting it to DateTime
        /// </summary>
        /// <remarks>Compatible with DateTime.FromOADate () method</remarks>
        public static DateTime DecodeDateTime(double dateTime) {
            return ScadaEpoch.AddDays(dateTime);
        }

        /// <summary>
        /// Combine the given date and time into a single value
        /// </summary>
        public static DateTime CombineDateTime(DateTime date, double time) {
            return date.AddDays(time - Math.Truncate(time));
        }

        /// <summary>
        /// Encode the first 8 characters of the ASCII string to a real number
        /// </summary>
        public static double EncodeAscii(string s) {
            var buf = new byte[8];
            int len = Math.Min(8, s.Length);
            Encoding.ASCII.GetBytes(s, 0, len, buf, 0);
            return BitConverter.ToDouble(buf, 0);
        }

        /// <summary>
        /// Decode a real number by converting it to an ASCII string
        /// </summary>
        public static string DecodeAscii(double val) {
            byte[] buf = BitConverter.GetBytes(val);
            return Encoding.ASCII.GetString(buf).TrimEnd((char) 0);
        }

        /// <summary>
        /// Encode the first 4 characters of the Unicode string to a real number
        /// </summary>
        public static double EncodeUnicode(string s) {
            var buf = new byte[8];
            int len = Math.Min(4, s.Length);
            Encoding.Unicode.GetBytes(s, 0, len, buf, 0);
            return BitConverter.ToDouble(buf, 0);
        }

        /// <summary>
        /// Decode a real number by converting it to a Unicode string
        /// </summary>
        public static string DecodeUnicode(double val) {
            byte[] buf = BitConverter.GetBytes(val);
            return Encoding.Unicode.GetString(buf).TrimEnd((char) 0);
        }

        /// <summary>
        /// Determine whether a given string is a date entry using Localization.Culture
        /// </summary>
        public static bool StrIsDate(string s) {
            return DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out var dateTime)
                ? dateTime.TimeOfDay.TotalMilliseconds == 0
                : false;
        }

        /// <summary>
        /// Convert string to date using Localization.Culture
        /// </summary>
        public static DateTime StrToDate(string s) {
            return DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out var dateTime)
                ? dateTime.Date
                : DateTime.MinValue;
        }

        /// <summary>
        /// Try to convert the string to the date and time using Localization.Culture
        /// </summary>
        public static bool TryParseDateTime(string s, out DateTime result) {
            return DateTime.TryParse(s, Localization.Culture, DateTimeStyles.None, out result);
        }

        /// <summary>
        /// Convert string to real number
        /// </summary>
        /// <remarks>The method works with integer delimiters '.' and ','.
        /// If the conversion is not possible, double.NaN is returned.</remarks>
        public static double StrToDouble(string s) {
            try {
                return ParseDouble(s);
            } catch {
                return double.NaN;
            }
        }

        /// <summary>
        /// Convert string to real number
        /// </summary>
        /// <remarks>The method works with integer delimiters '.' and ','.
        /// If the conversion is not possible, a FormatEx exception is thrown.</remarks>
        public static double StrToDoubleExc(string s) {
            try {
                return ParseDouble(s);
            } catch {
                throw new FormatException(string.Format(CommonPhrases.NotNumber, s));
            }
        }

        /// <summary>
        /// Convert string to real number
        /// </summary>
        /// <remarks>The method works with integer delimiters '.' and ','</remarks>
        public static double ParseDouble(string s) {
            return double.Parse(s, s.Contains(".") ? NfiDot : NfiComma);
        }

        /// <summary>
        /// Try to convert a string to a real number
        /// </summary>
        /// <remarks>The method works with integer delimiters '.' and ','</remarks>
        public static bool TryParseDouble(string s, out double result) {
            return double.TryParse(s, NumberStyles.Float, s.Contains(".") ? NfiDot : NfiComma, out result);
        }

        /// <summary>
        /// Convert byte array to string based on hexadecimal representation.
        /// </summary>
        public static string BytesToHex(byte[] bytes) {
            return BytesToHex(bytes, 0, bytes?.Length ?? 0);
        }

        /// <summary>
        /// Convert the specified range of the byte array to a string based on the hexadecimal representation
        /// </summary>
        public static string BytesToHex(byte[] bytes, int index, int count) {
            var sb = new StringBuilder();
            int last = index + count;
            for (int i = index; i < last; i++)
                sb.Append(bytes[i].ToString("X2"));
            return sb.ToString();
        }

        /// <summary>
        /// Convert a string of hexadecimal numbers into an array of bytes using an existing array.
        /// </summary>
        public static bool HexToBytes(string s, int strIndex, byte[] buf, int bufIndex, int byteCount) {
            int strLen = s?.Length ?? 0;
            var convBytes = 0;

            while (strIndex < strLen && convBytes < byteCount) {
                try {
                    buf[bufIndex] = byte.Parse(s.Substring(strIndex, 2), NumberStyles.AllowHexSpecifier);
                    bufIndex++;
                    convBytes++;
                    strIndex += 2;
                } catch (FormatException) {
                    return false;
                }
            }

            return convBytes > 0;
        }

        /// <summary>
        /// Convert a string of hexadecimal numbers into an array of bytes, creating a new array
        /// </summary>
        public static bool HexToBytes(string s, out byte[] bytes, bool skipWhiteSpace = false) {
            if (skipWhiteSpace)
                s = RemoveWhiteSpace(s);

            int strLen = s?.Length ?? 0;
            int bufLen = strLen / 2;
            bytes = new byte[bufLen];
            return HexToBytes(s, 0, bytes, 0, bufLen);
        }

        /// <summary>
        /// Convert a string of hexadecimal numbers into a byte array.
        /// </summary>
        public static byte[] HexToBytes(string s, bool skipWhiteSpace = false) {
            if (HexToBytes(s, out byte[] bytes, skipWhiteSpace))
                return bytes;

            throw new FormatException(CommonPhrases.NotHexadecimal);
        }

        /// <summary>
        /// Deep (full) object cloning
        /// </summary>
        /// <remarks>All objects to be cloned must have the attribute Serializable</remarks>
        public static object DeepClone(object obj, SerializationBinder binder = null) {
            using (var ms = new MemoryStream()) {
                var bf = new BinaryFormatter();
                if (binder != null)
                    bf.Binder = binder;
                bf.Serialize(ms, obj);

                ms.Position = 0;
                return bf.Deserialize(ms);
            }
        }

        /// <summary>
        /// Deep (full) object cloning
        /// </summary>
        public static T DeepClone<T>(T obj, SerializationBinder binder = null) {
            return (T) DeepClone((object) obj, binder);
        }

        /// <summary>
        /// Adjust the name of the type to work DeepClone
        /// </summary>
        public static void CorrectTypeName(ref string typeName) {
            if (!typeName.Contains("System.Collections.Generic.List")) return;

            // removing build information
            int ind1 = typeName.IndexOf(",", StringComparison.Ordinal);
            int ind2 = typeName.IndexOf("]", StringComparison.Ordinal);
            if (ind1 < ind2)
                typeName = typeName.Remove(ind1, ind2 - ind1);
        }

        /// <summary>
        /// Get the time of the last entry in the file
        /// </summary>
        public static DateTime GetLastWriteTime(string fileName) {
            try {
                return File.Exists(fileName) ? File.GetLastWriteTime(fileName) : DateTime.MinValue;
            } catch {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Identifies a nullable type.
        /// </summary>
        public static bool IsNullable(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Indicates whether the string is correct URL.
        /// </summary>
        public static bool IsValidUrl(string s) {
            return !string.IsNullOrEmpty(s) &&
                   Uri.TryCreate(s, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Determines whether two arrays are equal.
        /// </summary>
        public static bool ArraysEqual<T>(T[] a, T[] b) {
            if (a == b)
                return true;
            if (a == null || b == null)
                return false;

            return a.SequenceEqual(b);
        }
    }
}