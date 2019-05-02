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
 * Module   : Agent Console Application
 * Summary  : Agent console application designed for Mono .NET framework
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2018
 * Modified : 2018
 */

using Scada.Agent.Engine;
using Scada.Agent.Net;
using System;
using System.Threading;

namespace Scada.Agent.Mono {
    /// <summary>
    /// Agent console application designed for Mono .NET framework
    /// <para>Console agent application designed for the Mono .NET framework</para>
    /// </summary>
    class Program {
        /// <summary>
        /// The main entry point for the application
        /// <para>The main entry point for the application</para>
        /// </summary>
        static void Main(string[] args) {
            // agent start
            Console.WriteLine("Starting Agent...");
            var agentManager = new AgentManager();

            if (agentManager.StartAgent())
                Console.WriteLine("Agent is started successfully");
            else
                Console.WriteLine("Agent is started with errors");

            Console.WriteLine("Press 'x' or create 'agentstop' file to stop Agent");

            // stopping the service when you press 'x' or find the file stop
            FileListener stopFileListener = new FileListener(AppData.GetInstance().AppDirs.ConfigDir + "serverstop");

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.X || stopFileListener.FileFound)) {
                Thread.Sleep(ScadaUtils.ThreadDelay);
            }

            agentManager.StopAgent();
            stopFileListener.DeleteFile();
            stopFileListener.Abort();
            Console.WriteLine("Agent is stopped");
        }
    }
}