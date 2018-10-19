/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEditor;
using UnityEngine;

namespace Opsive.UltimateCharacterController.Inventory
{
    /// <summary>
    /// The ItemCollection ScriptableObject is a container for the static item data.
    /// </summary>
    public class AnimationStateCollection : ScriptableObject {
        private const string c_ItemIdsName = "Item Ids";
        private const string c_ItemStateIndexesName = "Item State Indexes";
        private const string c_AbilityIndexesName = "Ability Indexes";

        [SerializeField] protected AnimationStateSet m_ItemIds;
        [SerializeField] protected AnimationStateSet m_ItemStateIndexes;
        [SerializeField] protected AnimationStateSet m_AbilityIndexes;

        private AnimationStateSet CreateStateSet(string name) {
            AnimationStateSet set = new AnimationStateSet();
            set.name = name;
            AssetDatabase.AddObjectToAsset(set, this);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return set;
        }

        private void Deserialize() {
            Debug.Log("Loading assets in " + AssetDatabase.GetAssetPath(this));
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                AssetDatabase.GetAssetPath(this));
            if (null != assets) {
                foreach (Object asset in assets) {
                    if (null != asset && !string.IsNullOrEmpty(asset.name)) {
                        switch (asset.name) {
                            case c_ItemIdsName:
                                m_ItemIds = m_ItemIds ?? asset as AnimationStateSet;
                                break;
                            case c_ItemStateIndexesName:
                                m_ItemStateIndexes = m_ItemStateIndexes ?? asset as AnimationStateSet;
                                break;
                            case c_AbilityIndexesName:
                                m_AbilityIndexes = m_AbilityIndexes ?? asset as AnimationStateSet;
                                break;
                        }
                    }
                }
            }

            if (m_ItemIds == null) {
                m_ItemIds = CreateStateSet(c_ItemIdsName);
            }

            if (m_ItemStateIndexes == null) {
                m_ItemStateIndexes = CreateStateSet(c_ItemStateIndexesName);
            }

            if (m_AbilityIndexes == null) {
                m_AbilityIndexes = CreateStateSet(c_AbilityIndexesName);
            }

            Debug.Log("Item id count: " + m_ItemIds.AnimationStates.Length);
        }

        public AnimationStateSet ItemIds {
            get {
                if(m_ItemIds == null) {
                    Deserialize();
                }
                return m_ItemIds;
            }
            set { m_ItemIds = value; }
        }

        public AnimationStateSet ItemStateIndexes {
            get {
                if (m_ItemStateIndexes == null) {
                    Deserialize();
                }
                return m_ItemStateIndexes; 
            }
            set { m_ItemStateIndexes = value; }
        }

        public AnimationStateSet AbilityIndexes {
            get {
                if (m_AbilityIndexes == null) {
                    Deserialize();
                }
                return m_AbilityIndexes; 
            }
            set { m_AbilityIndexes = value; }
        }
    }
}