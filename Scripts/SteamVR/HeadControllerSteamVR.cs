using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR Steam/Head Controller")]
    public class HeadControllerSteamVR : HeadController {
        public override bool isDeviceAvailable {
            get {
                return SteamVR.instance != null;
            }
        }
    }
}