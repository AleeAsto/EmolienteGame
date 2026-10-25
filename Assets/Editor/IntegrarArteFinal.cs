using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class IntegrarArteFinal
{
    private const string Raiz = "Assets/Images/ArteFinal/";
    private const string Escenas = "Assets/Scenes/AudioMenus/";
    private static readonly Color Papel = new Color32(242, 210, 173, 255);
    private static readonly Color Rojo = new Color32(232, 24, 36, 255);

    [MenuItem("Emoliente/Integrar cartas y selector del menu")]
    public static void IntegrarCartasYSeleccion()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Deten Play y guarda la escena antes de integrar el arte.");
        AssetDatabase.Refresh();
        ImportarCartasYLogo();
        var escena = EditorSceneManager.OpenScene(Escenas + "MenuAudioMenus.unity");
        AplicarMenu(escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MenuPrincipal>(true)).Single());
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Emoliente/Integrar arte final y nuevos efectos")]
    public static void Integrar()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Deten Play y guarda la escena antes de integrar el arte.");
        AssetDatabase.Refresh();
        Importar();
        ConfigurarAudio();
        var menu = EditorSceneManager.OpenScene(Escenas + "MenuAudioMenus.unity");
        AplicarMenu(menu.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MenuPrincipal>(true)).Single());
        EditorSceneManager.SaveScene(menu);
        var partida = EditorSceneManager.OpenScene(Escenas + "PartidaAudioMenus.unity");
        AplicarPasillo(partida);
        foreach (var raiz in partida.GetRootGameObjects())
            foreach (var interfaz in raiz.GetComponentsInChildren<MenuPartida>(true)) AplicarTema(interfaz.transform);
        EditorSceneManager.SaveScene(partida);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Escenas + "MenuAudioMenus.unity");
    }

    private static void Importar()
    {
        Recortar(Raiz + "UI/Menu.png", new[] {
            Corte("Logo", 171, 33, 261, 170, 460),
            Corte("Play", 211, 204, 152, 49, 460),
            Corte("HowToPlay", 228, 280, 145, 31, 460),
            Corte("Options", 231, 312, 135, 31, 460)
        });
        foreach (string nombre in new[] { "Credits", "Quit" })
        {
            string ruta = Raiz + "UI/" + nombre + ".png";
            Recortar(ruta, new[] { nombre == "Credits" ? Corte(nombre, 197, 145, 247, 80, 460) :
                new SpriteRect { name = nombre, rect = LimitesVisibles(ruta) } });
        }
        foreach (string nombre in new[] { "CASILLERO CERRADO", "CASILLERO VACIO", "PUERTA ABIERTA", "PUERTA CERRADA" })
        {
            string ruta = Raiz + "Pasillo/" + nombre + ".png";
            Recortar(ruta, new[] { nombre == "PUERTA ABIERTA" ? Corte(nombre, 109, 36, 242, 288, 378) :
                new SpriteRect { name = nombre, rect = LimitesVisibles(ruta) } });
        }
        var fondo = (TextureImporter)AssetImporter.GetAtPath(Raiz + "Pasillo/Fondom y pintarm.png");
        ConfigurarTextura(fondo);
        fondo.spriteImportMode = SpriteImportMode.Single;
        fondo.maxTextureSize = 8192;
        fondo.SaveAndReimport();
        ImportarCartasYLogo();
    }

    private static void ImportarCartasYLogo()
    {
        if (!File.Exists(Raiz + "UI/LogoAmpliado.png")) return;
        foreach (string nombre in new[] { "LogoAmpliado", "LogoPequeno", "Seleccion" })
        {
            string ruta = Raiz + "UI/" + nombre + ".png";
            Recortar(ruta, new[] { new SpriteRect { name = nombre, rect = LimitesVisibles(ruta) } });
        }
        foreach (string grupo in new[] { "Ampliadas", "Pequenas" })
            foreach (string nombre in new[] { "BombPerfume", "Confusion", "GolpeFuerte", "Reverso", "Velocidad", "Swap" })
            {
                string ruta = Raiz + "Cartas/" + grupo + "/" + nombre + ".png";
                Recortar(ruta, new[] { new SpriteRect { name = nombre, rect = LimitesVisibles(ruta) } });
            }
        var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoCartas>("Assets/Resources/CatalogoCartas.asset");
        foreach (DefinicionCarta carta in catalogo.Cartas)
        {
            string nombre;
            switch (carta.Efecto)
            {
                case EfectoCarta.Velocidad: nombre = "Velocidad"; break;
                case EfectoCarta.Confusion: nombre = "Confusion"; break;
                case EfectoCarta.Perfume:
                case EfectoCarta.BombaApestosa: nombre = "BombPerfume"; break;
                case EfectoCarta.Latigo: nombre = "GolpeFuerte"; break;
                case EfectoCarta.IntercambioPosicion: nombre = "Swap"; break;
                default: continue;
            }
            var datos = new SerializedObject(carta);
            datos.FindProperty("frente").objectReferenceValue = Sprite("Cartas/Ampliadas/" + nombre + ".png");
            datos.FindProperty("reverso").objectReferenceValue = Sprite("Cartas/Ampliadas/Reverso.png");
            datos.FindProperty("frenteMano").objectReferenceValue = Sprite("Cartas/Pequenas/" + nombre + ".png");
            datos.FindProperty("reversoMano").objectReferenceValue = Sprite("Cartas/Pequenas/Reverso.png");
            datos.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static SpriteRect Corte(string nombre, int x, int y, int ancho, int alto, int alturaTextura) =>
        new SpriteRect { name = nombre, rect = new Rect(x, alturaTextura - y - alto, ancho, alto) };

    private static Rect LimitesVisibles(string ruta)
    {
        // Solo define el recorte del sprite. Los PNG originales no se modifican.
        var textura = new Texture2D(2, 2);
        try
        {
            textura.LoadImage(File.ReadAllBytes(ruta));
            Color32[] pixeles = textura.GetPixels32();
            int minX = textura.width, minY = textura.height, maxX = -1, maxY = -1;
            for (int y = 0; y < textura.height; y++)
                for (int x = 0; x < textura.width; x++)
                {
                    if (pixeles[y * textura.width + x].a < 32) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            if (maxX < minX) throw new InvalidOperationException("Sprite vacio: " + ruta);
            return new Rect(Mathf.Max(0, minX - 1), Mathf.Max(0, minY - 1),
                Mathf.Min(textura.width - Mathf.Max(0, minX - 1), maxX - minX + 3),
                Mathf.Min(textura.height - Mathf.Max(0, minY - 1), maxY - minY + 3));
        }
        finally { UnityEngine.Object.DestroyImmediate(textura); }
    }

    private static void ConfigurarTextura(TextureImporter importador)
    {
        importador.textureType = TextureImporterType.Sprite;
        importador.spritePixelsPerUnit = 100f;
        var ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        importador.SetTextureSettings(ajustes);
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.npotScale = TextureImporterNPOTScale.None;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.filterMode = FilterMode.Bilinear;
        importador.wrapMode = TextureWrapMode.Clamp;
    }

    private static void Recortar(string ruta, SpriteRect[] recortes)
    {
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        ConfigurarTextura(importador);
        importador.spriteImportMode = SpriteImportMode.Multiple;
        importador.SaveAndReimport();
        var fabricas = new SpriteDataProviderFactories();
        fabricas.Init();
        var proveedor = fabricas.GetSpriteEditorDataProviderFromObject(importador);
        proveedor.InitSpriteEditorDataProvider();
        SpriteRect[] anteriores = proveedor.GetSpriteRects();
        foreach (var recorte in recortes)
        {
            recorte.alignment = SpriteAlignment.Center;
            recorte.pivot = new Vector2(0.5f, 0.5f);
            recorte.spriteID = anteriores.FirstOrDefault(r => r.name == recorte.name)?.spriteID ?? GUID.Generate();
        }
        proveedor.SetSpriteRects(recortes);
        proveedor.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            recortes.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        proveedor.Apply();
        importador.SaveAndReimport();
    }

    private static Sprite Sprite(string ruta, string nombre = null) =>
        AssetDatabase.LoadAllAssetsAtPath(Raiz + ruta).OfType<Sprite>().Single(s => nombre == null || s.name == nombre);

    public static void AplicarMenu(MenuPrincipal menu)
    {
        if (!File.Exists(Raiz + "UI/Menu.png")) return;
        AplicarTema(menu.transform);
        var datos = new SerializedObject(menu);
        var pantallas = datos.FindProperty("pantallas");
        var inicio = ((GameObject)pantallas.GetArrayElementAtIndex(0).objectReferenceValue).transform;
        var estado = (Text)datos.FindProperty("estado").objectReferenceValue;
        // Se conserva la navegacion, los listeners y las pantallas funcionales existentes.
        foreach (Transform child in inicio)
            if (child.GetComponent<Button>() == null && child != estado.transform) child.gameObject.SetActive(false);
        Transform fondo = menu.transform.Find("Fondo");
        if (fondo != null)
            foreach (Transform child in fondo) child.gameObject.SetActive(false);
        foreach (Camera camara in menu.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
            camara.backgroundColor = Color.black;
        var logo = Imagen("LogoSocialOutcasts", inicio);
        logo.sprite = File.Exists(Raiz + "UI/LogoAmpliado.png") ? Sprite("UI/LogoAmpliado.png") : Sprite("UI/Menu.png", "Logo");
        Ubicar(logo.rectTransform, new Vector2(0, 170), new Vector2(440, 293));
        logo.gameObject.SetActive(true);
        Boton(inicio, "JUGAR", Sprite("UI/Menu.png", "Play"), -17, new Vector2(270, 87));
        Boton(inicio, "COMO JUGAR", Sprite("UI/Menu.png", "HowToPlay"), -90, new Vector2(245, 61));
        Boton(inicio, "OPCIONES", Sprite("UI/Menu.png", "Options"), -150, new Vector2(233, 59));
        Boton(inicio, "CREDITOS", Sprite("UI/Credits.png"), -214, new Vector2(225, 67));
        Boton(inicio, "SALIR", Sprite("UI/Quit.png"), -278, new Vector2(235, 66));
        estado.text = "FLECHAS elegir  /  ENTER confirmar  /  ESC volver";
        estado.color = Papel;
        Ubicar(estado.rectTransform, new Vector2(0, -336), new Vector2(1100, 25));
        estado.gameObject.SetActive(true);
        var creditos = ((GameObject)pantallas.GetArrayElementAtIndex(3).objectReferenceValue).transform;
        foreach (var texto in creditos.GetComponentsInChildren<Text>(true))
        {
            if (texto.text == "EMOLIENTE") texto.text = "SOCIAL OUTCASTS";
            if (texto.text == "CREDITOS" && texto.GetComponentInParent<Button>() == null) texto.gameObject.SetActive(false);
            if (texto.text == "Creado en equipo para Game Jam 2026.")
                texto.rectTransform.anchoredPosition = new Vector2(0f, 175f);
        }
        var titulo = Imagen("TituloCreditosArte", creditos);
        titulo.sprite = Sprite("UI/Credits.png");
        Ubicar(titulo.rectTransform, new Vector2(0, 240), new Vector2(290, 86));
        titulo.gameObject.SetActive(true);
    }

    public static void AplicarTema(Transform raiz)
    {
        foreach (var imagen in raiz.GetComponentsInChildren<Image>(true))
        {
            if (imagen.sprite != null || imagen.GetComponentInParent<Slider>() != null) continue;
            if (imagen.transform == raiz || imagen.name == "Fondo" || imagen.name == "ControlesTeclado" ||
                imagen.name == "Pausa" || imagen.name == "OpcionesPausa" || imagen.name == "Resultado")
                imagen.color = new Color(0f, 0f, 0f, imagen.color.a);
        }
        foreach (var boton in raiz.GetComponentsInChildren<Button>(true))
        {
            boton.transition = Selectable.Transition.ColorTint;
            if (boton.targetGraphic != null) boton.targetGraphic.color = Color.white;
            ColorBlock colores = boton.colors;
            colores.normalColor = Papel;
            colores.highlightedColor = colores.selectedColor = new Color32(255, 168, 133, 255);
            colores.pressedColor = new Color32(211, 110, 88, 255);
            colores.disabledColor = new Color32(110, 103, 94, 180);
            boton.colors = colores;
            foreach (Text texto in boton.GetComponentsInChildren<Text>(true)) texto.color = Color.black;
        }
        foreach (var slider in raiz.GetComponentsInChildren<Slider>(true))
            if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var relleno)) relleno.color = Rojo;
    }

    private static void Boton(Transform inicio, string nombre, Sprite sprite, float y, Vector2 tamano)
    {
        var boton = inicio.GetComponentsInChildren<Button>(true).Single(b => b.name == nombre);
        Ubicar((RectTransform)boton.transform, new Vector2(0, y), tamano);
        foreach (Text texto in boton.GetComponentsInChildren<Text>(true)) texto.gameObject.SetActive(false);
        Image imagen = boton.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.type = Image.Type.Simple;
        imagen.preserveAspect = true;
        imagen.color = Color.white;
        foreach (Outline borde in boton.GetComponents<Outline>()) UnityEngine.Object.DestroyImmediate(borde);
        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = colores.selectedColor = new Color(1f, 0.9f, 0.8f);
        colores.pressedColor = new Color(1f, 0.65f, 0.6f);
        boton.colors = colores;
        var marca = Imagen("SeleccionArte", boton.transform);
        if (File.Exists(Raiz + "UI/Seleccion.png"))
        {
            marca.sprite = Sprite("UI/Seleccion.png");
            marca.color = Color.white;
            marca.preserveAspect = false;
            Ubicar(marca.rectTransform, Vector2.zero, tamano + new Vector2(22f, 2f));
        }
        else
        {
            marca.color = Rojo;
            Ubicar(marca.rectTransform, new Vector2(-tamano.x * 0.5f - 16f, 0f), new Vector2(5f, 25f));
        }
        marca.transform.SetAsLastSibling();
        marca.gameObject.SetActive(false);
        var efecto = boton.GetComponent<BotonArteUI>();
        if (efecto == null) efecto = boton.gameObject.AddComponent<BotonArteUI>();
        efecto.Configurar(marca.gameObject);
        // Orden vertical explicito: el orden anterior era jugar/opciones/como jugar.
        string[] orden = { "JUGAR", "COMO JUGAR", "OPCIONES", "CREDITOS", "SALIR" };
        int i = Array.IndexOf(orden, nombre);
        Button[] lista = orden.Select(n => inicio.GetComponentsInChildren<Button>(true).Single(b => b.name == n)).ToArray();
        boton.navigation = new Navigation { mode = Navigation.Mode.Explicit,
            selectOnUp = lista[(i + lista.Length - 1) % lista.Length], selectOnDown = lista[(i + 1) % lista.Length] };
    }

    private static Image Imagen(string nombre, Transform padre)
    {
        Transform existente = padre.Find(nombre);
        var imagen = existente != null ? existente.GetComponent<Image>() :
            new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagen.transform.SetParent(padre, false);
        imagen.raycastTarget = false;
        imagen.preserveAspect = true;
        return imagen;
    }

    private static void Ubicar(RectTransform rect, Vector2 posicion, Vector2 tamano)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion; rect.sizeDelta = tamano;
    }

    private static void AplicarPasillo(UnityEngine.SceneManagement.Scene escena)
    {
        var renderers = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        foreach (var visual in renderers)
            if (visual.sprite != null && AssetDatabase.GetAssetPath(visual.sprite) == "Assets/Images/Fondo base B1.png")
                CambiarSprite(visual, Sprite("Pasillo/Fondom y pintarm.png"));
        foreach (var casillero in escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Casillero>(true)))
        {
            var datos = new SerializedObject(casillero);
            Sprite cerrado = Sprite("Pasillo/CASILLERO CERRADO.png");
            datos.FindProperty("spriteCerrado").objectReferenceValue = cerrado;
            datos.FindProperty("spriteAbierto").objectReferenceValue = Sprite("Pasillo/CASILLERO VACIO.png");
            datos.ApplyModifiedPropertiesWithoutUndo();
            var visual = casillero.GetComponent<SpriteRenderer>();
            Vector3 escalaFisica = visual.transform.localScale;
            Vector3 posicionFisica = visual.transform.localPosition;
            // El renderer comparte Transform con la colision: dimensionar el dibujo,
            // sin escalar el objeto fisico al cambiar la resolucion de la ilustracion.
            Vector2 tamano = visual.drawMode == SpriteDrawMode.Simple ? (Vector2)visual.sprite.bounds.size : visual.size;
            visual.sprite = cerrado;
            visual.drawMode = SpriteDrawMode.Sliced;
            visual.size = tamano;
            visual.color = Color.white;
            visual.transform.localScale = escalaFisica;
            visual.transform.localPosition = posicionFisica;
        }
        foreach (var puerta in escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PuertaTeletransporte>(true)))
        {
            var datos = new SerializedObject(puerta);
            Sprite abierta = Sprite("Pasillo/PUERTA ABIERTA.png");
            datos.FindProperty("spriteAbierta").objectReferenceValue = abierta;
            datos.FindProperty("spriteCerrada").objectReferenceValue = Sprite("Pasillo/PUERTA CERRADA.png");
            var panel = (SpriteRenderer)datos.FindProperty("panel").objectReferenceValue;
            datos.ApplyModifiedPropertiesWithoutUndo();
            CambiarSprite(panel, abierta);
            panel.sharedMaterial = MaterialPuerta();
            puerta.AjustarAccesos();
        }
    }

    private static Material MaterialPuerta()
    {
        const string ruta = Raiz + "Pasillo/MaterialPuerta.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Emoliente/SpriteRecorteAlpha"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        return material;
    }

    private static void CambiarSprite(SpriteRenderer visual, Sprite nuevo)
    {
        if (visual.sprite != null)
        {
            Bounds anterior = visual.sprite.bounds, siguiente = nuevo.bounds;
            Vector3 escala = visual.transform.localScale;
            Vector3 ajustada = new Vector3(escala.x * anterior.size.x / siguiente.size.x,
                escala.y * anterior.size.y / siguiente.size.y, escala.z);
            visual.transform.localPosition += visual.transform.localRotation *
                (Vector3.Scale(anterior.center, escala) - Vector3.Scale(siguiente.center, ajustada));
            visual.transform.localScale = ajustada;
        }
        visual.sprite = nuevo;
        visual.color = Color.white;
    }

    private static void ConfigurarAudio()
    {
        var config = AssetDatabase.LoadAssetAtPath<ConfiguracionAudio>("Assets/Resources/AudioJuego.asset");
        config.versus = Audio("V.S"); config.victoria = Audio("WIN");
        PrepararSonidosGolpe.Configurar();
        config.estudianteAgotado = Audio("muerte-de-NPC");
        config.abrirCasillero = Audio("casillero-abierto"); config.cerrarCasillero = Audio("casillero-cerrado");
        config.succion = Audio("absorber-hablando"); config.aturdimiento = Audio("stune-pajaros");
        foreach (var clip in new[] { config.versus, config.victoria, config.golpe, config.estudianteAgotado,
            config.abrirCasillero, config.cerrarCasillero, config.succion, config.aturdimiento })
        {
            var importador = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            var ajustes = importador.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.preloadAudioData = true;
            importador.defaultSampleSettings = ajustes;
            importador.SaveAndReimport();
        }
        EditorUtility.SetDirty(config);
    }

    private static AudioClip Audio(string nombre) =>
        AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/" + nombre + ".ogg");
}
