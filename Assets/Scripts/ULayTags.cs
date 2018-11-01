using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A general static class meant to hold all layers and tags that will be used throughout the project.
/// </summary>
public static class ULayTags
{
    public static int environmentLayer;
    public static int entityLayer;
    public static int cuttableLayers;
    public static string environmentTag = "Environment";
    public static string playerTag = "Player";
    public static string enemyTag = "Enemy";

    static ULayTags()
    {
        environmentLayer = LayerMask.GetMask("Environment");
        entityLayer = LayerMask.GetMask("Entity");
        cuttableLayers = LayerMask.GetMask("Environment");

        //Debug.Log("EntityLayer: " + entityLayer);
        //Debug.Log("EnvironmentLayer: " + environmentLayer);
        //Debug.Log("CuttableLayer: " + cuttableLayers);
    }
}
