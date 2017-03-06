using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("M8/VR Steam/Head")]
    public class HeadSteamVR : Head {
        public override bool isDeviceAvailable {
            get {
                return SteamVR.instance != null;
            }
        }
    }
}