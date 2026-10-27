using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public class VisualLatigoJugador : MonoBehaviour
{
    private AtaqueJugador ataque;
    private LineRenderer circulo;
    private LineRenderer cuerda;
    private Color colorJugador = Color.cyan;

    public void Inicializar(AtaqueJugador propietario)
    {
        ataque = propietario;
        gameObject.AddComponent<FlechaRivalLatigo>().Inicializar(propietario);
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
            if (sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null)
            {
                colorJugador = sprite.color;
                break;
            }

        var material = Resources.Load<Material>("MaterialLatigo");
        if (material == null)
        {
            Debug.LogError("Falta Resources/MaterialLatigo para dibujar el latigo.", this);
            enabled = false;
            return;
        }
        circulo = CrearLinea("ApuntadoLatigo", material, 48, 0.065f, 8);
        circulo.loop = true;
        cuerda = CrearLinea("CuerdaLatigo", material, 20, 0.09f, 31);
        cuerda.endWidth = 0.025f;
    }

    private LineRenderer CrearLinea(string nombre, Material material, int puntos, float ancho, int orden)
    {
        GameObject objeto = new GameObject(nombre, typeof(LineRenderer));
        objeto.layer = gameObject.layer;
        objeto.transform.SetParent(transform, false);
        var linea = objeto.GetComponent<LineRenderer>();
        linea.sharedMaterial = material;
        linea.useWorldSpace = true;
        linea.positionCount = puntos;
        linea.startWidth = linea.endWidth = ancho;
        linea.numCapVertices = 3;
        linea.sortingOrder = orden;
        var grupo = objeto.AddComponent<UnityEngine.Rendering.SortingGroup>();
        grupo.sortingOrder = orden;
        grupo.sortAtRoot = true;
        linea.enabled = false;
        return linea;
    }

    private void LateUpdate()
    {
        if (ataque == null || circulo == null || cuerda == null)
            return;
        bool activo = ataque.EsAtaqueLatigo;
        circulo.enabled = activo || ataque.MostrarApuntadoLatigo;
        cuerda.enabled = activo;
        if (circulo.enabled)
        {
            Vector3 centro = ataque.PuntoLatigo;
            Vector2 radio = ataque.RadioApuntadoLatigo;
            for (int i = 0; i < circulo.positionCount; i++)
            {
                float angulo = i * Mathf.PI * 2f / circulo.positionCount;
                circulo.SetPosition(i, centro + new Vector3(Mathf.Cos(angulo) * radio.x, Mathf.Sin(angulo) * radio.y, 0f));
            }
            Color color = activo ? new Color(1f, 0.8f, 0.25f, 1f) : ataque.ObjetivoEnApuntadoLatigo
                ? Color.Lerp(colorJugador, Color.white, 0.35f) : new Color(0.65f, 0.65f, 0.65f, 0.55f);
            circulo.startColor = circulo.endColor = color;
        }
        if (activo)
        {
            Vector3 mano = ataque.ManoLatigo;
            Vector3 punta = ataque.PosicionGolpe;
            float arco = 0.25f + 0.5f * Mathf.Sin(ataque.ProgresoGolpe * Mathf.PI);
            for (int i = 0; i < cuerda.positionCount; i++)
            {
                float t = (float)i / (cuerda.positionCount - 1);
                cuerda.SetPosition(i, Vector3.Lerp(mano, punta, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arco));
            }
            cuerda.startColor = Color.white;
            cuerda.endColor = new Color(1f, 0.7f, 0.2f, 1f);
        }
    }

    private void OnDisable()
    {
        if (circulo != null) circulo.enabled = false;
        if (cuerda != null) cuerda.enabled = false;
    }
}
