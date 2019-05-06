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
 * Module   : ScadaServerCommon
 * Summary  : Factory for creating module instances
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.IO;
using System.Reflection;

namespace Scada.Server.Modules {
    /// <summary>
    /// Factory for creating module instances.
    /// <para>Factory for creating instances of modules.</para>
    /// </summary>
    public static class ModFactory {
        /// <summary>
        /// Gets the type of the module user interface.
        /// </summary>
        public static Type GetModViewType(string dllPath) {
            try {
                var asm = Assembly.LoadFile(dllPath);
                string typeFullName = $"Scada.Server.Modules.{Path.GetFileNameWithoutExtension(dllPath)}View";
                return asm.GetType(typeFullName, true);
            } catch (Exception ex) {
                throw new ScadaException(string.Format(ModPhrases.GetViewTypeError,
                    Path.GetFileName(dllPath), ex.Message), ex);
            }
        }

        /// <summary>
        /// Gets the instance of the module user interface.
        /// </summary>
        public static ModView GetModView(string dllPath) {
            var modViewType = GetModViewType(dllPath);
            return GetKPView(modViewType);
        }

        /// <summary>
        /// Gets the instance of the module user interface of the specified type.
        /// </summary>
        public static ModView GetKPView(Type modViewType) {
            if (modViewType == null)
                throw new ArgumentNullException(nameof(modViewType));

            try {
                return (ModView) Activator.CreateInstance(modViewType);
            } catch (Exception ex) {
                throw new ScadaException(string.Format(ModPhrases.CreateViewError,
                    modViewType.Name, ex.Message), ex);
            }
        }
    }
}