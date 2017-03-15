using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    [AddComponentMenu("")]
    public abstract class HeadController : MonoBehaviour {
        /// <summary>
        /// Is the HMD available
        /// </summary>
        public abstract bool isDeviceAvailable { get; }
    }
}