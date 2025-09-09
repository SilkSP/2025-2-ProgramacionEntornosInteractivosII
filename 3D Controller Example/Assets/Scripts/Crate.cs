using System.Collections;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public enum CrateType
    {
        Normal,
        Multihit,
        Bounceable,
        Indestructable,
        BounceableIndestructable,
        TNT,
        Nitro
    }

    public CrateType crateType;

    public GameObject prefabFruit;

    private int initialResistance = 1;
    private int actualResistance;

    private bool canBeAttacked;
    private bool canBeAttackedByBounce;

    private bool hasTNTStarted = false;
    private bool hasExploded = false;

    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionDelay = 0.3f;

    [SerializeField] private float tntCountdown = 3f;
    [SerializeField] private float tntFlashPeriod = 0.2f;

    private Collider[] explosionHits = new Collider[20];

    private MeshRenderer meshRenderer;
    private Color originalColor;

    private MaterialPropertyBlock mpb;
    private static readonly int ColorPropID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorPropID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();

        if (meshRenderer != null)
        {
            if (meshRenderer.sharedMaterial != null)
            {
                if (meshRenderer.sharedMaterial.HasProperty(ColorPropID))
                    originalColor = meshRenderer.sharedMaterial.GetColor(ColorPropID);
                else if (meshRenderer.sharedMaterial.HasProperty(BaseColorPropID))
                    originalColor = meshRenderer.sharedMaterial.GetColor(BaseColorPropID);
                else
                    originalColor = Color.white;
            }
            else originalColor = Color.white;
        }

        UpdateColor();
    }

    void Start()
    {
        switch (crateType)
        {
            case CrateType.Normal:
                initialResistance = 1;
                canBeAttacked = true;
                canBeAttackedByBounce = true;
                ApplyColorToRenderer(Color.orange);
                break;
            case CrateType.Multihit:
                initialResistance = 5;
                canBeAttacked = true;
                canBeAttackedByBounce = true;
                ApplyColorToRenderer(Color.brown);
                break;
            case CrateType.Bounceable:
                canBeAttacked = true;
                canBeAttackedByBounce = false;
                ApplyColorToRenderer(Color.rosyBrown);
                break;
            case CrateType.Indestructable:
                canBeAttacked = false;
                canBeAttackedByBounce = false;
                ApplyColorToRenderer(Color.grey);
                break;
            case CrateType.BounceableIndestructable:
                canBeAttacked = false;
                canBeAttackedByBounce = false;
                ApplyColorToRenderer(Color.lightGray);
                break;
            case CrateType.TNT:
                initialResistance = 1;
                canBeAttacked = true;
                canBeAttackedByBounce = true;
                ApplyColorToRenderer(Color.darkRed);
                break;
            case CrateType.Nitro:
                initialResistance = 1;
                canBeAttacked = true;
                canBeAttackedByBounce = true;
                ApplyColorToRenderer(Color.green);
                explosionDelay = 0.05f;
                break;
        }

        actualResistance = initialResistance;

        if (meshRenderer != null)
            originalColor = GetRendererCurrentColor();
    }

    public void UpdateColor()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            switch (crateType)
            {
                case CrateType.Normal:
                    ApplyColorToRenderer(Color.orange);
                    break;
                case CrateType.Multihit:
                    ApplyColorToRenderer(Color.sandyBrown);
                    break;
                case CrateType.Bounceable:
                    ApplyColorToRenderer(Color.rosyBrown);
                    break;
                case CrateType.Indestructable:
                    ApplyColorToRenderer(Color.grey);
                    break;
                case CrateType.BounceableIndestructable:
                    ApplyColorToRenderer(Color.lightGray);
                    break;
                case CrateType.TNT:
                    ApplyColorToRenderer(Color.darkRed);
                    break;
                case CrateType.Nitro:
                    ApplyColorToRenderer(Color.green);
                    break;
            }

            originalColor = GetRendererCurrentColor();
        }
    }

    private void ApplyColorToRenderer(Color color)
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        meshRenderer.GetPropertyBlock(mpb);
        Material mat = meshRenderer.sharedMaterial;

        if (mat != null && mat.HasProperty(ColorPropID))
            mpb.SetColor(ColorPropID, color);
        if (mat != null && mat.HasProperty(BaseColorPropID))
            mpb.SetColor(BaseColorPropID, color);

        if (mat != null && mat.HasProperty(EmissionColorPropID))
        {
            Color emission = color;
            if (emission.maxColorComponent < 0.1f)
                emission = emission * 2f;
            mpb.SetColor(EmissionColorPropID, emission);
        }

        meshRenderer.SetPropertyBlock(mpb);
    }

    private Color GetRendererCurrentColor()
    {
        if (meshRenderer == null || meshRenderer.sharedMaterial == null) return Color.white;
        Material mat = meshRenderer.sharedMaterial;
        if (mat.HasProperty(ColorPropID))
            return mat.GetColor(ColorPropID);
        if (mat.HasProperty(BaseColorPropID))
            return mat.GetColor(BaseColorPropID);
        return Color.white;
    }

    public void OnPlayerBounce(PlayerCharacterController player)
    {
        if (crateType != CrateType.Indestructable && crateType != CrateType.BounceableIndestructable)
        {
            if (!(crateType == CrateType.TNT && hasTNTStarted))
            {
                if (player != null && player.pm != null)
                    player.pm.ExecuteBounceJump();
            }
        }

        if (canBeAttackedByBounce)
        {
            actualResistance--;

            if (actualResistance <= 0)
            {
                if (crateType == CrateType.TNT)
                {
                    if (!hasTNTStarted) StartTNTTimer();
                }
                else
                {
                    OnDestroyCrate(player);
                }
            }
            else
            {
                SpawnFruit();
            }
        }
    }

    private void StartTNTTimer()
    {
        if (hasTNTStarted) return;
        hasTNTStarted = true;
        StartCoroutine(TNTCountdownCoroutine());
    }

    private IEnumerator TNTCountdownCoroutine()
    {
        if (meshRenderer == null)
        {
            yield return new WaitForSeconds(tntCountdown);
            CrateExplosion();
            yield break;
        }

        float elapsed = 0f;
        bool flash = false;
        while (elapsed < tntCountdown)
        {
            ApplyColorToRenderer(flash ? Color.white : Color.darkRed);
            flash = !flash;

            yield return new WaitForSeconds(tntFlashPeriod);
            elapsed += tntFlashPeriod;
        }

        ApplyColorToRenderer(originalColor);

        CrateExplosion();
    }

    public void OnPlayerHeadBounce(PlayerCharacterController player)
    {
        actualResistance--;

        if (player != null && player.pm != null)
        {
            player.pm.ForceStopVertical();
            player.pm.CancelJump();
        }

        if (actualResistance <= 0)
        {
            if (crateType == CrateType.TNT)
                CrateExplosion();
            else
                OnDestroyCrate(player);
        }
        else
        {
            SpawnFruit();
        }
    }

    public void SpawnFruit()
    {
        if (prefabFruit == null) return;
        GameObject fruit = Instantiate(prefabFruit);
        fruit.transform.position = this.transform.position;
    }

    private void CrateExplosion()
    {
        if (hasExploded) return;
        hasExploded = true;
        StartCoroutine(CrateExplosionCoroutine());
    }

    private IEnumerator CrateExplosionCoroutine()
    {
        if (explosionDelay > 0f)
            yield return new WaitForSeconds(explosionDelay);

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, explosionHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = explosionHits[i];
            if (hit == null) continue;

            if (hit.gameObject.CompareTag("Box"))
            {
                Crate otherCrate = hit.GetComponent<Crate>();
                if (otherCrate != null && otherCrate != this)
                {
                    otherCrate.OnDestroyCrate(null);
                }
            }

            PlayerCharacterController player = hit.GetComponent<PlayerCharacterController>();
            if (player != null)
            {
                player.OnDamage();
            }
        }

        Destroy(this.gameObject);
        yield break;
    }

    public void OnDestroyCrate(PlayerCharacterController player)
    {
        if (hasExploded) return;

        switch (crateType)
        {
            case CrateType.Normal:
                SpawnFruit();
                if (player != null && player.playerStats != null)
                    player.playerStats.cajasDestruidas = player.playerStats.cajasDestruidas + 1;
                Destroy(this.gameObject);
                break;
            case CrateType.Multihit:
                SpawnFruit();
                if (player != null && player.playerStats != null)
                    player.playerStats.cajasDestruidas = player.playerStats.cajasDestruidas + 1;
                Destroy(this.gameObject);
                break;
            case CrateType.Bounceable:
                SpawnFruit();
                if (player != null && player.playerStats != null)
                    player.playerStats.cajasDestruidas = player.playerStats.cajasDestruidas + 1;
                Destroy(this.gameObject);
                break;
            case CrateType.TNT:
                CrateExplosion();
                break;
            case CrateType.Nitro:
                CrateExplosion();
                break;
        }
    }

}
