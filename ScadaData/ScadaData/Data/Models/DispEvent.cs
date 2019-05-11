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
 * Summary  : Displayed event
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

namespace Scada.Data.Models {
    /// <summary>
    /// Displayed event
    /// <para>Event Displayed</para>
    /// </summary>
    /// <remarks>Properties have short names for JSON transmission.</remarks>
    public class DispEvent {
        /// <summary>
        /// Constructor
        /// </summary>
        public DispEvent() {
            Num = 0;
            Time = "";
            Obj = "";
            KP = "";
            Cnl = "";
            Text = "";
            Ack = "";
            Color = "";
            Sound = false;
        }


        /// <summary>
        /// Get or set the sequence number
        /// </summary>
        public int Num { get; set; }

        /// <summary>
        /// Get or set the formatted date and time
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// Get or set the name of the object
        /// </summary>
        public string Obj { get; set; }

        /// <summary>
        /// Get or set the name of KP
        /// </summary>
        public string KP { get; set; }

        /// <summary>
        /// Get or set the name of the input channel
        /// </summary>
        public string Cnl { get; set; }

        /// <summary>
        /// Get or set event text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Get or set acknowledgment information
        /// </summary>
        public string Ack { get; set; }

        /// <summary>
        /// Get or set color
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Get or set the sound playMark
        /// </summary>
        public bool Sound { get; set; }
    }
}