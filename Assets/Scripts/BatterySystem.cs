using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BatterySystem : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField, Range(1f, 100f)] private float startingBattery = 100f;
    [Tooltip("Battery percentage lost each second. 0.5 lasts about 200 seconds.")]
    [SerializeField, Min(0f)] private float drainPerSecond = 0.5f;
    [SerializeField, Range(0f, 100f)] private float criticalBatteryLevel = 15f;
    [SerializeField, Min(0f)] private float criticalFlashSpeed = 5f;

    [Header("HUD Appearance")]
    [SerializeField] private Color fullBatteryColor = new Color(0.1f, 0.9f, 0.2f);
    [SerializeField] private Color emptyBatteryColor = new Color(0.95f, 0.05f, 0.03f);
    [SerializeField, Range(0.2f, 1f)] private float barScreenWidth = 0.6f;
    [SerializeField, Min(20f)] private float barHeight = 52f;
    [SerializeField, Min(0f)] private float topPadding = 24f;
    [SerializeField, Range(2, 20)] private int notchCount = 10;
    [SerializeField, Min(1f)] private float notchWidth = 3f;
    [SerializeField] private Color notchColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField, Min(0f)] private float energyScrollSpeed = 0.35f;
    [SerializeField, Range(0f, 1f)] private float energyTextureStrength = 0.45f;

    private float currentBattery;
    private Image batteryFill;
    private RawImage energyOverlay;
    private Text percentageText;
    private GameObject gameOverPanel;
    private bool gameOver;

    public float CurrentBattery => currentBattery;

    public void Recharge(float amount)
    {
        if (gameOver)
            return;

        currentBattery = Mathf.Min(100f, currentBattery + Mathf.Max(0f, amount));
        UpdateBatteryUI();
    }

    private void Awake()
    {
        currentBattery = startingBattery;
        CreateBatteryUI();
        UpdateBatteryUI();
    }

    private void Update()
    {
        if (gameOver)
            return;

        currentBattery = Mathf.Max(0f, currentBattery - drainPerSecond * Time.deltaTime);
        UpdateBatteryUI();

        if (currentBattery <= 0f)
            ShowGameOver();
    }

    private void UpdateBatteryUI()
    {
        float normalizedBattery = currentBattery / 100f;
        batteryFill.fillAmount = normalizedBattery;
        energyOverlay.rectTransform.anchorMax = new Vector2(normalizedBattery, 1f);
        energyOverlay.uvRect = new Rect(
            Time.unscaledTime * energyScrollSpeed,
            0f,
            Mathf.Max(0.01f, normalizedBattery * 12f),
            1f);
        percentageText.text = $"{Mathf.CeilToInt(currentBattery)}%";

        Color batteryColor = Color.Lerp(emptyBatteryColor, fullBatteryColor, normalizedBattery);

        if (currentBattery <= criticalBatteryLevel && currentBattery > 0f)
        {
            float flash = Mathf.PingPong(Time.unscaledTime * criticalFlashSpeed, 1f);
            batteryColor = Color.Lerp(new Color(0.25f, 0f, 0f), Color.red, flash);
            percentageText.color = Color.Lerp(Color.white, Color.red, flash);
        }
        else
        {
            percentageText.color = Color.white;
        }

        batteryFill.color = batteryColor;
    }

    private void ShowGameOver()
    {
        gameOver = true;
        gameOverPanel.SetActive(true);

        SimplePlayerController playerController = GetComponent<SimplePlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        Rigidbody playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        Time.timeScale = 0f;
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void CreateBatteryUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject(
            "Battery UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject barBackground = CreateImage("Battery Bar", canvasObject.transform, new Color(0f, 0f, 0f, 0.8f));
        RectTransform barRect = barBackground.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f - barScreenWidth * 0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f + barScreenWidth * 0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, -topPadding);
        barRect.sizeDelta = new Vector2(0f, barHeight);

        GameObject fillObject = CreateImage("Battery Fill", barBackground.transform, fullBatteryColor);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(5f, 5f);
        fillRect.offsetMax = new Vector2(-5f, -5f);

        batteryFill = fillObject.GetComponent<Image>();
        batteryFill.type = Image.Type.Filled;
        batteryFill.fillMethod = Image.FillMethod.Horizontal;
        // Keeping the origin on the left makes the right edge retreat left as charge falls.
        batteryFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        CreateEnergyOverlay(barBackground.transform);
        CreateNotches(barBackground.transform);

        percentageText = CreateText("Battery Percentage", barBackground.transform, font, 27, FontStyle.Bold);
        RectTransform percentageRect = percentageText.rectTransform;
        percentageRect.anchorMin = Vector2.zero;
        percentageRect.anchorMax = Vector2.one;
        percentageRect.offsetMin = Vector2.zero;
        percentageRect.offsetMax = Vector2.zero;
        percentageText.alignment = TextAnchor.MiddleCenter;

        gameOverPanel = CreateImage("Game Over Panel", canvasObject.transform, new Color(0f, 0f, 0f, 0.88f));
        StretchToParent(gameOverPanel.GetComponent<RectTransform>());

        Text gameOverText = CreateText("Game Over Text", gameOverPanel.transform, font, 58, FontStyle.Bold);
        gameOverText.text = "BATTERY DEPLETED\nGAME OVER";
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.color = Color.red;
        RectTransform gameOverRect = gameOverText.rectTransform;
        gameOverRect.anchorMin = new Vector2(0.2f, 0.52f);
        gameOverRect.anchorMax = new Vector2(0.8f, 0.78f);
        gameOverRect.offsetMin = Vector2.zero;
        gameOverRect.offsetMax = Vector2.zero;

        GameObject buttonObject = CreateImage("Retry Button", gameOverPanel.transform, new Color(0.8f, 0.12f, 0.08f));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.4f, 0.33f);
        buttonRect.anchorMax = new Vector2(0.6f, 0.43f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Button retryButton = buttonObject.AddComponent<Button>();
        retryButton.targetGraphic = buttonObject.GetComponent<Image>();
        retryButton.onClick.AddListener(Retry);

        Text retryText = CreateText("Retry Text", buttonObject.transform, font, 32, FontStyle.Bold);
        retryText.text = "RETRY";
        retryText.alignment = TextAnchor.MiddleCenter;
        StretchToParent(retryText.rectTransform);

        gameOverPanel.SetActive(false);
        EnsureEventSystem();
    }

    private void CreateEnergyOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject(
            "Animated Energy Texture",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        overlayObject.transform.SetParent(parent, false);
        energyOverlay = overlayObject.GetComponent<RawImage>();
        energyOverlay.texture = CreateEnergyTexture();
        energyOverlay.color = new Color(1f, 1f, 1f, energyTextureStrength);
        energyOverlay.raycastTarget = false;

        RectTransform overlayRect = energyOverlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = new Vector2(5f, 5f);
        overlayRect.offsetMax = new Vector2(-5f, -5f);
    }

    private void CreateNotches(Transform parent)
    {
        for (int i = 1; i < notchCount; i++)
        {
            GameObject notch = CreateImage($"Battery Notch {i}", parent, notchColor);
            Image notchImage = notch.GetComponent<Image>();
            notchImage.raycastTarget = false;

            RectTransform notchRect = notch.GetComponent<RectTransform>();
            float horizontalPosition = (float)i / notchCount;
            notchRect.anchorMin = new Vector2(horizontalPosition, 0f);
            notchRect.anchorMax = new Vector2(horizontalPosition, 1f);
            notchRect.pivot = new Vector2(0.5f, 0.5f);
            notchRect.anchoredPosition = Vector2.zero;
            notchRect.sizeDelta = new Vector2(notchWidth, -10f);
        }
    }

    private static Texture2D CreateEnergyTexture()
    {
        const int textureWidth = 64;
        const int textureHeight = 16;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "Generated Battery Energy";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int diagonal = (x + y * 2) % 16;
                bool brightStripe = diagonal < 4;
                bool centerPulse = Mathf.Abs(y - textureHeight / 2) <= 1;

                float alpha = brightStripe ? 0.8f : 0.08f;
                if (centerPulse)
                    alpha = Mathf.Max(alpha, 0.45f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    private static GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        Font font,
        int fontSize,
        FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        EventSystem.current = eventSystemObject.GetComponent<EventSystem>();
    }
}
