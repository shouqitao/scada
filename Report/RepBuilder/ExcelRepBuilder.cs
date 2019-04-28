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
 * Module   : Report Builder
 * Summary  : The base class for building reports in SpreadsheetML (Microsoft Excel 2003) format
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2016
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace Utils.Report {
    /// <summary>
    /// The base class for building reports in SpreadsheetML (Microsoft Excel 2003) format
    /// <para>The base class for building reports in SpreadsheetML (Microsoft Excel 2003) format</para>
    /// </summary>
    public abstract class ExcelRepBuilder : RepBuilder {
        /// <summary>
        /// SpreadsheetML中使用的命名空间
        /// </summary>
        protected static class XmlNamespaces {
            /// <summary>
            /// Пространстро имён xmlns
            /// </summary>
            public const string noprefix = "urn:schemas-microsoft-com:office:spreadsheet";

            /// <summary>
            /// Пространстро имён xmlns:o
            /// </summary>
            public const string o = "urn:schemas-microsoft-com:office:office";

            /// <summary>
            /// Пространстро имён xmlns:x
            /// </summary>
            public const string x = "urn:schemas-microsoft-com:office:excel";

            /// <summary>
            /// Пространстро имён xmlns:ss
            /// </summary>
            public const string ss = "urn:schemas-microsoft-com:office:spreadsheet";

            /// <summary>
            /// Пространстро имён xmlns:html
            /// </summary>
            public const string html = "http://www.w3.org/TR/REC-html40";
        }

        /// <summary>
        /// Книга Excel
        /// </summary>
        protected class Workbook {
            /// <summary>
            /// Ссылка на XML-узел, соответствующий книге Excel
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// Ссылка на XML-узел, содержащий стили книги Excel
            /// </summary>
            protected XmlNode stylesNode;

            /// <summary>
            /// Список стилей книги Excel с возможностью доступа по ID стиля
            /// </summary>
            protected SortedList<string, Style> styles;

            /// <summary>
            /// Список листов книги Excel
            /// </summary>
            protected List<Worksheet> worksheets;


            /// <summary>
            /// Конструктор
            /// </summary>
            protected Workbook() { }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="xmlNode">Ссылка на XML-узел, соответствующий книге Excel</param>
            public Workbook(XmlNode xmlNode) {
                node = xmlNode;
                styles = new SortedList<string, Style>();
                worksheets = new List<Worksheet>();
            }


            /// <summary>
            /// Получить ссылку на XML-узел, соответствующий книге Excel
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// Получить или установить ссылку на XML-узел, содержащий стили книги Excel
            /// </summary>
            public XmlNode StylesNode {
                get { return stylesNode; }
                set { stylesNode = value; }
            }

            /// <summary>
            /// Получить список стилей книги Excel с возможностью доступа по ID стиля
            /// </summary>
            public SortedList<string, Style> Styles {
                get { return styles; }
            }

            /// <summary>
            /// Получить список листов книги Excel
            /// </summary>
            public List<Worksheet> Worksheets {
                get { return worksheets; }
            }


            /// <summary>
            /// Добавить стиль в конец списка стилей книги Excel и модифицировать дерево XML-документа
            /// </summary>
            /// <param name="style">Добавляемый стиль</param>
            public void AppendStyle(Style style) {
                styles.Add(style.ID, style);
                stylesNode.AppendChild(style.Node);
            }

            /// <summary>
            /// Удалить стиль из списка стилей книги Excel и модифицировать дерево XML-документа
            /// </summary>
            /// <param name="listIndex">Индекс удаляемого стиля в списке</param>
            public void RemoveStyle(int listIndex) {
                var style = styles.Values[listIndex];
                stylesNode.RemoveChild(style.Node);
                styles.RemoveAt(listIndex);
            }

            /// <summary>
            /// Найти лист в списке листов Excel по имени без учёта регистра
            /// </summary>
            /// <param name="worksheetName">Имя листа Excel</param>
            /// <returns>Лист Excel или null, если он не найден</returns>
            public Worksheet FindWorksheet(string worksheetName) {
                foreach (var worksheet in worksheets)
                    if (worksheet.Name.Equals(worksheetName, StringComparison.OrdinalIgnoreCase))
                        return worksheet;

                return null;
            }

            /// <summary>
            /// Добавить лист в конец списка листов Excel и модифицировать дерево XML-документа
            /// </summary>
            /// <param name="worksheet">Добавляемый лист</param>
            public void AppendWorksheet(Worksheet worksheet) {
                worksheets.Add(worksheet);
                node.AppendChild(worksheet.Node);
            }

            /// <summary>
            /// Вставить лист в список листов Excel и модифицировать дерево XML-документа
            /// </summary>
            /// <param name="listIndex">Индекс вставляемого листа в списке</param>
            /// <param name="worksheet">Вставляемый лист</param>
            public void InsertWorksheet(int listIndex, Worksheet worksheet) {
                worksheets.Insert(listIndex, worksheet);

                if (worksheets.Count == 1)
                    node.AppendChild(worksheet.Node);
                else if (listIndex == 0)
                    node.PrependChild(worksheet.Node);
                else
                    node.InsertAfter(worksheet.Node, worksheets[listIndex - 1].Node);
            }

            /// <summary>
            /// 从Excel工作表列表中删除工作表并修改XML文档的树
            /// </summary>
            /// <param name="listIndex">要在列表中删除的工作表的索引</param>
            public void RemoveWorksheet(int listIndex) {
                var worksheet = worksheets[listIndex];
                node.RemoveChild(worksheet.Node);
                worksheets.RemoveAt(listIndex);
            }


            /// <summary>
            /// 设置对象的颜色，必要时创建新样式。
            /// </summary>
            /// <param name="targetNode">对要设置颜色的对象的XML节点的引用。</param>
            /// <param name="color">可设定的颜色</param>
            public void SetColor(XmlNode targetNode, string color) {
                var xmlDoc = targetNode.OwnerDocument;
                string namespaceURI = targetNode.NamespaceURI;

                var styleAttr = targetNode.Attributes["ss:StyleID"];
                if (styleAttr == null) {
                    styleAttr = xmlDoc.CreateAttribute("ss:StyleID");
                    targetNode.Attributes.Append(styleAttr);
                }

                string oldStyleID = styleAttr.Value;
                string newStyleID = oldStyleID + "_" + color;

                if (styles.ContainsKey(newStyleID)) {
                    // 使用指定的颜色设置以前创建的样式
                    styleAttr.Value = newStyleID;
                } else {
                    // 创造一种新的风格
                    Style newStyle;
                    if (styleAttr == null) {
                        var newStyleNode = xmlDoc.CreateNode(XmlNodeType.Element, "Style", namespaceURI);
                        newStyleNode.Attributes.Append(xmlDoc.CreateAttribute("ss", "ID", namespaceURI));
                        newStyle = new Style(newStyleNode);
                    } else
                        newStyle = styles[oldStyleID].Clone();

                    newStyle.ID = newStyleID;

                    // 设置创建样式的颜色
                    var interNode = newStyle.Node.FirstChild;
                    while (interNode != null && interNode.Name != "Interior")
                        interNode = interNode.NextSibling;

                    if (interNode == null) {
                        interNode = xmlDoc.CreateNode(XmlNodeType.Element, "Interior", namespaceURI);
                        newStyle.Node.AppendChild(interNode);
                    } else {
                        interNode.Attributes.RemoveNamedItem("ss:Color");
                        interNode.Attributes.RemoveNamedItem("ss:Pattern");
                    }

                    var xmlAttr = xmlDoc.CreateAttribute("ss", "Color", namespaceURI);
                    xmlAttr.Value = color;
                    interNode.Attributes.Append(xmlAttr);
                    xmlAttr = xmlDoc.CreateAttribute("ss", "Pattern", namespaceURI);
                    xmlAttr.Value = "Solid";
                    interNode.Attributes.Append(xmlAttr);

                    // 为对象设置新样式并向书籍添加样式
                    styleAttr.Value = newStyleID;
                    styles.Add(newStyleID, newStyle);
                    stylesNode.AppendChild(newStyle.Node);
                }
            }
        }

        /// <summary>
        /// Стиль книги Excel
        /// </summary>
        protected class Style {
            /// <summary>
            /// Ссылка на XML-узел, соответствующий стилю книги Excel
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// Идентификатор стиля
            /// </summary>
            protected string id;


            /// <summary>
            /// Конструктор
            /// </summary>
            protected Style() { }

            /// <summary>
            /// Конструктор
            /// </summary>
            /// <param name="xmlNode">Ссылка на XML-узел, соответствующий стилю книги Excel</param>
            public Style(XmlNode xmlNode) {
                node = xmlNode;
                id = xmlNode.Attributes["ss:ID"].Value;
            }


            /// <summary>
            /// Получить ссылку на XML-узел, соответствующий стилю книги Excel
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// Получить или установить идентификатор стиля, при установке модифицируется дерево XML-документа
            /// </summary>
            public string ID {
                get { return id; }
                set {
                    id = value;
                    node.Attributes["ss:ID"].Value = id;
                }
            }


            /// <summary>
            /// Клонировать стиль
            /// </summary>
            /// <returns>Копия стиля</returns>
            public Style Clone() {
                return new Style(node.Clone());
            }
        }

        /// <summary>
        /// Excel工作簿
        /// </summary>
        protected class Worksheet {
            /// <summary>
            /// 链接到与Excel工作簿对应的XML节点
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// 工作表名称
            /// </summary>
            protected string name;

            /// <summary>
            /// 表格内容
            /// </summary>
            protected Table table;

            /// <summary>
            /// 本表的父母书
            /// </summary>
            protected Workbook parentWorkbook;


            /// <summary>
            /// 构造函数
            /// </summary>
            protected Worksheet() { }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="xmlNode">链接到与Excel工作簿对应的XML节点</param>
            public Worksheet(XmlNode xmlNode) {
                node = xmlNode;
                name = xmlNode.Attributes["ss:Name"].Value;
                table = null;
            }


            /// <summary>
            /// 获取指向与Excel工作簿对应的XML节点的链接。
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// 获取或设置工作表名称，在安装过程中，将修改XML文档的树
            /// </summary>
            public string Name {
                get { return name; }
                set {
                    name = value;
                    node.Attributes["ss:Name"].Value = name;
                }
            }

            /// <summary>
            /// 获取或设置包含工作表内容的表格
            /// </summary>
            public Table Table {
                get { return table; }
                set {
                    table = value;
                    table.ParentWorksheet = this;
                }
            }

            /// <summary>
            /// 获取或设置此工作表的父级。
            /// </summary>
            public Workbook ParentWorkbook {
                get { return parentWorkbook; }
                set { parentWorkbook = value; }
            }


            /// <summary>
            /// 设置水平滚动区域分隔符
            /// </summary>
            public void SplitHorizontal(int rowIndex) {
                var xmlDoc = node.OwnerDocument;
                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("report", XmlNamespaces.x);

                var optionsNode = node.SelectSingleNode("report:WorksheetOptions", nsmgr);

                if (optionsNode != null) {
                    string rowIndexStr = rowIndex.ToString();

                    var splitNode = optionsNode.SelectSingleNode("report:SplitHorizontal", nsmgr);
                    if (splitNode == null) {
                        splitNode = xmlDoc.CreateElement("SplitHorizontal");
                        optionsNode.AppendChild(splitNode);
                    }

                    splitNode.InnerText = rowIndexStr;

                    var paneNode = optionsNode.SelectSingleNode("report:TopRowBottomPane", nsmgr);
                    if (paneNode == null) {
                        paneNode = xmlDoc.CreateElement("TopRowBottomPane");
                        optionsNode.AppendChild(paneNode);
                    }

                    paneNode.InnerText = rowIndexStr;
                }
            }

            /// <summary>
            /// Clone sheet
            /// </summary>
            /// <returns>Copies sheets</returns>
            public Worksheet Clone() {
                var nodeClone = node.CloneNode(false);
                var worksheetClone = new Worksheet(nodeClone);

                var tableClone = table.Clone();
                worksheetClone.table = tableClone;
                nodeClone.AppendChild(tableClone.Node);

                foreach (XmlNode childNode in node.ChildNodes)
                    if (childNode.Name != "Table")
                        nodeClone.AppendChild(childNode.Clone());

                return worksheetClone;
            }
        }

        /// <summary>
        /// Excel spreadsheet
        /// </summary>
        protected class Table {
            /// <summary>
            /// Reference to the XML node corresponding to the Excel worksheet
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// List of table columns
            /// </summary>
            protected List<Column> columns;

            /// <summary>
            /// List of table rows
            /// </summary>
            protected List<Row> rows;

            /// <summary>
            /// Parent sheet of this table
            /// </summary>
            protected Worksheet parentWorksheet;


            /// <summary>
            /// Constructor
            /// </summary>
            protected Table() { }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="xmlNode">Reference to the XML node corresponding to the Excel worksheet</param>
            public Table(XmlNode xmlNode) {
                node = xmlNode;
                columns = new List<Column>();
                rows = new List<Row>();
                parentWorksheet = null;
            }


            /// <summary>
            /// Get the link to the XML node corresponding to the Excel worksheet
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// Get a list of table columns
            /// </summary>
            public List<Column> Columns {
                get { return columns; }
            }

            /// <summary>
            /// Get a list of table rows
            /// </summary>
            public List<Row> Rows {
                get { return rows; }
            }

            /// <summary>
            /// Get or set the parent sheet of this table
            /// </summary>
            public Worksheet ParentWorksheet {
                get { return parentWorksheet; }
                set { parentWorksheet = value; }
            }


            /// <summary>
            /// 删除正确显示Excel工作簿所需的表XML节点的属性
            /// </summary>
            public void RemoveTableNodeAttrs() {
                node.Attributes.RemoveNamedItem("ss:ExpandedColumnCount");
                node.Attributes.RemoveNamedItem("ss:ExpandedRowCount");
            }

            /// <summary>
            /// 按索引查找表中的列
            /// </summary>
            /// <param name="columnIndex">所需列的索引，从1开始</param>
            /// <returns>与搜索条件匹配的列，如果未找到列，则为null</returns>
            public Column FindColumn(int columnIndex) {
                var index = 0;
                foreach (var column in columns) {
                    index = column.Index > 0 ? column.Index : index + 1;
                    int endIndex = index + column.Span;

                    if (index <= columnIndex && columnIndex <= endIndex)
                        return column;

                    index = endIndex;
                }

                return null;
            }

            /// <summary>
            /// Add a column to the end of the table column list and modify the tree of the XML document
            /// </summary>
            public void AppendColumn(Column column) {
                if (columns.Count > 0)
                    node.InsertAfter(column.Node, columns[columns.Count - 1].Node);
                else
                    node.PrependChild(column.Node);

                column.ParentTable = this;
                columns.Add(column);
            }

            /// <summary>
            /// Insert a column into the list of table columns and modify the tree of the XML document
            /// </summary>
            public void InsertColumn(int listIndex, Column column) {
                if (columns.Count == 0 || listIndex == 0)
                    node.PrependChild(column.Node);
                else
                    node.InsertAfter(column.Node, columns[listIndex - 1].Node);

                column.ParentTable = this;
                columns.Insert(listIndex, column);
            }

            /// <summary>
            /// Remove a column from the list of table columns and modify the tree of the XML document
            /// </summary>
            public void RemoveColumn(int listIndex) {
                var column = columns[listIndex];
                column.ParentTable = null;
                node.RemoveChild(column.Node);
                columns.RemoveAt(listIndex);
            }

            /// <summary>
            /// Remove all columns from the list of table columns and modify the tree of the XML document
            /// </summary>
            public void RemoveAllColumns() {
                while (columns.Count > 0)
                    RemoveColumn(0);
            }

            /// <summary>
            /// Add a row to the end of the table row list and modify the tree of the XML document
            /// </summary>
            public void AppendRow(Row row) {
                node.AppendChild(row.Node);
                row.ParentTable = this;
                rows.Add(row);
            }

            /// <summary>
            /// Insert a row into the list of rows of the table and modify the tree of the XML document
            /// </summary>
            public void InsertRow(int listIndex, Row row) {
                if (rows.Count == 0)
                    node.AppendChild(row.Node);
                else if (listIndex == 0)
                    node.InsertBefore(row.Node, rows[0].Node);
                else
                    node.InsertAfter(row.Node, rows[listIndex - 1].Node);

                row.ParentTable = this;
                rows.Insert(listIndex, row);
            }

            /// <summary>
            /// Remove a row from the list of rows of the table and modify the tree of the XML document
            /// </summary>
            public void RemoveRow(int listIndex) {
                var row = rows[listIndex];
                row.ParentTable = null;
                node.RemoveChild(row.Node);
                rows.RemoveAt(listIndex);
            }

            /// <summary>
            /// Remove all rows from the table row list and modify the tree of the XML document
            /// </summary>
            public void RemoveAllRows() {
                while (rows.Count > 0)
                    RemoveRow(0);
            }

            /// <summary>
            /// Clone table
            /// </summary>
            /// <returns>Copy table</returns>
            public Table Clone() {
                var nodeClone = node.CloneNode(false);
                var tableClone = new Table(nodeClone) {parentWorksheet = parentWorksheet};

                foreach (var column in columns) {
                    var columnClone = column.Clone();
                    tableClone.columns.Add(columnClone);
                    nodeClone.AppendChild(columnClone.Node);
                }

                foreach (var row in rows) {
                    var rowClone = row.Clone();
                    tableClone.rows.Add(rowClone);
                    nodeClone.AppendChild(rowClone.Node);
                }

                return tableClone;
            }
        }

        /// <summary>
        /// Excel spreadsheet column
        /// </summary>
        protected class Column {
            /// <summary>
            /// Link to XML node corresponding to an Excel table column
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// Parent table of this column
            /// </summary>
            protected Table parentTable;

            /// <summary>
            /// Column index, 0 - undefined
            /// </summary>
            protected int index;


            /// <summary>
            /// Constructor
            /// </summary>
            protected Column() { }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="xmlNode">Link to XML node corresponding to an Excel table column</param>
            public Column(XmlNode xmlNode) {
                node = xmlNode;
                parentTable = null;

                var attr = node.Attributes["ss:Index"];
                index = attr == null ? 0 : int.Parse(attr.Value);
            }


            /// <summary>
            /// Get the link to the XML node corresponding to the Excel table column
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// Get or set the parent table of this column
            /// </summary>
            public Table ParentTable {
                get { return parentTable; }
                set { parentTable = value; }
            }

            /// <summary>
            /// Get or set the column index (0 is undefined), the tree of the XML document is modified during the installation
            /// </summary>
            public int Index {
                get { return index; }
                set {
                    index = value;
                    SetAttribute(node, "Index", XmlNamespaces.ss, index <= 0 ? null : index.ToString(), true);
                }
            }

            /// <summary>
            /// Get or set width
            /// </summary>
            public double Width {
                get {
                    string widthStr = GetAttribute(node, "ss:Width");
                    double width;
                    return double.TryParse(widthStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out width)
                        ? width
                        : 0;
                }
                set { SetColumnWidth(node, value); }
            }

            /// <summary>
            /// Get or set the number of columns to be combined on the right.
            /// </summary>
            public int Span {
                get {
                    string valStr = GetAttribute(node, "ss:Span");
                    return valStr == "" ? 0 : int.Parse(valStr);
                }
                set { SetAttribute(node, "Span", XmlNamespaces.ss, value < 1 ? "" : value.ToString(), true); }
            }


            /// <summary>
            /// Clone column
            /// </summary>
            /// <returns>Column copy</returns>
            public Column Clone() {
                var columnClone = new Column(node.Clone()) {parentTable = parentTable};
                return columnClone;
            }

            /// <summary>
            /// Set column width
            /// </summary>
            public static void SetColumnWidth(XmlNode columnNode, double width) {
                SetAttribute(columnNode, "AutoFitWidth", XmlNamespaces.ss, "0");
                SetAttribute(columnNode, "Width", XmlNamespaces.ss,
                    width > 0 ? width.ToString(NumberFormatInfo.InvariantInfo) : "", true);
            }
        }

        /// <summary>
        /// Excel spreadsheet row
        /// </summary>
        protected class Row {
            /// <summary>
            /// Link to XML node corresponding to Excel row
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// List of row cells
            /// </summary>
            protected List<Cell> cells;

            /// <summary>
            /// Parent table of this row
            /// </summary>
            protected Table parentTable;


            /// <summary>
            /// Constructor
            /// </summary>
            protected Row() { }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="xmlNode">Link to XML node corresponding to Excel row</param>
            public Row(XmlNode xmlNode) {
                node = xmlNode;
                cells = new List<Cell>();
                parentTable = null;
            }


            /// <summary>
            /// Get the link to the XML node corresponding to the row in the Excel table
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// Get list of row cells
            /// </summary>
            public List<Cell> Cells {
                get { return cells; }
            }

            /// <summary>
            /// Get or set the parent table of this row
            /// </summary>
            public Table ParentTable {
                get { return parentTable; }
                set { parentTable = value; }
            }

            /// <summary>
            /// Get or set height
            /// </summary>
            public double Height {
                get {
                    string heightStr = GetAttribute(node, "ss:Height");
                    return double.TryParse(heightStr, NumberStyles.Any, NumberFormatInfo.InvariantInfo,
                        out double height)
                        ? height
                        : 0;
                }
                set { SetRowHeight(node, value); }
            }


            /// <summary>
            /// Clone string
            /// </summary>
            /// <returns>Copy string</returns>
            public Row Clone() {
                var nodeClone = node.CloneNode(false);
                var rowClone = new Row(nodeClone) {parentTable = parentTable};

                foreach (var cell in cells) {
                    var cellClone = cell.Clone();
                    rowClone.cells.Add(cellClone);
                    nodeClone.AppendChild(cellClone.Node);
                }

                return rowClone;
            }

            /// <summary>
            /// Find a cell in a row by index
            /// </summary>
            /// <param name="cellIndex">The index of the desired cell, starting with 1</param>
            /// <returns>A cell that meets the search criteria, or null if the cell is not found.</returns>
            public Cell FindCell(int cellIndex) {
                var index = 0;
                foreach (var cell in cells) {
                    index = cell.Index > 0 ? cell.Index : index + 1;
                    int endIndex = index + cell.MergeAcross;

                    if (index <= cellIndex && cellIndex <= endIndex)
                        return cell;

                    index = endIndex;
                }

                return null;
            }

            /// <summary>
            /// Add a cell to the end of the row cell list and modify the tree of the XML document
            /// </summary>
            public void AppendCell(Cell cell) {
                cells.Add(cell);
                cell.ParentRow = this;
                node.AppendChild(cell.Node);
            }

            /// <summary>
            /// Insert a cell into the list of cells in the row and modify the tree of the XML document
            /// </summary>
            public void InsertCell(int listIndex, Cell cell) {
                cells.Insert(listIndex, cell);
                cell.ParentRow = this;

                if (cells.Count == 1)
                    node.AppendChild(cell.Node);
                else if (listIndex == 0)
                    node.PrependChild(cell.Node);
                else
                    node.InsertAfter(cell.Node, cells[listIndex - 1].Node);
            }

            /// <summary>
            /// Remove a cell from the list of cells in the row and modify the tree of the XML document
            /// </summary>
            public void RemoveCell(int listIndex) {
                var cell = cells[listIndex];
                cell.ParentRow = null;
                node.RemoveChild(cell.Node);
                cells.RemoveAt(listIndex);
            }


            /// <summary>
            /// Set line height
            /// </summary>
            public static void SetRowHeight(XmlNode rowNode, double height) {
                SetAttribute(rowNode, "AutoFitHeight", XmlNamespaces.ss, "0");
                SetAttribute(rowNode, "Height", XmlNamespaces.ss,
                    height > 0 ? height.ToString(NumberFormatInfo.InvariantInfo) : "", true);
            }
        }

        /// <summary>
        /// Excel Row Cell
        /// </summary>
        protected class Cell {
            /// <summary>
            /// Types of cell data
            /// </summary>
            public static class DataTypes {
                /// <summary>
                /// String type
                /// </summary>
                public const string String = "String";

                /// <summary>
                /// Numeric type
                /// </summary>
                public const string Number = "Number";
            }

            /// <summary>
            /// 引用与Excel表行的单元格对应的XML节点
            /// </summary>
            protected XmlNode node;

            /// <summary>
            /// 链接到与单元格数据对应的XML节点
            /// </summary>
            protected XmlNode dataNode;

            /// <summary>
            /// 此单元格的父行
            /// </summary>
            protected Row parentRow;

            /// <summary>
            /// 单元格索引，0  - 未定义
            /// </summary>
            protected int index;


            /// <summary>
            /// Constructor
            /// </summary>
            protected Cell() { }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="xmlNode">引用与Excel表行的单元格对应的XML节点</param>
            public Cell(XmlNode xmlNode) {
                node = xmlNode;
                dataNode = null;
                parentRow = null;

                var attr = node.Attributes["ss:Index"];
                index = attr == null ? 0 : int.Parse(attr.Value);
            }


            /// <summary>
            /// 获取与Excel表行的单元格对应的XML节点的链接
            /// </summary>
            public XmlNode Node {
                get { return node; }
            }

            /// <summary>
            /// 获取或设置与单元格数据对应的XML节点的链接
            /// </summary>
            public XmlNode DataNode {
                get { return dataNode; }
                set { dataNode = value; }
            }

            /// <summary>
            /// 获取或设置此单元格的父行。
            /// </summary>
            public Row ParentRow {
                get { return parentRow; }
                set { parentRow = value; }
            }

            /// <summary>
            /// 获取或设置单元格索引（0未定义），在安装期间修改XML文档的树
            /// </summary>
            public int Index {
                get { return index; }
                set {
                    index = value;
                    SetAttribute(node, "Index", XmlNamespaces.ss, index <= 0 ? null : index.ToString(), true);
                }
            }

            /// <summary>
            /// 获取或设置单元格的数据类型（格式）
            /// </summary>
            public string DataType {
                get { return GetAttribute(dataNode, "ss:Type"); }
                set {
                    SetAttribute(dataNode, "Type", XmlNamespaces.ss,
                        string.IsNullOrEmpty(value) ? DataTypes.String : value);
                }
            }

            /// <summary>
            /// 获取或设置文本
            /// </summary>
            public string Text {
                get { return dataNode == null ? "" : dataNode.InnerText; }
                set {
                    if (dataNode != null)
                        dataNode.InnerText = value;
                }
            }

            /// <summary>
            /// 获取或设置公式
            /// </summary>
            public string Formula {
                get { return GetAttribute(node, "ss:Formula"); }
                set { SetAttribute(node, "Formula", XmlNamespaces.ss, value, true); }
            }

            /// <summary>
            /// 获取或设置ID。风格
            /// </summary>
            public string StyleID {
                get { return GetAttribute(node, "ss:StyleID"); }
                set { SetAttribute(node, "StyleID", XmlNamespaces.ss, value, true); }
            }

            /// <summary>
            /// 获取或设置右侧要合并的单元格数。
            /// </summary>
            public int MergeAcross {
                get {
                    string valStr = GetAttribute(node, "ss:MergeAcross");
                    return valStr == "" ? 0 : int.Parse(valStr);
                }
                set { SetAttribute(node, "MergeAcross", XmlNamespaces.ss, value < 1 ? "" : value.ToString(), true); }
            }

            /// <summary>
            /// 获取或设置要合并的单元格数
            /// </summary>
            public int MergeDown {
                get {
                    string valStr = GetAttribute(node, "ss:MergeDown");
                    return valStr == "" ? 0 : int.Parse(valStr);
                }
                set { SetAttribute(node, "MergeDown", XmlNamespaces.ss, value < 1 ? "" : value.ToString(), true); }
            }


            /// <summary>
            /// 计算行中的索引
            /// </summary>
            public int CalcIndex() {
                if (index > 0) {
                    return index;
                } else {
                    var index = 0;
                    foreach (var cell in parentRow.Cells) {
                        index = cell.Index > 0 ? cell.Index : index + 1;
                        if (cell == this)
                            return index;
                        index += cell.MergeAcross;
                    }

                    return 0;
                }
            }

            /// <summary>
            /// Clone cell
            /// </summary>
            /// <returns>Cell copy</returns>
            public Cell Clone() {
                var nodeClone = node.CloneNode(false);
                var cellClone = new Cell(nodeClone) {parentRow = parentRow};

                if (dataNode != null) {
                    cellClone.dataNode = dataNode.Clone();
                    nodeClone.AppendChild(cellClone.dataNode);

                    foreach (XmlNode childNode in node.ChildNodes)
                        if (childNode.Name != "Data")
                            nodeClone.AppendChild(childNode.Clone());
                }

                return cellClone;
            }

            /// <summary>
            /// Set cell number type
            /// </summary>
            public void SetNumberType() {
                DataType = DataTypes.Number;
            }
        }


        /// <summary>
        /// 在SpreadsheetML中指定过渡到新行
        /// </summary>
        protected const string Break = "&#10;";

        /// <summary>
        /// 模板中可包含转换指令的XML元素的名称
        /// </summary>
        protected const string DirectiveElem = "Data";


        /// <summary>
        /// 正在处理的XML文档
        /// </summary>
        protected XmlDocument xmlDoc;

        /// <summary>
        /// 基于XML文档的Excel工作簿
        /// </summary>
        protected Workbook workbook;

        /// <summary>
        /// Excel工作表
        /// </summary>
        protected Worksheet procWorksheet;

        /// <summary>
        /// 要处理的Excel工作表
        /// </summary>
        protected Table procTable;

        /// <summary>
        /// 要处理的Excel行
        /// </summary>
        protected Row procRow;

        /// <summary>
        /// 正在处理的Excel表行单元格
        /// </summary>
        protected Cell procCell;

        /// <summary>
        /// XML节点可以有换行符
        /// </summary>
        protected bool textBroken;


        /// <summary>
        /// Constructor
        /// </summary>
        protected ExcelRepBuilder()
            : base() {
            xmlDoc = null;
        }


        /// <summary>
        /// Get report format
        /// </summary>
        public override string RepFormat {
            get { return "SpreadsheetML"; }
        }


        /// <summary>
        /// Get XML node attribute value
        /// </summary>
        protected static string GetAttribute(XmlNode xmlNode, string name) {
            if (xmlNode == null) {
                return "";
            } else {
                var xmlAttr = xmlNode.Attributes[name];
                return xmlAttr == null ? "" : xmlAttr.Value;
            }
        }

        /// <summary>
        /// Set the XML node attribute, creating it if necessary
        /// </summary>
        protected static void SetAttribute(XmlNode xmlNode, string localName, string namespaceURI, string value,
            bool removeEmpty = false) {
            if (xmlNode != null) {
                if (string.IsNullOrEmpty(value) && removeEmpty) {
                    xmlNode.Attributes.RemoveNamedItem(localName, namespaceURI);
                } else {
                    var xmlAttr = xmlNode.Attributes.GetNamedItem(localName, namespaceURI) as XmlAttribute;
                    if (xmlAttr == null) {
                        xmlAttr = xmlNode.OwnerDocument.CreateAttribute("", localName, namespaceURI);
                        xmlAttr.Value = value;
                        xmlNode.Attributes.SetNamedItem(xmlAttr);
                    } else {
                        xmlAttr.Value = value;
                    }
                }
            }
        }

        /// <summary>
        /// Find a directive in a string, get its value and the rest of the string
        /// </summary>
        protected bool FindDirective(string s, string attrName, out string attrVal, out string rest) {
            // "attrName=attrVal", instead '=' can be any character
            int valStartInd = attrName.Length + 1;
            if (valStartInd <= s.Length && s.StartsWith(attrName, StringComparison.Ordinal)) {
                int valEndInd = s.IndexOf(" ", valStartInd);
                if (valEndInd < 0) {
                    attrVal = s.Substring(valStartInd);
                    rest = "";
                } else {
                    attrVal = s.Substring(valStartInd, valEndInd - valStartInd);
                    rest = s.Substring(valEndInd + 1);
                }

                return true;
            } else {
                attrVal = "";
                rest = s;
                return false;
            }
        }

        /// <summary>
        /// 设置包含换行符的XML节点文本，将元素分成几个
        /// </summary>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, string text, string textBreak) {
            if (text == null)
                text = "";
            xmlNode.InnerText = text.Replace(textBreak, Break);
            textBroken = true;
        }

        /// <summary>
        /// 设置包含换行符的XML节点文本，将元素分成几个
        /// </summary>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, object text, string textBreak) {
            string textStr = text == null ? "" : text.ToString();
            SetNodeTextWithBreak(xmlNode, textStr, textBreak);
        }

        /// <summary>
        /// 设置包含换行符“\n”的XML节点文本，将元素分成几个
        /// </summary>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, string text) {
            SetNodeTextWithBreak(xmlNode, text, "\n");
        }

        /// <summary>
        /// 设置包含换行符“\n”的XML节点文本，将元素分成几个
        /// </summary>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, object text) {
            SetNodeTextWithBreak(xmlNode, text, "\n");
        }


        /// <summary>
        /// 预处理XML文档树
        /// </summary>
        protected virtual void StartXmlDocProc() { }

        /// <summary>
        /// 根据指令递归处理XML文档树
        /// </summary>
        protected virtual void XmlDocProc(XmlNode xmlNode) {
            if (xmlNode.Name == DirectiveElem) {
                procCell.DataNode = xmlNode;
                FindDirectives(procCell); // 搜索和处理指令
            } else {
                // 基于XML文档形成Excel工作簿
                if (xmlNode.Name == "Workbook") {
                    workbook = new Workbook(xmlNode);
                } else if (xmlNode.Name == "Styles") {
                    workbook.StylesNode = xmlNode;
                } else if (xmlNode.Name == "Style") {
                    var style = new Style(xmlNode);
                    workbook.Styles.Add(style.ID, style);
                } else if (xmlNode.Name == "Worksheet") {
                    procWorksheet = new Worksheet(xmlNode) {ParentWorkbook = workbook};
                    workbook.Worksheets.Add(procWorksheet);
                } else if (xmlNode.Name == "Table") {
                    procTable = new Table(xmlNode) {ParentWorksheet = procWorksheet};
                    procWorksheet.Table = procTable;
                } else if (xmlNode.Name == "Column") {
                    var column = new Column(xmlNode) {ParentTable = procTable};
                    procTable.Columns.Add(column);
                } else if (xmlNode.Name == "Row") {
                    procRow = new Row(xmlNode) {ParentTable = procTable};
                    procTable.Rows.Add(procRow);
                } else if (xmlNode.Name == "Cell") {
                    procCell = new Cell(xmlNode) {ParentRow = procRow};
                    procRow.Cells.Add(procCell);
                }

                // recursive enumeration of the descendants of the current element
                var children = xmlNode.ChildNodes;
                foreach (XmlNode node in children)
                    XmlDocProc(node);
            }
        }

        /// <summary>
        /// Finalize the XML document tree
        /// </summary>
        protected virtual void FinalXmlDocProc() { }

        /// <summary>
        /// Process structures representing an Excel worksheet
        /// </summary>
        protected virtual void ExcelProc(Table table) {
            foreach (var row in table.Rows)
                ExcelProc(row);
        }

        /// <summary>
        /// Process structures representing a row in an Excel worksheet
        /// </summary>
        protected virtual void ExcelProc(Row row) {
            // search and processing of directives for row cells
            foreach (var cell in row.Cells)
                FindDirectives(cell);
        }

        /// <summary>
        /// Find and process directives that may be contained in a given cell
        /// </summary>
        protected virtual void FindDirectives(Cell cell) {
            var dataNode = cell.DataNode;
            if (dataNode != null) {
                string attrVal;
                string rest;
                if (FindDirective(dataNode.InnerText, "repRow", out attrVal, out rest)) {
                    dataNode.InnerText = rest;
                    ProcRow(cell, attrVal);
                } else if (FindDirective(dataNode.InnerText, "repVal", out attrVal, out rest)) {
                    ProcVal(cell, attrVal);
                }
            }
        }

        /// <summary>
        /// Process the directive associated with the cell value
        /// </summary>
        protected virtual void ProcVal(Cell cell, string valName) { }

        /// <summary>
        /// Process the directive associated with the table row
        /// </summary>
        protected virtual void ProcRow(Cell cell, string rowName) { }


        /// <summary>
        /// Generate report to stream in SpreadsheetML format
        /// </summary>
        /// <remarks>Template directory should end with a slash</remarks>
        public override void Make(Stream outStream, string templateDir) {
            // report template file name
            string templFileName = templateDir + TemplateFileName;

            // loading and parsing a template XML file
            xmlDoc = new XmlDocument();
            xmlDoc.Load(templFileName);

            // field initialization
            workbook = null;
            procWorksheet = null;
            procTable = null;
            procRow = null;
            procCell = null;
            textBroken = false;

            // report creation - modification of xmlDoc
            StartXmlDocProc();
            XmlDocProc(xmlDoc.DocumentElement);
            FinalXmlDocProc();

            // write to output stream
            if (textBroken) {
                var stringWriter = new StringWriter();
                xmlDoc.Save(stringWriter);
                string xmlText = stringWriter.GetStringBuilder()
                    .Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"")
                    .Replace("&amp;#10", "&#10").ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(xmlText);
                outStream.Write(bytes, 0, bytes.Length);
            } else {
                xmlDoc.Save(outStream);
            }
        }
    }
}