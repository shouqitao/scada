/*
 * Copyright 2014 Mikhail Shiryaev
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
 * Summary  : The base class for building reports in WordprocessingML (Microsoft Word 2003) format
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2013
 */

using System;
using System.IO;
using System.Xml;

namespace Utils.Report {
    /// <summary>
    /// The base class for building reports in WordprocessingML (Microsoft Word 2003) format
    /// <para>用于以WordprocessingML格式构建报表的基类（Microsoft Word 2003）</para>
    /// </summary>
    public abstract class WordRepBuilder : RepBuilder {
        /// <summary>
        /// WordML中换行符的XML元素的前缀
        /// </summary>
        protected const string BrPref = "w";

        /// <summary>
        /// WordML中换行符的XML元素的名称
        /// </summary>
        protected const string BrName = "br";

        /// <summary>
        /// 名称前缀为模板的XML元素，其值可能包含更改指令
        /// </summary>
        protected const string ElemName = "w:t";

        /// <summary>
        /// XML树的高度，从表行的元素到带有单元格文本的元素
        /// </summary>
        protected const int RowTreeHeight = 4;


        /// <summary>
        /// 正在处理的XML文档
        /// </summary>
        protected XmlDocument xmlDoc;


        /// <inheritdoc />
        /// <summary>
        /// 构造函数
        /// </summary>
        protected WordRepBuilder()
            : base() {
            xmlDoc = null;
        }


        /// <inheritdoc />
        /// <summary>
        /// 获取报告格式
        /// </summary>
        public override string RepFormat {
            get { return "WordprocessingML"; }
        }


        /// <summary>
        /// 在字符串中查找属性（指令）并获取其值
        /// </summary>
        /// <param name="str">搜索字符串</param>
        /// <param name="attrName">要搜索的属性的名称</param>
        /// <param name="attrVal">属性值</param>
        /// <returns>找到属性</returns>
        protected bool FindAttr(string str, string attrName, out string attrVal) {
            // “attrName = attrVal”，而不是'='任何字符都可以
            attrVal = "";
            if (str.StartsWith(attrName)) {
                int start = attrName.Length + 1;
                if (start < str.Length) {
                    int end = str.IndexOf(" ", start, StringComparison.Ordinal);
                    if (end < 0)
                        end = str.IndexOf("x", start, StringComparison.OrdinalIgnoreCase);
                    if (end < 0)
                        end = str.Length;
                    attrVal = str.Substring(start, end - start);
                }

                return true;
            } else
                return false;
        }

        /// <summary>
        /// 获取表行的XML节点以及包含该指令的节点的XML报告树的表本身
        /// </summary>
        /// <param name="xmlNode">包含指令的XML节点</param>
        /// <param name="rowNode">报表表行xml节点</param>
        /// <param name="tblNode">报告表XML节点</param>
        /// <returns>找到XML节点</returns>
        protected bool GetTreeNodes(XmlNode xmlNode, out XmlNode rowNode, out XmlNode tblNode) {
            try {
                rowNode = xmlNode;
                for (var i = 0; i < RowTreeHeight; i++)
                    rowNode = rowNode.ParentNode;
                tblNode = rowNode.ParentNode;
                return true;
            } catch {
                rowNode = null;
                tblNode = null;
                return false;
            }
        }

        /// <summary>
        /// 设置包含换行符的XML节点文本，将元素分成几个
        /// </summary>
        /// <param name="xmlNode">XML节点</param>
        /// <param name="text">可设置的文字</param>
        /// <param name="textBreak">可设置文本中的换行符指定</param>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, string text, string textBreak) {
            var parNode = xmlNode.ParentNode;
            if (parNode == null)
                throw new Exception("Parent XML element is missing.");

            // detach the node being broken
            parNode.RemoveChild(xmlNode);
            var cloneNode = xmlNode.Clone();

            if (text == null) text = "";
            string uri = parNode.NamespaceURI;
            int breakLen = textBreak.Length;

