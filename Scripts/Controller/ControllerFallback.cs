//=============================================================================
// Based on parts of Hand.cs
// Copyright (c) Valve Corporation, All rights reserved.
//=============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR/Controller Fallback")]
    public class ControllerFallback : Controller {
        public Camera noSteamVRFallbackCamera;

        public LayerMask traceMask;

        public float noSteamVRFallbackMaxDistanceNoItem = 10.0f;
        public float noSteamVRFallbackMaxDistanceWithItem = 0.5f;

        public int attachCounter {
            get { return mAttachCounter; }
            set { mAttachCounter = value; }
        }

        private float noSteamVRFallbackInteractorDistance = -1.0f;
        
        private int mAttachCounter;

        private Vector3 mLastPosition;

        public override Vector3 GetTrackedVelocity() {
            return transform.position - mLastPosition;
        }
        
        public override Vector3 GetTrackedAngularVelocity() {
            return Vector3.zero;
        }
        
        public override Vector2 GetAxis(ControlMap axis) {
            //TODO: emulate certain axis if needed
            return Vector2.zero;
        }

        public override bool GetButtonDown(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return false;

                    case ControlMap.Menu:
                        return false;

                    case ControlMap.Touchpad:
                        return Input.GetMouseButton(2);

                    case ControlMap.Trigger:
                        return Input.GetMouseButton(0);

                    case ControlMap.Grip:
                        return Input.GetMouseButton(1);
                }
            }

            return false;
        }

        public override bool GetButtonPressed(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return false;

                    case ControlMap.Menu:
                        return false;

                    case ControlMap.Touchpad:
                        return Input.GetMouseButtonDown(2);

                    case ControlMap.Trigger:
                        return Input.GetMouseButtonDown(0);

                    case ControlMap.Grip:
                        return Input.GetMouseButtonDown(1);
                }
            }

            return false;
        }

        public override bool GetButtonReleased(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return false;

                    case ControlMap.Menu:
                        return false;

                    case ControlMap.Touchpad:
                        return Input.GetMouseButtonUp(2);

                    case ControlMap.Trigger:
                        return Input.GetMouseButtonUp(0);

                    case ControlMap.Grip:
                        return Input.GetMouseButtonUp(1);
                }
            }

            return false;
        }

        void Update() {
            if(noSteamVRFallbackCamera) {
                mLastPosition = transform.position;
                
                Ray ray = noSteamVRFallbackCamera.ScreenPointToRay(Input.mousePosition);
                                
                if(mAttachCounter > 0) {
                    // Holding down the mouse:
                    // move around a fixed distance from the camera
                    transform.position = ray.origin + noSteamVRFallbackInteractorDistance * ray.direction;
                }
                else {
                    // Not holding down the mouse:
                    // cast out a ray to see what we should mouse over

                    // Don't want to hit the hand and anything underneath it
                    // So move it back behind the camera when we do the raycast                    
                    transform.position = noSteamVRFallbackCamera.transform.forward * (-1000.0f);

                    RaycastHit raycastHit;
                    if(Physics.Raycast(ray, out raycastHit, noSteamVRFallbackMaxDistanceNoItem, traceMask)) {
                        transform.position = raycastHit.point;

                        // Remember this distance in case we click and drag the mouse
                        noSteamVRFallbackInteractorDistance = Mathf.Min(noSteamVRFallbackMaxDistanceNoItem, raycastHit.distance);
                    }
                    else if(noSteamVRFallbackInteractorDistance > 0.0f) {
                        // Move it around at the distance we last had a hit
                        transform.position = ray.origin + Mathf.Min(noSteamVRFallbackMaxDistanceNoItem, noSteamVRFallbackInteractorDistance) * ray.direction;
                    }
                    else {
                        // Didn't hit, just leave it where it was
                        transform.position = mLastPosition;
                    }
                }
            }
        }
    }
}