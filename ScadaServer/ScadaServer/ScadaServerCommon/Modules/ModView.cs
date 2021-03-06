﻿/*
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
 * Module   : ScadaServerCommon
 * Summary  : The base class for server module user interface
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2017
 */

using Scada.Client;
using System;

namespace Scada.Server.Modules {
    /// <summary>
    /// The base class for server module user interface
    /// <para>Parent class of server module user interface</para>
    /// </summary>
    public abstract class ModView {
        private AppDirs appDirs; // application directories


        /// <summary>
        /// Constructor
        /// </summary>
        protected ModView() {
            appDirs = new AppDirs();
            ServerComm = null;
            CanShowProps = false;
        }


        /// <summary>
        /// Get module description
        /// </summary>
        public abstract string Descr { get; }

        /// <summary>
        /// Get the module version
        /// </summary>
        /// <remarks>In the future, make this property abstract.</remarks>
        public virtual string Version {
            get { return ""; }
        }

        /// <summary>
        /// Get or set application directories
        /// </summary>
        public AppDirs AppDirs {
            get { return appDirs; }
            set {
                appDirs = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Get or set the object for data exchange with SCADA Server.
        /// </summary>
        public ServerComm ServerComm { get; set; }

        /// <summary>
        /// Get the ability to display module properties
        /// </summary>
        public bool CanShowProps { get; protected set; }


        /// <summary>
        /// Display module properties
        /// </summary>
        public virtual void ShowProps() { }
    }
}