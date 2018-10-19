/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Editor.Controls;
using Opsive.UltimateCharacterController.Editor.Inspectors.Utility;
using System;
using System.Collections.Generic;

namespace Opsive.UltimateCharacterController.Editor.Managers {
    /// <summary>
    /// The ItemTypeManager will draw any ItemType properties
    /// </summary>
    [Serializable]
    [ManagerMenuItem("Animation States", 9)]
    public class AnimationStateManager : Manager {
        private static GUIStyle s_TreeRowHeaderGUIStyle;
        public static GUIStyle TreeRowHeaderGUIStyle {
            get {
                if (s_TreeRowHeaderGUIStyle == null) {
                    s_TreeRowHeaderGUIStyle = new GUIStyle("RL Header");
                    // The header background image should stretch with the size of the rect.
                    s_TreeRowHeaderGUIStyle.fixedHeight = 0;
                    s_TreeRowHeaderGUIStyle.stretchHeight = true;
                }
                return s_TreeRowHeaderGUIStyle;
            }
        }
        private static GUIStyle s_TreeRowBackgroundGUIStyle;
        public static GUIStyle TreeRowBackgroundGUIStyle {
            get {
                if (s_TreeRowBackgroundGUIStyle == null) {
                    s_TreeRowBackgroundGUIStyle = new GUIStyle("RL Background");
                }
                return s_TreeRowBackgroundGUIStyle;
            }
        }

        private class StateTab {
            public string name;
            public TreeViewState state;
            public FlatTreeView<AnimationStateModel> treeView;
            public SearchField searchField;
            public AnimationStateSet animationStateSet;

            public void Initialize(AnimationStateSet animationStateSet = null) {
                treeView.state.expandedIDs = state.expandedIDs;
                treeView.state.lastClickedID = state.lastClickedID;
                treeView.state.searchString = state.searchString;
                treeView.state.selectedIDs = state.selectedIDs;
                state = treeView.state;
                this.animationStateSet = animationStateSet;
                (treeView.TreeModal as AnimationStateModel).AnimationStateSet = this.animationStateSet;
                treeView.Reload();
            }

            public override string ToString() {
                return name;
            }
        }
 
        private string[] m_ToolbarStrings = { "Item Ids", "State Indexes", "Ability Indexes" };
        private StateTab[] m_Tabs;

        [SerializeField] private AnimationStateCollection m_AnimationStateCollection;
        [SerializeField] private TreeViewState m_ItemIdTreeViewState;
        [SerializeField] private TreeViewState m_ItemStateIndexTreeViewState;
        [SerializeField] private TreeViewState m_AbilityIndexTreeViewState;
        [SerializeField] private string m_AnimationStateName;
        [SerializeField] private bool m_DrawAnimationStateSet = true;

        private StateTab m_ItemIdTab;
        private StateTab m_ItemStateIndexTab;
        private StateTab m_AbilityIndexTab;

        private int m_ToolbarSelection;

        private StateTab CreateTab(string name, out TreeViewState state) {
            StateTab tab = new StateTab();
            tab.state = state = new TreeViewState();

            var model = new AnimationStateModel();

            model.BeforeModalChange += OnTreeWillChange;
            model.AfterModalChange += OnTreeChangeReload;

            tab.treeView = new FlatTreeView<AnimationStateModel>(tab.state, model);
            tab.treeView.TreeChange += OnTreeChange;
            tab.searchField = new SearchField();
            tab.searchField.downOrUpArrowKeyPressed += tab.treeView.SetFocusAndEnsureSelectedItem;
            return tab;
        }

