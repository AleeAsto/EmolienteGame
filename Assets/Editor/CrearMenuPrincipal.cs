using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CrearMenuPrincipal
{
    private static Font fuente;
    private static readonly Color Fondo = new Color32(17, 24, 35, 255);
    private static readonly Color Oro = new Color32(244, 187, 83, 255);

    [MenuItem("Emoliente/Crear menu principal")]
    public static void Crear()
    {
        const string ruta = "Assets/Scenes/MenuPrincipal.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) != null)
            throw new System.InvalidOperationException("MenuPrincipal ya existe; editalo directamente para conservar tus cambios.");
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var raiz = new GameObject("MenuPrincipal", typeof(MenuPrincipal));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(raiz, escena);
        var menu = raiz.GetComponent<MenuPrincipal>();
        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(raiz.transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var fondo = Rect("Fondo", canvasGO.transform);
        fondo.gameObject.AddComponent<Image>().color = Fondo;
        var acento = Rect("Franja dorada", fondo);
        acento.anchorMin = new Vector2(0, 0.975f);
        acento.gameObject.AddComponent<Image>().color = Oro;
        var evento = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        evento.transform.SetParent(raiz.transform, false);
        evento.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        string[] nombres = { "Titulo", "Opciones", "Tutorial", "Creditos" };
        var paneles = nombres.Select(n => Rect(n, canvasGO.transform).gameObject).ToArray();
        var primeros = new Button[4];
        Texto(paneles[0].transform, "GAME JAM 2026 · VERSUS LOCAL", 17, 230, 35, Oro);
        Texto(paneles[0].transform, "EMOLIENTE", 76, 147, 110, Color.white);
        Texto(paneles[0].transform, "Dos jugadores. Un pasillo. Todas las almas.", 22, 73, 45, Oro);
        primeros[0] = Boton(paneles[0].transform, "JUGAR", 0);
        UnityEventTools.AddPersistentListener(primeros[0].onClick, menu.Jugar);
        string[] etiquetas = { "OPCIONES", "TUTORIAL", "CRÉDITOS" };
        for (int i = 1; i < 4; i++)
        {
            var b = Boton(paneles[0].transform, etiquetas[i - 1], -64 * i);
            UnityEventTools.AddIntPersistentListener(b.onClick, menu.Mostrar, i);
        }
        var estado = Texto(paneles[0].transform, "Comparte el teclado y desafía a otro jugador", 17, -278, 50, Color.white);

        for (int i = 1; i < 4; i++)
        {
            Texto(paneles[i].transform, etiquetas[i - 1], 48, 230, 70, Oro);
            primeros[i] = Boton(paneles[i].transform, "VOLVER", -240);
            if (i == 1) UnityEventTools.AddPersistentListener(primeros[i].onClick, menu.GuardarOpciones);
            else UnityEventTools.AddIntPersistentListener(primeros[i].onClick, menu.Mostrar, 0);
        }
        Texto(paneles[1].transform, "VOLUMEN GENERAL", 24, 115, 50, Color.white);
        var pista = Rect("Volumen", paneles[1].transform);
        Centrar(pista, 35, 450, 36);
        pista.gameObject.AddComponent<Image>().color = new Color32(49, 62, 78, 255);
        var slider = pista.gameObject.AddComponent<Slider>();
        var area = Rect("Recorrido", pista);
        area.offsetMin = new Vector2(14, 0); area.offsetMax = new Vector2(-14, 0);
        var mango = Rect("Control", area);
        mango.sizeDelta = new Vector2(24, 0);
        var imagen = mango.gameObject.AddComponent<Image>(); imagen.color = Oro;
        slider.handleRect = mango; slider.targetGraphic = imagen;
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        UnityEventTools.AddPersistentListener(slider.onValueChanged, menu.CambiarVolumen);
        Texto(paneles[1].transform, "La asignación de controles se añadirá en el siguiente paso.", 21, -80, 90, Color.white);
        Texto(paneles[2].transform, "COMPITE POR LAS ALMAS\n\nAbsorbe almas de los estudiantes para sumar puntos.\nUsa ataques y cartas para frenar a tu rival.\nAl terminar el tiempo, gana quien tenga más almas.\n\nLos dos jugadores comparten el teclado.", 24, 10, 330, Color.white);
        Texto(paneles[3].transform, "EMOLIENTE\n\nUn juego creado para Game Jam 2026\n\nEquipo y agradecimientos\nPendientes de completar", 25, 10, 330, Color.white);

        var datos = new SerializedObject(menu);
        var p = datos.FindProperty("pantallas"); p.arraySize = 4;
        var bts = datos.FindProperty("primerosBotones"); bts.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            p.GetArrayElementAtIndex(i).objectReferenceValue = paneles[i];
            bts.GetArrayElementAtIndex(i).objectReferenceValue = primeros[i];
            paneles[i].SetActive(i == 0);
        }
        datos.FindProperty("volumen").objectReferenceValue = slider;
        datos.FindProperty("estado").objectReferenceValue = estado;
        datos.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(escena, ruta);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ruta, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != ruta)).ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(escena, true);
        Debug.Log("MenuPrincipal creada y configurada como primera escena del build.");
    }

    private static RectTransform Rect(string nombre, Transform padre)
    {
        var rect = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static void Centrar(RectTransform rect, float y, float ancho, float alto)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(ancho, alto);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private static Text Texto(Transform padre, string contenido, int tamano, float y, float alto, Color color)
    {
        var rect = Rect("Texto", padre); Centrar(rect, y, 960, alto);
        var texto = rect.gameObject.AddComponent<Text>();
        texto.font = fuente; texto.text = contenido; texto.fontSize = tamano;
        texto.alignment = TextAnchor.MiddleCenter; texto.color = color;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button Boton(Transform padre, string etiqueta, float y)
    {
        var rect = Rect(etiqueta, padre); Centrar(rect, y, 370, 52);
        var imagen = rect.gameObject.AddComponent<Image>();
        imagen.color = Color.white;
        var boton = rect.gameObject.AddComponent<Button>(); boton.targetGraphic = imagen;
        var colores = boton.colors;
        colores.normalColor = new Color32(41, 54, 70, 255);
        colores.highlightedColor = new Color32(85, 104, 125, 255);
        colores.selectedColor = colores.highlightedColor;
        colores.pressedColor = new Color32(132, 92, 37, 255);
        boton.colors = colores;
        var texto = Texto(rect, etiqueta, 22, 0, 52, Color.white);
        texto.rectTransform.sizeDelta = new Vector2(360, 52);
        return boton;
    }
}
