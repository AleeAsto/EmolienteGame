using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class PrepararCasillero
{
    private const string Solicitud = "Temp/PrepararCasillero.request";
    private const string Resultado = "Temp/PrepararCasillero.resultado.txt";
    static PrepararCasillero() => EditorApplication.update += Consultar;

    private static void Consultar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlaying || !File.Exists(Solicitud)) return;
        string comando;
        try
        {
            comando = File.ReadAllText(Solicitud).Trim();
            File.Delete(Solicitud);
        }
        catch (IOException) { return; }
        try
        {
            if (comando == "verificar") Verificar();
            else Preparar();
        }
        catch (Exception error) { File.WriteAllText(Resultado, error.ToString()); }
    }

    [MenuItem("Emoliente/Preparar casillero reutilizable")]
    public static void Preparar()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Deten Play para preparar el casillero.");
        Sprite cerrado = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/Casillero cerrado.png").OfType<Sprite>().First();
        Sprite abierto = AssetDatabase.LoadAllAssetsAtPath("Assets/Images/Casillero abierto.png").OfType<Sprite>().First();
        SpriteRenderer[] candidatos = Resources.FindObjectsOfTypeAll<SpriteRenderer>().Where(s =>
            !EditorUtility.IsPersistent(s) && s.gameObject.scene.IsValid() &&
            !EditorSceneManager.IsPreviewScene(s.gameObject.scene) && s.sprite == cerrado).ToArray();
        if (candidatos.Length != 1)
            throw new InvalidOperationException($"Se esperaba el casillero colocado en escena; encontrados: {candidatos.Length}.");
        GameObject objeto = candidatos[0].gameObject;
        string ruta = "Assets/Prefabs/Casillero.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null)
            throw new InvalidOperationException("El prefab Casillero ya existe; no se sobrescribe.");
        Undo.RegisterFullObjectHierarchyUndo(objeto, "Preparar casillero");
        Casillero casillero = objeto.GetComponent<Casillero>();
        if (casillero == null) casillero = Undo.AddComponent<Casillero>(objeto);
        BoxCollider2D caja = objeto.GetComponent<BoxCollider2D>();
        caja.isTrigger = false;
        Bounds limites = cerrado.bounds;
        // Solo la base ocupa suelo: se puede pasar por detras del mueble.
        caja.size = new Vector2(limites.size.x * 0.8f, limites.size.y * 0.22f);
        caja.offset = new Vector2(limites.center.x, limites.min.y + caja.size.y * 0.5f);
        SortingGroup grupo = objeto.GetComponent<SortingGroup>();
        if (grupo == null) grupo = Undo.AddComponent<SortingGroup>(objeto);
        grupo.sortingLayerID = candidatos[0].sortingLayerID;
        OrdenProfundidadPersonaje profundidad = objeto.GetComponent<OrdenProfundidadPersonaje>();
        if (profundidad == null) profundidad = Undo.AddComponent<OrdenProfundidadPersonaje>(objeto);
        var datosProfundidad = new SerializedObject(profundidad);
        datosProfundidad.FindProperty("referenciaPies").objectReferenceValue = caja;
        datosProfundidad.ApplyModifiedProperties();
        var datos = new SerializedObject(casillero);
        datos.FindProperty("spriteAbierto").objectReferenceValue = abierto;
        datos.FindProperty("probabilidadEstudiante").floatValue = 0.3f;
        float escalaY = Mathf.Max(0.001f, Mathf.Abs(objeto.transform.lossyScale.y));
        datos.FindProperty("offsetSalida").vector2Value = new Vector2(limites.center.x,
            limites.min.y - 0.8f / escalaY);
        datos.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAssetAndConnect(objeto, ruta, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(objeto.scene);
        // Conservar los ajustes que el usuario tiene actualmente abiertos en la escena.
        EditorSceneManager.SaveScene(objeto.scene);
        File.WriteAllText(Resultado, $"OK: {ruta}; escena={objeto.scene.path}; posicion={objeto.transform.position}; collider={caja.bounds}");
        Debug.Log("Casillero listo: bloquea el paso, abre una vez y tiene 30% de generar un estudiante.", objeto);
    }

    [MenuItem("Emoliente/Verificar casillero")]
    public static void Verificar()
    {
        var escenaAnterior = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject prueba = null;
        try
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Casillero.prefab");
            if (prefab == null) throw new Exception("Falta el prefab.");
            prueba = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            prueba.transform.position = Vector3.zero;
            Casillero casillero = prueba.GetComponent<Casillero>();
            typeof(Casillero).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(casillero, null);
            BoxCollider2D obstaculo = prueba.GetComponent<BoxCollider2D>();
            GameObject jugador = new GameObject("PiesPrueba", typeof(BoxCollider2D));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(jugador, escena);
            BoxCollider2D pies = jugador.GetComponent<BoxCollider2D>();
            pies.size = Vector2.one * 0.5f;
            Physics2D.SyncTransforms();
            Bounds limites = obstaculo.bounds;
            jugador.transform.position = new Vector3(limites.min.x - 2f, limites.center.y, 0f);
            Physics2D.SyncTransforms();
            Vector3 destino = new Vector3(limites.max.x + 5f, limites.center.y, 0f);
            Vector3 resultado = Casillero.LimitarMovimiento(pies, jugador.transform.position, destino);
            if (resultado.x + pies.bounds.extents.x > limites.min.x + 0.0001f)
                throw new Exception($"Atraviesa horizontalmente: resultado={resultado}, caja={limites}, pies={pies.bounds}.");
            jugador.transform.position = new Vector3(limites.center.x, limites.min.y - 2f, 0f);
            Physics2D.SyncTransforms();
            resultado = Casillero.LimitarMovimiento(pies, jugador.transform.position,
                new Vector3(limites.center.x, limites.max.y + 5f, 0f));
            if (resultado.y + pies.bounds.extents.y > limites.min.y) throw new Exception("Atraviesa el casillero verticalmente.");
            jugador.transform.position = new Vector3(limites.min.x - 2f, limites.max.y + 2f, 0f);
            Physics2D.SyncTransforms();
            destino = jugador.transform.position + Vector3.right * 10f;
            resultado = Casillero.LimitarMovimiento(pies, jugador.transform.position, destino);
            if ((resultado - destino).sqrMagnitude > 0.0001f) throw new Exception("Bloquea pasar por detras.");
            var datos = new SerializedObject(casillero);
            if (Mathf.Abs(datos.FindProperty("probabilidadEstudiante").floatValue - 0.3f) > 0.001f)
                throw new Exception("Probabilidad incorrecta.");
            Sprite abierto = (Sprite)datos.FindProperty("spriteAbierto").objectReferenceValue;
            datos.FindProperty("probabilidadEstudiante").floatValue = 0f;
            datos.ApplyModifiedPropertiesWithoutUndo();
            casillero.Abrir();
            if (prueba.GetComponent<SpriteRenderer>().sprite != abierto) throw new Exception("No cambia al sprite abierto.");
            casillero.Abrir();
            if (!obstaculo.enabled) throw new Exception("Pierde el bloqueo al abrir.");
            GameObject generadorGO = new GameObject("GeneradorPrueba", typeof(BoxCollider2D), typeof(GeneradorEstudiantes));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(generadorGO, escena);
            generadorGO.GetComponent<BoxCollider2D>().size = Vector2.one * 100f;
            GeneradorEstudiantes generador = generadorGO.GetComponent<GeneradorEstudiantes>();
            var datosGenerador = new SerializedObject(generador);
            datosGenerador.FindProperty("zonaPasillo").objectReferenceValue = generadorGO.GetComponent<BoxCollider2D>();
            var modelos = datosGenerador.FindProperty("prefabsEstudiantes");
            modelos.arraySize = 1;
            modelos.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Estudiante.prefab").GetComponent<EstudiantePasillo>();
            datosGenerador.ApplyModifiedPropertiesWithoutUndo();
            GameObject segundaPrueba = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
            var segundo = segundaPrueba.GetComponent<Casillero>();
            var datosSegundo = new SerializedObject(segundo);
            datosSegundo.FindProperty("probabilidadEstudiante").floatValue = 1f;
            datosSegundo.ApplyModifiedPropertiesWithoutUndo();
            segundo.Abrir();
            if (generador.CantidadActiva != 1) throw new Exception("No genera al estudiante al acertar la tirada.");
            segundo.Abrir();
            if (generador.CantidadActiva != 1) throw new Exception("Repite la tirada al volver a golpear.");
            segundo.Cerrar();
            segundo.Abrir();
            if (generador.CantidadActiva != 1) throw new Exception("Genera otro estudiante al reabrir.");
            File.WriteAllText(Resultado, "OK: bloqueo horizontal/vertical; paso por detras; probabilidad 30%; cambio de sprite; genera estudiante; una sola tirada.");
        }
        finally
        {
            if (prueba != null)
                typeof(Casillero).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(prueba.GetComponent<Casillero>(), null);
            EditorSceneManager.CloseScene(escena, true);
            if (escenaAnterior.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(escenaAnterior);
        }
    }
}