        /// <summary>
        /// Default ItemTypeManager constructor.
        /// </summary>
        public AnimationStateManager() {
            m_ItemIdTab = CreateTab("Item Id", out m_ItemIdTreeViewState);
            m_ItemStateIndexTab = CreateTab("Item State Index", out m_ItemStateIndexTreeViewState);
            m_AbilityIndexTab = CreateTab("Ability Index", out m_AbilityIndexTreeViewState);
            m_Tabs = new StateTab[] {
                m_ItemIdTab,
                m_ItemStateIndexTab,
                m_AbilityIndexTab
            };

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        /// <summary>
        /// Unsubscribe from any events when the object is destroyed.
        /// </summary>
        ~AnimationStateManager() {
            Undo.undoRedoPerformed -= OnUndoRedo;
            foreach(StateTab tab in m_Tabs) {
                tab.treeView.TreeChange -= OnTreeChange;
            }
        }

        /// <summary>
        /// Initialize the manager after deserialization.
        /// </summary>
        public override void Initialize(MainManagerWindow mainManagerWindow) {
            base.Initialize(mainManagerWindow);

            // Look for the ItemCollection within the scene if it isn't already populated.
            if (m_AnimationStateCollection == null) {
                m_AnimationStateCollection = UCCEManagerUtility.FindAnimStateColection(m_MainManagerWindow);
            }

            // The ItemCollection may have been serialized.
            if (m_AnimationStateCollection != null) {
                m_ItemIdTab.Initialize(m_AnimationStateCollection.ItemIds);
                m_ItemStateIndexTab.Initialize(m_AnimationStateCollection.ItemStateIndexes);
                m_AbilityIndexTab.Initialize(m_AnimationStateCollection.AbilityIndexes);
            }
        }

        /// <summary>
        /// Draws the ItemTypeManager.
        /// </summary>
        public override void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            var animStateCollection = EditorGUILayout.ObjectField("Animation State Collection", m_AnimationStateCollection, typeof(AnimationStateCollection), false) as AnimationStateCollection;
            if (GUILayout.Button("Create", GUILayout.MaxWidth(100))) {
                var path = EditorUtility.SaveFilePanel("Save Animation State Collection", "Assets", "AnimationStateCollection.asset", "asset");
                if (path.Length != 0 && Application.dataPath.Length < path.Length) {
                    animStateCollection = ScriptableObject.CreateInstance<AnimationStateCollection>();

                    // Save the collection.
                    path = string.Format("Assets/{0}", path.Substring(Application.dataPath.Length + 1));
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(animStateCollection, path);
                    AssetDatabase.ImportAsset(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (animStateCollection != m_AnimationStateCollection) {
                m_AnimationStateCollection = animStateCollection;
                if (m_AnimationStateCollection != null) {
                    EditorPrefs.SetString(UCCEManagerUtility.LastAnimationStateCollectionGUIDString, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(animStateCollection)));
                    m_ItemIdTab.Initialize(m_AnimationStateCollection.ItemIds);
                    m_ItemStateIndexTab.Initialize(m_AnimationStateCollection.ItemStateIndexes);
                    m_AbilityIndexTab.Initialize(m_AnimationStateCollection.AbilityIndexes);
                } else {
                    EditorPrefs.SetString(ManagerUtility.LastItemCollectionGUIDString, string.Empty);
                }
            }

            // ItemCollection must be populated in order to create Categories/ItemTypes.
            if (m_AnimationStateCollection == null) {
                EditorGUILayout.HelpBox("An AnimatioStateCollection must be selected. Use the \"Create\" button to create a new collection.", MessageType.Error);
                return;
            }

            if (m_ItemIdTab.animationStateSet == null) {
                m_ItemIdTab.Initialize(m_AnimationStateCollection.ItemIds);
            }

            if (m_ItemStateIndexTab.animationStateSet == null) {
                m_ItemStateIndexTab.Initialize(m_AnimationStateCollection.ItemStateIndexes);
            }

            if (m_AbilityIndexTab.animationStateSet == null) {
                m_AbilityIndexTab.Initialize(m_AnimationStateCollection.AbilityIndexes);
            }

            m_ToolbarSelection = GUILayout.Toolbar(m_ToolbarSelection, m_ToolbarStrings, EditorStyles.toolbarButton);
            DrawTab(m_Tabs[m_ToolbarSelection]);
        }

        /// <summary>
        /// Draws the ItemTypes editor.
        /// </summary>
        private void DrawTab(StateTab tab) {
            if (null == tab.animationStateSet) {
                EditorGUILayout.HelpBox("An invalid animation collection was selected.", MessageType.Error);

            }
            var animationStates = tab.animationStateSet != null ? tab.animationStateSet.AnimationStates : null;

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("AnimationStateName");
            m_AnimationStateName = EditorGUILayout.TextField("Name", m_AnimationStateName);
            GUI.enabled = !string.IsNullOrEmpty(m_AnimationStateName) && (tab.treeView.TreeModal as AnimationStateModel).IsUniqueName(m_AnimationStateName);
            if (GUILayout.Button("Add", GUILayout.Width(100)) || (Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "AnimationStateName")) {
                // Create the new ItemType.
                var itemType = new Inventory.AnimationState();
                itemType.name = m_AnimationStateName;

                // Add the ItemType to the ItemCollection.
                Array.Resize(ref animationStates, animationStates != null ? animationStates.Length + 1 : 1);
                itemType.ID = animationStates.Length - 1;
                animationStates[animationStates.Length - 1] = itemType;
                tab.animationStateSet.AnimationStates = animationStates;
                Debug.Log("Asset path: " + AssetDatabase.GetAssetPath(tab.animationStateSet));
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(tab.animationStateSet));

                // Select the newly added item.
                tab.treeView.SetSelection(new List<int>() { itemType.ID }, TreeViewSelectionOptions.FireSelectionChanged);

                // Reset.
                EditorUtility.SetDirty(m_AnimationStateCollection);
                EditorUtility.SetDirty(tab.animationStateSet);
                m_AnimationStateName = string.Empty;
                GUI.FocusControl("");
                tab.treeView.Reload();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.Space(5);

            if (animationStates != null && animationStates.Length > 0) {
                var guiRect = GUILayoutUtility.GetLastRect();
                var height = m_MainManagerWindow.position.height - guiRect.yMax - 21;
                tab.treeView.searchString = tab.searchField.OnGUI(new Rect(0, guiRect.yMax, m_MainManagerWindow.position.width - m_MainManagerWindow.MenuWidth - 2, 20), tab.treeView.searchString);
                tab.treeView.OnGUI(new Rect(0, guiRect.yMax + 20, m_MainManagerWindow.position.width - m_MainManagerWindow.MenuWidth - 1, height));
                // OnGUI doesn't update the GUILayout rect so add a blank space to account for it.
                GUILayout.Space(height + 10);
            }
        }

        /// <summary>
        /// Save the current state of the tree before it is changed for the undo manager.
        /// </summary>
        private void OnTreeWillChange() {
            Undo.RegisterCompleteObjectUndo(m_AnimationStateCollection, "Change Tree");
        }

        /// <summary>
        /// The tree has changed - mark for serialization.
        /// </summary>
        private void OnTreeChange() {
            // Marking the GUI as changed will reserialize the managers.
            GUI.changed = true;
        }

        /// <summary>
        /// The tree has changed - mark for serialization and reload the tree.
        /// </summary>
        private void OnTreeChangeReload() {
            // Marking the GUI as changed will reserialize the managers.
            GUI.changed = true;
            foreach(StateTab tab in m_Tabs) {
                tab.treeView.Reload();
            }
        }

        /// <summary>
        /// Reload the TreeView with an undo redo.
        /// </summary>
        private void OnUndoRedo() {
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m_AnimationStateCollection));
            foreach (StateTab tab in m_Tabs) {
                tab.treeView.Reload();
            }
        }
    }

