using TMPro;
using UnityEngine;
using Vuforia;

/// <summary>
/// Lanza la demostración de una estructura apenas Vuforia detecta su marcador (QR), para que
/// el estudiante sepa de inmediato que el QR fue reconocido.
///
/// CÓMO USARLO:
/// 1. Agrega este script al Image Target (el mismo objeto que tiene "Image Target Behaviour"):
///    qr_pila, qr_cola y qr_arbol.
/// 2. La estructura (PilaController, ColaController o ArbolController) debe estar en un
///    hijo de ese Image Target. El script la encuentra solo; no hay que arrastrar nada.
/// 3. (Opcional) Arrastra un TextMeshPro de la interfaz (Canvas) a "Texto Estado" para mostrar
///    un mensaje como "Marcador detectado: Pila".
/// </summary>
[RequireComponent(typeof(ObserverBehaviour))]
public class MarcadorDemo : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("Solo lanza la demostración si la estructura está vacía (así no borra lo que el estudiante ya hizo si se pierde y recupera el QR)")]
    public bool soloSiEstaVacia = true;

    [Tooltip("Si el QR se pierde de vista, cancela los valores de demostración que faltaban")]
    public bool detenerAlPerder = true;

    [Header("Mensaje en pantalla (opcional)")]
    public TMP_Text textoEstado;
    public string mensajeDetectado = "Marcador detectado";
    public string mensajePerdido = "Apunta la cámara al marcador";

    private ObserverBehaviour observer;
    private IDemoEstructura estructura;
    private bool visible;

    private void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
        estructura = GetComponentInChildren<IDemoEstructura>(true);
        if (estructura == null)
        {
            Debug.LogWarning("MarcadorDemo: no se encontró ninguna estructura (Pila/Cola/Árbol) como hijo de " + name);
        }
    }

    private void OnEnable()
    {
        if (observer != null) observer.OnTargetStatusChanged += AlCambiarEstado;
    }

    private void OnDisable()
    {
        if (observer != null) observer.OnTargetStatusChanged -= AlCambiarEstado;
    }

    private void Start()
    {
        // Por si el marcador ya estaba a la vista antes de suscribirnos
        if (observer != null) Evaluar(observer.TargetStatus.Status);
        if (!visible && textoEstado != null) textoEstado.text = mensajePerdido;
    }

    private void AlCambiarEstado(ObserverBehaviour behaviour, TargetStatus status)
    {
        Evaluar(status.Status);
    }

    private void Evaluar(Status estado)
    {
        Debug.Log("MarcadorDemo (" + name + "): estado del marcador = " + estado);

        // Solo TRACKED cuenta como "detectado". Con Extended Tracking activo, al apartar la cámara
        // Vuforia pasa a EXTENDED_TRACKED (sigue dibujando el contenido) y no a NO_POSE; por eso
        // ese estado ya no se considera "a la vista" para el mensaje.
        bool ahoraVisible = estado == Status.TRACKED;
        if (ahoraVisible == visible) return;
        visible = ahoraVisible;

        if (visible)
        {
            if (textoEstado != null) textoEstado.text = mensajeDetectado;
            if (estructura != null && (!soloSiEstaVacia || estructura.EstaVacia))
            {
                estructura.IniciarDemo();
            }
        }
        else
        {
            if (textoEstado != null) textoEstado.text = mensajePerdido;
            if (detenerAlPerder && estructura != null) estructura.DetenerDemo();
        }
    }
}
