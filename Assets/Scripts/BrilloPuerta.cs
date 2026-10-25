using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class BrilloPuerta : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radioLuz = 4.5f;
    [SerializeField, Min(0f)] private float intensidadLuz = 0.35f;
    private SpriteRenderer fuente;
    private Light2D luz;
    private bool encendido;
    private SpriteRenderer halo;
    private Material materialHalo;
    private static Sprite spriteHalo;

    public void Inicializar(SpriteRenderer sprite)
    {
        fuente = sprite;
        if (fuente == null) return;
        CrearHalo();

        // Una base blanca conserva la iluminacion original al activar luces locales.
        bool tieneGlobal = false;
        foreach (Light2D existente in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (existente.gameObject.scene == gameObject.scene && existente.lightType == Light2D.LightType.Global)
                tieneGlobal = true;
        int[] capas = new int[SortingLayer.layers.Length];
        for (int i = 0; i < capas.Length; i++) capas[i] = SortingLayer.layers[i].id;
        if (!tieneGlobal)
        {
            GameObject ambiente = new GameObject("LuzBasePuertas");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ambiente, gameObject.scene);
            Light2D global = ambiente.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.blendStyleIndex = 0;
            global.color = Color.white;
            global.intensity = 1f;
            global.targetSortingLayers = capas;
        }
        GameObject objetoLuz = new GameObject("LuzBlancaPuerta");
        objetoLuz.transform.SetParent(transform, false);
        luz = objetoLuz.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        // Iluminar el color del sprite: la mezcla aditiva pintaba de blanco incluso la tinta.
        luz.blendStyleIndex = 0;
        luz.color = Color.white;
        luz.pointLightInnerAngle = luz.pointLightOuterAngle = 360f;
        luz.pointLightInnerRadius = 0f;
        luz.targetSortingLayers = capas;
        Mostrar(false);
    }

    public void Mostrar(bool visible)
    {
        encendido = visible;
        if (luz != null) luz.enabled = visible;
        if (halo != null) halo.enabled = visible;
    }

    private void LateUpdate()
    {
        if (!encendido || fuente == null || luz == null) return;
        luz.transform.position = fuente.bounds.center;
        luz.pointLightOuterRadius = Mathf.Max(0.7f, radioLuz);
        luz.intensity = intensidadLuz;
        if (halo != null)
        {
            halo.transform.position = fuente.bounds.center;
            halo.transform.localScale = new Vector3(
                (fuente.bounds.size.x + 2f) / Mathf.Max(0.001f, transform.lossyScale.x),
                (fuente.bounds.size.y + 2f) / Mathf.Max(0.001f, transform.lossyScale.y), 1f);
            halo.sortingLayerID = fuente.sortingLayerID;
            halo.sortingOrder = fuente.sortingOrder - 1;
        }
    }

    private void CrearHalo()
    {
        if (spriteHalo == null)
        {
            const int lado = 64;
            Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            textura.wrapMode = TextureWrapMode.Clamp;
            Color[] pixeles = new Color[lado * lado];
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    Vector2 uv = new Vector2(x / (lado - 1f), y / (lado - 1f)) * 2f - Vector2.one;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - uv.sqrMagnitude), 2f);
                    pixeles[y * lado + x] = new Color(1f, 1f, 1f, alpha);
                }
            textura.SetPixels(pixeles);
            textura.Apply();
            spriteHalo = Sprite.Create(textura, new Rect(0, 0, lado, lado), Vector2.one * 0.5f, lado);
        }
        GameObject objeto = new GameObject("ResplandorDetrasPuerta", typeof(SpriteRenderer));
        objeto.transform.SetParent(transform, false);
        halo = objeto.GetComponent<SpriteRenderer>();
        halo.sprite = spriteHalo;
        // La referencia del material en Resources incluye el shader en la build.
        Material plantilla = Resources.Load<Material>("MaterialHaloPuerta");
        if (plantilla != null)
        {
            materialHalo = new Material(plantilla);
            halo.sharedMaterial = materialHalo;
        }
        else
        {
            halo.enabled = false;
            Debug.LogWarning("Falta Resources/MaterialHaloPuerta; se omite el halo de la puerta.", this);
            Destroy(halo.gameObject);
            halo = null;
        }
        halo.color = new Color(1f, 1f, 1f, 0.28f);
    }

    private void OnDisable() => Mostrar(false);
    private void OnDestroy()
    {
        if (luz != null) Destroy(luz.gameObject);
        if (halo != null) Destroy(halo.gameObject);
        if (materialHalo != null) Destroy(materialHalo);
    }
}
