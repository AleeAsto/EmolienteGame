using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SortingGroup))]
[DefaultExecutionOrder(500)]
public class OrdenProfundidadPersonaje : MonoBehaviour
{
    [SerializeField, Tooltip("Caja que marca donde apoya los pies el personaje.")]
    private BoxCollider2D referenciaPies;

    private static readonly List<OrdenProfundidadPersonaje> personajes = new List<OrdenProfundidadPersonaje>();
    private SortingGroup grupo;
    private float AlturaPies => referenciaPies != null
        ? referenciaPies.transform.TransformPoint(referenciaPies.offset).y : transform.position.y;

    private void OnEnable()
    {
        grupo = GetComponent<SortingGroup>();
        if (!personajes.Contains(this)) personajes.Add(this);
    }

    private void OnDisable() => personajes.Remove(this);

    private void LateUpdate()
    {
        if (grupo == null) return;
        // Rangos consecutivos conservan el suelo debajo y los efectos (orden 30)
        // encima de los tres actores, sin modificar los colliders ni los sprites.
        int orden = 10;
        float altura = AlturaPies;
        int indicePropio = personajes.IndexOf(this);
        for (int i = 0; i < personajes.Count; i++)
        {
            var otro = personajes[i];
            if (otro == null || otro == this || !otro.isActiveAndEnabled ||
                otro.gameObject.scene != gameObject.scene || otro.grupo == null ||
                otro.grupo.sortingLayerID != grupo.sortingLayerID)
                continue;
            float otraAltura = otro.AlturaPies;
            if (otraAltura > altura || (otraAltura == altura && i < indicePropio))
                orden++;
        }
        grupo.sortingOrder = orden;
    }
}
