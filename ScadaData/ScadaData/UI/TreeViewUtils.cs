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
 * Summary  : Utility methods for TreeView control
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2018
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Scada.UI {
    /// <summary>
    /// Utility methods for TreeView control
    /// <para>Helper methods for working with the TreeView control</para>
    /// <remarks>
    /// Objects of tree node tags must implement ITreeNode interface
    /// <para>Tree node tag objects must support the ITreeNode interface.</para>
    /// </remarks>
    /// </summary>
    public static class TreeViewUtils {
        /// <summary>
        /// Behavior when moving tree nodes
        /// </summary>
        public enum MoveBehavior {
            /// <summary>
            /// Within its parent
            /// </summary>
            WithinParent,

            /// <summary>
            /// Through parents of the same type
            /// </summary>
            ThroughSimilarParents
        }


        /// <summary>
        /// Add a node to the end of the list of child nodes of the specified parent node or the tree itself
        /// <remarks>The method is recommended to use if the objects do not support ITreeNode.</remarks>
        /// </summary>
        public static void Add(this TreeView treeView, TreeNode parentNode, TreeNode nodeToAdd,
            IList destList, object objToAdd) {
            if (nodeToAdd == null)
                throw new ArgumentNullException(nameof(nodeToAdd));
            if (destList == null)
                throw new ArgumentNullException(nameof(destList));
            if (objToAdd == null)
                throw new ArgumentNullException(nameof(objToAdd));

            var nodes = treeView.GetChildNodes(parentNode);
            nodes.Add(nodeToAdd);
            destList.Add(objToAdd);
            treeView.SelectedNode = nodeToAdd;
        }

        /// <summary>
        /// Add a node to the end of the list of child nodes of the specified parent node
        /// </summary>
        /// <remarks>Argument parentNode cannot be null</remarks>
        public static void Add(this TreeView treeView, TreeNode parentNode, TreeNode nodeToAdd) {
            if (parentNode == null)
                throw new ArgumentNullException(nameof(parentNode));

            if (GetRelatedObject(parentNode) is ITreeNode parentObj &&
                GetRelatedObject(nodeToAdd) is ITreeNode objToAdd) {
                treeView.Add(parentNode, nodeToAdd, parentObj.Children, objToAdd);
            }
        }

        /// <summary>
        /// Create a tree node
        /// </summary>
        public static TreeNode CreateNode(string text, string imageKey, bool expand = false) {
            var node = new TreeNode(text) {
                ImageKey = imageKey,
                SelectedImageKey = imageKey
            };

            if (expand)
                node.Expand();

            return node;
        }

        /// <summary>
        /// Create a tree node based on the specified object
        /// </summary>
        public static TreeNode CreateNode(object tag, string imageKey, bool expand = false) {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            var node = CreateNode(tag.ToString(), imageKey, expand);
            node.Tag = tag;
            return node;
        }

        /// <summary>
        /// Find the nearest tree node of the specified type with respect to the specified node up the tree
        /// </summary>
        public static TreeNode FindClosest(this TreeNode treeNode, Type tagType) {
            if (tagType == null)
                throw new ArgumentNullException(nameof(tagType));

            while (treeNode != null && !tagType.IsInstanceOfType(treeNode.Tag)) {
                treeNode = treeNode.Parent;
            }

            return treeNode;
        }

        /// <summary>
        /// Find the nearest tree node of the specified type with respect to the specified node up the tree
        /// </summary>
        public static TreeNode FindClosest(this TreeNode treeNode, string nodeType) {
            while (treeNode != null && !(treeNode.Tag is TreeNodeTag tag && tag.NodeType == nodeType)) {
                treeNode = treeNode.Parent;
            }

            return treeNode;
        }

        /// <summary>
        /// Find the first tree node of the specified type among all child nodes,
        /// including the specified node
        /// </summary>
        public static TreeNode FindFirst(this TreeNode treeNode, Type tagType) {
            foreach (var childNode in IterateNodes(treeNode)) {
                if (tagType.IsInstanceOfType(childNode.Tag))
                    return childNode;
            }

            return null;
        }

        /// <summary>
        /// Find the first tree node of the specified type among all child nodes, including the specified node
        /// </summary>
        public static TreeNode FindFirst(this TreeNode treeNode, string nodeType) {
            foreach (var childNode in IterateNodes(treeNode)) {
                if (childNode.Tag is TreeNodeTag tag && tag.NodeType == nodeType)
                    return childNode;
            }

            return null;
        }

        /// <summary>
        /// Find the index to insert into the list of child nodes of the specified parent node,
        /// taking into account the selected node
        /// </summary>
        public static int FindInsertIndex(this TreeView treeView, TreeNode parentNode) {
            var node = treeView.SelectedNode;

            while (node != null && node.Parent != parentNode) {
                node = node.Parent;
            }

            return node?.Index + 1 ?? treeView.GetChildNodes(parentNode).Count;
        }

        /// <summary>
        /// Find the parent node and index to insert a new tree node,
        /// taking into account the selected node
        /// </summary>
        public static bool FindInsertLocation(this TreeView treeView, Type tagType,
            out TreeNode parentNode, out int index) {
            var node = treeView.SelectedNode?.FindClosest(tagType);

            if (node == null) {
                parentNode = null;
                index = -1;
                return false;
            }

            parentNode = node.Parent;
            index = node.Index + 1;
            return true;
        }

        /// <summary>
        /// Get the object of the selected tree node
        /// </summary>
        public static object GetSelectedObject(this TreeView treeView) {
            return GetRelatedObject(treeView.SelectedNode);
        }

        /// <summary>
        /// Insert a node into the list of child nodes of the specified parent node or the tree itself 
        /// after the selected tree node
        /// </summary>
        /// <remarks>
        /// The method is recommended to be used if parentNode is null or objects do not support ITreeNode
        /// </remarks>
        public static void Insert(this TreeView treeView, TreeNode parentNode, TreeNode nodeToInsert,
            IList destList, object objToInsert) {
            if (nodeToInsert == null)
                throw new ArgumentNullException(nameof(nodeToInsert));
            if (destList == null)
                throw new ArgumentNullException(nameof(destList));
            if (objToInsert == null)
                throw new ArgumentNullException(nameof(objToInsert));

            int index = treeView.FindInsertIndex(parentNode);
            var nodes = treeView.GetChildNodes(parentNode);

            nodes.Insert(index, nodeToInsert);
            destList.Insert(index, objToInsert);
            treeView.SelectedNode = nodeToInsert;
        }

        /// <summary>
        /// Insert a node into the list of child nodes of the specified parent node after the selected tree node
        /// </summary>
        /// <remarks>Argument parentNode cannot be null</remarks>
        public static void Insert(this TreeView treeView, TreeNode parentNode, TreeNode nodeToInsert) {
            if (parentNode == null)
                throw new ArgumentNullException(nameof(parentNode));

            if (GetRelatedObject(parentNode) is ITreeNode parentObj &&
                GetRelatedObject(nodeToInsert) is ITreeNode objToInsert) {
                treeView.Insert(parentNode, nodeToInsert, parentObj.Children, objToInsert);
            }
        }

        /// <summary>
        /// Recursive traversal of tree nodes
        /// </summary>
        public static IEnumerable<TreeNode> IterateNodes(TreeNodeCollection nodes) {
            foreach (TreeNode node in nodes) {
                yield return node;

                foreach (var childNode in IterateNodes(node.Nodes)) {
                    yield return childNode;
                }
            }
        }

        /// <summary>
        /// Recursive traversal of tree nodes
        /// </summary>
        public static IEnumerable<TreeNode> IterateNodes(TreeNode treeNode) {
            yield return treeNode;

            foreach (var childNode in IterateNodes(treeNode.Nodes)) {
                yield return childNode;
            }
        }

        /// <summary>
        /// Move the selected tree node and the element of the specified list down the tree
        /// </summary>
        /// <remarks>The method is recommended to use if the objects do not support ITreeNode.</remarks>
        public static void MoveDownSelectedNode(this TreeView treeView, IList sourceList) {
            var selectedNode = treeView.SelectedNode;

            if (selectedNode != null) {
                try {
                    treeView.BeginUpdate();

                    var nodes = treeView.GetChildNodes(selectedNode.Parent);
                    int index = selectedNode.Index;
                    int newIndex = index + 1;

                    if (newIndex < nodes.Count) {
                        nodes.RemoveAt(index);
                        nodes.Insert(newIndex, selectedNode);

                        var selectedObj = sourceList[index];
                        sourceList.RemoveAt(index);
                        sourceList.Insert(newIndex, selectedObj);

                        treeView.SelectedNode = selectedNode;
                    }
                } finally {
                    treeView.EndUpdate();
                }
            }
        }

        /// <summary>
        /// Move the selected tree node and the element of the corresponding list down the tree
        /// </summary>
        public static void MoveDownSelectedNode(this TreeView treeView, MoveBehavior moveBehavior) {
            var selectedNode = treeView.SelectedNode;

            if (!(GetRelatedObject(selectedNode) is ITreeNode selectedObj)) return;

            try {
                treeView.BeginUpdate();

                var nodes = treeView.GetChildNodes(selectedNode.Parent);
                var list = selectedObj.Parent.Children;

                int index = selectedNode.Index;
                int newIndex = index + 1;

                if (newIndex < nodes.Count) {
                    nodes.RemoveAt(index);
                    nodes.Insert(newIndex, selectedNode);

                    list.RemoveAt(index);
                    list.Insert(newIndex, selectedObj);

                    treeView.SelectedNode = selectedNode;
                } else if (moveBehavior == MoveBehavior.ThroughSimilarParents) {
                    var parentNode = selectedNode.Parent;
                    var nextParentNode = parentNode == null ? null : parentNode.NextNode;

                    if (parentNode != null && nextParentNode != null &&
                        parentNode.Tag is ITreeNode && nextParentNode.Tag is ITreeNode &&
                        parentNode.Tag.GetType() == nextParentNode.Tag.GetType()) {
                        // change the parent of the node being moved
                        nodes.RemoveAt(index);
                        nextParentNode.Nodes.Insert(0, selectedNode);

                        var nextParentObj = (ITreeNode) nextParentNode.Tag;
                        list.RemoveAt(index);
                        nextParentObj.Children.Insert(0, selectedObj);
                        selectedObj.Parent = nextParentObj;

                        treeView.SelectedNode = selectedNode;
                    }
                }
            } finally {
                treeView.EndUpdate();
            }
        }

        /// <summary>
        /// Check that moving the selected tree node down is possible
        /// </summary>
        public static bool MoveDownSelectedNodeIsEnabled(this TreeView treeView, MoveBehavior moveBehavior) {
            var selectedNode = treeView.SelectedNode;

            if (selectedNode == null) {
                return false;
            }

            if (selectedNode.NextNode == null) {
                if (moveBehavior == MoveBehavior.ThroughSimilarParents) {
                    var parentNode = selectedNode.Parent;
                    var nextParentNode = parentNode?.NextNode;

                    return parentNode != null && nextParentNode != null &&
                           (parentNode.Tag is ITreeNode && nextParentNode.Tag is ITreeNode &&
                            parentNode.Tag.GetType() == nextParentNode.Tag.GetType() ||
                            parentNode.Tag is TreeNodeTag tag1 && nextParentNode.Tag is TreeNodeTag tag2 &&
                            tag1.NodeType == tag2.NodeType);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Move the selected tree node and the corresponding list item to the specified position
        /// </summary>
        public static void MoveSelectedNode(this TreeView treeView, int newIndex) {
            var selectedNode = treeView.SelectedNode;

            if (!(GetRelatedObject(selectedNode) is ITreeNode selectedObj)) return;

            try {
                treeView.BeginUpdate();

                var nodes = treeView.GetChildNodes(selectedNode.Parent);
                var list = selectedObj.Parent.Children;

                if (0 <= newIndex && newIndex < nodes.Count) {
                    int index = selectedNode.Index;

                    nodes.RemoveAt(index);
                    nodes.Insert(newIndex, selectedNode);

                    list.RemoveAt(index);
                    list.Insert(newIndex, selectedObj);

                    treeView.SelectedNode = selectedNode;
                }
            } finally {
                treeView.EndUpdate();
            }
        }

        /// <summary>
        /// Move the selected tree node and the element of the specified list up the tree
        /// </summary>
        /// <remarks>The method is recommended to use if the objects do not support ITreeNode.</remarks>
        public static void MoveUpSelectedNode(this TreeView treeView, IList sourceList) {
            var selectedNode = treeView.SelectedNode;

            if (selectedNode != null) {
                try {
                    treeView.BeginUpdate();

                    var nodes = treeView.GetChildNodes(selectedNode.Parent);
                    int index = selectedNode.Index;
                    int newIndex = index - 1;

                    if (newIndex < 0) return;

                    nodes.RemoveAt(index);
                    nodes.Insert(newIndex, selectedNode);

                    var selectedObj = sourceList[index];
                    sourceList.RemoveAt(index);
                    sourceList.Insert(newIndex, selectedObj);

                    treeView.SelectedNode = selectedNode;
                } finally {
                    treeView.EndUpdate();
                }
            }
        }

        /// <summary>
        /// Move the selected tree node and the corresponding list item up the tree
        /// </summary>
        public static void MoveUpSelectedNode(this TreeView treeView, MoveBehavior moveBehavior) {
            var selectedNode = treeView.SelectedNode;

            if (!(GetRelatedObject(selectedNode) is ITreeNode selectedObj)) return;

            try {
                treeView.BeginUpdate();

                var nodes = treeView.GetChildNodes(selectedNode.Parent);
                var list = selectedObj.Parent.Children;

                int index = selectedNode.Index;
                int newIndex = index - 1;

                if (newIndex >= 0) {
                    nodes.RemoveAt(index);
                    nodes.Insert(newIndex, selectedNode);

                    list.RemoveAt(index);
                    list.Insert(newIndex, selectedObj);

                    treeView.SelectedNode = selectedNode;
                } else if (moveBehavior == MoveBehavior.ThroughSimilarParents) {
                    var parentNode = selectedNode.Parent;
                    var prevParentNode = parentNode?.PrevNode;

                    if (parentNode != null && prevParentNode != null &&
                        parentNode.Tag is ITreeNode && prevParentNode.Tag is ITreeNode &&
                        parentNode.Tag.GetType() == prevParentNode.Tag.GetType()) {
                        // change the parent of the node being moved
                        nodes.RemoveAt(index);
                        prevParentNode.Nodes.Add(selectedNode);

                        var prevParentObj = (ITreeNode) prevParentNode.Tag;
                        list.RemoveAt(index);
                        prevParentObj.Children.Add(selectedObj);
                        selectedObj.Parent = prevParentObj;

                        treeView.SelectedNode = selectedNode;
                    }
                }
            } finally {
                treeView.EndUpdate();
            }
        }

        /// <summary>
        /// Check that moving the selected tree node upwards is possible
        /// </summary>
        public static bool MoveUpSelectedNodeIsEnabled(this TreeView treeView, MoveBehavior moveBehavior) {
            var selectedNode = treeView.SelectedNode;

            if (selectedNode == null) {
                return false;
            }

            if (selectedNode.PrevNode == null) {
                if (moveBehavior == MoveBehavior.ThroughSimilarParents) {
                    var parentNode = selectedNode.Parent;
                    var prevParentNode = parentNode?.PrevNode;

                    return parentNode != null && prevParentNode != null &&
                           (parentNode.Tag is ITreeNode && prevParentNode.Tag is ITreeNode &&
                            parentNode.Tag.GetType() == prevParentNode.Tag.GetType() ||
                            parentNode.Tag is TreeNodeTag tag1 && prevParentNode.Tag is TreeNodeTag tag2 &&
                            tag1.NodeType == tag2.NodeType);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Delete the specified tree node and list item
        /// </summary>
        /// <remarks>The method is recommended to use if the objects do not support ITreeNode.</remarks>
        public static void RemoveNode(this TreeView treeView, TreeNode nodeToRemove, IList sourceList) {
            if (nodeToRemove != null) {
                var nodes = treeView.GetChildNodes(nodeToRemove.Parent);
                int index = nodeToRemove.Index;
                nodes.RemoveAt(index);
                sourceList.RemoveAt(index);
            }
        }

        /// <summary>
        /// Delete the selected tree node and item from the corresponding list
        /// </summary>
        public static void RemoveSelectedNode(this TreeView treeView) {
            if (GetRelatedObject(treeView.SelectedNode) is ITreeNode treeNodeObj)
                RemoveNode(treeView, treeView.SelectedNode, treeNodeObj.Parent.Children);
        }

        /// <summary>
        /// Set the main image of the node and the image in the selected state
        /// </summary>
        public static void SetImageKey(this TreeNode treeNode, string imageKey) {
            treeNode.ImageKey = treeNode.SelectedImageKey = imageKey;
        }

        /// <summary>
        /// Check if the tree node tag has the specified type
        /// </summary>
        public static bool TagIs(this TreeNode treeNode, string nodeType) {
            return treeNode.Tag is TreeNodeTag tag && tag.NodeType == nodeType;
        }

        /// <summary>
        /// Update the text of the selected tree node in accordance with the string representation of its object
        /// </summary>
        public static void UpdateSelectedNodeText(this TreeView treeView) {
            var relatedObj = GetRelatedObject(treeView.SelectedNode);
            if (relatedObj != null)
                treeView.SelectedNode.Text = relatedObj.ToString();
        }

        /// <summary>
        /// Get a collection of child nodes of a given parent node or the tree itself
        /// </summary>
        private static TreeNodeCollection GetChildNodes(this TreeView treeView, TreeNode parentNode) {
            return parentNode == null ? treeView.Nodes : parentNode.Nodes;
        }

        /// <summary>
        /// Get the object associated with the tree node
        /// </summary>
        private static object GetRelatedObject(TreeNode treeNode) {
            return treeNode?.Tag is TreeNodeTag treeNodeTag ? treeNodeTag.RelatedObject : treeNode?.Tag;
        }
    }
}