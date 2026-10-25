using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class ConstruirMenusAudio
{
    private const string Carpeta = "Assets/Scenes/AudioMenus/";
    private static readonly Color Fondo = new Color32(23, 20, 31, 255);
    private static readonly Color Superficie = new Color32(38, 33, 48, 255);
    private static readonly Color Papel = new Color32(248, 240, 221, 255);
    private static readonly Color Suave = new Color32(191, 181, 201, 255);
    private static readonly Color Oro = new Color32(244, 187, 83, 255);
    private static readonly Color Azul = new Color32(120, 197, 255, 255);
    private static readonly Color Rojo = new Color32(255, 117, 117, 255);
    private static Font Fuente => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Emoliente/Construir menus en escenas AudioMenus")]
    public static void Construir()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Deten Play y guarda la escena antes de construir los menus.");
        ConstruirPrincipal();
        ConstruirPartida();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Carpeta + "MenuAudioMenus.unity");
    }

    private static void ConstruirPrincipal()
    {
        var escena = EditorSceneManager.OpenScene(Carpeta + "MenuAudioMenus.unity");
        foreach (var raiz in escena.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(raiz);
        var camara = new GameObject("CamaraMenu", typeof(Camera)).GetComponent<Camera>();
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = Fondo;
        camara.cullingMask = 0;
        var canvas = Canvas("MenuPrincipal", 100);
        var menu = canvas.gameObject.AddComponent<MenuPrincipal>();
        CrearEventos(canvas);
        var fondo = Panel("Fondo", canvas, Fondo);
        var ilustracion = Caja("Pasillo", fondo, new Vector2(0, -314), new Vector2(1280, 86));
        var imagen = ilustracion.gameObject.AddComponent<Image>();
        imagen.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Fondo base B1.png");
        imagen.color = new Color(0.55f, 0.5f, 0.65f, 0.2f);
        imagen.raycastTarget = false;
        var pantallas = new GameObject[4];
        var primeros = new Button[4];
        string[] nombres = { "Inicio", "Opciones", "Controles", "Creditos" };
        for (int i = 0; i < 4; i++) pantallas[i] = Panel(nombres[i], canvas, Color.clear).gameObject;

        Transform inicio = pantallas[0].transform;
        Texto(inicio, "GAME JAM 2026  /  VERSUS LOCAL", new Vector2(-285, 270), new Vector2(470, 26), 15, Oro, TextAnchor.MiddleLeft);
        Texto(inicio, "EMOLIENTE", new Vector2(-285, 205), new Vector2(470, 90), 59, Papel, TextAnchor.MiddleLeft, true);
        Texto(inicio, "Dos jugadores. Un pasillo.\nTodas las almas.", new Vector2(-285, 115), new Vector2(470, 70), 24, Suave, TextAnchor.MiddleLeft);
        primeros[0] = Boton(inicio, "JUGAR", new Vector2(-285, 25), new Vector2(470, 54), menu.Jugar, true);
        var opciones = Boton(inicio, "OPCIONES", new Vector2(-285, -41), new Vector2(470, 50));
        UnityEventTools.AddIntPersistentListener(opciones.onClick, menu.Mostrar, 1);
        var controles = Boton(inicio, "COMO JUGAR", new Vector2(-285, -103), new Vector2(470, 50));
        UnityEventTools.AddIntPersistentListener(controles.onClick, menu.Mostrar, 2);
        var creditos = Boton(inicio, "CREDITOS", new Vector2(-285, -165), new Vector2(470, 50));
        UnityEventTools.AddIntPersistentListener(creditos.onClick, menu.Mostrar, 3);
        Boton(inicio, "SALIR", new Vector2(-285, -227), new Vector2(470, 46), menu.Salir);
        Texto(inicio, "EL PASILLO TE ESPERA", new Vector2(285, 233), new Vector2(380, 30), 19, Oro, TextAnchor.MiddleLeft, true);
        TarjetaJugador(inicio, new Vector2(285, 118), Azul, "JUGADOR 1", "W A S D", "X para absorber  /  C para golpear");
        TarjetaJugador(inicio, new Vector2(285, -49), Rojo, "JUGADOR 2", "I J K L  o  flechas", "N para absorber  /  M para golpear");
        Texto(inicio, "Comparte el teclado.\nCompite por las almas.", new Vector2(285, -192), new Vector2(380, 65), 23, Suave);
        var estado = Texto(inicio, "ENTER confirmar  /  ESC volver", new Vector2(0, -320), new Vector2(1050, 27), 15, Suave);

        Encabezado(pantallas[1].transform, "OPCIONES", "Ajusta la mezcla. Los cambios se guardan al volver.");
        Slider general = AudioOpciones(pantallas[1].transform);
        primeros[1] = Boton(pantallas[1].transform, "GUARDAR Y VOLVER", new Vector2(0, -250), new Vector2(420, 52), menu.GuardarOpciones, true);

        Encabezado(pantallas[2].transform, "COMO JUGAR", "Consigue mas almas que tu rival antes de que termine el tiempo.");
        TarjetaControles(pantallas[2].transform, -260, Azul, "JUGADOR 1", "W A S D", "X", "C");
        TarjetaControles(pantallas[2].transform, 260, Rojo, "JUGADOR 2", "I J K L o flechas", "N", "M");
        Texto(pantallas[2].transform, "CARTAS   Manten C / M, elige con izquierda o derecha y confirma con X / N.\nPUERTAS   Colocate frente a una y pulsa arriba para salir por otra al azar.\nCUIDADO   La profesora aturde. Los estudiantes raros dan mas almas y una carta.",
            new Vector2(0, -133), new Vector2(1010, 105), 19, Suave, TextAnchor.MiddleLeft);
        primeros[2] = Boton(pantallas[2].transform, "VOLVER", new Vector2(0, -270), new Vector2(360, 50));
        UnityEventTools.AddIntPersistentListener(primeros[2].onClick, menu.Mostrar, 0);

        Encabezado(pantallas[3].transform, "CREDITOS", "Creado en equipo para Game Jam 2026.");
        Texto(pantallas[3].transform, "EMOLIENTE", new Vector2(0, 95), new Vector2(900, 85), 52, Papel, TextAnchor.MiddleCenter, true);
        Texto(pantallas[3].transform, "EQUIPO\n\nPicatzo\nMalica\nNico F\nCris R\nAleeAsto\nZayd-A", new Vector2(0, -65), new Vector2(900, 240), 23, Suave);
        primeros[3] = Boton(pantallas[3].transform, "VOLVER", new Vector2(0, -250), new Vector2(360, 50));
        UnityEventTools.AddIntPersistentListener(primeros[3].onClick, menu.Mostrar, 0);
        var datos = new SerializedObject(menu);
        var panels = datos.FindProperty("pantallas"); panels.arraySize = pantallas.Length;
        var buttons = datos.FindProperty("primerosBotones"); buttons.arraySize = primeros.Length;
        for (int i = 0; i < pantallas.Length; i++)
        {
            panels.GetArrayElementAtIndex(i).objectReferenceValue = pantallas[i];
            buttons.GetArrayElementAtIndex(i).objectReferenceValue = primeros[i];
            pantallas[i].SetActive(i == 0);
        }
        datos.FindProperty("volumen").objectReferenceValue = general;
        datos.FindProperty("estado").objectReferenceValue = estado;
        datos.FindProperty("escenaPartida").stringValue = "PartidaAudioMenus";
        datos.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(escena);
    }

    private static void ConstruirPartida()
    {
        var escena = EditorSceneManager.OpenScene(Carpeta + "PartidaAudioMenus.unity");
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.name == "MenusPartida" || raiz.name == "FondoPantalla") UnityEngine.Object.DestroyImmediate(raiz);
        // Las camaras de juego ocupan el 70% central; limpiar tambien los margenes.
        var fondo = new GameObject("FondoPantalla", typeof(Camera)).GetComponent<Camera>();
        fondo.clearFlags = CameraClearFlags.SolidColor;
        fondo.backgroundColor = Fondo;
        fondo.cullingMask = 0;
        fondo.depth = -100;
        CicloPartida ciclo = null;
        AlmasJugador p1 = null, p2 = null;
        bool eventos = false;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (ciclo == null) ciclo = raiz.GetComponentInChildren<CicloPartida>(true);
            if (raiz.name == "P1") p1 = raiz.GetComponent<AlmasJugador>();
            if (raiz.name == "P2") p2 = raiz.GetComponent<AlmasJugador>();
            eventos |= raiz.GetComponentInChildren<EventSystem>(true) != null;
        }
        if (ciclo == null || p1 == null || p2 == null) throw new InvalidOperationException("Faltan partida o jugadores en la copia.");
        var canvas = Canvas("MenusPartida", 1000);
        var menu = canvas.gameObject.AddComponent<MenuPartida>();
        if (!eventos) CrearEventos(canvas);
        var botonPausa = Boton(canvas, "PAUSA / ESC", Vector2.zero, new Vector2(148, 34), menu.Pausar);
        var pausaRect = (RectTransform)botonPausa.transform;
        pausaRect.anchorMin = pausaRect.anchorMax = Vector2.one;
        pausaRect.anchoredPosition = new Vector2(-94, -26);

        var pausa = Panel("Pausa", canvas, new Color(0.055f, 0.04f, 0.08f, 0.94f));
        Encabezado(pausa, "PAUSA", "El pasillo puede esperar.");
        var continuar = Boton(pausa, "CONTINUAR", new Vector2(0, 65), new Vector2(440, 58), menu.Continuar, true);
        var opcionesBoton = Boton(pausa, "OPCIONES DE AUDIO", new Vector2(0, -15), new Vector2(440, 54), menu.MostrarOpciones);
        Boton(pausa, "VOLVER AL MENU", new Vector2(0, -91), new Vector2(440, 54), menu.VolverMenu);
        Texto(pausa, "ESC para continuar", new Vector2(0, -220), new Vector2(900, 35), 18, Suave);

        var opciones = Panel("OpcionesPausa", canvas, new Color(0.055f, 0.04f, 0.08f, 0.97f));
        Encabezado(opciones, "OPCIONES", "La partida sigue en pausa.");
        AudioOpciones(opciones);
        Boton(opciones, "VOLVER A PAUSA", new Vector2(0, -250), new Vector2(420, 52), menu.VolverPausa, true);

        var resultado = Panel("Resultado", canvas, new Color(0.055f, 0.04f, 0.08f, 0.96f));
        var titulo = Texto(resultado, "FIN DE PARTIDA", new Vector2(0, 235), new Vector2(1020, 75), 48, Oro, TextAnchor.MiddleCenter, true);
        Texto(resultado, "CADA ALMA CUENTA", new Vector2(0, 175), new Vector2(900, 30), 17, Suave);
        var puntos1 = Puntuacion(resultado, -250, Azul, "JUGADOR 1");
        var puntos2 = Puntuacion(resultado, 250, Rojo, "JUGADOR 2");
        Texto(resultado, "VS", new Vector2(0, 35), new Vector2(90, 70), 30, Suave, TextAnchor.MiddleCenter, true);
        var revancha = Boton(resultado, "REVANCHA", new Vector2(-230, -205), new Vector2(420, 56), menu.Revancha, true);
        Boton(resultado, "VOLVER AL MENU", new Vector2(230, -205), new Vector2(420, 56), menu.VolverMenu);
        var mensaje = Texto(resultado, "", new Vector2(0, -290), new Vector2(1020, 30), 18, Suave);
        Asignar(menu, "partida", ciclo, "jugador1", p1, "jugador2", p2, "pausa", pausa.gameObject,
            "botonPausa", botonPausa,
            "opciones", opciones.gameObject, "resultado", resultado.gameObject, "continuar", continuar,
            "volverOpciones", opcionesBoton, "revancha", revancha, "tituloResultado", titulo,
            "puntuacion1", puntos1, "puntuacion2", puntos2, "mensaje", mensaje);
        pausa.gameObject.SetActive(false);
        opciones.gameObject.SetActive(false);
        resultado.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(escena);
    }

    private static Slider AudioOpciones(Transform padre)
    {
        var grupo = Caja("MezclaAudio", padre, Vector2.zero, new Vector2(900, 360));
        var opciones = grupo.gameObject.AddComponent<OpcionesAudioUI>();
        Slider general = FilaVolumen(grupo, "GENERAL", 105, out Text textoGeneral);
        Slider musica = FilaVolumen(grupo, "MUSICA", 0, out Text textoMusica);
        Slider efectos = FilaVolumen(grupo, "EFECTOS Y PASOS", -105, out Text textoEfectos);
        Asignar(opciones, "general", general, "musica", musica, "efectos", efectos,
            "valorGeneral", textoGeneral, "valorMusica", textoMusica, "valorEfectos", textoEfectos);
        return general;
    }

    private static Slider FilaVolumen(Transform padre, string etiqueta, float y, out Text valor)
    {
        Texto(padre, etiqueta, new Vector2(-295, y + 22), new Vector2(300, 32), 20, Papel, TextAnchor.MiddleLeft, true);
        valor = Texto(padre, "100%", new Vector2(390, y + 22), new Vector2(110, 32), 20, Oro, TextAnchor.MiddleRight);
        var rect = Caja(etiqueta, padre, new Vector2(0, y - 15), new Vector2(890, 38));
        var slider = rect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
        var track = Caja("Pista", rect, Vector2.zero, new Vector2(870, 7));
        track.gameObject.AddComponent<Image>().color = new Color32(71, 61, 83, 255);
        var area = Caja("AreaControl", rect, Vector2.zero, new Vector2(870, 38));
        var mango = Caja("Control", area, Vector2.zero, new Vector2(22, 32));
        var img = mango.gameObject.AddComponent<Image>(); img.color = Oro;
        slider.handleRect = mango; slider.targetGraphic = img;
        var colors = slider.colors; colors.highlightedColor = Papel; colors.selectedColor = Papel; slider.colors = colors;
        return slider;
    }

    private static void TarjetaJugador(Transform padre, Vector2 pos, Color color, string jugador, string mover, string acciones)
    {
        var rect = Caja(jugador, padre, pos, new Vector2(380, 135));
        rect.gameObject.AddComponent<Image>().color = Superficie;
        Texto(rect, jugador, new Vector2(0, 42), new Vector2(335, 28), 17, color, TextAnchor.MiddleLeft, true);
        Texto(rect, mover, new Vector2(0, 5), new Vector2(335, 35), 26, Papel, TextAnchor.MiddleLeft, true);
        Texto(rect, acciones, new Vector2(0, -37), new Vector2(335, 26), 17, Suave, TextAnchor.MiddleLeft);
    }

    private static void TarjetaControles(Transform padre, float x, Color color, string jugador, string mover, string absorber, string golpear)
    {
        var rect = Caja(jugador, padre, new Vector2(x, 64), new Vector2(490, 208));
        rect.gameObject.AddComponent<Image>().color = Superficie;
        Texto(rect, jugador, new Vector2(0, 69), new Vector2(430, 35), 23, color, TextAnchor.MiddleLeft, true);
        Texto(rect, $"MOVERSE       {mover}\nABSORBER      Manten {absorber}\nGOLPEAR       Pulsa {golpear}\nCARTAS        Manten {golpear}",
            new Vector2(0, -17), new Vector2(430, 125), 20, Papel, TextAnchor.MiddleLeft);
    }

    private static Text Puntuacion(Transform padre, float x, Color color, string jugador)
    {
        var rect = Caja(jugador, padre, new Vector2(x, 28), new Vector2(420, 210));
        rect.gameObject.AddComponent<Image>().color = Superficie;
        Texto(rect, jugador, new Vector2(0, 68), new Vector2(370, 35), 22, color, TextAnchor.MiddleCenter, true);
        Texto(rect, "ALMAS", new Vector2(0, -72), new Vector2(370, 30), 17, Suave);
        return Texto(rect, "0", Vector2.zero, new Vector2(370, 96), 68, Papel, TextAnchor.MiddleCenter, true);
    }

    private static void Encabezado(Transform padre, string titulo, string descripcion)
    {
        Texto(padre, titulo, new Vector2(0, 265), new Vector2(1020, 65), 46, Papel, TextAnchor.MiddleCenter, true);
        Texto(padre, descripcion, new Vector2(0, 202), new Vector2(1040, 40), 20, Suave);
    }

    private static RectTransform Canvas(string nombre, int orden)
    {
        var rect = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<RectTransform>();
        var canvas = rect.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = orden;
        var scaler = rect.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        return rect;
    }

    private static void CrearEventos(Transform padre)
    {
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(padre, false);
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static RectTransform Panel(string nombre, Transform padre, Color color)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        if (color.a > 0f) rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform Caja(string nombre, Transform padre, Vector2 pos, Vector2 size)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = pos; rect.sizeDelta = size;
        return rect;
    }

    private static Text Texto(Transform padre, string contenido, Vector2 pos, Vector2 size, int tamano, Color color,
        TextAnchor alineacion = TextAnchor.MiddleCenter, bool negrita = false)
    {
        var rect = Caja(contenido.Length > 45 ? contenido.Substring(0, 45) : contenido, padre, pos, size);
        var texto = rect.gameObject.AddComponent<Text>(); texto.font = Fuente; texto.fontSize = tamano;
        texto.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal; texto.color = color; texto.alignment = alineacion;
        texto.text = contenido; texto.raycastTarget = false;
        return texto;
    }

    private static Button Boton(Transform padre, string nombre, Vector2 pos, Vector2 size, UnityAction accion = null, bool principal = false)
    {
        var rect = Caja(nombre, padre, pos, size); var image = rect.gameObject.AddComponent<Image>();
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = principal ? Oro : Superficie;
        colors.highlightedColor = principal ? new Color32(255, 214, 136, 255) : new Color32(77, 62, 94, 255);
        colors.selectedColor = colors.highlightedColor; colors.pressedColor = principal ? new Color32(205, 139, 48, 255) : new Color32(103, 78, 118, 255);
        colors.disabledColor = new Color32(65, 58, 71, 160); button.colors = colors;
        Texto(rect, nombre, Vector2.zero, size - new Vector2(20, 4), 21, principal ? Fondo : Papel, TextAnchor.MiddleCenter, true);
        if (accion != null) UnityEventTools.AddPersistentListener(button.onClick, accion);
        return button;
    }

    private static void Asignar(UnityEngine.Object objeto, params object[] campos)
    {
        var datos = new SerializedObject(objeto);
        for (int i = 0; i < campos.Length; i += 2)
            datos.FindProperty((string)campos[i]).objectReferenceValue = (UnityEngine.Object)campos[i + 1];
        datos.ApplyModifiedPropertiesWithoutUndo();
    }
}