    /// <summary>
    /// The AnimationStateModel inherits TreeModal to provide a tree modal for the AnimationSate class.
    /// </summary>
    public class AnimationStateModel : TreeModal {
        // Specifies the height of the row.
        private const float c_RowHeight = 30;
        // Specifies the height of the selected row.
        private const float c_SelectedRowHeight = 60;

        private AnimationStateSet m_AnimationStateSet;

        public AnimationStateSet AnimationStateSet { get { return m_AnimationStateSet; } set { m_AnimationStateSet = value; } }

        /// <summary>
        /// Returns the number of rows in the tree.
        /// </summary>
        /// <returns>The number of rows in the tree.</returns>
        public override int GetRowCount() {
            if (m_AnimationStateSet == null || m_AnimationStateSet.AnimationStates == null) {
                return 0;
            }
            return m_AnimationStateSet.AnimationStates.Length;
        }

        /// <summary>
        /// Returns the height of the row.
        /// </summary>
        /// <param name="item">The item that occupies the row with the requested height.</param>
        /// <param name="state">The state of the tree.</param>
        /// <returns>The height of the row.</returns>
        public override float GetRowHeight(TreeViewItem item, TreeViewState state) {
            var selected = IsSelected(item, state);
            var height = selected ? c_SelectedRowHeight : c_RowHeight;
            // Extra spacing needs to be added for each referenced dropped ItemType.
            if (selected) {
                var itemType = m_AnimationStateSet.AnimationStates[item.id];
                // Height of id field
                height += 20;
            }

            return height;
        }

