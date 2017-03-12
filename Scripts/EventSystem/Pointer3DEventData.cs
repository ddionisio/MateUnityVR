using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

namespace M8.VR.EventSystems {
    public class Pointer3DEventData : PointerEventData {
        /// <summary>
        /// if this is valid, 2D telemetry will be mapped to this
        /// </summary>
        public Canvas canvas;
        
        /// <summary>
        /// This is the delta in world space from collision, relative to raycast hit
        /// </summary>
        public Vector3 worldDelta;
        
        public new bool IsPointerMoving() {
            if(canvas)
                return delta.sqrMagnitude > 0.0f;

            return worldDelta.sqrMagnitude > 0.0f;
        }

        public Pointer3DEventData(EventSystem eventSystem) : base(eventSystem) {

        }
        
    }
}