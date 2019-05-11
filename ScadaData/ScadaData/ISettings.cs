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
 * Summary  : Interface that represents settings
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

namespace Scada {
    /// <summary>
    /// Interface that represents settings
    /// <para>Interface representing settings</para>
    /// </summary>
    public interface ISettings {
        /// <summary>
        /// Create a new settings object
        /// </summary>
        ISettings Create();

        /// <summary>
        /// Determine whether the specified settings are equal to the current settings
        /// </summary>
        bool Equals(ISettings settings);

        /// <summary>
        /// Load settings from file
        /// </summary>
        bool LoadFromFile(string fileName, out string errMsg);

        /// <summary>
        /// Save settings to file
        /// </summary>
        bool SaveToFile(string fileName, out string errMsg);
    }
}