        /// <summary>
        /// Draws the GUI for the row.
        /// </summary>
        /// <param name="rowRect">The rect of the row being drawn.</param>
        /// <param name="item">The item that occupies the row which is being drawn.</param>
        /// <param name="state">The state of the tree.</param>
        public override void RowGUI(Rect rowRect, TreeViewItem item, TreeViewState state) {
            var isSelected = IsSelected(item, state);

            // Leave some spacing on the sides of the row.
            rowRect.xMax -= 2;
            rowRect.yMin += 2;
            rowRect.yMax -= 2;

            // Draw the background.
            DrawBackground(rowRect, isSelected);

            // Draw the header.
            DrawHeader(rowRect, item.id);

            EditorGUI.BeginChangeCheck();

            // Draws the ItemType controls.
            if (DrawControls(rowRect, item.id)) {
                if (EditorGUI.EndChangeCheck()) {
                    // Serialize the changes.
                    EditorUtility.SetDirty(m_AnimationStateSet);
                    if (m_AfterModalChange != null) {
                        m_AfterModalChange();
                    }
                }
                return;
            }

            // If the ItemType is selected then draw the details.
            if (isSelected) {
                DrawDetails(rowRect, item.id);
            }

            if (EditorGUI.EndChangeCheck()) {
                // Serialize the changes.
                EditorUtility.SetDirty(m_AnimationStateSet);
                if (m_AfterModalChange != null) {
                    m_AfterModalChange();
                }
            }
        }

        /// <summary>
        /// Draws the background of the ItemType.
        /// </summary>
        /// <param name="rowRect">The rect to draw the background in.</param>
        /// <param name="isSelected">Is the current ItemType selected?</param>
        private void DrawBackground(Rect rowRect, bool isSelected) {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            var rect = rowRect;
            // If the row is selected then clamp the header background to the row height to prevent it from taking up the entire height.
            if (isSelected) {
                rect.height = c_RowHeight;
            }
            ItemTypeManager.TreeRowHeaderGUIStyle.Draw(rect, false, false, false, false);

            if (isSelected) {
                rect.y += rect.height;
                rect.height = rowRect.height - rect.height;
                ItemTypeManager.TreeRowBackgroundGUIStyle.Draw(rect, false, false, false, false);
            }
        }

        /// <summary>
        /// Draws the header of the ItemType.
        /// </summary>
        /// <param name="rowRect">The rect of the ItemType row.</param>
        /// <param name="id">The id of the ItemType to draw the header of.</param>
        private void DrawHeader(Rect rowRect, int id) {
            var rect = rowRect;
            rect.x += 4;
            rect.y += 4;
            GUI.Label(rect, m_AnimationStateSet.AnimationStates[id].name);
        }