            do {
                // text line definition
                int breakPos = text.IndexOf(textBreak, StringComparison.Ordinal);
                bool haveBreak = breakPos >= 0;
                string line = haveBreak ? text.Substring(0, breakPos) : text;

                // add text line
                var newNode = cloneNode.Clone();
                newNode.InnerText = line;
                parNode.AppendChild(newNode);

                // add line break tag if necessary
                if (haveBreak)
                    parNode.AppendChild(xmlDoc.CreateElement(BrPref, BrName, uri));

                // trimming the processed part of the text
                text = haveBreak && breakPos + breakLen < text.Length ? text.Substring(breakPos + breakLen) : "";
            } while (text != "");
        }

        /// <summary>
        /// Set the XML node text containing line breaks, breaking the element into several
        /// </summary>
        /// <param name="xmlNode">XML-taken</param>
        /// <param name="text">Settable text</param>
        /// <param name="textBreak">Line break designation in settable text</param>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, object text, string textBreak) {
            string textStr = text == null || text.ToString() == "" ? " " : text.ToString();
            SetNodeTextWithBreak(xmlNode, textStr, textBreak);
        }

        /// <summary>
        /// Set the XML node text containing line breaks "\ n", breaking the element into several
        /// </summary>
        /// <param name="xmlNode">XML-taken</param>
        /// <param name="text">Settable text</param>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, string text) {
            SetNodeTextWithBreak(xmlNode, text, "\n");
        }

        /// <summary>
        /// Set the XML node text containing line breaks "\ n", breaking the element into several
        /// </summary>
        /// <param name="xmlNode">XML-taken</param>
        /// <param name="text">Settable text</param>
        protected void SetNodeTextWithBreak(XmlNode xmlNode, object text) {
            SetNodeTextWithBreak(xmlNode, text, "\n");
        }


        /// <summary>
        /// Initial processing of the XML document tree
        /// </summary>
        protected virtual void StartXmlDocProc() { }

        /// <summary>
        /// Recursive traversal and processing of the XML document tree according to the directives for receiving the report
        /// </summary>
        /// <param name="xmlNode">XML node being processed</param>
        protected virtual void XmlDocProc(XmlNode xmlNode) {
            if (xmlNode.Name == ElemName) {
                // search for directives of transformations of elements
                string nodeVal = xmlNode.InnerText;
                string attrVal;
                if (FindAttr(nodeVal, "repRow", out attrVal)) {
                    if (nodeVal.Length < 8 /*"repRow=".Length + 1*/ + attrVal.Length)
                        xmlNode.InnerText = "";
                    else
                        xmlNode.InnerText = nodeVal.Substring(8 + attrVal.Length);
                    ProcRow(xmlNode, attrVal);
                } else if (FindAttr(nodeVal, "repVal", out attrVal))
                    ProcVal(xmlNode, attrVal);
            } else {
                // recursive enumeration of the descendants of the current element
                var children = xmlNode.ChildNodes;
                foreach (XmlNode node in children)
                    XmlDocProc(node);
            }
        }

        /// <summary>
        /// Final processing of the XML document tree
        /// </summary>
        protected virtual void FinalXmlDocProc() { }

        /// <summary>
        /// Processing a directive that changes the value of an element
        /// </summary>
        /// <param name="xmlNode">XML node containing directive</param>
        /// <param name="valName">Element name given by directive</param>
        protected virtual void ProcVal(XmlNode xmlNode, string valName) { }

        /// <summary>
        /// Processing directive creating table rows
        /// </summary>
        /// <param name="xmlNode">XML node containing directive</param>
        /// <param name="rowName">The string name given by the directive</param>
        protected virtual void ProcRow(XmlNode xmlNode, string rowName) { }


        /// <inheritdoc />
        /// <summary>
        /// Generate report to stream in WordML format
        /// </summary>
        /// <param name="outStream">Output stream</param>
        /// <param name="templateDir">Template directory with '\' at the end</param>
        public override void Make(Stream outStream, string templateDir) {
            // report template file name
            string templFileName = templateDir + TemplateFileName;

            // loading and parsing a template XML file
            xmlDoc = new XmlDocument();
            xmlDoc.Load(templFileName);

            // report creation - modification of xmlDoc
            StartXmlDocProc();
            XmlDocProc(xmlDoc.DocumentElement);
            FinalXmlDocProc();

            // write to output stream
            xmlDoc.Save(outStream);
        }
    }
}