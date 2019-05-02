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
 * Module   : KpHttpNotif
 * Summary  : Parameterized string
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using System.Collections.Generic;

namespace Scada.Comm.Devices.HttpNotif {
    /// <summary>
    /// Parameterized string
    /// <para>Parameterized string</para>
    /// </summary>
    internal class ParamString {
        /// <summary>
        /// String parameter
        /// </summary>
        public class Param {
            /// <inheritdoc />
            /// <summary>
            /// Constructor
            /// </summary>
            public Param()
                : this("") { }

            /// <summary>
            /// Constructor
            /// </summary>
            public Param(string name) {
                Name = name;
                PartIndexes = new List<int>();
            }

            /// <summary>
            /// Get or set the name
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Get parameter indexes among parts of a string
            /// </summary>
            public List<int> PartIndexes { get; protected set; }
        }


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected ParamString() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public ParamString(string srcString) {
            SrcString = srcString;
            Parse();
        }


        /// <summary>
        /// Get source string
        /// </summary>
        public string SrcString { get; protected set; }

        /// <summary>
        /// Get parts of a string
        /// </summary>
        public string[] StringParts { get; protected set; }

        /// <summary>
        /// Get parameter dictionary, key - parameter name
        /// </summary>
        public Dictionary<string, Param> Params { get; protected set; }


        /// <summary>
        /// Perform string parsing
        /// </summary>
        protected void Parse() {
            var stringParts = new List<string>();
            var stringParams = new Dictionary<string, Param>();

            // splitting a string into parts separated by brackets {and}
            if (!string.IsNullOrEmpty(SrcString)) {
                var ind = 0;
                int len = SrcString.Length;

                while (ind < len) {
                    int braceInd1 = SrcString.IndexOf('{', ind);
                    if (braceInd1 < 0) {
                        stringParts.Add(SrcString.Substring(ind));
                        break;
                    } else {
                        int braceInd2 = SrcString.IndexOf('}', braceInd1 + 1);
                        int paramNameLen = braceInd2 - braceInd1 - 1;

                        if (paramNameLen <= 0) {
                            stringParts.Add(SrcString.Substring(ind));
                            break;
                        } else {
                            string paramName = SrcString.Substring(braceInd1 + 1, paramNameLen);
                            Param param;

                            if (!stringParams.TryGetValue(paramName, out param)) {
                                param = new Param(paramName);
                                stringParams.Add(paramName, param);
                            }

                            stringParts.Add(SrcString.Substring(ind, braceInd1 - ind));
                            param.PartIndexes.Add(stringParts.Count);
                            stringParts.Add("");
                            ind = braceInd2 + 1;
                        }
                    }
                }
            }

            StringParts = stringParts.ToArray();
            Params = stringParams;
        }

        /// <summary>
        /// Set parameter value
        /// </summary>
        public void SetParam(string paramName, string paramVal) {
            Param param;
            if (Params.TryGetValue(paramName, out param)) {
                int partsLen = StringParts.Length;

                foreach (int index in param.PartIndexes) {
                    if (0 <= index && index < partsLen)
                        StringParts[index] = paramVal;
                }
            }
        }

        /// <summary>
        /// Get a string representation of the object
        /// </summary>
        public override string ToString() {
            return string.Join("", StringParts);
        }
    }
}