        /// <summary>
        /// Draws the identify, duplicate and delete buttons for the ItemType.
        /// </summary>
        /// <param name="rowRect">The rect of the ItemType row.</param>
        /// <param name="id">The id of the ItemType to draw the controls of.</param>
        /// <returns>True if the controls changed the ItemCollection.</returns>
        private bool DrawControls(Rect rowRect, int id) {
            var identifyRect = rowRect;
            identifyRect.x = identifyRect.width - 68;
            identifyRect.width = 20;
            identifyRect.y += 4;
            identifyRect.height = 16;
            if (GUI.Button(identifyRect, InspectorStyles.InfoIcon, InspectorStyles.NoPaddingButtonStyle)) {
                Selection.activeObject = m_AnimationStateSet;
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            var duplicateRect = rowRect;
            duplicateRect.x = duplicateRect.width - 44;
            duplicateRect.width = 20;
            duplicateRect.y += 4;
            duplicateRect.height = 16;
            if (GUI.Button(duplicateRect, InspectorStyles.DuplicateIcon, InspectorStyles.NoPaddingButtonStyle)) {
                var itemType = m_AnimationStateSet.AnimationStates[id];
                var clonedItemType = new Inventory.AnimationState();
                // Generate a unique name for the item.
                var index = 1;
                string name;
                do {
                    name = itemType.name + " (" + index + ")";
                    index++;
                } while (!IsUniqueName(name));
                clonedItemType.name = name;

                // Add the ItemType to the ItemCollection.
                var itemTypes = m_AnimationStateSet.AnimationStates;
                Array.Resize(ref itemTypes, itemTypes.Length + 1);
                clonedItemType.ID = itemTypes.Length - 1;
                itemTypes[itemTypes.Length - 1] = clonedItemType;
                m_AnimationStateSet.AnimationStates = itemTypes;
                EditorUtility.SetDirty(m_AnimationStateSet);
                return true;
            }

            var deleteRect = rowRect;
            deleteRect.x = deleteRect.width - 20;
            deleteRect.width = 18;
            deleteRect.y += 4;
            deleteRect.height = 16;
            if (GUI.Button(deleteRect, InspectorStyles.DeleteIcon, InspectorStyles.NoPaddingButtonStyle)) {
                if (m_BeforeModalChange != null) {
                    m_BeforeModalChange();
                }

                // Remove the ItemType.
                var itemTypes = new List<Inventory.AnimationState>(m_AnimationStateSet.AnimationStates);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m_AnimationStateSet));
                itemTypes.RemoveAt(id);
                m_AnimationStateSet.AnimationStates = itemTypes.ToArray();
                EditorUtility.SetDirty(m_AnimationStateSet);

                // Update all of the ItemIDs.
                for (int i = 0; i < itemTypes.Count; ++i) {
                    m_AnimationStateSet.AnimationStates[i].ID = i;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Draws the details of the ItemType.
        /// </summary>
        /// <param name="rowRect">The rect of the ItemType row.</param>
        /// <param name="id">The id of the ItemType to draw the details of.</param>
        private void DrawDetails(Rect rowRect, int id) {
            var rect = rowRect;
            rect.x += 4;
            rect.y += c_RowHeight;

            var animSet = m_AnimationStateSet.AnimationStates[id];
            Undo.RecordObject(m_AnimationStateSet, "ItemType Change");

            // Name and description properties.
            var nameRect = rect;
            nameRect.y += 4;
            nameRect.width -= 12;
            nameRect.height = 16;

            // Prevent the label from being far away from the text.
            var name = InspectorUtility.DrawEditorWithoutSelectAll(() => InspectorUtility.ClampTextField(nameRect, "Name", animSet.name, 2));
            if (animSet.name != name) {
                if (IsUniqueName(name)) {
                    animSet.name = name;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m_AnimationStateSet));
                }
            }

            // Name and description properties.
            var idRect = rect;
            idRect.y += 24;
            idRect.width -= 12;
            idRect.height = 16;

            // Prevent the label from being far away from the text.
            var sid = InspectorUtility.DrawEditorWithoutSelectAll(() => InspectorUtility.ClampTextField(idRect, "ID", "" + animSet.ID, 2));
            int typeId = animSet.ID;
            if(!Int32.TryParse(sid, out typeId)) {
                typeId = animSet.ID;
            }
            if (animSet.ID != typeId) {
                animSet.ID = typeId;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(m_AnimationStateSet));
            }
        }

