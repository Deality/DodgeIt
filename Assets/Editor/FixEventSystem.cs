using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixEventSystem
{
    public static void Execute()
    {
        var allEventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (allEventSystems.Length == 0)
        {
            Debug.Log("No EventSystem found in the current scene.");
            return;
        }

        int fixed_count = 0;
        foreach (var es in allEventSystems)
        {
            var old = es.GetComponent<StandaloneInputModule>();
            if (old != null)
            {
                Object.DestroyImmediate(old);
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                EditorUtility.SetDirty(es.gameObject);
                fixed_count++;
                Debug.Log($"Fixed EventSystem on '{es.gameObject.name}': replaced StandaloneInputModule with InputSystemUIInputModule.");
            }
        }

        if (fixed_count > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"Done. Fixed {fixed_count} EventSystem(s). Save the scene to persist.");
        }
        else
        {
            Debug.Log("No StandaloneInputModule found — EventSystem may already be correct.");
        }
    }
}
