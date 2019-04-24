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
 * Module   : ScadaServerCommon
 * Summary  : Interface that defines access to server data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2017
 * Modified : 2017
 */

using Scada.Data.Tables;
using System;

namespace Scada.Server.Modules {
    /// <summary>
    /// Interface that defines access to server data
    /// <para>定义对服务器数据的访问的接口</para>
    /// </summary>
    public interface IServerData {
        /// <summary>
        /// 获取包含给定通道数据的当前切片
        /// </summary>
        /// <remarks>频道号必须按升序排序。</remarks>
        SrezTableLight.Srez GetCurSnapshot(int[] cnlNums);

        /// <summary>
        /// 获取包含给定通道数据的切片
        /// </summary>
        /// <remarks>频道号必须按升序排序。</remarks>
        SrezTableLight.Srez GetSnapshot(DateTime dateTime, SnapshotTypes snapshotType, int[] cnlNums);

        /// <summary>
        /// 处理（写入）新的当前数据
        /// </summary>
        bool ProcCurData(SrezTableLight.Srez snapshot);

        /// <summary>
        /// 编辑（写入）新的存档数据
        /// </summary>
        bool ProcArcData(SrezTableLight.Srez snapshot);

        /// <summary>
        /// 处理（写）新事件
        /// </summary>
        bool ProcEvent(EventTableLight.Event ev);
    }
}