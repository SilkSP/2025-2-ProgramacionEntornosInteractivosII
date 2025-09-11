using System.Collections;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public enum CrateType
    {
        Normal,
        Indestructable,
        IndestructableHazard,
        StaticEnemy
    }

    public CrateType crateType;

    public GameObject prefabGeo;

    private int initialResistance = 1;
    private int actualResistance;

    private bool canBeAttackedByNeedle;


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
                canBeAttackedByNeedle = true;
                ApplyColorToRenderer(Color.black);
                break;
            
            case CrateType.Indestructable:
                canBeAttackedByNeedle = false;
                ApplyColorToRenderer(Color.grey);
                break;
            case CrateType.IndestructableHazard:
                canBeAttackedByNeedle = false;
                ApplyColorToRenderer(Color.softRed);
                break;
            
            case CrateType.StaticEnemy:
                initialResistance = 3;
                canBeAttackedByNeedle = true;
                ApplyColorToRenderer(Color.darkBlue);
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
                
                case CrateType.Indestructable:
                    ApplyColorToRenderer(Color.grey);
                    break;
                case CrateType.IndestructableHazard:
                    ApplyColorToRenderer(Color.lightGray);
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


    public void OnPlayerNeedleAttack(SideScrollerPlayerCharacterController player)
    {
        if (canBeAttackedByNeedle)
        {
            actualResistance--;

            if (actualResistance <= 0)
            {
                OnDestroyCrate(player);
            }
            else
            {
                SpawnGeo();
            }
        }
    }

    public void SpawnGeo()
    {
        if (prefabGeo == null) return;
        GameObject geo = Instantiate(prefabGeo);
        geo.transform.position = this.transform.position;
    }


    public void OnDestroyCrate(SideScrollerPlayerCharacterController player)
    {
        switch (crateType)
        {
            case CrateType.Normal:
                SpawnGeo();
                SpawnGeo();
                SpawnGeo();
                SpawnGeo();
                SpawnGeo();
                Destroy(this.gameObject);
                break;
            case CrateType.StaticEnemy:
                SpawnGeo();
                Destroy(this.gameObject);
                break;
            
        }
    }

}
