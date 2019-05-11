/*
 * Copyright 2015 Mikhail Shiryaev
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
 * Summary  : File creation listener
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2015
 */

using System.IO;
using System.Threading;

namespace Scada {
    /// <summary>
    /// File creation listener
    /// <para>File creation listener</para>
    /// <remarks>
    /// The class is used for receiving stop service command when running on Mono .NET framework
    /// <para>The class is used to get the command to stop the service when running in the Mono .NET framework</para>
    /// </remarks>
    /// </summary>
    public class FileListener {
        private readonly string fileName; // expected file name
        private Thread thread; // file wait thread

        /// <summary>
        /// Expected file found
        /// </summary>
        public volatile bool FileFound;


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        protected FileListener() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public FileListener(string fileName) {
            FileFound = false;
            this.fileName = fileName;
            thread = new Thread(WaitForFile) {Priority = ThreadPriority.BelowNormal};
            thread.Start();
        }


        /// <summary>
        /// Wait for the file to appear
        /// </summary>
        private void WaitForFile() {
            while (!FileFound) {
                if (File.Exists(fileName))
                    FileFound = true;
                else
                    Thread.Sleep(ScadaUtils.ThreadDelay);
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        public void DeleteFile() {
            try {
                File.Delete(fileName);
            } catch {
                // ignored
            }
        }

        /// <summary>
        /// Abort file waiting
        /// </summary>
        public void Abort() {
            if (thread == null) return;

            thread.Abort();
            thread = null;
        }
    }
}