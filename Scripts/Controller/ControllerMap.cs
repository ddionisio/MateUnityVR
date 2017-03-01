
namespace M8.VR {
    /// <summary>
    /// This is a generalized mapping of the VR buttons and axis to allow for porting to various devices via derivatives of InputManager (e.g. InputManagerSteamVR).
    /// </summary>
    [System.Flags]
    public enum ControlMap {
        None = 0x0,


    }
    
    public enum Hand {
        None = -1,

        Left,
        Right,

        Both,

        Any
    }
}