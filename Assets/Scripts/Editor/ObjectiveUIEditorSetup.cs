using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ObjectiveUIEditorSetup
{
    static ObjectiveUIEditorSetup()
    {
        EditorApplication.delayCall += EnsureObjectiveUIExists;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += EnsureObjectiveUIExists;
    }

    private static void EnsureObjectiveUIExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        SimplePlayerController[] players =
            Object.FindObjectsByType<SimplePlayerController>(FindObjectsInactive.Include);

        foreach (SimplePlayerController player in players)
        {
            if (player.GetComponent<ObjectiveUI>() == null)
                Undo.AddComponent<ObjectiveUI>(player.gameObject);
        }
    }
}