        /// <summary>
        /// Is the item type name unique?
        /// </summary>
        /// <param name="name">The name of the item type.</param>
        /// <returns>True if the item type is unique.</returns>
        public bool IsUniqueName(string name) {
            if (m_AnimationStateSet.AnimationStates == null) {
                return true;
            }
            for (int i = 0; i < m_AnimationStateSet.AnimationStates.Length; ++i) {
                if (m_AnimationStateSet.AnimationStates[i].name.ToLower().CompareTo(name.ToLower()) == 0) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Drops the Popup for the reference of the drop ItemType.
        /// </summary>
        private void DrawDropItemTypePopup(Rect rect, ItemType itemType, Dictionary<string, ItemType> nameItemTypeMap, ItemType droppedItemType, bool showLabel) {
            /* TODO: Figure out what drag and drop support we want to add.
             * var allItemTypeNames = new List<string>();
            allItemTypeNames.Add("(Add)");
            var prevSelected = 0;
            for (int i = 0; i < m_AnimationStateSet.AnimationStates.Length; ++i) {
                // The ItemType can always drop itself.
                if (m_AnimationStateSet.AnimationStates[i] == itemType) {
                    continue;
                }

                if (m_AnimationStateSet.AnimationStates[i] == droppedItemType) {
                    prevSelected = allItemTypeNames.Count;
                } else {
                    // Don't show any ItemTypes that are already included.
                    var containsItemType = false;
                    if (itemType.DroppedItemTypes != null) {
                        for (int j = 0; j < itemType.DroppedItemTypes.Length; ++j) {
                            if (m_AnimationStateSet.AnimationStates[i] == itemType.DroppedItemTypes[j]) {
                                containsItemType = true;
                                break;
                            }
                        }
                    }
                    if (containsItemType) {
                        continue;
                    }
                }

                allItemTypeNames.Add(m_AnimationStateSet.AnimationStates[i].name);
            }

            // Determine the index of the popup.
            if (droppedItemType != null) {
                for (int i = 1; i < allItemTypeNames.Count; ++i) {
                    if (allItemTypeNames[i] == droppedItemType.name) {
                        prevSelected = i;
                        break;
                    }
                }
            }

            var selected = InspectorUtility.ClampPopupField(rect, showLabel ? "Drop ItemTypes" : string.Empty, prevSelected, allItemTypeNames.ToArray(), 15);
            // A new selection has been made.
            if (prevSelected != selected) {
                var droppedItemTypes = itemType.DroppedItemTypes;
                if (droppedItemTypes == null) {
                    droppedItemTypes = new ItemType[0];
                }

                var droppedItemTypesList = new List<ItemType>(droppedItemTypes);
                // Remove the old.
                if (prevSelected != 0) {
                    droppedItemTypesList.Remove(nameItemTypeMap[allItemTypeNames[prevSelected]]);
                }
                // Add the new.
                if (selected != 0) {
                    droppedItemTypesList.Add(nameItemTypeMap[allItemTypeNames[selected]]);
                }
                itemType.DroppedItemTypes = droppedItemTypesList.ToArray();
            }

            // Remove the name from the list so tat ItemType name is no longer drawn.
            if (selected != 0) {
                allItemTypeNames.Remove(allItemTypeNames[selected]);
            }*/
        }

        /// <summary>
        /// Moves the rows to the specified index.
        /// </summary>
        /// <param name="rows">The rows being moved.</param>
        /// <param name="insertIndex">The index to insert the rows at.</param>
        /// <returns>An updated list of row ids.</returns>
        public override List<int> MoveRows(List<int> rows, int insertIndex) {
            if (m_BeforeModalChange != null) {
                m_BeforeModalChange();
            }

            var insertIDs = new List<int>();
            var itemTypes = m_AnimationStateSet.AnimationStates;
            // Move the rows in the array. This method will shift each rows without needing to allocate a new array for each move.
            for (int i = 0; i < rows.Count; ++i) {
                // Shift the array rows up to make space for the moved rows.
                if (insertIndex < rows[i]) {
                    var insertElement = itemTypes[rows[i]];
                    for (int j = rows[i]; j > insertIndex + i; --j) {
                        itemTypes[j] = itemTypes[j - 1];
                        itemTypes[j].ID = j;
                    }
                    itemTypes[insertIndex + i] = insertElement;
                    itemTypes[insertIndex + i].ID = insertIndex + i;
                } else {
                    // Shift the array rows down to make space for the moved rows.
                    insertIndex--;
                    var insertElement = itemTypes[rows[i] - i];
                    for (int j = rows[i] - i; j < insertIndex + i; ++j) {
                        itemTypes[j] = itemTypes[j + 1];
                        itemTypes[j].ID = j;
                    }
                    itemTypes[insertIndex + i] = insertElement;
                    itemTypes[insertIndex + i].ID = insertIndex + i;
                }
                insertIDs.Add(insertIndex + i);
            }
            return insertIDs;
        }

        /// <summary>
        /// Does the specified row id match the search?
        /// </summary>
        /// <param name="id">The id of the row.</param>
        /// <param name="searchString">The string value of the search.</param>
        /// <returns>True if the row matches the search string.</returns>
        public override bool MatchesSearch(int id, string searchString) {
            return m_AnimationStateSet.AnimationStates[id].name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Is the specified item selected?
        /// </summary>
        /// <param name="item">The item to test against.</param>
        /// <param name="state">The state of the TreeView.</param>
        /// <returns>True if the item is selected.</returns>
        private bool IsSelected(TreeViewItem item, TreeViewState state) {
            if (state.selectedIDs.Count > 0) {
                if (item.id == state.selectedIDs[0]) { // Only one row can be selected at a time.
                    return true;
                }
            }
            return false;
        }
    }
}