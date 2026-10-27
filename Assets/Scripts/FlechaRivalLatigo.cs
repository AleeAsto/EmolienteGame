using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public sealed class FlechaRivalLatigo : MonoBehaviour
{
    private AtaqueJugador ataque;
    private Camera camara;
    private CicloPartida partida;
    private GameObject lienzo;
    private RectTransform flecha;

    public bool Visible => flecha != null && flecha.gameObject.activeSelf;
    public int Direccion { get; private set; }

    public void Inicializar(AtaqueJugador propietario)
    {
        ataque = propietario;
        foreach (var seguimiento in FindObjectsByType<CamaraPasillo>(FindObjectsSortMode.None))
            if (seguimiento.Jugador == transform) { camara = seguimiento.GetComponent<Camera>(); break; }
        foreach (var ciclo in FindObjectsByType<CicloPartida>(FindObjectsSortMode.None))
            if (ciclo.gameObject.scene == gameObject.scene) { partida = ciclo; break; }
        lienzo = new GameObject("GuiaLatigo_" + name, typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(lienzo, gameObject.scene);
        var canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        var escalador = lienzo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1280,720);
        escalador.matchWidthOrHeight = 0.5f;
        var objeto = new GameObject("FlechaRival", typeof(RectTransform), typeof(CanvasRenderer), typeof(GraficoFlechaUI));
        objeto.transform.SetParent(lienzo.transform, false);
        flecha = (RectTransform)objeto.transform;
        flecha.sizeDelta = new Vector2(64,48);
        var grafico = objeto.GetComponent<GraficoFlechaUI>();
        grafico.color = new Color32(235,38,48,255);
        grafico.raycastTarget = false;
        objeto.SetActive(false);
    }

    private void LateUpdate()
    {
        if (flecha == null) return;
        Transform objetivo = ataque != null ? ataque.ObjetivoLatigo : null;
        bool activo = objetivo != null && camara != null && camara.isActiveAndEnabled &&
            partida != null && partida.EnCurso && !partida.Terminada && Time.timeScale > 0f;
        Vector3 vista = Vector3.zero;
        if (activo)
        {
            var movimiento = objetivo.GetComponent<MovimientoJugador2D>();
            vista = camara.WorldToViewportPoint(movimiento != null ? movimiento.PosicionPies : objetivo.position);
            activo = vista.z > 0f && (vista.x < 0f || vista.x > 1f);
        }
        flecha.gameObject.SetActive(activo);
        Direccion = activo ? (vista.x < 0f ? -1 : 1) : 0;
        if (!activo) return;
        Rect zona = camara.rect;
        flecha.anchorMin = flecha.anchorMax = new Vector2(Direccion < 0 ? zona.xMin : zona.xMax, zona.yMax);
        float margen = 38f + Mathf.Sin(Time.time * 6f) * 6f;
        flecha.anchoredPosition = new Vector2(-Direccion * margen, -38f);
        flecha.localScale = new Vector3(Direccion,1,1);
    }

    private void OnDisable()
    {
        Direccion = 0;
        if (flecha != null) flecha.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (lienzo != null) Destroy(lienzo);
    }
}
