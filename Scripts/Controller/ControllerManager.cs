//=============================================================================
// Based on parts of Player.cs
// Copyright (c) Valve Corporation, All rights reserved.
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR/ControllerManager")]
    public class ControllerManager : SingletonBehaviour<ControllerManager> {
        [Tooltip("Virtual transform corresponding to the meatspace tracking origin. Devices are tracked relative to this.")]
        public Transform trackingOrigin;

        [Tooltip("List of possible Heads, including the no-SteamVR fallback.")]
        public HeadController[] headControllers;

        public bool headEnableFallback = true;

        [Tooltip("List of possible Controllers, including no-SteamVR fallback Controllers.")]
        public Controller[] controllers;

        [Tooltip("These objects are enabled when VR is available")]
        public GameObject rigVR;

        [Tooltip("These objects are enabled when VR is not available, or when the user toggles out of VR")]
        public GameObject rigFallback;

        [Tooltip("This is attached to the active head. Should include the audio listener and collider.")]
        public Transform attach;
        
        /// <summary>
        /// Grab currently active head mount display
        /// </summary>
        public HeadController activeHeadController {
            get {
                for(int i = 0; i < headControllers.Length; i++) {
                    if(headControllers[i].gameObject.activeInHierarchy)
                        return headControllers[i];
                }
                return null;
            }
        }

        /// <summary>
        /// Height of the eyes above the ground - useful for estimating player height. 
        /// </summary>
        public float eyeHeight {
            get {
                var hmd = activeHeadController;
                if(hmd) {
                    Vector3 eyeOffset = Vector3.Project(hmd.transform.position - trackingOrigin.position, trackingOrigin.up);
                    return eyeOffset.magnitude / trackingOrigin.lossyScale.x;
                }
                return 0.0f;
            }
        }

        /// <summary>
        /// Guess for the world-space position of the player's feet, directly beneath the HMD.
        /// </summary>
        public Vector3 feetPositionGuess {
            get {
                var hmd = activeHeadController;
                if(hmd) {
                    return trackingOrigin.position + Vector3.ProjectOnPlane(hmd.transform.position - trackingOrigin.position, trackingOrigin.up);
                }
                return trackingOrigin.position;
            }
        }

        /// <summary>
        /// Guess for the world-space direction of the player's hips/torso. This is effectively just the gaze direction projected onto the floor plane.
        /// </summary>
        public Vector3 bodyDirectionGuess {
            get {
                var hmd = activeHeadController;
                if(hmd) {
                    var hmdT = hmd.transform;
                    Vector3 direction = Vector3.ProjectOnPlane(hmdT.forward, trackingOrigin.up);
                    if(Vector3.Dot(hmdT.up, trackingOrigin.up) < 0.0f) {
                        // The HMD is upside-down. Either
                        // -The player is bending over backwards
                        // -The player is bent over looking through their legs
                        direction = -direction;
                    }
                    return direction;
                }
                return trackingOrigin.forward;
            }
        }

        public bool isFallback {
            get { return rigFallback && rigFallback.activeSelf; }
            set {
                if(value)
                    ActivateRig(rigFallback);
                else
                    ActivateRig(rigVR);
            }
        }

        /// <summary>
        /// Get the number of active Controllers.
        /// </summary>
        public int activeControllerCount {
            get {
                int count = 0;
                for(int i = 0; i < controllers.Length; i++) {
                    if(controllers[i].gameObject.activeInHierarchy) {
                        count++;
                    }
                }
                return count;
            }
        }
        
        //-------------------------------------------------
        public Controller leftHand {
            get {
                for(int j = 0; j < controllers.Length; j++) {
                    if(!controllers[j].gameObject.activeInHierarchy) {
                        continue;
                    }

                    if(controllers[j].CurrentHand() != Controller.Hand.Left) {
                        continue;
                    }

                    return controllers[j];
                }

                return null;
            }
        }


        //-------------------------------------------------
        public Controller rightHand {
            get {
                for(int j = 0; j < controllers.Length; j++) {
                    if(!controllers[j].gameObject.activeInHierarchy) {
                        continue;
                    }

                    if(controllers[j].CurrentHand() != Controller.Hand.Right) {
                        continue;
                    }

                    return controllers[j];
                }

                return null;
            }
        }

        /// <summary>
        /// Get the i-th active Controller. Starting at 0
        /// </summary>
        public Controller GetActive(int i) {
            for(int j = 0; j < controllers.Length; j++) {
                if(!controllers[j].gameObject.activeInHierarchy) {
                    continue;
                }

                if(i > 0) {
                    i--;
                    continue;
                }

                return controllers[j];
            }

            return null;
        }

        public Controller GetActiveByDeviceID(int id) {
            for(int i = 0; i < controllers.Length; i++) {
                if(!controllers[i].isDeviceAvailable)
                    continue;

                if(!controllers[i].gameObject.activeInHierarchy)
                    continue;

                if(controllers[i].deviceID == id)
                    return controllers[i];
            }

            return null;
        }
        
        protected override void OnInstanceDeinit() {
            
        }

        protected override void OnInstanceInit() {
            if(!trackingOrigin)
                trackingOrigin = transform;
        }

        void OnEnable() {
            bool isRigVRActivated = false;

            //check for valid head device, only activate rigVR if the valid head is in its hierarchy
            for(int i = 0; i < headControllers.Length; i++) {
                if(headControllers[i] && headControllers[i].isDeviceAvailable) {
                    if(IsInHierarchy(rigVR.transform, headControllers[i].transform)) {                        
                        ActivateRig(rigVR);
                        isRigVRActivated = true;
                        break;
                    }   
                }
            }

            //fallback
            if(!isRigVRActivated && headEnableFallback)
                ActivateRig(rigFallback);
        }

        private void ActivateRig(GameObject rig) {
            if(rigVR) rigVR.SetActive(rig == rigVR);
            if(rigFallback) rigFallback.SetActive(rig == rigFallback);

            if(attach) {
                var hmd = activeHeadController;
                attach.parent = hmd ? hmd.transform : null;
                attach.localPosition = Vector3.zero;
                attach.localRotation = Quaternion.identity;
            }
        }

        private bool IsInHierarchy(Transform root, Transform check) {
            if(check == root)
                return true;

            var parent = check.parent;

            if(parent == root)
                return true;

            if(!parent)
                return false;

            return IsInHierarchy(root, parent);
        }
    }
}