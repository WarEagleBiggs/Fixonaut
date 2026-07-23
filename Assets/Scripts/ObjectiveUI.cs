using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ObjectiveUI : MonoBehaviour
{
    [Header("Objective")]
    [SerializeField, TextArea] private string objectiveText = "COMPLETE THE STATION TASKS";
    [SerializeField] private Color textColor = new Color(0.5f, 1f, 0.65f);
    [SerializeField] private int fontSize = 25;

    [SerializeField, HideInInspector] private Canvas objectiveCanvas;
    [SerializeField, HideInInspector] private Image objectivePanel;
    [SerializeField, HideInInspector] private Image objectiveAccent;
    private Text objectiveLabel;

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            FindExistingUI();
            ApplyObjectiveStyle();
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += EnsureEditorUI;
#endif
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += EnsureEditorUI;
#endif
    }

    public void SetObjective(string newObjective)
    {
        objectiveText = newObjective;
        ApplyObjectiveStyle();
    }

#if UNITY_EDITOR
    private void EnsureEditorUI()
    {
        if (this == null
            || Application.isPlaying
            || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureObjectiveUI();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void EnsureObjectiveUI()
    {
        if (objectiveCanvas == null || objectiveLabel == null)
            FindExistingUI();

        if (objectiveCanvas != null && objectiveLabel != null)
        {
            ApplyObjectiveStyle();
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject(
            "Objective UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        objectiveCanvas = canvasObject.GetComponent<Canvas>();
        objectiveCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        objectiveCanvas.sortingOrder = 80;
        objectiveCanvas.pixelPerfect = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject(
            "Objective Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        panelObject.transform.SetParent(canvasObject.transform, false);
        objectivePanel = panelObject.GetComponent<Image>();
        objectivePanel.raycastTarget = false;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -92f);
        panelRect.sizeDelta = new Vector2(560f, 88f);

        GameObject accentObject = new GameObject(
            "Pixel Accent",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        accentObject.transform.SetParent(panelObject.transform, false);
        objectiveAccent = accentObject.GetComponent<Image>();
        objectiveAccent.raycastTarget = false;
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(7f, 0f);

        GameObject textObject = new GameObject(
            "Pixel Objective Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text),
            typeof(Outline));

        textObject.transform.SetParent(panelObject.transform, false);
        objectiveLabel = textObject.GetComponent<Text>();
        objectiveLabel.font = font;
        objectiveLabel.fontStyle = FontStyle.Bold;
        objectiveLabel.alignment = TextAnchor.MiddleLeft;
        objectiveLabel.raycastTarget = false;
        objectiveLabel.horizontalOverflow = HorizontalWrapMode.Overflow;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);

        RectTransform textRect = objectiveLabel.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-10f, -8f);

        ApplyObjectiveStyle();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.Undo.RegisterCreatedObjectUndo(canvasObject, "Create Objective UI");
#endif
    }

    private void FindExistingUI()
    {
        Transform existingCanvas = transform.Find("Objective UI");

        if (existingCanvas == null)
        {
            Canvas[] sceneCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);

            foreach (Canvas sceneCanvas in sceneCanvases)
            {
                if (sceneCanvas.name == "Objective UI"
                    && sceneCanvas.gameObject.scene == gameObject.scene)
                {
                    existingCanvas = sceneCanvas.transform;
                    break;
                }
            }
        }

        if (existingCanvas == null)
            return;

        objectiveCanvas = existingCanvas.GetComponent<Canvas>();

        Transform panelTransform = existingCanvas.Find("Objective Panel");
        if (panelTransform == null)
            return;

        objectivePanel = panelTransform.GetComponent<Image>();
        objectiveLabel = panelTransform.GetComponentInChildren<Text>(true);

        Transform accentTransform = panelTransform.Find("Pixel Accent");
        if (accentTransform != null)
            objectiveAccent = accentTransform.GetComponent<Image>();
    }

    private void ApplyObjectiveStyle()
    {
        if (objectiveCanvas != null)
        {
            objectiveCanvas.transform.localPosition = Vector3.zero;
            objectiveCanvas.transform.localRotation = Quaternion.identity;
            objectiveCanvas.transform.localScale = Vector3.one;
        }

        if (objectivePanel != null)
            objectivePanel.color = new Color(0.01f, 0.04f, 0.025f, 0.82f);

        if (objectiveAccent != null)
            objectiveAccent.color = textColor;

        if (objectiveLabel == null)
            return;

        objectiveLabel.fontSize = fontSize;
        objectiveLabel.color = textColor;
        objectiveLabel.text = $"> OBJECTIVE_01\n  {objectiveText.ToUpperInvariant()}";
    }
}
