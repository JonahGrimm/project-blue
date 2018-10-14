using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CombatModule))]
public class CombatModuleEditor : Editor
{/*
    public void ShowArrayProperty(SerializedProperty list)
    {
        EditorGUILayout.PropertyField(list);

        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(0), new GUIContent("Jab".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(1), new GUIContent("Ftilt".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(2), new GUIContent("Utilt".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(3), new GUIContent("Dtilt".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(4), new GUIContent("Dashattack".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(5), new GUIContent("Nair".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(6), new GUIContent("Fair".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(7), new GUIContent("Bair".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(8), new GUIContent("Uair".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(9), new GUIContent("Dair".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(10), new GUIContent("Fcharge".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(11), new GUIContent("Ucharge".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(12), new GUIContent("Dcharge".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(13), new GUIContent("Nspecial".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(14), new GUIContent("Fpecial".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(15), new GUIContent("Uspecial".ToString()));
        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(16), new GUIContent("Dspecial".ToString()));

    }

    public override void OnInspectorGUI()
    {
        ShowArrayProperty(serializedObject.FindProperty("activeAttacks"));
    }*/
}
