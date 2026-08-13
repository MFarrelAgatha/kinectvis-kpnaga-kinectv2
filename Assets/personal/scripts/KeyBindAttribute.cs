using UnityEngine;

/// <summary>
/// Marks a KeyCode field to be drawn as a compact "click to bind" control in the Inspector,
/// instead of Unity's default long key dropdown list. The key is captured by pressing it.
/// </summary>
public class KeyBindAttribute : PropertyAttribute
{
}
