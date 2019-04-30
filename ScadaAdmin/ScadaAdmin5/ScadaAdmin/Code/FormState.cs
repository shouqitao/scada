﻿/*
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
 * Module   : Administrator
 * Summary  : State of form controls.
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

namespace Scada.Admin.App.Code {
    /// <summary>
    /// State of form controls.
    /// <para>State of form controls.</para>
    /// </summary>
    public class FormState {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public FormState() {
            SetToDefault();
        }


        /// <summary>
        /// Tests whether the form state is empty.
        /// </summary>
        public bool IsEmpty {
            get { return Width > 0 && Height > 0; }
        }

        /// <summary>
        /// Gets or sets the form horizontal position.
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// Gets or sets the form vertical position.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Gets or sets the form width.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the form height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the form is maximized.
        /// </summary>
        public bool Maximized { get; set; }


        /// <summary>
        /// Sets the default values.
        /// </summary>
        private void SetToDefault() {
            Left = 0;
            Top = 0;
            Width = 0;
            Height = 0;
            Maximized = false;
        }
    }
}