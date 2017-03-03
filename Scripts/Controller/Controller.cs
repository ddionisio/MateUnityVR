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
}