using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("")]
    public abstract class Controller : MonoBehaviour {
        public enum Hand {
            None = -1,

            Left,
            Right,

            Any
        }

        public GameObject prefab;

        public Controller otherController;
        public Hand startingHand = Hand.Any;

        public GameObject instantiatedGO { get { return mPrefabInstance; } }

        public bool isDeviceAvailable { get { return mIsDeviceAvailable; } }

        public bool isInputFocus {
            get {
                return mIsInputFocus;
            }

            protected set {
                if(mIsInputFocus != value) {
                    mIsInputFocus = value;

                    if(inputFocusCallback != null)
                        inputFocusCallback(this);
                }
            }
        }

        public event System.Action<Controller> initializedCallback;
        public event System.Action<Controller> inputFocusCallback;

        protected bool mIsDeviceAvailable; //set this to true once you have acquired the specific device
        
        private GameObject mPrefabInstance;

        private bool mIsInputFocus;

        /// <summary>
        /// Determine which hand this controller is held at. If the Hand is set to Left or Right, that is returned.
        /// </summary>
        public Hand CurrentHand() {
            if(startingHand == Hand.Left || startingHand == Hand.Right) {
                return startingHand;
            }

            if(startingHand == Hand.Any && otherController && !otherController.isDeviceAvailable) {
                return Hand.Right;
            }

            if(!isDeviceAvailable || !otherController || !otherController.isDeviceAvailable) {
                return startingHand;
            }
            
            return GuessCurrentHand();
        }

        /// <summary>
        /// Get the world velocity of the VR Hand.
        /// Note: controller velocity value only updates on controller events (Button but and down) so good for throwing
        /// </summary>
        public abstract Vector3 GetTrackedVelocity();

        /// <summary>
        /// Get the world angular velocity of the VR Hand.
        /// Note: controller velocity value only updates on controller events (Button but and down) so good for throwing
        /// </summary>
        public abstract Vector3 GetTrackedAngularVelocity();

        /// <summary>
        /// Get the current axis. For 1D axis, use X
        /// </summary>
        public abstract Vector2 GetAxis(ControlMap axis);
        
        /// <summary>
        /// Check if given button is held down
        /// </summary>
        public abstract bool GetButtonDown(ControlMap button);

        /// <summary>
        /// Check if button was pressed from last frame
        /// </summary>
        public abstract bool GetButtonPressed(ControlMap button);

        /// <summary>
        /// Check if button was released from last frame
        /// </summary>
        public abstract bool GetButtonReleased(ControlMap button);

        protected virtual Hand GuessCurrentHand() {
            return Hand.Right;
        }

        /// <summary>
        /// Call this once you have obtained a device
        /// </summary>
        protected void Initialized() {
            if(mPrefabInstance)
                Object.Destroy(mPrefabInstance);

            mPrefabInstance = GameObject.Instantiate(prefab);
            mPrefabInstance.SetActive(true);
            mPrefabInstance.name = prefab.name + "_" + name;
            mPrefabInstance.transform.SetParent(transform, false);

            mPrefabInstance.transform.localPosition = Vector3.zero;
            mPrefabInstance.transform.localRotation = Quaternion.identity;
            mPrefabInstance.transform.localScale = prefab.transform.localScale;

            //controller.TriggerHapticPulse(800);

            if(initializedCallback != null)
                initializedCallback(this);
        }
    }

#if UNITY_EDITOR
    //-------------------------------------------------------------------------
    [UnityEditor.CustomEditor(typeof(Controller), true)]
    public class ControllerInspector : UnityEditor.Editor {
        //-------------------------------------------------
        // Custom Inspector GUI allows us to click from within the UI
        //-------------------------------------------------
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            Controller ctrl = (Controller)target;

            if(ctrl.otherController) {
                if(ctrl.otherController.otherController != ctrl) {
                    UnityEditor.EditorGUILayout.HelpBox("The otherController of this Controller's otherController is not this Controller.", UnityEditor.MessageType.Warning);
                }

                if(ctrl.startingHand == Controller.Hand.Left && ctrl.otherController.startingHand != Controller.Hand.Right) {
                    UnityEditor.EditorGUILayout.HelpBox("This is a left Hand but otherController is not a right Hand.", UnityEditor.MessageType.Warning);
                }

                if(ctrl.startingHand == Controller.Hand.Right && ctrl.otherController.startingHand != Controller.Hand.Left) {
                    UnityEditor.EditorGUILayout.HelpBox("This is a right Hand but otherController is not a left Hand.", UnityEditor.MessageType.Warning);
                }

                if(ctrl.startingHand == Controller.Hand.Any && ctrl.otherController.startingHand != Controller.Hand.Any) {
                    UnityEditor.EditorGUILayout.HelpBox("This is an any-handed Hand but otherController is not an any-handed Hand.", UnityEditor.MessageType.Warning);
                }
            }
        }
    }
#endif
}