using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    /// <summary>
    /// Put this on the spawn prefab of the Controller, or within the Controller hierarchy
    /// </summary>
    [AddComponentMenu("M8/VR/ControllerHideOnFocusLost")]
    public class ControllerHideOnFocusLost : MonoBehaviour, IControllerInitialize {
        public Controller controller;
        public GameObject target;

        void OnDestroy() {
            if(controller)
                controller.inputFocusCallback -= OnInputFocus;
        }

        void Awake() {
            if(!target)
                target = gameObject;

            if(controller) {
                controller.inputFocusCallback += OnInputFocus;
            }
        }

        void OnInputFocus(Controller ctrl) {
            target.SetActive(ctrl.isInputFocus);
        }

        void IControllerInitialize.OnInitialized(Controller ctrl) {
            if(controller) //already obtained and initialized
                return;

            controller = ctrl;

            controller.inputFocusCallback += OnInputFocus;
        }
    }
}