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
    private readonly Image[,] fondosEstado = new Image[2, 3];
    private readonly Outline[,] bordes = new Outline[2, 3];
    private static readonly Vector2 TamanoCarta = new Vector2(150f, 250f);
    private static readonly Vector2 TamanoVuelo = new Vector2(228f, 380f);
    private const float PrimerHueco = 100f, Separacion = 160f;
    private readonly DefinicionCarta[,] cartasAnteriores = new DefinicionCarta[2, 3];
    private readonly Vector2[,] inicioMezcla = new Vector2[2, 3];
    private readonly bool[] confusionAnterior = new bool[2];
    private readonly bool[] animandoMano = new bool[2];
    private readonly float[] inicioGiroMano = new float[2];
    private readonly int[] cantidadAnimada = new int[2];
    private const float DuracionGiroMano = 0.4f;
    private const float DuracionMezclaMano = 0.55f;
    private RectTransform raiz;
    private RectTransform presentacion;
    private RectTransform capaVuelo;
    private RectTransform ventanaLanzamiento;
    private const float DuracionDespegue = 0.4f;
    private const float DuracionEntrada = 0.45f;
    private const float DuracionSalida = 0.5f;
    private sealed class Vuelo
    {
        public RectTransform carta, ventana;
        public float desplazamiento;
    }
    private readonly System.Collections.Generic.Dictionary<int, Vuelo> vuelos =
        new System.Collections.Generic.Dictionary<int, Vuelo>();
    private Image imagenPresentacion;
    private Text textoPresentacion;
    private Outline bordePresentacion;
    private readonly Image[] flashSwap = new Image[2];
    private float opacidadFlashSwap;
    public const float DuracionEntregaPotente = 2.2f;
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
        RectTransform capaFlash = Rect("FlashSwap", raiz);
        capaFlash.anchorMin = Vector2.zero;
        capaFlash.anchorMax = Vector2.one;
        capaFlash.offsetMin = capaFlash.offsetMax = Vector2.zero;
        Canvas canvasFlash = capaFlash.gameObject.AddComponent<Canvas>();
        canvasFlash.overrideSorting = true;
        canvasFlash.sortingOrder = 250; // Blanco sobre las camaras y HUD, debajo de las cartas.
        for (int i = 0; i < flashSwap.Length; i++)
        {
            RectTransform pantalla = Rect("FlashP" + (i + 1), capaFlash);
            flashSwap[i] = pantalla.gameObject.AddComponent<Image>();
            flashSwap[i].raycastTarget = false;
            flashSwap[i].color = new Color(1f, 1f, 1f, 0f);
        }
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
                huecos[i, j].sizeDelta = TamanoCarta;
                huecos[i, j].anchoredPosition = new Vector2(0f, -250f);
                imagenes[i, j] = huecos[i, j].gameObject.AddComponent<Image>();
                imagenes[i, j].raycastTarget = false;
                imagenes[i, j].preserveAspect = true;
                bordes[i, j] = huecos[i, j].gameObject.AddComponent<Outline>();
                bordes[i, j].effectColor = colores[i];
                bordes[i, j].effectDistance = new Vector2(3f, -3f);
                bordes[i, j].enabled = false;
                RectTransform etiqueta = Rect("EstadoCarta", huecos[i, j]);
                etiqueta.anchorMin = etiqueta.anchorMax = new Vector2(0.5f, 1f);
                etiqueta.pivot = new Vector2(0.5f, 0f);
                etiqueta.anchoredPosition = new Vector2(0f, 3f);
                etiqueta.sizeDelta = new Vector2(150f, 28f);
                fondosEstado[i, j] = FondoFicha(etiqueta);
                textos[i, j] = Texto("Precio", etiqueta, 20);
                textos[i, j].resizeTextForBestFit = true;
                textos[i, j].resizeTextMinSize = 12; textos[i, j].resizeTextMaxSize = 20;
                Estirar(textos[i, j].rectTransform);
                textos[i, j].rectTransform.offsetMin = new Vector2(4f, 2f);
                textos[i, j].rectTransform.offsetMax = new Vector2(-4f, -2f);
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
        imagenPresentacion.preserveAspect = true;
        bordePresentacion = presentacion.gameObject.AddComponent<Outline>();
        bordePresentacion.effectColor = new Color(1f, 0.8f, 0.2f);
        bordePresentacion.effectDistance = new Vector2(5f, -5f);
        bordePresentacion.enabled = false;
        // El HUD versus usa orden 100 y la cuenta regresiva 200. El vuelo
        // necesita su propio orden para pasar por encima de ambos.
        Canvas canvasVuelo = capaVuelo.gameObject.AddComponent<Canvas>();
        canvasVuelo.overrideSorting = true;
        canvasVuelo.sortingOrder = 300;
        RectTransform etiquetaVuelo = Rect("EtiquetaVuelo", presentacion);
        etiquetaVuelo.anchorMin = etiquetaVuelo.anchorMax = new Vector2(0.5f, 1f);
        etiquetaVuelo.pivot = new Vector2(0.5f, 0f);
        etiquetaVuelo.anchoredPosition = new Vector2(0f, 6f);
        etiquetaVuelo.sizeDelta = new Vector2(190f, 32f);
        FondoFicha(etiquetaVuelo);
        textoPresentacion = Texto("Frente", etiquetaVuelo, 21);
        textoPresentacion.color = Color.white;
        textoPresentacion.resizeTextForBestFit = true;
        textoPresentacion.resizeTextMinSize = 16; textoPresentacion.resizeTextMaxSize = 21;
        Estirar(textoPresentacion.rectTransform);
        textoPresentacion.rectTransform.offsetMin = new Vector2(4f, 2f);
        textoPresentacion.rectTransform.offsetMax = new Vector2(-4f, -2f);
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
            // El recorte inferior coincide con el borde real de cada camara.
            paneles[i].offsetMin = new Vector2(16f, 0f);
            paneles[i].offsetMax = new Vector2(-16f, -12f);
            float escalaDiseno = Mathf.Clamp(Mathf.Min(paneles[i].rect.height / 440f,
                paneles[i].rect.width / 510f), 0.3f, 1f);
            // Elevar la mano sobre el marcador central sin mover su mascara de recorte.
            float elevacionMano = viewport.yMin > 0f ? 28f : 0f;
            ManoCartasJugador mano = manos[i];
            bool confundido = mano.EstaConfundido;
            if (confundido != confusionAnterior[i])
                IniciarTransicionMano(i, mano, confundido);
            if (cantidadAnimada[i] != mano.Cantidad) animandoMano[i] = false;
            float tiempoMano = Time.time - inicioGiroMano[i];
            if (tiempoMano >= DuracionGiroMano + (confundido ? DuracionMezclaMano : 0f))
                animandoMano[i] = false;
            float progresoGiro = Mathf.Clamp01(tiempoMano / DuracionGiroMano);
            bool reverso = animandoMano[i] && progresoGiro < 0.5f ? !confundido : confundido;
            float anchoGiro = animandoMano[i] ? Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(progresoGiro * Mathf.PI))) : 1f;
            bool recibida = Time.time < mano.RecibidaHasta && !mano.MenuAbierto &&
                mano.IndiceRecibida >= 0 && mano.IndiceRecibida < mano.Cantidad;
            for (int j = 0; j < 3; j++)
            {
                bool visible = j < mano.Cantidad;
                huecos[i, j].gameObject.SetActive(visible);
                if (!visible) continue;
                bool seleccionada = mano.MenuAbierto && mano.Seleccion == j;
                bool esRecibida = recibida && j == mano.IndiceRecibida;
                // Elevar la mano abierta y mostrar brevemente las cartas recibidas.
                float y = mano.MenuAbierto ? (seleccionada ? 165f : 140f) : esRecibida ? 124f : -82f;
                float x = i == 0 ? PrimerHueco + j * Separacion : -PrimerHueco - (mano.Cantidad - 1 - j) * Separacion;
                Vector2 destino = new Vector2(x, y) * escalaDiseno;
                destino.y += elevacionMano;
                if (animandoMano[i])
                {
                    float mezcla = confundido ? Mathf.SmoothStep(0f, 1f,
                        (tiempoMano - DuracionGiroMano) / DuracionMezclaMano) : 0f;
                    Vector2 salida = inicioMezcla[i, j];
                    Vector2 llegada = new Vector2(destino.x, 128f * escalaDiseno + elevacionMano);
                    Vector2 posicion = Vector2.Lerp(salida, llegada, mezcla);
                    float levantar = Mathf.SmoothStep(0f, 1f, progresoGiro);
                    posicion.y = Mathf.Lerp(salida.y, llegada.y, levantar) +
                        Mathf.Sin(mezcla * Mathf.PI) * (j % 2 == 0 ? 32f : 56f) * escalaDiseno;
                    huecos[i, j].anchoredPosition = posicion;
                    huecos[i, j].localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Sin(mezcla * Mathf.PI) * (j % 2 == 0 ? -12f : 12f));
                }
                else
                {
                    huecos[i, j].anchoredPosition = Vector2.MoveTowards(huecos[i, j].anchoredPosition,
                        destino, 1800f * Time.deltaTime);
                    huecos[i, j].localRotation = Quaternion.identity;
                }
                float suavizado = 1f - Mathf.Exp(-18f * Time.deltaTime);
                float escalaCarta = Mathf.Lerp(huecos[i, j].localScale.y,
                    escalaDiseno * (seleccionada ? 1.08f : 1f), suavizado);
                huecos[i, j].localScale = new Vector3(escalaCarta * anchoGiro, escalaCarta, 1f);
                DefinicionCarta carta = mano.Cartas[j];
                cartasAnteriores[i, j] = carta;
                imagenes[i, j].sprite = reverso
                    ? (seleccionada ? carta.Reverso : carta.ReversoMano)
                    : (seleccionada || esRecibida ? carta.Frente : carta.FrenteMano);
                bool suficiente = mano.Almas.Cantidad >= carta.PuntosNecesarios;
                float brillo = (reverso || suficiente ? 1f : 0.72f) *
                    (mano.MenuAbierto && !seleccionada ? 0.72f : 1f);
                imagenes[i, j].color = new Color(brillo, brillo, brillo, 1f);
                fondosEstado[i, j].enabled = !reverso;
                textos[i, j].text = reverso ? "" :
                    $"{carta.PuntosNecesarios:0.#} almas";
                textos[i, j].color = suficiente ? Color.white : new Color(1f, 0.62f, 0.55f);
                bordes[i, j].effectColor = !reverso && carta.EsPotente ? new Color(1f, 0.8f, 0.2f) : colores[i];
                bordes[i, j].enabled = seleccionada || (!reverso && carta.EsPotente);
            }
            // La seleccion se dibuja por encima de sus vecinas al elevarse.
            if (mano.MenuAbierto && mano.Cantidad > 0)
                huecos[i, mano.Seleccion].SetAsLastSibling();
            else if (recibida)
                huecos[i, mano.IndiceRecibida].SetAsLastSibling();
        }
    }

    private static Image FondoFicha(RectTransform rect)
    {
        Image fondo = rect.gameObject.AddComponent<Image>();
        fondo.color = new Color(0.035f, 0.02f, 0.025f, 0.94f);
        fondo.raycastTarget = false;
        return fondo;
    }

    private void IniciarTransicionMano(int jugador, ManoCartasJugador mano, bool confundido)
    {
        var posiciones = new Vector2[3];
        var usados = new bool[3];
        for (int i = 0; i < 3; i++) posiciones[i] = huecos[jugador, i].anchoredPosition;
        for (int destino = 0; destino < mano.Cantidad; destino++)
        {
            int origen = destino;
            // Seguir cada carta desde su hueco anterior hasta el nuevo, incluso con duplicadas.
            for (int candidato = 0; candidato < mano.Cantidad; candidato++)
            {
                if (!usados[candidato] && cartasAnteriores[jugador, candidato] == mano.Cartas[destino])
                { origen = candidato; break; }
            }
            usados[origen] = true;
            inicioMezcla[jugador, destino] = posiciones[origen];
        }
        confusionAnterior[jugador] = confundido;
        inicioGiroMano[jugador] = Time.time;
        cantidadAnimada[jugador] = mano.Cantidad;
        animandoMano[jugador] = mano.Cantidad > 0;
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
        float tiempo, float vistaFrente, float giro, float desplazamiento = 0f)
    {
        int origen = jugador == manos[0] ? 0 : 1;
        int destino = carta.Destino == DestinoCarta.Propio ? origen : 1 - origen;
        Vector2 tamano = raiz.rect.size;
        Rect viewport = camaras[origen].rect;
        Vector2 inicio = Vector2.Scale(origenPantalla, tamano);
        Vector2 centro = new Vector2(viewport.center.x * tamano.x, tamano.y * 0.5f);
        centro.x += desplazamiento;
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
        presentacion.localRotation = Quaternion.identity;
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
                posicion = centro; // Vistazo breve al icono antes del giro.
            else if (tiempo < inicioSalida)
            {
                posicion = centro;
                float t = Mathf.SmoothStep(0f, 1f, (tiempo - inicioGiro) / Mathf.Max(0.1f, giro));
                float coseno = Mathf.Cos(t * Mathf.PI * 2f);
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
        presentacion.sizeDelta = TamanoVuelo * escala;
        imagenPresentacion.sprite = frente ? carta.Frente : carta.Reverso;
        imagenPresentacion.color = Color.white;
        bordePresentacion.enabled = carta.EsPotente;
        textoPresentacion.transform.parent.gameObject.SetActive(frente);
        textoPresentacion.text = $"{carta.PuntosNecesarios:0.#} almas";
    }

    public void AnimarEntregaPotente(ManoCartasJugador jugador, DefinicionCarta carta, float tiempo,
        float desplazamiento = 0f, int indiceEntrega = 0)
    {
        int indice = jugador == manos[0] ? 0 : 1;
        Rect viewport = camaras[indice].rect;
        Vector2 tamano = raiz.rect.size;
        Vector2 centro = Vector2.Scale(viewport.center, tamano);
        centro.x += desplazamiento;
        if (presentacion.parent != capaVuelo) presentacion.SetParent(capaVuelo, false);
        float escalaDiseno = Mathf.Clamp(Mathf.Min(paneles[indice].rect.height / 440f,
            paneles[indice].rect.width / 510f), 0.3f, 1f);
        // La carta aun no esta en la mano. Reservar el cupo impide llenarlo durante el giro.
        int hueco = Mathf.Min(2, jugador.Cantidad + indiceEntrega);
        float x = indice == 0 ? PrimerHueco + hueco * Separacion : -PrimerHueco - indiceEntrega * Separacion;
        float y = jugador.MenuAbierto ? 140f : 124f;
        Vector3 localDestino = new Vector3(x * escalaDiseno, y * escalaDiseno, 0f);
        // La mano usa esquinas distintas: TransformPoint espera coordenadas desde su pivote central.
        localDestino.x += indice == 0 ? paneles[indice].rect.xMin : paneles[indice].rect.xMax;
        localDestino.y += paneles[indice].rect.yMin;
        if (viewport.yMin > 0f) localDestino.y += 28f;
        Vector3 destinoLocal = raiz.InverseTransformPoint(paneles[indice].TransformPoint(localDestino));
        Vector2 destino = new Vector2(destinoLocal.x - raiz.rect.xMin, destinoLocal.y - raiz.rect.yMin);
        float giro = Mathf.Clamp01((tiempo - 0.25f) / 1f);
        float coseno = Mathf.Cos(giro * Mathf.PI * 4f);
        bool frente = coseno >= 0f;
        float llegada = Mathf.SmoothStep(0f, 1f, (tiempo - 1.6f) / 0.6f);
        float aparicion = Mathf.SmoothStep(0.15f, 1f, tiempo / 0.25f);
        float escala = Mathf.Lerp(aparicion, TamanoCarta.x / TamanoVuelo.x * escalaDiseno, llegada);
        presentacion.gameObject.SetActive(true);
        presentacion.anchoredPosition = Vector2.Lerp(centro, destino, llegada);
        presentacion.sizeDelta = TamanoVuelo * escala;
        presentacion.localScale = new Vector3(Mathf.Max(0.02f, Mathf.Abs(coseno)), 1f, 1f);
        presentacion.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(giro * Mathf.PI * 2f) * 8f);
        imagenPresentacion.sprite = frente ? carta.Frente : carta.Reverso;
        imagenPresentacion.color = Color.white;
        bordePresentacion.enabled = true;
        textoPresentacion.transform.parent.gameObject.SetActive(frente);
        textoPresentacion.text = $"{carta.PuntosNecesarios:0.#} almas";
    }

    public void OcultarPresentacion()
    {
        if (presentacion != null) presentacion.gameObject.SetActive(false);
    }

    public void ActualizarFlashSwap(float intensidad, bool animando)
    {
        opacidadFlashSwap = animando ? Mathf.Max(opacidadFlashSwap, Mathf.Clamp01(intensidad)) :
            Mathf.MoveTowards(opacidadFlashSwap, 0f, Time.deltaTime / 0.1f);
        MostrarFlashSwap();
    }

    public void LimpiarFlashSwap()
    {
        opacidadFlashSwap = 0f;
        MostrarFlashSwap();
    }

    private void MostrarFlashSwap()
    {
        for (int i = 0; i < flashSwap.Length; i++)
        {
            if (flashSwap[i] == null || camaras[i] == null) continue;
            RectTransform rect = flashSwap[i].rectTransform;
            rect.anchorMin = camaras[i].rect.min;
            rect.anchorMax = camaras[i].rect.max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            flashSwap[i].color = new Color(1f, 1f, 1f, opacidadFlashSwap);
            flashSwap[i].gameObject.SetActive(opacidadFlashSwap > 0f);
        }
    }

    public void AnimarUso(int id, ManoCartasJugador jugador, DefinicionCarta carta, Vector2 origen,
        float tiempo, float frente, float giro, int indice, int cantidad, bool esEntrega = false)
    {
        if (!vuelos.TryGetValue(id, out Vuelo vuelo))
        {
            vuelo = new Vuelo
            {
                carta = Instantiate(presentacion, capaVuelo),
                ventana = Rect("RecorteUso" + id, capaVuelo)
            };
            vuelo.ventana.gameObject.AddComponent<RectMask2D>();
            vuelos.Add(id, vuelo);
        }
        float espacio = Mathf.Min(260f, raiz.rect.width / Mathf.Max(1, cantidad));
        float destino = (indice - (cantidad - 1) * 0.5f) * espacio;
        vuelo.desplazamiento = Mathf.Lerp(vuelo.desplazamiento, destino, 1f - Mathf.Exp(-16f * Time.deltaTime));
        RectTransform original = presentacion;
        RectTransform ventanaOriginal = ventanaLanzamiento;
        presentacion = vuelo.carta;
        ventanaLanzamiento = vuelo.ventana;
        imagenPresentacion = presentacion.GetComponent<Image>();
        textoPresentacion = presentacion.GetComponentInChildren<Text>(true);
        bordePresentacion = presentacion.GetComponent<Outline>();
        if (esEntrega)
            AnimarEntregaPotente(jugador, carta, tiempo, vuelo.desplazamiento, indice);
        else
            Animar(jugador, carta, origen, tiempo, frente, giro, vuelo.desplazamiento);
        presentacion = original;
        ventanaLanzamiento = ventanaOriginal;
        imagenPresentacion = original.GetComponent<Image>();
        textoPresentacion = original.GetComponentInChildren<Text>(true);
        bordePresentacion = original.GetComponent<Outline>();
    }

    public void OcultarUso(int id)
    {
        if (!vuelos.TryGetValue(id, out Vuelo vuelo)) return;
        vuelo.carta.gameObject.SetActive(false);
        Destroy(vuelo.carta.gameObject);
        Destroy(vuelo.ventana.gameObject);
        vuelos.Remove(id);
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
