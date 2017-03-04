
namespace M8.VR {
    /// <summary>
    /// This is a generalized mapping of the VR buttons and axis to allow for porting to various devices via derivatives of InputManager (e.g. InputManagerSteamVR).
    /// </summary>
    public enum ControlMap {
        None = -1,

        System, //This is overriden by Valve to open up Steam

        Menu,
                
        Touchpad, //Button, 2D Axis [-1, 1]

        Trigger, //Button, 1D Axis [0, 1]

        Grip,

        //dpad?
    }    
}