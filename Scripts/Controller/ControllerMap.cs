
namespace M8.VR {
    /// <summary>
    /// This is a generalized mapping of the VR buttons and axis to allow for porting to various devices via derivatives of InputManager (e.g. InputManagerSteamVR).
    /// </summary>
    public enum ControlMap {
        None = -1,

        Menu,

        System, //This is overriden by Valve to open up Steam

        TrackpadPress,
        TrackpadTouch,
        TrackpadX, //Axis X [-1, 1]
        TrackpadY, //Axis Y [-1, 1]

        TriggerPress,
        Trigger, //Axis [0, 1]

        Grip
    }
}