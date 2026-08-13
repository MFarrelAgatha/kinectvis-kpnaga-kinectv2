using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector drawer for fields tagged with [KeyBind].
/// Replaces Unity's bulky KeyCode dropdown with a single button that opens a small
/// key-capture popup window, exactly like Unity's own "rebind shortcut" UI.
/// </summary>
[CustomPropertyDrawer(typeof(KeyBindAttribute))]
public class KeyBindDrawer : PropertyDrawer
{
    private const string BindPrompt = "Click to Bind Key";
    private const string ClearPrompt = "X";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // KeyCode is a C# enum, so Unity reports its SerializedPropertyType as Enum.
        // Its underlying storage is an int, which we read/write through property.intValue.
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        KeyCode currentKey = (KeyCode)property.intValue;

        // Property label (left side).
        EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

        // Reserve space for a small clear ("X") button when a key is bound.
        float clearWidth = (currentKey != KeyCode.None) ? 18f : 0f;
        Rect buttonRect = new Rect(
            position.x + EditorGUIUtility.labelWidth,
            position.y,
            position.width - EditorGUIUtility.labelWidth - clearWidth - 2f,
            position.height);

        if (GUI.Button(buttonRect, currentKey == KeyCode.None ? BindPrompt : currentKey.ToString(), EditorStyles.miniButton))
        {
            // Capture the target object and property path so the popup's callback can
            // safely re-resolve and write the chosen key once the popup closes.
            Object target = property.serializedObject.targetObject;
            string propertyPath = property.propertyPath;

            KeyBindPopup.Show(key =>
            {
                SerializedObject so = new SerializedObject(target);
                SerializedProperty sp = so.FindProperty(propertyPath);
                if (sp != null && sp.propertyType == SerializedPropertyType.Enum)
                {
                    sp.intValue = (int)key;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                }
            });
        }

        // Manual reset button so a bound key can always be cleared with a click.
        if (currentKey != KeyCode.None)
        {
            Rect clearRect = new Rect(buttonRect.xMax + 2f, position.y, clearWidth, position.height);
            if (GUI.Button(clearRect, ClearPrompt, EditorStyles.miniButton))
            {
                property.intValue = (int)KeyCode.None;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
