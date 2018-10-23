using System.Collections.Generic;
using UnityEditor.Animations;

namespace UCCExtensions {
    public class AnimatorGroup {
        private string m_Name;
        private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
        private SortedDictionary<string, ActionSet> m_Actions = new SortedDictionary<string, ActionSet>();
        private string[] m_ActionNames = new string[0];

        public string Name {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public SortedDictionary<string, AnimatorControllerLayer> Layers {
            get {
                return m_Layers;
            }
        }

        public SortedDictionary<string, ActionSet> Actions {
            get {
                return m_Actions;
            }
        }

        public void AddLayer(AnimatorControllerLayer layer) {
            m_Layers.Add(layer.name, layer);
        }

        public string[] ActionNames {
            get {
                if (m_ActionNames.Length != m_Layers.Count) {
                    m_ActionNames = CollectionUtil.UpdateNames(m_Actions.Keys);
                }
                return m_ActionNames;
            }

        }
    }
}
