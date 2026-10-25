using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AlmaEstudiante))]
public class BrillosEstudianteRaro : MonoBehaviour
{
    [SerializeField] private Sprite spriteBrillo;
    [SerializeField, Range(3, 8)] private int cantidad = 5;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.45f, 1f);
    private Transform[] estrellas;
    private SpriteRenderer[,] destellos;
    private Vector3 centro;
    private Vector2 radio;
    private AlmaEstudiante alma;
    private EstudiantePasillo recorrido;

    private void Start()
    {
        alma = GetComponent<AlmaEstudiante>();
        recorrido = GetComponent<EstudiantePasillo>();
        var cuerpo = GetComponentInChildren<SpriteRenderer>();
        if (!alma.EsRaro || cuerpo == null || spriteBrillo == null)
        {
            enabled = false;
            return;
        }

        // Crear despues del Awake de EstadoVisualEstudiante: el brillo tiene su propio fade.
        centro = transform.InverseTransformPoint(cuerpo.bounds.center);
        Vector3 escala = transform.lossyScale;
        radio = new Vector2(cuerpo.bounds.extents.x / Mathf.Max(0.001f, Mathf.Abs(escala.x)) + 0.3f,
            cuerpo.bounds.extents.y / Mathf.Max(0.001f, Mathf.Abs(escala.y)) + 0.15f);
        estrellas = new Transform[cantidad];
        destellos = new SpriteRenderer[cantidad, 2];
        for (int i = 0; i < cantidad; i++)
        {
            var estrella = new GameObject("BrilloRaro" + (i + 1));
            estrella.layer = gameObject.layer;
            estrella.transform.SetParent(transform, false);
            estrellas[i] = estrella.transform;
            for (int eje = 0; eje < 2; eje++)
            {
                var rayo = new GameObject("Rayo", typeof(SpriteRenderer));
                rayo.layer = gameObject.layer;
                rayo.transform.SetParent(estrella.transform, false);
                rayo.transform.localRotation = Quaternion.Euler(0, 0, eje * 90f);
                rayo.transform.localScale = new Vector3(0.32f / spriteBrillo.bounds.size.x,
                    0.065f / spriteBrillo.bounds.size.y, 1f);
                var sprite = rayo.GetComponent<SpriteRenderer>();
                sprite.sprite = spriteBrillo;
                sprite.sharedMaterial = cuerpo.sharedMaterial;
                sprite.sortingLayerID = cuerpo.sortingLayerID;
                sprite.sortingOrder = cuerpo.sortingOrder + 1;
                destellos[i, eje] = sprite;
            }
        }
        ActualizarBrillos();
    }

    private void LateUpdate() => ActualizarBrillos();

    private void ActualizarBrillos()
    {
        if (estrellas == null)
            return;
        float opacidad = recorrido != null ? recorrido.OpacidadAgotado : 1f;
        for (int i = 0; i < estrellas.Length; i++)
        {
            float fase = i * Mathf.PI * 2f / estrellas.Length;
            float angulo = fase + Time.time * 0.45f;
            estrellas[i].localPosition = centro + new Vector3(Mathf.Cos(angulo) * radio.x,
                Mathf.Sin(angulo) * radio.y, 0f);
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f + fase);
            estrellas[i].localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, pulso);
            estrellas[i].localRotation = Quaternion.Euler(0, 0, 45f + Mathf.Sin(angulo) * 20f);
            Color tinte = color;
            tinte.a *= Mathf.Lerp(0.35f, 1f, pulso) * opacidad;
            for (int eje = 0; eje < 2; eje++)
                destellos[i, eje].color = tinte;
        }
    }
}
