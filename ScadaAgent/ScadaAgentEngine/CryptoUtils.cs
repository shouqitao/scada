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
 * Module   : ScadaAgentEngine
 * Summary  : The class contains utility cryptographic methods
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;

namespace Scada.Agent.Engine {
    /// <summary>
    /// The class contains utility cryptographic methods
    /// <para>Class containing cryptographic support methods</para>
    /// </summary>
    public static class CryptoUtils {
        /// <summary>
        /// Create an initialization vector on the template. sessions
        /// </summary>
        private static byte[] CreateIV(long sessionID) {
            byte[] iv = new byte[ScadaUtils.IVSize];
            byte[] sessBuf = BitConverter.GetBytes(sessionID);
            int sessBufLen = sessBuf.Length;

            for (int i = 0; i < ScadaUtils.IVSize; i++) {
                iv[i] = sessBuf[i % sessBufLen];
            }

            return iv;
        }

        /// <summary>
        /// Encrypt password
        /// </summary>
        public static string EncryptPassword(string password, long sessionID, byte[] secretKey) {
            return ScadaUtils.Encrypt(password, secretKey, CreateIV(sessionID));
        }

        /// <summary>
        /// Decrypt password
        /// </summary>
        public static string DecryptPassword(string encryptedPassword, long sessionID, byte[] secretKey) {
            return ScadaUtils.Decrypt(encryptedPassword, secretKey, CreateIV(sessionID));
        }

        /// <summary>
        /// Decrypt password without causing exceptions in case of error
        /// </summary>
        public static string SafelyDecryptPassword(string encryptedPassword, long sessionID, byte[] secretKey) {
            try {
                return DecryptPassword(encryptedPassword, sessionID, secretKey);
            } catch {
                return "";
            }
        }
    }
}