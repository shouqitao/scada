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
 * Module   : SCADA-Server Service
 * Summary  : Mechanism providing calculation of formulas
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2013
 * Modified : 2017
 */

using System;
using System.Collections.Generic;
using System.Text;
using Scada.Data;
using Scada.Data.Tables;

namespace Scada.Server.Engine {
    /// <summary>
    /// Mechanism providing calculation of formulas
    /// <para>A mechanism that provides formula calculations.</para>
    /// </summary>
    public class CalcEngine {
        /// <summary>
        /// The channel number for which the formula is calculated
        /// <para>Channel number for which the formula is calculated </para>
        /// </summary>
        protected int curCnlNum;

        /// <summary>
        /// Input channel data transmitted to the server before the calculation
        /// <para>The input channel data to the server before the calculation</para>
        /// </summary>
        protected SrezTableLight.CnlData initialCnlData;

        /// <summary>
        /// Standard command value transmitted to the server before the calculation
        /// <para>The value of the standard control command transmitted to the server before the calculation</para>
        /// </summary>
        protected double initialCmdVal;

        /// <summary>
        /// Binary command data transmitted to the server before the calculation
        /// <para>The data of the binary control command transmitted to the server before the calculation</para>
        /// </summary>
        protected byte[] initialCmdData;

        /// <summary>
        /// Calculate the input channel formula flag
        /// <para>Sign of calculating the formula of the input channel</para>
        /// </summary>
        protected bool calcInCnl;

        /// <summary>
        /// Method of getting input channel data
        /// <para>Input Channel Acquisition Method</para>
        /// </summary>
        protected Func<int, SrezTableLight.CnlData> getCnlData;

        /// <summary>
        /// Method of setting input channel data
        /// <para>Input Channel Installation Method</para>
        /// </summary>
        protected Action<int, SrezTableLight.CnlData> setCnlData;


        /// <summary>
        /// Initializes a new instance of the class
        /// <para>Constructor</para>
        /// </summary>
        public CalcEngine() {
            curCnlNum = -1;
            initialCnlData = SrezTableLight.CnlData.Empty;
            initialCmdVal = 0.0;
            initialCmdData = null;
            calcInCnl = false;
            getCnlData = null;
        }

        /// <summary>
        /// Initializes a new instance of the class
        /// <para>Constructor</para>
        /// </summary>
        public CalcEngine(
            Func<int, SrezTableLight.CnlData> getCnlData,
            Action<int, SrezTableLight.CnlData> setCnlData)
            : this() {
            this.getCnlData = getCnlData;
            this.setCnlData = setCnlData;
        }


        /// <summary>
        /// Gets input channel value transmitted to the server before the calculation
        /// <para>Get the value of the input channel transmitted to the server before the calculation</para>
        /// </summary>
        public double Cnl {
            get { return initialCnlData.Val; }
        }

        /// <summary>
        /// Gets input channel value transmitted to the server before the calculation
        /// <para>Get the value of the input channel transmitted to the server before the calculation</para>
        /// </summary>
        public double CnlVal {
            get { return initialCnlData.Val; }
        }

        /// <summary>
        /// Gets input channel status transmitted to the server before the calculation
        /// <para>Get the status of the input channel transmitted to the server before the calculation</para>
        /// </summary>
        public int CnlStat {
            get { return initialCnlData.Stat; }
        }

        /// <summary>
        /// Gets command value transmitted to the server before the calculation
        /// <para>Get the value of the control command transmitted to the server before the calculation</para>
        /// </summary>
        public double Cmd {
            get { return initialCmdVal; }
        }

        /// <summary>
        /// Gets standard command value transmitted to the server before the calculation
        /// <para>Get the standard control command transmitted to the server before the calculation</para>
        /// </summary>
        public double CmdVal {
            get { return initialCmdVal; }
        }

        /// <summary>
        /// Gets binary command data transmitted to the server before the calculation
        /// <para>Get the data of the binary control command transmitted to the server before the calculation</para>
        /// </summary>
        public byte[] CmdData {
            get { return initialCmdData; }
        }

        /// <summary>
        /// Gets the channel number for which the formula is calculated
        /// <para>Get the channel number for which the formula is calculated.</para>
        /// </summary>
        public int CnlNum {
            get { return curCnlNum; }
        }

        /// <summary>
        /// Gets the natural logarithmic base, specified by the constant, e
        /// <para>Получить число e</para>
        /// </summary>
        public double E {
            get { return Math.E; }
        }

        /// <summary>
        /// Gets the ratio of the circumference of a circle to its diameter, specified by the constant, π
        /// <para>Получить число π</para>
        /// </summary>
        public double PI {
            get { return Math.PI; }
        }


        /// <summary>
        /// Starts the input channel data calculation
        /// <para>Начать вычисление данных входного канала</para>
        /// </summary>
        protected void BeginCalcCnlData(int cnlNum, SrezTableLight.CnlData initialCnlData) {
            curCnlNum = cnlNum;
            this.initialCnlData = initialCnlData;
            initialCmdVal = 0.0;
            initialCmdData = null;
            calcInCnl = true;
        }

