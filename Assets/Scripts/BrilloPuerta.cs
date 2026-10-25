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

    public void Inicializar(SpriteRenderer sprite)
    {
        fuente = sprite;
        if (fuente == null) return;

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
    }

    private void LateUpdate()
    {
        if (!encendido || fuente == null || luz == null) return;
        luz.transform.position = fuente.bounds.center;
        luz.pointLightOuterRadius = Mathf.Max(0.7f, radioLuz);
        luz.intensity = intensidadLuz;

    }

    private void OnDisable() => Mostrar(false);
    private void OnDestroy()
    {
        if (luz != null) Destroy(luz.gameObject);
    }
}
