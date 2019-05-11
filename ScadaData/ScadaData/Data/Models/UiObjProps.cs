/*
 * Copyright 2016 Mikhail Shiryaev
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
 * Summary  : User interface object properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System;

namespace Scada.Data.Models {
    /// <summary>
    /// User interface object properties
    /// <para>UI Object Properties</para>
    /// </summary>
    public class UiObjProps {
        /// <summary>
        /// Basic UI Object Types
        /// </summary>
        [Flags]
        public enum BaseUiTypes {
            /// <summary>
            /// View (default)
            /// </summary>
            View,

            /// <summary>
            /// Report
            /// </summary>
            Report,

            /// <summary>
            /// Data window
            /// </summary>
            DataWnd
        }

        /// <summary>
        /// Types of path
        /// </summary>
        public enum PathKinds {
            /// <summary>
            /// Not determined
            /// </summary>
            Undefined,

            /// <summary>
            /// File
            /// </summary>
            File,

            /// <summary>
            /// Link
            /// </summary>
            Url
        }


        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        public UiObjProps()
            : this(0) { }

        /// <summary>
        /// Constructor
        /// </summary>
        public UiObjProps(int viewID) {
            UiObjID = viewID;
            Title = "";
            Path = "";
            TypeCode = "";
            BaseUiType = BaseUiTypes.View;
        }


        /// <summary>
        /// Get or set user interface object identifier
        /// </summary>
        public int UiObjID { get; set; }

        /// <summary>
        /// Get or set header
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Get or set path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Get or set type code
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// Get or set base type
        /// </summary>
        public BaseUiTypes BaseUiType { get; set; }

        /// <summary>
        /// Get the sign that the interface object is empty
        /// </summary>
        public bool IsEmpty {
            get { return string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Path); }
        }

        /// <summary>
        /// Get the look of the way
        /// </summary>
        public PathKinds PathKind {
            get {
                if (string.IsNullOrEmpty(Path))
                    return PathKinds.Undefined;

                return Path.Contains("://") ? PathKinds.Url : PathKinds.File;
            }
        }


        /// <summary>
        /// Extract the path, type code and base type of the interface object from the specified string
        /// </summary>
        public static UiObjProps Parse(string s) {
            s = s ?? "";
            int sepInd = s.IndexOf('@');
            string path = (sepInd >= 0 ? s.Substring(0, sepInd) : s).Trim();
            string typeCode = sepInd >= 0 ? s.Substring(sepInd + 1).Trim() : "";
            var baseUiType = BaseUiTypes.View;

            if (typeCode.EndsWith("Rep", StringComparison.Ordinal)) {
                baseUiType = BaseUiTypes.Report;
            } else if (typeCode.EndsWith("Wnd", StringComparison.Ordinal)) {
                baseUiType = BaseUiTypes.DataWnd;
            } else if (typeCode == "") {
                if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                    typeCode = "WebPageView";
                } else {
                    string ext = System.IO.Path.GetExtension(path);
                    typeCode = ext.TrimStart('.');
                }
            }

            return new UiObjProps() {
                Path = path,
                TypeCode = typeCode,
                BaseUiType = baseUiType
            };
        }
    }
}