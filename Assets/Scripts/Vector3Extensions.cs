using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    ///<summary>Can adjust single values of the Vector3 via... myVector3.With(x: 5);</summary>
    public static Vector3 With(this Vector3 original, float? x = null, float? y = null, float? z = null)
    {
        return new Vector3(x ?? original.x, y ?? original.y, z ?? original.z);
    }

    ///<summary>Returns Vector3 but no Y</summary>
    public static Vector3 Flat(this Vector3 original)
    {
        return new Vector3(original.x, 0, original.z);
    }

    ///<summary>Returns a directional vector to a target</summary>
    public static Vector3 DirectionTo(this Vector3 source, Vector3 destination)
    {
        return Vector3.Normalize(destination - source);
    }
}
