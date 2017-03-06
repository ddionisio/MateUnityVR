using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.VR {
    /// <summary>
    /// Used during Controller.Initialized for all MonoBehaviour within the hierarchy
    /// </summary>
    public interface IControllerInitialize {
        void OnInitialized(Controller ctrl);
    }
}