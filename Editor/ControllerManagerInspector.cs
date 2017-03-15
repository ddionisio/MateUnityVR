using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace M8.VR {
    [CustomEditor(typeof(ControllerManager))]
    public class ControllerManagerInspector : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();

            var dat = (ControllerManager)target;

            if(GUILayout.Button("Toggle Fallback"))
                dat.isFallback = !dat.isFallback;
        }
    }
}