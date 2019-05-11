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
 * Module   : ScadaData
 * Summary  : The class contains user interface utility methods for the whole system
 *
 * Author   : Mikhail Shiryaev
 * Created  : 2015
 * Modified : 2018
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Scada.UI {
    /// <summary>
    /// The class contains user interface utility methods for the whole system
    /// <para>A class that contains utility methods for working with the user interface for the entire system.</para>
    /// </summary>
    public static class ScadaUiUtils {
        /// <summary>
        /// The log refresh timer interval when the form is hidden, ms.
        /// </summary>
        public const int LogInactiveTimerInterval = 10000;

        /// <summary>
        /// Log refresh interval on local connection, ms.
        /// </summary>
        public const int LogLocalRefreshInterval = 500;

        /// <summary>
        /// Log refresh interval on remote connection, ms.
        /// </summary>
        public const int LogRemoteRefreshInterval = 1000;

        /// <summary>
        /// The threshold number of rows in the table to select the auto-fit column widths
        /// </summary>
        private const int GridAutoResizeBoundary = 100;

        /// <summary>
        /// The size of the displayed data logs, 10 KB
        /// </summary>
        private const long LogViewSize = 10240;

        /// <summary>
        /// The address of the English-language site of the project
        /// </summary>
        private const string WebsiteEn = "http://rapidscada.org";

        /// <summary>
        /// Address of the Russian-language project site
        /// </summary>
        private const string WebsiteRu = "http://rapidscada.ru";

        /// <summary>
        /// Automatic selection of the width of the table columns with the choice of mode depending on the number of rows
        /// </summary>
        public static void AutoResizeColumns(this DataGridView dataGridView) {
            dataGridView.AutoResizeColumns(dataGridView.RowCount <= GridAutoResizeBoundary
                ? DataGridViewAutoSizeColumnsMode.AllCells
                : DataGridViewAutoSizeColumnsMode.DisplayedCells);
        }

        /// <summary>
        /// Get link to online key generator
        /// </summary>
        public static string GetKeyGenUrl(string prod, bool trial, bool? useRussian = null) {
            if (useRussian ?? Localization.UseRussian) {
                return trial
                    ? "http://trial.rapidscada.net/?prod=" + prod + "&lang=ru"
                    : "http://rapidscada.ru/download-all-files/purchase-module/";
            }

            return trial
                ? "http://trial.rapidscada.net/?prod=" + prod
                : "http://rapidscada.org/download-all-files/purchase-module/";
        }

        /// <summary>
        /// Get the selected item from the drop-down list,
        /// using the map of correspondence of indexes of elements of the list and values.
        /// </summary>
        public static object GetSelectedItem(this ComboBox comboBox, Dictionary<int, object> itemIndexToValue) {
            if (itemIndexToValue.TryGetValue(comboBox.SelectedIndex, out var val))
                return val;
            throw new InvalidOperationException("Unable to find combo box selected index in the dictionary.");
        }

        /// <summary>
        /// Load image and hyperlink from the file for the program form
        /// </summary>
        public static bool LoadAboutForm(string exeDir, Form frmAbout, PictureBox pictureBox, Label lblLink,
            out bool imgLoaded, out string linkUrl, out string errMsg) {
            imgLoaded = false;
            linkUrl = Localization.UseRussian ? WebsiteRu : WebsiteEn;
            errMsg = "";

            // load screen saver from file if it exists
            try {
                string imgFileName = exeDir + "About.jpg";
                if (File.Exists(imgFileName)) {
                    var image = System.Drawing.Image.FromFile(imgFileName);
                    pictureBox.Image = image;
                    imgLoaded = true;

                    // checking, adjusting and setting the size of the form and image
                    int width;
                    if (image.Width < 100)
                        width = 100;
                    else if (image.Width > 800)
                        width = 800;
                    else
                        width = image.Width;

                    int height;
                    if (image.Height < 100)
                        height = 100;
                    else if (image.Height > 600)
                        height = 600;
                    else
                        height = image.Height;

                    frmAbout.Width = pictureBox.Width = width;
                    frmAbout.Height = pictureBox.Height = height;
                }
            } catch (OutOfMemoryException) {
                errMsg = string.Format(CommonPhrases.LoadImageError, CommonPhrases.IncorrectFileFormat);
            } catch (Exception ex) {
                errMsg = string.Format(CommonPhrases.LoadImageError, ex.Message);
            }

            if (errMsg == "") {
                // download hyperlink from file if it exists
                StreamReader reader = null;
                try {
                    string linkFileName = exeDir + "About.txt";
                    if (File.Exists(linkFileName)) {
                        reader = new StreamReader(linkFileName, Encoding.Default);
                        linkUrl = reader.ReadLine();

                        if (string.IsNullOrEmpty(linkUrl)) {
                            lblLink.Visible = false;
                        } else {
                            linkUrl = linkUrl.Trim();
                            string pos = reader.ReadLine();

                            if (!string.IsNullOrEmpty(pos)) {
                                string[] parts = pos.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                                if (parts.Length >= 4 && int.TryParse(parts[0], out int x) &&
                                    int.TryParse(parts[1], out int y) &&
                                    int.TryParse(parts[2], out int w) && int.TryParse(parts[3], out int h)) {
                                    // check position and size
                                    if (x < 0)
                                        x = 0;
                                    else if (x >= frmAbout.Width)
                                        x = frmAbout.Width - 1;
                                    if (y < 0)
                                        y = 0;
                                    else if (y >= frmAbout.Height)
                                        y = frmAbout.Height - 1;

                                    if (x + w >= frmAbout.Width)
                                        w = frmAbout.Width - x;
                                    if (w <= 0)
                                        w = 1;
                                    if (y + h >= frmAbout.Height)
                                        h = frmAbout.Height - y;
                                    if (h <= 0)
                                        h = 1;

                                    lblLink.Left = x;
                                    lblLink.Top = y;
                                    lblLink.Width = w;
                                    lblLink.Height = h;
                                    lblLink.Visible = true;
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    linkUrl = "";
                    lblLink.Visible = false;
                    errMsg = string.Format(CommonPhrases.LoadHyperlinkError, ex.Message);
                } finally {
                    reader?.Close();
                }
            } else {
                lblLink.Visible = false;
            }

            return errMsg == "";
        }

        /// <summary>
        /// Recognize the text of the control and transform it into an enumerated type value.
        /// </summary>
        public static T ParseText<T>(this Control control) where T : struct {
            if (Enum.TryParse<T>(control.Text, true, out var val))
                return val;
            throw new FormatException("Unable to parse text of the control.");
        }

        /// <summary>
        /// Upload new data to list item from file
        /// </summary>
        [Obsolete("Use LogBox")]
        public static void ReloadItems(this ListBox listBox, string fileName, bool fullLoad,
            ref DateTime fileAge) {
            Monitor.Enter(listBox);

            try {
                if (File.Exists(fileName)) {
                    var newFileAge = ScadaUtils.GetLastWriteTime(fileName);

                    if (fileAge == newFileAge) return;

                    // loading lines from file
                    List<string> stringList = LoadStrings(fileName, fullLoad);
                    int newLineCnt = stringList.Count;

                    // check to exclude the display of data read at the time of writing the file
                    if (newLineCnt <= 0 && !((DateTime.Now - newFileAge).TotalMilliseconds > 50)) return;

                    fileAge = newFileAge;

                    // list output
                    int oldLineCnt = listBox.Items.Count;
                    int selectedIndex = listBox.SelectedIndex;
                    int topIndex = listBox.TopIndex;

                    listBox.BeginUpdate();

                    for (var i = 0; i < newLineCnt; i++) {
                        if (i < oldLineCnt)
                            listBox.Items[i] = stringList[i];
                        else
                            listBox.Items.Add(stringList[i]);
                    }

                    for (int i = newLineCnt; i < oldLineCnt; i++)
                        listBox.Items.RemoveAt(newLineCnt);

                    // setting the scroll position
                    if (listBox.SelectionMode == SelectionMode.One && newLineCnt > 0) {
                        if (selectedIndex < 0 && !fullLoad)
                            listBox.SelectedIndex = newLineCnt - 1; // scroll to end of list
                        else
                            listBox.TopIndex = topIndex;
                    }

                    listBox.EndUpdate();
                } else {
                    if (listBox.Items.Count == 1) {
                        listBox.Items[0] = CommonPhrases.NoData;
                    } else {
                        listBox.Items.Clear();
                        listBox.Items.Add(CommonPhrases.NoData);
                    }

                    fileAge = DateTime.MinValue;
                }
            } catch (Exception ex) {
                if (listBox.Items.Count == 2) {
                    listBox.Items[0] = CommonPhrases.ErrorWithColon;
                    listBox.Items[1] = ex.Message;
                } else {
                    listBox.Items.Clear();
                    listBox.Items.Add(CommonPhrases.ErrorWithColon);
                    listBox.Items.Add(ex.Message);
                }

                fileAge = DateTime.MinValue;
            } finally {
                Monitor.Exit(listBox);
            }
        }

        /// <summary>
        /// Set the selected item in the drop-down list,
        /// using the map of values and indexes of the elements of the list.
        /// </summary>
        public static void SetSelectedItem(this ComboBox comboBox, object value,
            Dictionary<string, int> valueToItemIndex, int defaultIndex = -1) {
            string valStr = value.ToString();
            if (valueToItemIndex.ContainsKey(valStr))
                comboBox.SelectedIndex = valueToItemIndex[valStr];
            else if (defaultIndex >= 0)
                comboBox.SelectedIndex = defaultIndex;
        }

        /// <summary>
        /// Set the time of a DateTimePicker control
        /// </summary>
        public static void SetTime(this DateTimePicker picker, DateTime time) {
            var date = picker.MinDate;
            picker.Value = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }

        /// <summary>
        /// Set the time of a DateTimePicker control
        /// </summary>
        public static void SetTime(this DateTimePicker picker, TimeSpan timeSpan) {
            var date = picker.MinDate;
            picker.Value = (new DateTime(date.Year, date.Month, date.Day)).Add(timeSpan);
        }

        /// <summary>
        /// Set the value of the control type NumericUpDown within its allowable range
        /// </summary>
        public static void SetValue(this NumericUpDown num, decimal val) {
            if (val < num.Minimum)
                num.Value = num.Minimum;
            else if (val > num.Maximum)
                num.Value = num.Maximum;
            else
                num.Value = val;
        }

        /// <summary>
        /// Show error message
        /// </summary>
        public static void ShowError(string message) {
            MessageBox.Show(message, CommonPhrases.ErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Show informational message
        /// </summary>
        public static void ShowInfo(string message) {
            MessageBox.Show(message, CommonPhrases.InfoCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Show warning
        /// </summary>
        public static void ShowWarning(string message) {
            MessageBox.Show(message, CommonPhrases.WarningCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Load lines from file
        /// </summary>
        /// <remarks>If fullLoad is false, then the amount of data loaded is no more than LogViewSize</remarks>
        private static List<string> LoadStrings(string fileName, bool fullLoad) {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var stringList = new List<string>();
                long fileSize = fileStream.Length;
                long dataSize = fullLoad ? fileSize : LogViewSize;
                long filePos = fileSize - dataSize;
                if (filePos < 0)
                    filePos = 0;

                if (fileStream.Seek(filePos, SeekOrigin.Begin) != filePos) return stringList;

                using (var reader = new StreamReader(fileStream, Encoding.UTF8)) {
                    bool addLine = fileSize <= dataSize;
                    while (!reader.EndOfStream) {
                        string line = reader.ReadLine();
                        if (addLine)
                            stringList.Add(line);
                        else
                            addLine = true;
                    }
                }

                return stringList;
            }
        }
    }
}