using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class ConstruirModoCPU
{
    private static readonly Color Papel = new Color32(248, 240, 221, 255);

    [MenuItem("Emoliente/Agregar seleccion de modo CPU")]
    public static void Construir()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Deten Play y guarda la escena antes de actualizar el menu.");
        var escena = EditorSceneManager.OpenScene("Assets/Scenes/AudioMenus/MenuAudioMenus.unity");
        Aplicar(escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MenuPrincipal>(true)).Single());
        EditorSceneManager.SaveScene(escena);
    }

    public static void Aplicar(MenuPrincipal menu)
    {
        foreach (string nombre in new[] { "ModoJuego", "DificultadCPU" })
        {
            var anterior = menu.transform.Find(nombre);
            if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior.gameObject);
        }
        var modo = Panel(menu.transform, "ModoJuego", "ELIGE TU RIVAL");
        Texto(modo, "SOCIAL OUTCASTS", 240, 18);
        var cpu = Boton(modo, "CPU", 90, menu.ElegirCPU);
        Boton(modo, "PvP", -5, menu.JugarPvP);
        var volver = Boton(modo, "VOLVER", -200);
        UnityEventTools.AddIntPersistentListener(volver.onClick, menu.Mostrar, 0);
        Texto(modo, "Un jugador contra la CPU o dos jugadores en el mismo teclado.", -105, 19);

        var dificultad = Panel(menu.transform, "DificultadCPU", "ELIGE LA DIFICULTAD");
        Texto(dificultad, "JUGADOR 1  VS  CPU", 240, 18);
        var facil = Boton(dificultad, "FACIL", 100);
        var normal = Boton(dificultad, "NORMAL", 10);
        var dificil = Boton(dificultad, "DIFICIL", -80);
        UnityEventTools.AddIntPersistentListener(facil.onClick, menu.JugarCPU, 0);
        UnityEventTools.AddIntPersistentListener(normal.onClick, menu.JugarCPU, 1);
        UnityEventTools.AddIntPersistentListener(dificil.onClick, menu.JugarCPU, 2);
        var atras = Boton(dificultad, "VOLVER", -235);
        UnityEventTools.AddIntPersistentListener(atras.onClick, menu.Mostrar, 4);
        foreach (Transform panel in new[] { modo, dificultad })
        {
            Texto(panel, "FLECHAS elegir  /  ENTER confirmar  /  ESC volver", -325, 16);
            Navegacion(panel.GetComponentsInChildren<Button>());
            panel.gameObject.SetActive(false);
        }

        var datos = new SerializedObject(menu);
        var pantallas = datos.FindProperty("pantallas");
        var primeros = datos.FindProperty("primerosBotones");
        pantallas.arraySize = primeros.arraySize = 6;
        pantallas.GetArrayElementAtIndex(4).objectReferenceValue = modo.gameObject;
        pantallas.GetArrayElementAtIndex(5).objectReferenceValue = dificultad.gameObject;
        primeros.GetArrayElementAtIndex(4).objectReferenceValue = cpu;
        primeros.GetArrayElementAtIndex(5).objectReferenceValue = normal;
        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Panel(Transform padre, string nombre, string titulo)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Texto(rect, titulo, 175, 38, true);
        return rect;
    }

    private static void Texto(Transform padre, string valor, float y, int tamano, bool negrita = false)
    {
        var objeto = new GameObject(valor, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(padre, false);
        var texto = objeto.GetComponent<Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = valor; texto.fontSize = tamano;
        texto.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        texto.color = Papel; texto.alignment = TextAnchor.MiddleCenter;
        texto.raycastTarget = false;
        texto.rectTransform.sizeDelta = new Vector2(1100, tamano > 30 ? 65 : 45);
        texto.rectTransform.anchoredPosition = new Vector2(0, y);
    }

    private static Button Boton(Transform padre, string titulo, float y, UnityAction accion = null)
    {
        var objeto = new GameObject(titulo, typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(padre, false);
        var rect = (RectTransform)objeto.transform;
        rect.sizeDelta = new Vector2(390, 65); rect.anchoredPosition = new Vector2(0, y);
        var fondo = objeto.GetComponent<Image>();
        fondo.color = Papel;
        var boton = objeto.GetComponent<Button>();
        boton.targetGraphic = fondo;
        var colores = boton.colors;
        colores.highlightedColor = colores.selectedColor = new Color32(255, 168, 133, 255);
        colores.pressedColor = new Color32(211, 110, 88, 255);
        boton.colors = colores;
        Texto(rect, titulo, 0, 27, true);
        var texto = rect.GetComponentInChildren<Text>();
        texto.color = Color.black; texto.rectTransform.sizeDelta = rect.sizeDelta - new Vector2(30, 4);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/ArteFinal/UI/Seleccion.png");
        var indicador = new GameObject("SeleccionArte", typeof(RectTransform), typeof(Image));
        indicador.transform.SetParent(rect, false);
        var imagen = indicador.GetComponent<Image>(); imagen.sprite = sprite;
        imagen.raycastTarget = false;
        imagen.rectTransform.sizeDelta = rect.sizeDelta + new Vector2(28, 8);
        indicador.SetActive(false);
        objeto.AddComponent<BotonArteUI>().Configurar(indicador);
        if (accion != null) UnityEventTools.AddPersistentListener(boton.onClick, accion);
        return boton;
    }

    private static void Navegacion(Button[] botones)
    {
        for (int i = 0; i < botones.Length; i++)
            botones[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = botones[(i + botones.Length - 1) % botones.Length],
                selectOnDown = botones[(i + 1) % botones.Length] };
    }
}
