using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BotonArteUI : MonoBehaviour, ISelectHandler, IDeselectHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject indicador;
    private bool seleccionado, encima;
    private Button boton;

    private void Awake() => boton = GetComponent<Button>();

    public void Configurar(GameObject marca) => indicador = marca;
    public void OnSelect(BaseEventData evento) { seleccionado = true; Actualizar(); }
    public void OnDeselect(BaseEventData evento) { seleccionado = false; Actualizar(); }
    public void OnPointerEnter(PointerEventData evento)
    {
        if (boton != null && !boton.IsInteractable()) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        encima = true; Actualizar();
    }
    public void OnPointerExit(PointerEventData evento) { encima = false; Actualizar(); }

    private void Actualizar()
    {
        bool enfocado = seleccionado || encima;
        transform.localScale = Vector3.one * (enfocado ? 1.045f : 1f);
        if (indicador != null) indicador.SetActive(enfocado);
    }

    private void OnDisable()
    {
        seleccionado = encima = false;
        Actualizar();
    }
}
