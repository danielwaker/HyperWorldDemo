using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameKey {
    FORWARD,
    BACKWARD,
    LEFT,
    RIGHT,
    ACTION,
    JUMP,
    MAP_TOGGLE,
    PROJECTION_CHANGE,
    MENU,
    DEBUG1,
    DEBUG2,
}

public enum GameAxis {
    LOOK_HORIZONTAL,
    LOOK_VERTICAL,
    MOVE_SIDEWAYS,
    MOVE_FORWARDS,
}

public class InputManager {
    public static Dictionary<GameKey, KeyCode> keyMapping = new Dictionary<GameKey, KeyCode> {
        { GameKey.FORWARD, KeyCode.W },
        { GameKey.BACKWARD, KeyCode.S },
        { GameKey.LEFT, KeyCode.A },
        { GameKey.RIGHT, KeyCode.D },
        { GameKey.ACTION, KeyCode.E },
        { GameKey.JUMP, KeyCode.Space },
        { GameKey.MAP_TOGGLE, KeyCode.Q },
        { GameKey.PROJECTION_CHANGE, KeyCode.F },
        { GameKey.MENU, KeyCode.Escape },
        { GameKey.DEBUG1, KeyCode.Alpha0 },
        { GameKey.DEBUG2, KeyCode.Alpha1 },
    };
    public static Dictionary<GameAxis, string> axisMapping = new Dictionary<GameAxis, string> {
        { GameAxis.LOOK_HORIZONTAL, "Mouse X" },
        { GameAxis.LOOK_VERTICAL, "Mouse Y" },
        { GameAxis.MOVE_SIDEWAYS, "Horizontal" },
        { GameAxis.MOVE_FORWARDS, "Vertical" },
    };

    public static bool GetKey(GameKey key) {
        if (keyMapping.ContainsKey(key)) {
            return Input.GetKey(keyMapping[key]);
        } else {
            return false;
        }
    }

    public static bool GetKeyDown(GameKey key) {
        if (keyMapping.ContainsKey(key)) {
            return Input.GetKeyDown(keyMapping[key]);
        } else {
            return false;
        }
    }

    public static bool GetKeyUp(GameKey key) {
        if (keyMapping.ContainsKey(key)) {
            return Input.GetKeyUp(keyMapping[key]);
        } else {
            return false;
        }
    }

    public static float GetAxis(GameAxis axis) {
        if (axisMapping.ContainsKey(axis)) {
            return Input.GetAxisRaw(axisMapping[axis]);
        } else {
            return 0.0f;
        }
    }
}
