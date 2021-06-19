using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextAreaScript)), CanEditMultipleObjects]
public class TextAreaEditor : Editor
{

    public SerializedProperty longStringProp;
    void OnEnable()
    {
        longStringProp = serializedObject.FindProperty("longString");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        longStringProp.stringValue = EditorGUILayout.TextArea(longStringProp.stringValue,
            GUILayout.MaxHeight(75));
        serializedObject.ApplyModifiedProperties();
    }
}