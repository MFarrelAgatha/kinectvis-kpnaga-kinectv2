using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Inspector drawer for fields tagged with [KeyBind].
/// Replaces Unity's bulky KeyCode dropdown with a single button that captures the next
/// key press, exactly like a "rebind key" menu in a game's settings screen.
/// </summary>
[CustomPropertyDrawer(typeof(KeyBindAttribute))]
public class KeyBindDrawer : PropertyDrawer
{
    // Only one field may listen at a time. We track the field currently waiting for a
    // key press by its target object and serialized property path (no instance-id hashing).
    private static Object listeningTarget;
    private static string listeningPath;

    private const string BindPrompt = "Click to Bind Key";
    private const string ListeningPrompt = "Press a key... (Esc to cancel)";
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

        bool isListening = IsListening(property);
        KeyCode currentKey = (KeyCode)property.intValue;

        // Property label (left side).
        EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

        // Reserve space for a small clear ("X") button when a key is bound.
        float clearWidth = (!isListening && currentKey != KeyCode.None) ? 18f : 0f;
        Rect buttonRect = new Rect(
            position.x + EditorGUIUtility.labelWidth,
            position.y,
            position.width - EditorGUIUtility.labelWidth - clearWidth - 2f,
            position.height);

        Color originalBg = GUI.backgroundColor;

        if (isListening)
        {
            // --- PHASE 1: intercept the current key event BEFORE any control draws ---
            // We must do this first so the key is not consumed by the hidden TextField
            // (which we draw afterwards solely to hold genuine keyboard focus for the
            //  Shortcut Manager).
            bool keyCaptured = false;
            Event evt = Event.current;
            if (evt != null && (evt.type == EventType.KeyDown || evt.type == EventType.KeyUp))
            {
                evt.Use();
                keyCaptured = true;

                if (evt.keyCode != KeyCode.Escape)
                {
                    property.intValue = (int)evt.keyCode;
                    property.serializedObject.ApplyModifiedProperties();
                }

                StopListening(property);
                GUI.changed = true;
            }

            // --- PHASE 2: draw an invisible, genuinely-focused TextField ---
            // This is the secret sauce: Unity's global Shortcut Manager checks whether
            // a REAL Unity text-editing control currently holds keyboard focus. When a
            // TextField is focused, the Shortcut Manager suppresses single-key tool
            // shortcuts (Q/W/E/R/T/Y/1-9 etc.) that would otherwise swallow alphanumeric
            // keys before we ever see them. An invisible TextField with real focus is
            // the only reliable way to make ALL keys capturable.
            string controlName = "KeyBindCapture_" + property.propertyPath;
            GUI.SetNextControlName(controlName);

            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0f);  // fully transparent
            GUI.TextField(buttonRect, "", EditorStyles.textField);
            GUI.color = oldColor;

            // Only keep focus alive while we are still listening (keyCaptured == false
            // means StopListening was NOT called above, so we stay in capture mode).
            if (!keyCaptured)
            {
                EditorGUI.FocusTextInControl(controlName);

                // Draw the yellow "press a key" visual prompt on top.
                GUI.backgroundColor = Color.yellow;
                GUI.Box(buttonRect, ListeningPrompt, EditorStyles.miniButton);
            }
        }
        else
        {
            if (GUI.Button(buttonRect, currentKey == KeyCode.None ? BindPrompt : currentKey.ToString(), EditorStyles.miniButton))
            {
                SetListening(property, true);
                GUI.changed = true;
            }
        }

        GUI.backgroundColor = originalBg;

        // Manual reset button so a bound key can always be cleared with a click.
        if (!isListening && currentKey != KeyCode.None)
        {
            Rect clearRect = new Rect(buttonRect.xMax + 2f, position.y, clearWidth, position.height);
            if (GUI.Button(clearRect, ClearPrompt, EditorStyles.miniButton))
            {
                property.intValue = (int)KeyCode.None;
                property.serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            }
        }
    }

    private static bool IsListening(SerializedProperty property)
    {
        return listeningTarget == property.serializedObject.targetObject
            && listeningPath == property.propertyPath;
    }

    private static void SetListening(SerializedProperty property, bool listening)
    {
        if (listening)
        {
            // Starting a new bind cancels any other field that was still waiting.
            listeningTarget = property.serializedObject.targetObject;
            listeningPath = property.propertyPath;
        }
        else
        {
            if (IsListening(property))
            {
                listeningTarget = null;
                listeningPath = null;
            }
        }
    }

    private static void StopListening(SerializedProperty property)
    {
        SetListening(property, false);

        // Release keyboard focus so normal Editor shortcuts work again.
        GUIUtility.keyboardControl = 0;
    }
}
