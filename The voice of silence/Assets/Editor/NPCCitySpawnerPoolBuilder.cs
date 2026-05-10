using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(NPCCitySpawner))]
public class NPCCitySpawnerPoolBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NPCCitySpawner spawner = (NPCCitySpawner)target;

        GUILayout.Space(6);
        if (GUILayout.Button("Build Pool From Prefab (create children)"))
        {
            BuildPool(spawner);
        }

        if (GUILayout.Button("Clear Pool Children (remove created children)"))
        {
            if (EditorUtility.DisplayDialog("Clear pool children?", "This will delete child objects that look like pooled NPCs. Continue?", "Yes", "No"))
            {
                ClearPoolChildren(spawner);
            }
        }
    }

    [MenuItem("GameObject/NPCCitySpawner/Build Pool From Prefab", false, 0)]
    private static void MenuBuildPoolFromPrefab()
    {
        if (Selection.activeGameObject == null) return;
        NPCCitySpawner spawner = Selection.activeGameObject.GetComponent<NPCCitySpawner>();
        if (spawner == null)
        {
            EditorUtility.DisplayDialog("NPCCitySpawner not found", "Select a GameObject with NPCCitySpawner component.", "OK");
            return;
        }

        var editor = CreateEditor(spawner) as NPCCitySpawnerPoolBuilderEditor;
        editor.BuildPool(spawner);
    }

    private void BuildPool(NPCCitySpawner spawner)
    {
        // Access private serialized fields via reflection
        GameObject prefab = GetPrivateField<GameObject>(spawner, "npcModelPrefab");
        int npcCount = GetPrivateField<int>(spawner, "npcCount");
        int poolLimit = GetPrivateField<int>(spawner, "poolLimit");

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("No prefab", "NPCCitySpawner has no `npcModelPrefab` assigned.", "OK");
            return;
        }

        int target = poolLimit > 0 ? poolLimit : npcCount;
        if (target <= 0)
        {
            EditorUtility.DisplayDialog("Invalid target", "Pool target must be > 0.", "OK");
            return;
        }

        // Count existing child candidates
        int existing = 0;
        foreach (Transform child in spawner.transform)
        {
            if (child == null) continue;
            GameObject go = child.gameObject;
            if (go == null) continue;
            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() != null || go.GetComponent<NPCWanderer>() != null)
            {
                existing++;
            }
        }

        int toCreate = Mathf.Max(0, target - existing);
        if (toCreate == 0)
        {
            EditorUtility.DisplayDialog("Pool already filled", $"Found {existing} candidate child objects. Target is {target}.", "OK");
            return;
        }

        // Instantiate prefab as child objects (editor-time)
        for (int i = 0; i < toCreate; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) break;
            Undo.RegisterCreatedObjectUndo(instance, "Create pooled NPC");
            instance.transform.SetParent(spawner.transform, false);
            instance.name = prefab.name + "_pool_" + (existing + i + 1);
            instance.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        EditorUtility.DisplayDialog("Pool built", $"Added {toCreate} children. Total candidates now: {existing + toCreate}", "OK");
    }

    private void ClearPoolChildren(NPCCitySpawner spawner)
    {
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in spawner.transform)
        {
            if (child == null) continue;
            GameObject go = child.gameObject;
            if (go == null) continue;
            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() != null || go.GetComponent<NPCWanderer>() != null)
            {
                toDelete.Add(go);
            }
        }

        if (toDelete.Count == 0)
        {
            EditorUtility.DisplayDialog("Nothing to delete", "No candidate child NPCs found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm delete", $"Delete {toDelete.Count} child NPCs? This cannot be undone except via Undo.", "Delete", "Cancel"))
            return;

        foreach (var go in toDelete)
        {
            Undo.DestroyObjectImmediate(go);
        }

        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        EditorUtility.DisplayDialog("Deleted", $"Deleted {toDelete.Count} child objects.", "OK");
    }

    private T GetPrivateField<T>(object obj, string name)
    {
        FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null) return default;
        object val = f.GetValue(obj);
        if (val == null) return default;
        return (T)val;
    }
}
#endif
