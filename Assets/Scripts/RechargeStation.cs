using UnityEngine;
using UnityEngine.Rendering;

public class RechargeStation : MonoBehaviour
{
    [Header("Recharge Station")]
    [SerializeField, Min(0.5f)] private float stationRadius = 1.6f;
    [SerializeField, Min(0f)] private float rechargePerSecond = 20f;
    [SerializeField] private Color stationColor = new Color(0.1f, 1f, 0.25f, 1f);
    [SerializeField, Min(0f)] private float auraPulseSpeed = 2.5f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(1f)] private float groundRayDistance = 10f;
    [SerializeField, Min(0f)] private float groundSurfaceOffset = 0.025f;

    private BatterySystem battery;
    private Vector3 stationPosition;
    private Quaternion stationRotation = Quaternion.identity;
    private Transform aura;
    private Light auraLight;

    private void Start()
    {
        battery = GetComponent<BatterySystem>();
        FindGroundPose();
        CreateStationVisual();
    }

    private void Update()
    {
        Vector3 playerOffset = transform.position - stationPosition;
        playerOffset.y = 0f;

        if (playerOffset.sqrMagnitude <= stationRadius * stationRadius)
            battery.Recharge(rechargePerSecond * Time.deltaTime);

        float pulse = (Mathf.Sin(Time.time * auraPulseSpeed) + 1f) * 0.5f;
        float auraScale = stationRadius * Mathf.Lerp(1.9f, 2.15f, pulse);
        aura.localScale = new Vector3(auraScale, 0.035f, auraScale);
        auraLight.intensity = Mathf.Lerp(1.2f, 2.4f, pulse);
    }

    private void FindGroundPose()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 2f;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            groundRayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        bool foundGround = false;
        float closestDistance = float.MaxValue;
        RaycastHit closestHit = default;

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                foundGround = true;
                closestDistance = hit.distance;
                closestHit = hit;
            }
        }

        if (foundGround)
        {
            stationPosition = closestHit.point + closestHit.normal * groundSurfaceOffset;
            stationRotation = Quaternion.FromToRotation(Vector3.up, closestHit.normal);
            return;
        }

        Collider playerCollider = GetComponent<Collider>();
        float floorHeight = playerCollider != null
            ? playerCollider.bounds.min.y + groundSurfaceOffset
            : transform.position.y - 0.975f + groundSurfaceOffset;

        stationPosition = new Vector3(transform.position.x, floorHeight, transform.position.z);
    }

    private void CreateStationVisual()
    {
        GameObject stationRoot = new GameObject("Spawn Recharge Station");
        stationRoot.transform.SetPositionAndRotation(stationPosition, stationRotation);

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Circular Charging Pad";
        pad.transform.SetParent(stationRoot.transform, false);
        pad.transform.localScale = new Vector3(stationRadius * 2f, 0.025f, stationRadius * 2f);
        pad.GetComponent<Collider>().enabled = false;
        pad.GetComponent<Renderer>().sharedMaterial = CreatePadMaterial();

        GameObject auraObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        auraObject.name = "Green Recharge Aura";
        auraObject.transform.SetParent(stationRoot.transform, false);
        auraObject.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        auraObject.GetComponent<Collider>().enabled = false;
        auraObject.GetComponent<Renderer>().sharedMaterial = CreateAuraMaterial();
        aura = auraObject.transform;

        CreateRing(stationRoot.transform, stationRadius * 0.58f, 0.085f);
        CreateRing(stationRoot.transform, stationRadius * 0.88f, 0.09f);

        GameObject lightObject = new GameObject("Recharge Glow");
        lightObject.transform.SetParent(stationRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        auraLight = lightObject.AddComponent<Light>();
        auraLight.type = LightType.Point;
        auraLight.color = stationColor;
        auraLight.range = stationRadius * 3f;
        auraLight.shadows = LightShadows.None;
    }

    private Material CreatePadMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader);
        material.name = "Recharge Pad Material";
        material.color = new Color(0.025f, 0.09f, 0.04f);
        material.SetColor("_BaseColor", new Color(0.025f, 0.09f, 0.04f));
        material.SetColor("_EmissionColor", stationColor * 0.45f);
        material.EnableKeyword("_EMISSION");
        return material;
    }

    private Material CreateAuraMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material material = new Material(shader);
        material.name = "Recharge Aura Material";
        material.SetColor("_BaseColor", new Color(stationColor.r, stationColor.g, stationColor.b, 0.22f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }

    private void CreateRing(Transform parent, float radius, float height)
    {
        GameObject ringObject = new GameObject("Recharge Energy Ring");
        ringObject.transform.SetParent(parent, false);
        ringObject.transform.localPosition = new Vector3(0f, height, 0f);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 48;
        ring.startWidth = 0.045f;
        ring.endWidth = 0.045f;
        ring.sharedMaterial = CreateAuraMaterial();
        ring.startColor = stationColor;
        ring.endColor = stationColor;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        ring.receiveShadows = false;

        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i / (float)ring.positionCount * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }
}
