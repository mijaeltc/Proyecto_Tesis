using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hace que el botón "Quitar" (BtnPop) tenga dos usos:
///   - Toque corto: quita un elemento (pop / dequeue / eliminar), como antes.
///   - Mantenerlo presionado 1.5 segundos: borra TODOS los datos de la estructura que se está viendo.
/// Mientras se mantiene presionado, el botón se va poniendo rojo para avisar que está por borrar.
///
/// CÓMO USARLO:
/// 1. Selecciona el botón BtnPop. En su Button → On Click () BORRA el evento de "Quitar"
///    (botón −); ahora este script se encarga de llamarlo.
/// 2. Add Component → BotonQuitarMantener en BtnPop.
/// 3. Arrastra el objeto "BotonesEstructura" al campo "Botones".
/// </summary>
public class BotonQuitarMantener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Objeto con el script BotonesEstructura")]
    public BotonesEstructura botones;

    [Tooltip("Segundos que hay que mantener presionado para borrar todo")]
    public float segundosMantener = 1.5f;

    [Tooltip("Color al que se va poniendo el botón mientras se mantiene presionado")]
    public Color colorAlMantener = new Color(0.9f, 0.2f, 0.2f);

    private bool presionado;
    private bool yaBorro;
    private float inicio;
    private Image imagen;
    private Color colorOriginal = Color.white;

    private void Awake()
    {
        imagen = GetComponent<Image>();
        if (imagen != null) colorOriginal = imagen.color;
    }

    public void OnPointerDown(PointerEventData evento)
    {
        presionado = true;
        yaBorro = false;
        inicio = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData evento)
    {
        if (!presionado) return;
        presionado = false;
        Restaurar();
        // Si no llegó al tiempo indicado, es un toque normal
        if (!yaBorro && botones != null) botones.Quitar();
    }

    // Si el dedo o el mouse se sale del botón, se cancela
    public void OnPointerExit(PointerEventData evento)
    {
        if (!presionado) return;
        presionado = false;
        Restaurar();
    }

    private void Update()
    {
        if (!presionado || yaBorro) return;
        float avance = (Time.unscaledTime - inicio) / Mathf.Max(0.1f, segundosMantener);
        if (imagen != null) imagen.color = Color.Lerp(colorOriginal, colorAlMantener, Mathf.Clamp01(avance));
        if (avance >= 1f)
        {
            yaBorro = true;
            Restaurar();
            if (botones != null) botones.LimpiarTodo();
        }
    }

    private void OnDisable()
    {
        presionado = false;
        Restaurar();
    }

    private void Restaurar()
    {
        if (imagen != null) imagen.color = colorOriginal;
    }
}