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
 * Summary  : Session manager
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using System;
using System.Collections.Generic;
using System.Text;
using Utils;

namespace Scada.Agent.Engine {
    /// <summary>
    /// Session manager
    /// <para>Session manager</para>
    /// </summary>
    public class SessionManager {
        /// <summary>
        /// Max. number of sessions
        /// </summary>
        private const int MaxSessionCnt = 100;

        /// <summary>
        /// Max. number of attempts to get a unique id. sessions
        /// </summary>
        private const int MaxGetSessionIDAttempts = 100;

        /// <summary>
        /// The lifetime of the session, if there is no activity
        /// </summary>
        private readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(1);

        private Dictionary<long, Session> sessions; // list of sessions, key - id. sessions
        private ILog log; // application log


        /// <summary>
        /// Constructor restricting the creation of an object without parameters
        /// </summary>
        private SessionManager() { }

        /// <summary>
        /// Constructor
        /// </summary>
        public SessionManager(ILog log) {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            sessions = new Dictionary<long, Session>();
        }


        /// <summary>
        /// Create a session and add to the list of sessions
        /// </summary>
        public Session CreateSession() {
            lock (sessions) {
                long sessionID = 0;
                var sessionOK = false;

                if (sessions.Count < MaxSessionCnt) {
                    sessionID = ScadaUtils.GetRandomLong();
                    var attemptNum = 0;
                    bool duplicated;

                    while (duplicated = sessionID == 0 || sessions.ContainsKey(sessionID) &&
                                        ++attemptNum <= MaxGetSessionIDAttempts) {
                        sessionID = ScadaUtils.GetRandomLong();
                    }

                    sessionOK = !duplicated;
                }

                if (sessionOK) {
                    var session = new Session(sessionID);
                    sessions.Add(sessionID, session);
                    log.WriteAction(string.Format(
                        Localization.UseRussian ? "Создана сессия с ид. {0}" : "Session with ID {0} created",
                        sessionID));
                    return session;
                } else {
                    log.WriteError(Localization.UseRussian ? "Не удалось создать сессию" : "Unable to create session");
                    return null;
                }
            }
        }

        /// <summary>
        /// Get session by id
        /// </summary>
        public Session GetSession(long sessionID) {
            lock (sessions) {
                return sessions.TryGetValue(sessionID, out var session) ? session : null;
            }
        }

        /// <summary>
        /// Delete inactive sessions
        /// </summary>
        public void RemoveInactiveSessions() {
            var utcNowDT = DateTime.UtcNow;
            var keysToRemove = new List<long>();

            lock (sessions) {
                foreach (KeyValuePair<long, Session> pair in sessions) {
                    if (utcNowDT - pair.Value.ActivityDT > SessionLifetime)
                        keysToRemove.Add(pair.Key);
                }

                foreach (long key in keysToRemove) {
                    sessions.Remove(key);
                }
            }
        }

        /// <summary>
        /// Delete all sessions
        /// </summary>
        public void RemoveAllSessions() {
            lock (sessions) {
                sessions.Clear();
            }
        }

        /// <summary>
        /// Get session information
        /// </summary>
        public string GetInfo() {
            lock (sessions) {
                if (sessions.Count > 0) {
                    var sbInfo = new StringBuilder();

                    foreach (var session in sessions.Values) {
                        sbInfo.AppendLine(session.ToString());
                    }

                    return sbInfo.ToString();
                } else {
                    return Localization.UseRussian ? "Нет" : "No";
                }
            }
        }
    }
}