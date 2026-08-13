using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// A small modal utility window used to capture a single key press for [KeyBind] fields.
/// Because it is a standalone modal window it has exclusive, unambiguous keyboard focus,
/// so it reliably receives ALL keys — letters, digits, function keys, arrows, Escape —
/// without Unity's global shortcuts or the Inspector's embedded text-field routing
/// swallowing printable keys. This mirrors how Unity's own "rebind shortcut" dialog works.
/// </summary>
public class KeyBindPopup : EditorWindow
{
    private Action<KeyCode> onKeyBound;

    public static void Show(Action<KeyCode> onKeyBound)
    {
        KeyBindPopup popup = CreateInstance<KeyBindPopup>();
        popup.onKeyBound = onKeyBound;
        popup.titleContent = new GUIContent("Bind Key");
        popup.minSize = new Vector2(260f, 70f);
        popup.maxSize = new Vector2(260f, 70f);
        popup.ShowModalUtility();
    }

    private void OnGUI()
    {
        // A modal window receives every key press; capture the first one.
        Event evt = Event.current;
        if (evt != null && evt.type == EventType.KeyDown)
        {
            evt.Use();

            if (evt.keyCode != KeyCode.Escape && onKeyBound != null)
            {
                onKeyBound(evt.keyCode);
            }

            Close();
            return;
        }

        GUILayout.Space(10f);
        EditorGUILayout.LabelField("Press any key to bind...", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Esc to cancel", EditorStyles.miniLabel);
    }
}
