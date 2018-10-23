using System.Collections.Generic;
using UnityEditor.Animations;

namespace UCCExtensions {
    public class ActionSet {
        private string m_Name;
        private List<CombinedAnimatorState> m_States = new List<CombinedAnimatorState>();
        private SortedDictionary<string, AnimatorControllerLayer> m_Layers = new SortedDictionary<string, AnimatorControllerLayer>();
        private string[] m_StateNames = new string[0];

        // This will map to something like
        // Bow (AnimatorGroup) -> Draw (Action) -> Full Body Layer (State)
        private SortedDictionary<string, CombinedAnimatorState> m_LayerToStates = new SortedDictionary<string, CombinedAnimatorState>();

        public string Name {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public List<CombinedAnimatorState> States {
            get { return m_States; }
        }

        public SortedDictionary<string, AnimatorControllerLayer> Layers {
            get {
                return m_Layers;
            }
        }
    }
}
