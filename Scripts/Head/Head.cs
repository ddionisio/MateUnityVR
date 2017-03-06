//=============================================================================
// Based on parts of Player.cs
// Copyright (c) Valve Corporation, All rights reserved.
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    /// <summary>
    /// Essentially anything to do with the head mount display, along with some telemetry features
    /// </summary>
    [AddComponentMenu("")]
    public abstract class Head : SingletonBehaviour<Head> {
        [Tooltip("Virtual transform corresponding to the meatspace tracking origin. Devices are tracked relative to this.")]
        public Transform trackingOrigin;

        [Tooltip("List of possible transforms for the head/HMD, including the no-SteamVR fallback camera.")]
        public Transform[] hmdTransforms;

        [Tooltip("These objects are enabled when VR is available")]
        public GameObject rigVR;

        [Tooltip("These objects are enabled when VR is not available, or when the user toggles out of VR")]
        public GameObject rigFallback;

        [Tooltip("This is attached to the active head. Should include the audio listener and collider.")]
        public Transform attach;

        public bool enableFallbackIfNoDevice;

        /// <summary>
        /// Grab currently active head mount display
        /// </summary>
        public Transform hmdTransform {
            get {
                for(int i = 0; i < hmdTransforms.Length; i++) {
                    if(hmdTransforms[i].gameObject.activeInHierarchy)
                        return hmdTransforms[i];
                }
                return null;
            }
        }
        
        /// <summary>
        /// Height of the eyes above the ground - useful for estimating player height. 
        /// </summary>
        public float eyeHeight {
            get {
                Transform hmd = hmdTransform;
                if(hmd) {
                    Vector3 eyeOffset = Vector3.Project(hmd.position - trackingOrigin.position, trackingOrigin.up);
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
                Transform hmd = hmdTransform;
                if(hmd) {
                    return trackingOrigin.position + Vector3.ProjectOnPlane(hmd.position - trackingOrigin.position, trackingOrigin.up);
                }
                return trackingOrigin.position;
            }
        }
                
        /// <summary>
        /// Guess for the world-space direction of the player's hips/torso. This is effectively just the gaze direction projected onto the floor plane.
        /// </summary>
        public Vector3 bodyDirectionGuess {
            get {
                Transform hmd = hmdTransform;
                if(hmd) {
                    Vector3 direction = Vector3.ProjectOnPlane(hmd.forward, trackingOrigin.up);
                    if(Vector3.Dot(hmd.up, trackingOrigin.up) < 0.0f) {
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
        /// Is the HMD available
        /// </summary>
        public abstract bool isDeviceAvailable { get; }

        void OnEnable() {
            if(isDeviceAvailable) {
                ActivateRig(rigVR);
            }
            else {
                if(enableFallbackIfNoDevice)
                    ActivateRig(rigFallback);
            }
        }

        protected override void OnInstanceDeinit() {
            if(!trackingOrigin)
                trackingOrigin = transform;
        }

        protected override void OnInstanceInit() {

        }

        private void ActivateRig(GameObject rig) {
            rigVR.SetActive(rig == rigVR);
            rigFallback.SetActive(rig == rigFallback);

            if(attach) {
                attach.parent = hmdTransform;
                attach.localPosition = Vector3.zero;
                attach.localRotation = Quaternion.identity;
            }
        }
    }
}