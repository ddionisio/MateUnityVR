using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR Steam/Controller")]
    public class ControllerSteamVR : Controller {
        
        public SteamVR_Controller.Device device { get { return mDevice; } }

        
        private SteamVR_Controller.Device mDevice;

        private SteamVR_Events.Action mInputFocusAction;

        public override Vector3 GetTrackedVelocity() {
            if(mIsDeviceAvailable) {
                return transform.parent.TransformVector(mDevice.velocity);
            }

            return Vector3.zero;
        }
        
        public override Vector3 GetTrackedAngularVelocity() {
            if(mIsDeviceAvailable) {
                return transform.parent.TransformVector(mDevice.angularVelocity);
            }

            return Vector3.zero;
        }
        
        public override Vector2 GetAxis(ControlMap axis) {
            if(mIsDeviceAvailable) {
                switch(axis) {
                    case ControlMap.Touchpad:
                        return mDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);

                    case ControlMap.Trigger:
                        return mDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis1);
                }
            }

            return Vector2.zero;
        }
        
        public override bool GetButtonDown(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return mDevice.GetPress(SteamVR_Controller.ButtonMask.System);

                    case ControlMap.Menu:
                        return mDevice.GetPress(SteamVR_Controller.ButtonMask.ApplicationMenu);

                    case ControlMap.Touchpad:
                        return mDevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad);

                    case ControlMap.Trigger:
                        return mDevice.GetHairTrigger();

                    case ControlMap.Grip:
                        return mDevice.GetPress(SteamVR_Controller.ButtonMask.Grip);
                }
            }

            return false;
        }
        
        public override bool GetButtonPressed(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return mDevice.GetPressDown(SteamVR_Controller.ButtonMask.System);

                    case ControlMap.Menu:
                        return mDevice.GetPressDown(SteamVR_Controller.ButtonMask.ApplicationMenu);

                    case ControlMap.Touchpad:
                        return mDevice.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad);

                    case ControlMap.Trigger:
                        return mDevice.GetHairTriggerDown();

                    case ControlMap.Grip:
                        return mDevice.GetPressDown(SteamVR_Controller.ButtonMask.Grip);
                }
            }

            return false;
        }
        
        public override bool GetButtonReleased(ControlMap button) {
            if(mIsDeviceAvailable) {
                switch(button) {
                    case ControlMap.System:
                        return mDevice.GetPressUp(SteamVR_Controller.ButtonMask.System);

                    case ControlMap.Menu:
                        return mDevice.GetPressUp(SteamVR_Controller.ButtonMask.ApplicationMenu);

                    case ControlMap.Touchpad:
                        return mDevice.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad);

                    case ControlMap.Trigger:
                        return mDevice.GetHairTriggerUp();

                    case ControlMap.Grip:
                        return mDevice.GetPressUp(SteamVR_Controller.ButtonMask.Grip);
                }
            }

            return false;
        }

        protected override Hand GuessCurrentHand() {
            if(mDevice.index == SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost)) {
                return Hand.Left;
            }

            return Hand.Right;
        }

        void OnEnable() {
            mInputFocusAction.enabled = true;
        }

        void OnDisable() {
            mInputFocusAction.enabled = false;
        }

        void Awake() {
            mInputFocusAction = SteamVR_Events.InputFocusAction(OnInputFocus);
        }

        IEnumerator Start() {
            // Acquire the correct device index for the hand we want to be
            // Also for the other hand if we get there first
            while(true) {
                // Don't need to run this every frame
                yield return new WaitForSeconds(1.0f);

                // We have a controller now, break out of the loop!
                if(mIsDeviceAvailable)
                    break;

                //Debug.Log( "Hand - checking controllers..." );

                // Initialize both hands simultaneously
                if(startingHand == Hand.Left || startingHand == Hand.Right) {
                    // Left/right relationship.
                    // Wait until we have a clear unique left-right relationship to initialize.
                    int leftIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
                    int rightIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
                    if(leftIndex == -1 || rightIndex == -1 || leftIndex == rightIndex) {
                        //Debug.Log( string.Format( "...Left/right hand relationship not yet established: leftIndex={0}, rightIndex={1}", leftIndex, rightIndex ) );
                        continue;
                    }

                    int myIndex = (startingHand == Hand.Right) ? rightIndex : leftIndex;
                    int otherIndex = (startingHand == Hand.Right) ? leftIndex : rightIndex;

                    InitController(myIndex);                    
                    if(otherController) {
                        var otherSteamController = otherController as ControllerSteamVR;
                        if(otherSteamController)
                            otherSteamController.InitController(otherIndex);
                        else
                            Debug.LogWarning("Other Controller: "+otherController.name+" is not ControllerSteamVR");
                    }
                }
                else {
                    // No left/right relationship. Just wait for a connection

                    var vr = SteamVR.instance;
                    for(int i = 0; i < Valve.VR.OpenVR.k_unMaxTrackedDeviceCount; i++) {
                        if(vr.hmd.GetTrackedDeviceClass((uint)i) != Valve.VR.ETrackedDeviceClass.Controller) {
                            //Debug.Log( string.Format( "Hand - device {0} is not a controller", i ) );
                            continue;
                        }

                        var _device = SteamVR_Controller.Input(i);
                        if(!_device.valid) {
                            //Debug.Log( string.Format( "Hand - device {0} is not valid", i ) );
                            continue;
                        }

                        if(otherController && otherController.isDeviceAvailable) {
                            // Other hand is using this index, so we cannot use it.
                            var otherSteamController = otherController as ControllerSteamVR;
                            if(otherSteamController) {
                                // Other hand is using this index, so we cannot use it.
                                if(i == (int)otherSteamController.mDevice.index) {
                                    //Debug.Log( string.Format( "Hand - device {0} is owned by the other hand", i ) );
                                    continue;
                                }
                            }
                        }

                        InitController(i);
                    }
                }
            }
        }

        void FixedUpdate() {
            UpdateHandPoses();
        }

        private void InitController(int index) {
            if(mDevice == null) {
                mDevice = SteamVR_Controller.Input(index);
                mIsDeviceAvailable = true;
                
                Initialized();
            }
        }

        private void UpdateHandPoses() {
            if(mIsDeviceAvailable) {
                SteamVR vr = SteamVR.instance;
                if(vr != null) {
                    var pose = new Valve.VR.TrackedDevicePose_t();
                    var gamePose = new Valve.VR.TrackedDevicePose_t();
                    var err = vr.compositor.GetLastPoseForTrackedDeviceIndex(mDevice.index, ref pose, ref gamePose);
                    if(err == Valve.VR.EVRCompositorError.None) {
                        var t = new SteamVR_Utils.RigidTransform(gamePose.mDeviceToAbsoluteTracking);
                        transform.localPosition = t.pos;
                        transform.localRotation = t.rot;
                    }
                }
            }
        }

        void OnInputFocus(bool hasFocus) {
            isInputFocus = hasFocus;

            if(isInputFocus) {
                UpdateHandPoses();
            }
        }
    }
}