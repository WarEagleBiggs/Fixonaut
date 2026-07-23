using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CrateInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string crateTag = "Crate";
    [SerializeField, Min(0.5f)] private float interactionRadius = 1.35f;
    [SerializeField] private Color highlightColor = new Color(0.1f, 1f, 0.2f, 1f);

    [Header("Carrying")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, 0.25f, 2f);
    [SerializeField, Min(0.5f)] private float minimumCarryDistance = 2f;
    [SerializeField, Min(0f)] private float crateClearance = 0.35f;
    [SerializeField, Min(1f)] private float holdFollowSpeed = 18f;

    private GameObject nearbyCrate;
    private GameObject heldCrate;
    private Rigidbody heldRigidbody;
    private Transform holdPoint;
    private Text promptText;
    private GameObject promptPanel;

    private bool originalIsKinematic;
    private bool originalUseGravity;
    private bool originalDetectCollisions;
    private RigidbodyConstraints originalConstraints;
    private Transform originalParent;

    private void Awake()
    {
        CreateHoldPoint();
        CreateInteractionPrompt();
    }

    private void Update()
    {
        if (heldCrate == null)
            FindNearbyCrate();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldCrate != null)
                DropCrate();
            else if (nearbyCrate != null)
                PickUpCrate();
        }

        UpdatePrompt();
    }

    private void FixedUpdate()
    {
        if (heldRigidbody == null)
            return;

        float followAmount = 1f - Mathf.Exp(-holdFollowSpeed * Time.fixedDeltaTime);
        Vector3 newPosition = Vector3.Lerp(heldRigidbody.position, holdPoint.position, followAmount);
        Quaternion newRotation = Quaternion.Slerp(
            heldRigidbody.rotation,
            holdPoint.rotation,
            followAmount);

        heldRigidbody.MovePosition(newPosition);
        heldRigidbody.MoveRotation(newRotation);
    }

    private void FindNearbyCrate()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position,
            interactionRadius,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        GameObject closestCrate = null;
        float closestDistance = float.MaxValue;

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            GameObject crate = FindTaggedCrate(nearbyCollider.transform);
            if (crate == null)
                continue;

            Vector3 closestPoint = nearbyCollider.ClosestPoint(transform.position);
            float distance = (closestPoint - transform.position).sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCrate = crate;
            }
        }

        SetNearbyCrate(closestCrate);
    }

    private GameObject FindTaggedCrate(Transform startingTransform)
    {
        Transform current = startingTransform;

        while (current != null)
        {
            if (current.CompareTag(crateTag))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private void SetNearbyCrate(GameObject crate)
    {
        if (nearbyCrate == crate)
            return;

        if (nearbyCrate != null)
            SetHighlight(nearbyCrate, false);

        nearbyCrate = crate;

        if (nearbyCrate != null)
            SetHighlight(nearbyCrate, true);
    }

    private void PickUpCrate()
    {
        heldCrate = nearbyCrate;
        PositionHoldPointForCrate();
        heldRigidbody = heldCrate.GetComponent<Rigidbody>();

        if (heldRigidbody == null)
            heldRigidbody = heldCrate.AddComponent<Rigidbody>();

        originalParent = heldCrate.transform.parent;
        originalIsKinematic = heldRigidbody.isKinematic;
        originalUseGravity = heldRigidbody.useGravity;
        originalDetectCollisions = heldRigidbody.detectCollisions;
        originalConstraints = heldRigidbody.constraints;

        heldRigidbody.linearVelocity = Vector3.zero;
        heldRigidbody.angularVelocity = Vector3.zero;
        heldRigidbody.useGravity = false;
        heldRigidbody.isKinematic = true;
        heldRigidbody.detectCollisions = false;
        heldRigidbody.constraints = RigidbodyConstraints.None;
        heldRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void PositionHoldPointForCrate()
    {
        float playerFront = GetFrontExtent(gameObject, transform.position);
        float crateHalfSize = GetHalfExtent(heldCrate);
        float safeDistance = playerFront + crateHalfSize + crateClearance;

        Vector3 safeOffset = holdOffset;
        safeOffset.z = Mathf.Max(minimumCarryDistance, holdOffset.z, safeDistance);
        holdPoint.localPosition = safeOffset;
    }

    private float GetFrontExtent(GameObject target, Vector3 origin)
    {
        float furthestPoint = 0f;

        foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>())
        {
            if (!targetCollider.enabled || targetCollider.transform.IsChildOf(heldCrate.transform))
                continue;

            Bounds bounds = targetCollider.bounds;
            float centerDistance = Vector3.Dot(bounds.center - origin, transform.forward);
            furthestPoint = Mathf.Max(furthestPoint, centerDistance + ProjectBounds(bounds));
        }

        return furthestPoint;
    }

    private float GetHalfExtent(GameObject target)
    {
        float halfExtent = 0f;

        foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>())
        {
            if (targetCollider.enabled)
                halfExtent = Mathf.Max(halfExtent, ProjectBounds(targetCollider.bounds));
        }

        return halfExtent;
    }

    private float ProjectBounds(Bounds bounds)
    {
        Vector3 direction = transform.forward;
        Vector3 extents = bounds.extents;

        return Mathf.Abs(direction.x) * extents.x
            + Mathf.Abs(direction.y) * extents.y
            + Mathf.Abs(direction.z) * extents.z;
    }

    private void DropCrate()
    {
        heldCrate.transform.SetParent(originalParent, true);
        heldRigidbody.detectCollisions = originalDetectCollisions;
        heldRigidbody.constraints = originalConstraints;
        heldRigidbody.isKinematic = originalIsKinematic;
        heldRigidbody.useGravity = originalUseGravity;

        SetHighlight(heldCrate, false);
        heldCrate = null;
        heldRigidbody = null;
        nearbyCrate = null;
    }

    private void SetHighlight(GameObject crate, bool highlighted)
    {
        Renderer[] renderers = crate.GetComponentsInChildren<Renderer>();

        foreach (Renderer crateRenderer in renderers)
        {
            if (!highlighted)
            {
                crateRenderer.SetPropertyBlock(null);
                continue;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            crateRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", highlightColor);
            properties.SetColor("_Color", highlightColor);
            crateRenderer.SetPropertyBlock(properties);
        }
    }

    private void UpdatePrompt()
    {
        bool shouldShow = heldCrate != null || nearbyCrate != null;
        promptPanel.SetActive(shouldShow);

        if (shouldShow)
            promptText.text = heldCrate != null ? "[ E ]  DROP CRATE" : "[ E ]  PICK UP CRATE";
    }

    private void CreateHoldPoint()
    {
        GameObject holdPointObject = new GameObject("Crate Hold Point");
        holdPointObject.transform.SetParent(transform, false);
        holdPointObject.transform.localPosition = holdOffset;
        holdPoint = holdPointObject.transform;
    }

    private void CreateInteractionPrompt()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject(
            "Crate Interaction UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        promptPanel = new GameObject(
            "Crate Prompt",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        promptPanel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = promptPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);
        panelImage.raycastTarget = false;

        RectTransform panelRect = promptPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 90f);
        panelRect.sizeDelta = new Vector2(390f, 58f);

        GameObject textObject = new GameObject(
            "Prompt Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));

        textObject.transform.SetParent(promptPanel.transform, false);
        promptText = textObject.GetComponent<Text>();
        promptText.font = font;
        promptText.fontSize = 25;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = highlightColor;
        promptText.raycastTarget = false;

        RectTransform textRect = promptText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptPanel.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = highlightColor;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
