using UnityEngine;
using UnityEngine.UI;

// UI generada dentro de la escena de prueba; no requiere EventSystem ni captura entradas.
public class VistaCartas : MonoBehaviour
{
    private readonly ManoCartasJugador[] manos = new ManoCartasJugador[2];
    private readonly Camera[] camaras = new Camera[2];
    private readonly RectTransform[] paneles = new RectTransform[2];
    private readonly RectTransform[,] huecos = new RectTransform[2, 3];
    private readonly Image[,] imagenes = new Image[2, 3];
    private readonly Text[,] textos = new Text[2, 3];
    private readonly Outline[,] bordes = new Outline[2, 3];
    private RectTransform raiz;
    private RectTransform presentacion;
    private RectTransform capaVuelo;
    private RectTransform ventanaLanzamiento;
    private const float DuracionDespegue = 0.4f;
    private const float DuracionEntrada = 0.45f;
    private const float DuracionSalida = 0.5f;
    private Image imagenPresentacion;
    private Text textoPresentacion;
    private Font fuente;
    private readonly Color[] colores = { new Color(0.48f, 0.83f, 1f), new Color(0.95f, 0.2f, 0.25f) };

    public void Inicializar(ManoCartasJugador mano1, ManoCartasJugador mano2, Camera camara1, Camera camara2)
    {
        manos[0] = mano1; manos[1] = mano2;
        camaras[0] = camara1; camaras[1] = camara2;
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var lienzo = new GameObject("CanvasCartas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        lienzo.transform.SetParent(transform, false);
        raiz = (RectTransform)lienzo.transform;
        Canvas canvas = lienzo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = lienzo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        for (int i = 0; i < 2; i++)
        {
            paneles[i] = Rect("ManoP" + (i + 1), raiz);
            // Recorta las cartas escondidas en el borde de cada camara.
            paneles[i].gameObject.AddComponent<RectMask2D>();
            float esquina = i == 0 ? 0f : 1f;
            for (int j = 0; j < 3; j++)
            {
                huecos[i, j] = Rect("Carta" + (j + 1), paneles[i]);
                huecos[i, j].anchorMin = huecos[i, j].anchorMax = new Vector2(esquina, 0f);
                huecos[i, j].sizeDelta = new Vector2(170, 236);
                huecos[i, j].anchoredPosition = new Vector2(0f, -236f);
                imagenes[i, j] = huecos[i, j].gameObject.AddComponent<Image>();
                imagenes[i, j].raycastTarget = false;
                bordes[i, j] = huecos[i, j].gameObject.AddComponent<Outline>();
                bordes[i, j].effectColor = colores[i];
                bordes[i, j].effectDistance = new Vector2(3f, -3f);
                bordes[i, j].enabled = false;
                textos[i, j] = Texto("Frente", huecos[i, j], 20);
                textos[i, j].alignment = TextAnchor.UpperCenter;
                Estirar(textos[i, j].rectTransform);
                textos[i, j].rectTransform.offsetMin = new Vector2(14f, 14f);
                textos[i, j].rectTransform.offsetMax = new Vector2(-14f, -14f);
            }
        }
        capaVuelo = Rect("CapaVuelo", raiz);
        capaVuelo.anchorMin = Vector2.zero;
        capaVuelo.anchorMax = Vector2.one;
        capaVuelo.offsetMin = capaVuelo.offsetMax = Vector2.zero;
        ventanaLanzamiento = Rect("RecorteCamaraLanzador", capaVuelo);
        ventanaLanzamiento.gameObject.AddComponent<RectMask2D>();
        presentacion = Rect("CartaEnVuelo", capaVuelo);
        presentacion.anchorMin = presentacion.anchorMax = Vector2.zero;
        imagenPresentacion = presentacion.gameObject.AddComponent<Image>();
        imagenPresentacion.raycastTarget = false;
        // El HUD versus usa orden 100 y la cuenta regresiva 200. El vuelo
        // necesita su propio orden para pasar por encima de ambos.
        Canvas canvasVuelo = capaVuelo.gameObject.AddComponent<Canvas>();
        canvasVuelo.overrideSorting = true;
        canvasVuelo.sortingOrder = 300;
        textoPresentacion = Texto("Frente", presentacion, 26);
        Estirar(textoPresentacion.rectTransform);
        OcultarPresentacion();
    }

    private void LateUpdate()
    {
        if (raiz == null)
            return;
        for (int i = 0; i < 2; i++)
        {
            Rect viewport = camaras[i].rect;
            paneles[i].anchorMin = viewport.min;
            paneles[i].anchorMax = viewport.max;
            // Separa la mano superior de la barra versus y recorta solo en su camara.
            paneles[i].offsetMin = new Vector2(16f, viewport.yMin > 0f ? 52f : 12f);
            paneles[i].offsetMax = new Vector2(-16f, -12f);
            float escalaDiseno = Mathf.Clamp(Mathf.Min(paneles[i].rect.height / 440f,
                paneles[i].rect.width / 510f), 0.3f, 1f);
            ManoCartasJugador mano = manos[i];
            bool recibida = Time.time < mano.RecibidaHasta && !mano.MenuAbierto &&
                mano.IndiceRecibida >= 0 && mano.IndiceRecibida < mano.Cantidad;
            for (int j = 0; j < 3; j++)
            {
                bool visible = j < mano.Cantidad;
                huecos[i, j].gameObject.SetActive(visible);
                if (!visible) continue;
                bool seleccionada = mano.MenuAbierto && mano.Seleccion == j;
                bool esRecibida = recibida && j == mano.IndiceRecibida;
                // Mitad del recorrido anterior desde el reposo: -82 + (206 + 82) / 2.
                float y = seleccionada ? 62f : esRecibida ? 124f : -82f;
                float x = i == 0 ? 124f + j * 116f : -124f - (mano.Cantidad - 1 - j) * 116f;
                Vector2 destino = new Vector2(x, y) * escalaDiseno;
                huecos[i, j].anchoredPosition = Vector2.MoveTowards(huecos[i, j].anchoredPosition,
                    destino, 1800f * Time.deltaTime);
                float suavizado = 1f - Mathf.Exp(-18f * Time.deltaTime);
                huecos[i, j].localScale = Vector3.Lerp(huecos[i, j].localScale,
                    Vector3.one * escalaDiseno * (seleccionada ? 1.18f : 1f), suavizado);
                DefinicionCarta carta = mano.Cartas[j];
                imagenes[i, j].sprite = carta.Frente;
                bool suficiente = mano.Almas.Cantidad >= carta.PuntosNecesarios;
                float brillo = (suficiente ? 1f : 0.72f) *
                    (mano.MenuAbierto && !seleccionada ? 0.45f : 1f);
                Color colorCarta = carta.ColorCarta * brillo;
                colorCarta.a = 1f;
                imagenes[i, j].color = colorCarta;
                textos[i, j].text = $"Costo {carta.PuntosNecesarios:0.#}\n\n{carta.Titulo}\n\n{carta.Descripcion}";
                textos[i, j].color = suficiente ? new Color(0.12f, 0.16f, 0.22f) : new Color(0.48f, 0.12f, 0.1f);
                if (mano.MenuAbierto && !seleccionada)
                {
                    Color tinta = textos[i, j].color * 0.55f;
                    tinta.a = 1f;
                    textos[i, j].color = tinta;
                }
                bordes[i, j].enabled = seleccionada;
            }
            // La seleccion se dibuja por encima de sus vecinas al elevarse.
            if (mano.MenuAbierto && mano.Cantidad > 0)
                huecos[i, mano.Seleccion].SetAsLastSibling();
            else if (recibida)
                huecos[i, mano.IndiceRecibida].SetAsLastSibling();
        }
    }

    public static float DuracionAnimacion(float vistaFrente, float giro) =>
        DuracionDespegue + DuracionEntrada + vistaFrente + giro + DuracionSalida;

    public Vector2 ObtenerOrigenLanzamiento(ManoCartasJugador jugador)
    {
        int indice = jugador == manos[0] ? 0 : 1;
        Vector3 local = raiz.InverseTransformPoint(huecos[indice, jugador.Seleccion].position);
        return new Vector2((local.x - raiz.rect.xMin) / raiz.rect.width,
            (local.y - raiz.rect.yMin) / raiz.rect.height);
    }

    public void Animar(ManoCartasJugador jugador, DefinicionCarta carta, Vector2 origenPantalla,
        float tiempo, float vistaFrente, float giro)
    {
        int origen = jugador == manos[0] ? 0 : 1;
        int destino = carta.Destino == DestinoCarta.Propio ? origen : 1 - origen;
        Vector2 tamano = raiz.rect.size;
        Rect viewport = camaras[origen].rect;
        Vector2 inicio = Vector2.Scale(origenPantalla, tamano);
        Vector2 centro = new Vector2(viewport.center.x * tamano.x, tamano.y * 0.5f);
        // Entrada global desde el borde correspondiente al lanzador.
        float lado = viewport.center.y < centro.y / tamano.y ? -1f : 1f;
        Vector2 entrada = new Vector2(centro.x, lado < 0f ? -190f : tamano.y + 190f);
        Rect llegada = camaras[destino].rect;
        Vector2 final = new Vector2(centro.x,
            llegada.center.y < 0.5f ? -190f : tamano.y + 190f);
        float inicioVista = DuracionDespegue + DuracionEntrada;
        float inicioGiro = inicioVista + vistaFrente;
        float inicioSalida = inicioGiro + giro;
        float escala = 1f;
        bool frente = true;
        Vector2 posicion;
        presentacion.localScale = Vector3.one;
        if (tiempo < DuracionDespegue)
        {
            // Despegue vertical, recortado a la camara propia hasta desaparecer arriba.
            ventanaLanzamiento.anchorMin = viewport.min;
            ventanaLanzamiento.anchorMax = viewport.max;
            ventanaLanzamiento.offsetMin = ventanaLanzamiento.offsetMax = Vector2.zero;
            if (presentacion.parent != ventanaLanzamiento)
                presentacion.SetParent(ventanaLanzamiento, false);
            float t = Mathf.Clamp01(tiempo / DuracionDespegue);
            Vector2 salidaLocal = new Vector2(inicio.x, viewport.yMax * tamano.y + 190f);
            posicion = Vector2.Lerp(inicio, salidaLocal, t * t) - Vector2.Scale(viewport.min, tamano);
            escala = 0.72f;
        }
        else
        {
            if (presentacion.parent != capaVuelo)
                presentacion.SetParent(capaVuelo, false);
            if (tiempo < inicioVista)
            {
                float t = Mathf.SmoothStep(0f, 1f, (tiempo - DuracionDespegue) / DuracionEntrada);
                posicion = Vector2.Lerp(entrada, centro, t);
            }
            else if (tiempo < inicioGiro)
                posicion = centro; // Frente quieto para que se pueda leer antes del giro.
            else if (tiempo < inicioSalida)
            {
                posicion = centro;
                float t = Mathf.SmoothStep(0f, 1f, (tiempo - inicioGiro) / Mathf.Max(0.1f, giro));
                float coseno = Mathf.Cos(t * Mathf.PI * 4f);
                frente = coseno >= 0f;
                presentacion.localScale = new Vector3(Mathf.Max(0.015f, Mathf.Abs(coseno)), 1f, 1f);
            }
            else
            {
                float t = Mathf.Clamp01((tiempo - inicioSalida) / DuracionSalida);
                posicion = Vector2.Lerp(centro, final, t * t);
            }
        }
        presentacion.gameObject.SetActive(true);
        presentacion.anchoredPosition = posicion;
        presentacion.sizeDelta = new Vector2(230, 320) * escala;
        imagenPresentacion.sprite = frente ? carta.Frente : carta.Reverso;
        imagenPresentacion.color = carta.ColorCarta;
        textoPresentacion.gameObject.SetActive(frente);
        textoPresentacion.text = $"{carta.Titulo}\n\nCosto {carta.PuntosNecesarios:0.#}\n\n{carta.Descripcion}\nP{origen + 1}";
    }

    public void OcultarPresentacion()
    {
        if (presentacion != null) presentacion.gameObject.SetActive(false);
    }

    private RectTransform Rect(string nombre, Transform padre)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        return (RectTransform)objeto.transform;
    }

    private Text Texto(string nombre, Transform padre, int tamano)
    {
        Text texto = Rect(nombre, padre).gameObject.AddComponent<Text>();
        texto.font = fuente;
        texto.fontSize = tamano;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(0.12f, 0.16f, 0.22f);
        texto.raycastTarget = false;
        return texto;
    }

    private static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 8f);
        rect.offsetMax = new Vector2(-8f, -8f);
    }
}
