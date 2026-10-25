using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// La misma pareja de paneles presenta el VS y despues cubre las franjas laterales.
public class PresentacionVersus : MonoBehaviour
{
    private RectTransform raiz;
    private readonly RectTransform[] paneles = new RectTransform[2];
    private readonly RectTransform[] retratos = new RectTransform[2];
    private readonly RectTransform[] bordes = new RectTransform[2];
    private readonly CanvasGroup[] nombres = new CanvasGroup[2];
    private RectTransform centro;
    private CanvasGroup grupoCentro;
    private Camera[] camaras;
    private float apertura;
    private bool creada;

    public void Inicializar(Sprite morado, Sprite rojo, string nombre1, string nombre2, Camera[] vistas)
    {
        camaras = vistas;
        GameObject lienzo = new GameObject("CanvasVersusYMarcos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        raiz = (RectTransform)lienzo.transform;
        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        CanvasScaler scaler = lienzo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Sprite[] imagenes = { morado, rojo };
        string[] etiquetas = { nombre1, nombre2 };
        Color[] fondos = { new Color(0.13f, 0.07f, 0.31f), new Color(0.25f, 0.035f, 0.05f) };
        for (int i = 0; i < 2; i++)
        {
            paneles[i] = Rect(i == 0 ? "MarcoMorado" : "MarcoRojo", raiz);
            paneles[i].anchorMin = new Vector2(i, 0f);
            paneles[i].anchorMax = new Vector2(i, 1f);
            paneles[i].pivot = new Vector2(i, 0.5f);
            Imagen(paneles[i], fondos[i]);
            paneles[i].gameObject.AddComponent<RectMask2D>();
            retratos[i] = Rect("Retrato", paneles[i]);
            Image retrato = Imagen(retratos[i], Color.white);
            retrato.sprite = imagenes[i];
            // Voltear solo la ilustracion; los nombres siempre se leen normalmente.
            retratos[i].localScale = new Vector3(i == 0 ? 1f : -1f, 1f, 1f);
            RectTransform etiqueta = Rect("Nombre", paneles[i]);
            etiqueta.anchorMin = new Vector2(0f, 0.83f);
            etiqueta.anchorMax = new Vector2(1f, 0.95f);
            etiqueta.offsetMin = new Vector2(24f, 0f);
            etiqueta.offsetMax = new Vector2(-24f, 0f);
            Imagen(etiqueta, new Color(0.02f, 0.015f, 0.025f, 0.7f));
            nombres[i] = etiqueta.gameObject.AddComponent<CanvasGroup>();
            Text nombre = Texto("TextoNombre", etiqueta, etiquetas[i], 60);
            nombre.resizeTextForBestFit = true;
            nombre.resizeTextMinSize = 22;
            nombre.resizeTextMaxSize = 60;
            Estirar(nombre.rectTransform, 12f);
            bordes[i] = Rect("BordeInterior", paneles[i]);
            bordes[i].anchorMin = new Vector2(1 - i, 0f);
            bordes[i].anchorMax = new Vector2(1 - i, 1f);
            bordes[i].sizeDelta = new Vector2(4f, 0f);
            Imagen(bordes[i], new Color(0.92f, 0.86f, 0.72f));
        }
        centro = Rect("SeparadorVersus", raiz);
        centro.anchorMin = new Vector2(0.5f, 0f);
        centro.anchorMax = new Vector2(0.5f, 1f);
        centro.sizeDelta = new Vector2(6f, 0f);
        Imagen(centro, new Color(0.97f, 0.9f, 0.72f));
        grupoCentro = centro.gameObject.AddComponent<CanvasGroup>();
        RectTransform insignia = Rect("InsigniaVS", centro);
        insignia.sizeDelta = new Vector2(250f, 180f);
        Imagen(insignia, new Color(0.04f, 0.025f, 0.06f));
        Text vs = Texto("VS", insignia, "VS", 112);
        vs.fontStyle = FontStyle.BoldAndItalic;
        vs.color = new Color(1f, 0.9f, 0.62f);
        Estirar(vs.rectTransform, 0f);
        creada = true;
        Canvas.ForceUpdateCanvases();
        ActualizarDiseno();
    }

    public IEnumerator Mostrar(float duracionVS, float duracionApertura)
    {
        float tiempo = 0f;
        while (tiempo < duracionVS)
        {
            float golpe = Mathf.Clamp01(tiempo / 0.25f);
            centro.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, 1f - Mathf.Pow(1f - golpe, 3f));
            tiempo += Time.unscaledDeltaTime;
            yield return null;
        }
        tiempo = 0f;
        while (tiempo < duracionApertura)
        {
            tiempo = Mathf.Min(duracionApertura, tiempo + Time.unscaledDeltaTime);
            float t = tiempo / Mathf.Max(0.05f, duracionApertura);
            apertura = t * t * (3f - 2f * t);
            grupoCentro.alpha = 1f - Mathf.Clamp01(t * 3f);
            for (int i = 0; i < 2; i++) nombres[i].alpha = 1f - Mathf.Clamp01(t * 2f);
            ActualizarDiseno();
            yield return null;
        }
        apertura = 1f;
        centro.gameObject.SetActive(false);
        ActualizarDiseno();
    }

    private void LateUpdate()
    {
        if (creada) ActualizarDiseno();
    }

    private void ActualizarDiseno()
    {
        float izquierda = 1f;
        float derecha = 0f;
        bool encontrada = false;
        if (camaras != null)
            foreach (Camera camara in camaras)
            {
                if (camara == null) continue;
                izquierda = Mathf.Min(izquierda, camara.rect.xMin);
                derecha = Mathf.Max(derecha, camara.rect.xMax);
                encontrada = true;
            }
        if (!encontrada) { izquierda = 0.15f; derecha = 0.85f; }
        for (int i = 0; i < 2; i++)
        {
            float final = i == 0 ? izquierda : 1f - derecha;
            float ancho = Mathf.Lerp(0.5f, final, apertura) * raiz.rect.width;
            paneles[i].sizeDelta = new Vector2(ancho, 0f);
            paneles[i].anchoredPosition = Vector2.zero;
            // Mantener proporcion de la ilustracion; la mascara recorta el retrato, no lo deforma.
            float lado = Mathf.Max(raiz.rect.height, ancho);
            retratos[i].sizeDelta = new Vector2(lado, lado);
            retratos[i].anchoredPosition = Vector2.zero;
            bordes[i].gameObject.SetActive(ancho > 1f);
        }
    }

    private static RectTransform Rect(string nombre, Transform padre)
    {
        RectTransform rect = (RectTransform)new GameObject(nombre, typeof(RectTransform)).transform;
        rect.SetParent(padre, false);
        return rect;
    }

    private static Image Imagen(RectTransform rect, Color color)
    {
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static Text Texto(string nombre, Transform padre, string contenido, int tamano)
    {
        Text texto = Rect(nombre, padre).gameObject.AddComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = contenido;
        texto.fontSize = tamano;
        texto.fontStyle = FontStyle.Bold;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.raycastTarget = false;
        Shadow sombra = texto.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.75f);
        sombra.effectDistance = new Vector2(3f, -3f);
        return texto;
    }

    private static void Estirar(RectTransform rect, float margen)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * margen;
        rect.offsetMax = Vector2.one * -margen;
    }
}