        /// <summary>
        /// Ends the input channel data calculation
        /// <para>Завершить вычисление данных входного канала</para>
        /// </summary>
        protected void EndCalcCnlData() {
            curCnlNum = -1;
            initialCnlData = SrezTableLight.CnlData.Empty;
            calcInCnl = false;
        }

        /// <summary>
        /// Starts the command value or data calculation
        /// <para>Начать вычисление значения или данных команды</para>
        /// </summary>
        protected void BeginCalcCmdData(int ctrlCnlNum, double initialCmdVal, byte[] initialCmdData) {
            curCnlNum = ctrlCnlNum;
            this.initialCnlData = SrezTableLight.CnlData.Empty;
            this.initialCmdVal = initialCmdVal;
            this.initialCmdData = initialCmdData;
        }

        /// <summary>
        /// Ends the command value or data calculation
        /// <para>Завершить вычисление значения или данных команды</para>
        /// </summary>
        protected void EndCalcCmdData() {
            curCnlNum = -1;
            initialCmdVal = 0.0;
            initialCmdData = null;
        }


        /// <summary>
        /// Gets the current value of the formula channel
        /// <para>Получить текущее значение канала формулы</para>
        /// </summary>
        public double Val() {
            return calcInCnl ? Val(curCnlNum) : SrezTableLight.CnlData.Empty.Val;
        }

        /// <summary>
        /// Gets the current value of the channel n
        /// <para>Получить текущее значение канала n</para>
        /// </summary>
        public double Val(int n) {
            return (getCnlData == null ? SrezTableLight.CnlData.Empty : getCnlData(n)).Val;
        }

        /// <summary>
        /// Sets the current value of the channel n
        /// <para>Установить текущее значение канала n</para>
        /// </summary>
        public double SetVal(int n, double val) {
            if (setCnlData == null) {
                return double.NaN;
            } else {
                setCnlData(n, new SrezTableLight.CnlData(val, Stat(n)));
                return val;
            }
        }

        /// <summary>
        /// Gets the current status of the formula channel
        /// <para>Получить текущий статус канала формулы</para>
        /// </summary>
        public int Stat() {
            return Stat(curCnlNum);
        }

        /// <summary>
        /// Gets the current status of the channel n
        /// <para>Получить текущий статус канала n</para>
        /// </summary>
        public int Stat(int n) {
            return (getCnlData == null ? SrezTableLight.CnlData.Empty : getCnlData(n)).Stat;
        }

        /// <summary>
        /// Sets the current status of the channel n
        /// <para>Установить текущий статус канала n</para>
        /// </summary>
        public int SetStat(int n, int stat) {
            if (setCnlData == null) {
                return 0;
            } else {
                setCnlData(n, new SrezTableLight.CnlData(Val(n), stat));
                return stat;
            }
        }

        /// <summary>
        /// Sets the current value and status of the channel n
        /// <para>Установить текущее значение и статус канала n</para>
        /// </summary>
        public double SetData(int n, double val, int stat) {
            if (setCnlData == null) {
                return double.NaN;
            } else {
                setCnlData(n, new SrezTableLight.CnlData(val, stat));
                return val;
            }
        }

        /// <summary>
        /// Calculates the absolute value of a double-precision floating-point number
        /// <para>Вычислить модуль</para>
        /// </summary>
        public double Abs(double x) {
            return Math.Abs(x);
        }

        /// <summary>
        /// Calculates the sine of the specified angle
        /// <para>Вычислить синус</para>
        /// </summary>
        public double Sin(double x) {
            return Math.Sin(x);
        }

        /// <summary>
        /// Calculates the cosine of the specified angle
        /// <para>Вычислить косинус</para>
        /// </summary>
        public double Cos(double x) {
            return Math.Cos(x);
        }

        /// <summary>
        /// Calculates the tangent of the specified angle
        /// <para>Вычислить тангенс</para>
        /// </summary>
        public double Tan(double x) {
            return Math.Tan(x);
        }

        /// <summary>
        /// Calculates e raised to the specified power
        /// <para>Вычислить экспоненту</para>
        /// </summary>
        public double Exp(double x) {
            return Math.Exp(x);
        }

        /// <summary>
        /// Calculates the natural (base e) logarithm of a specified number
        /// <para>Вычислить натуральный логарифм</para>
        /// </summary>
        public double Ln(double x) {
            return Math.Log(x);
        }

        /// <summary>
        /// Calculates the natural (base e) logarithm of a specified number
        /// <para>Вычислить натуральный логарифм</para>
        /// </summary>
        public double Log(double x) {
            return Math.Log(x);
        }

        /// <summary>
        /// Calculates the square of a specified number
        /// <para>Возвести в квадрат</para>
        /// </summary>
        public double Sqr(double x) {
            return x * x;
        }

        /// <summary>
        /// Calculates the square root of a specified number
        /// <para>Вычислить квадратный корень</para>
        /// </summary>
        public double Sqrt(double x) {
            return Math.Sqrt(x);
        }

        #region Custom source code. Пользовательский исходный код

/*TODO*/

        #endregion
    }
}