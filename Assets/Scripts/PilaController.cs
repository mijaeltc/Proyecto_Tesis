using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla la animación de una Pila (Stack) en la escena de RA: agrega y quita
/// elementos con las operaciones push/pop, animando su entrada y salida.
///
/// CÓMO USARLO:
/// 1. Crea un GameObject vacío como hijo de tu Image Target "qr_pila" (por ejemplo,
///    llámalo "PilaContainer") y agrégale este script.
/// 2. Crea un Prefab llamado "ElementoPila": un GameObject vacío con escala (1,1,1)
///    que contiene dos hijos: "Cubo" (Cube con escala 0.08, 0.03, 0.08) y "Texto"
///    (TextMeshPro 3D) pegado a la cara frontal del cubo, para mostrar el valor.
/// 3. Arrastra ese Prefab al campo "Elemento Prefab" en el Inspector de este script.
/// 4. Llama a pilaController.Push("10") o pilaController.Pop() desde cualquier otro
///    script (por ejemplo, desde botones de la UI o desde la lógica del chat con IA).
/// 5. Para que la demostración salte sola al detectar el QR, agrega MarcadorDemo al
///    Image Target (ver MarcadorDemo.cs). Los valores de la demostración se editan en
///    "Valores Demo" en el Inspector.
/// </summary>
public class PilaController : MonoBehaviour, IDemoEstructura
{
    [Header("Configuración de la Pila")]
    [Tooltip("Prefab del cubo que representa cada elemento de la pila (con un TextMeshPro hijo)")]
    public GameObject elementoPrefab;

    [Tooltip("Altura que ocupa cada elemento apilado (en unidades locales, relativo al marcador)")]
    public float alturaElemento = 0.03f;

    [Tooltip("Duración de la animación de push/pop en segundos")]
    public float duracionAnimacion = 0.5f;

    [Tooltip("Altura desde la que cae un nuevo elemento al hacer push")]
    public float alturaCaida = 0.2f;

    [Tooltip("Cantidad máxima de elementos. 0 = sin límite (la pila se va achicando para caber en el marcador)")]
    public int limiteElementos = 0;

    [Tooltip("Cantidad de elementos que caben a tamaño normal; al pasar de ahí, toda la pila se reduce")]
    public int elementosVisibles = 6;

    [Tooltip("Tamaño mínimo al que se puede reducir la pila (1 = tamaño normal)")]
    public float escalaMinima = 0.15f;

    [Header("Demostración al detectar el QR")]
    [Tooltip("Valores que se apilan automáticamente cuando se detecta el marcador")]
    public string[] valoresDemo = { "10", "20", "30" };

    [Tooltip("Pausa entre un valor de la demostración y el siguiente (segundos)")]
    public float pausaDemo = 0.8f;

    private List<GameObject> pila = new List<GameObject>();
    private bool animando = false;
    private float animFin = 0f;      // seguro: si una animación se queda trabada, se libera sola
    private Coroutine demoRutina;

    /// <summary>Cantidad actual de elementos en la pila.</summary>
    public int Cantidad => pila.Count;

    // true solo si hay una animación en curso (y no lleva "demasiado" tiempo)
    public bool Ocupado
    {
        get
        {
            if (animando && Time.time > animFin)
            {
                Debug.LogWarning("PilaController: una animación se quedó trabada; se libera automáticamente.");
                animando = false;
            }
            return animando;
        }
    }


    // ---- Ajuste automático de tamaño: sin límite de elementos ----
    private Vector3 escalaBase = Vector3.one;
    private Vector3 posicionBase = Vector3.zero;
    private float factor = 1f;
    private float factorObjetivo = 1f;

    private void Awake()
    {
        escalaBase = transform.localScale;
        posicionBase = transform.localPosition;
    }

    private void FijarObjetivo(int cuantos)
    {
        factorObjetivo = cuantos <= elementosVisibles
            ? 1f
            : Mathf.Clamp(elementosVisibles / (float)cuantos, escalaMinima, 1f);
    }

    private void Update()
    {
        if (Mathf.Approximately(factor, factorObjetivo)) return;
        factor = Mathf.Abs(factor - factorObjetivo) < 0.0005f
            ? factorObjetivo
            : Mathf.Lerp(factor, factorObjetivo, 1f - Mathf.Exp(-8f * Time.deltaTime));
        transform.localScale = escalaBase * factor;
    }

    // Si el objeto se desactiva (p. ej., al perder el marcador), las corrutinas se cancelan sin terminar
    private void OnDisable()
    {
        animando = false;
        demoRutina = null;
    }

    /// <summary>true si la pila no tiene elementos (IDemoEstructura).</summary>
    public bool EstaVacia => pila.Count == 0;

    /// <summary>Apila automáticamente los valores de demostración (IDemoEstructura).</summary>
    public void IniciarDemo()
    {
        DetenerDemo();
        demoRutina = StartCoroutine(DemoCoroutine());
    }

    /// <summary>Cancela los valores de demostración pendientes (IDemoEstructura).</summary>
    public void DetenerDemo()
    {
        if (demoRutina != null)
        {
            StopCoroutine(demoRutina);
            demoRutina = null;
        }
    }

    private IEnumerator DemoCoroutine()
    {
        foreach (string valor in valoresDemo)
        {
            while (Ocupado) yield return null;
            Push(valor);
            yield return new WaitForSeconds(pausaDemo);
        }
        demoRutina = null;
    }

    /// <summary>Agrega un nuevo elemento a la cima de la pila, animando su caída.</summary>
    public void Push(string valor)
    {
        if (elementoPrefab == null)
        {
            Debug.LogError("PilaController: falta asignar 'Elemento Prefab' en el Inspector de PilaContainer (arrastra el prefab ElementoPila).");
            return;
        }
        if (Ocupado)
        {
            Debug.Log("PilaController: espera a que termine la animación actual antes de otro push.");
            return;
        }
        if (limiteElementos > 0 && pila.Count >= limiteElementos)
        {
            Debug.Log("PilaController: la pila alcanzó el límite de elementos configurado.");
            return;
        }
        StartCoroutine(PushCoroutine(valor));
    }

    /// <summary>Elimina el elemento superior de la pila, animando su salida.</summary>
    public void Pop()
    {
        if (Ocupado)
        {
            Debug.Log("PilaController: espera a que termine la animación actual antes de otro pop.");
            return;
        }
        if (pila.Count == 0)
        {
            Debug.Log("PilaController: la pila está vacía, no se puede hacer pop.");
            return;
        }
        StartCoroutine(PopCoroutine());
    }

    /// <summary>Elimina todos los elementos de golpe (útil para reiniciar la demostración).</summary>
    public void LimpiarPila()
    {
        if (Ocupado) return;
        foreach (var elemento in pila)
        {
            if (elemento != null) Destroy(elemento);
        }
        pila.Clear();
        FijarObjetivo(0);
    }

    private IEnumerator PushCoroutine(string valor)
    {
        animando = true;
        animFin = Time.time + duracionAnimacion + 1f;
        FijarObjetivo(pila.Count + 1);
        Debug.Log("PilaController: push(" + valor + ") iniciado. Elementos actuales: " + pila.Count);

        GameObject nuevo = null;
        Vector3 posicionInicial = Vector3.zero;
        Vector3 posicionFinal = Vector3.zero;
        bool listo = false;

        // Todo lo que puede fallar va aquí; si falla, se informa en rojo en la Console y se libera la pila
        try
        {
            nuevo = Instantiate(elementoPrefab, transform);
            // Respeta la escala del prefab (el marcador mide solo 0.1 m de ancho)
            nuevo.transform.localScale = elementoPrefab.transform.localScale;
            nuevo.transform.localRotation = Quaternion.identity;

            float posicionFinalY = pila.Count * alturaElemento;
            posicionInicial = new Vector3(0f, posicionFinalY + alturaCaida, 0f);
            posicionFinal = new Vector3(0f, posicionFinalY, 0f);
            nuevo.transform.localPosition = posicionInicial;

            TextMeshPro etiqueta = nuevo.GetComponentInChildren<TextMeshPro>();
            if (etiqueta != null) etiqueta.text = valor;
            else Debug.LogWarning("PilaController: el prefab no tiene un TextMeshPro (3D) hijo; el número no se mostrará.");
            listo = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("PilaController: falló el push. " + e.GetType().Name + ": " + e.Message);
        }

        if (!listo)
        {
            if (nuevo != null) Destroy(nuevo);
            animando = false;
            yield break;
        }

        float tiempo = 0f;
        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacion);
            // easing simple tipo "ease-out" para que la caída se sienta más natural
            float tSuavizado = 1f - Mathf.Pow(1f - t, 3f);
            nuevo.transform.localPosition = Vector3.Lerp(posicionInicial, posicionFinal, tSuavizado);
            yield return null;
        }
        nuevo.transform.localPosition = posicionFinal;

        pila.Add(nuevo);
        animando = false;
        Debug.Log("PilaController: push(" + valor + ") terminado.");
    }

    private IEnumerator PopCoroutine()
    {
        animando = true;
        animFin = Time.time + duracionAnimacion + 1f;

        GameObject tope = pila[pila.Count - 1];
        pila.RemoveAt(pila.Count - 1);
        FijarObjetivo(pila.Count);

        Vector3 posicionInicial = tope.transform.localPosition;
        Vector3 posicionFinal = posicionInicial + new Vector3(alturaCaida, alturaCaida * 0.5f, 0f);
        Vector3 escalaInicial = tope.transform.localScale;

        float tiempo = 0f;
        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacion);
            tope.transform.localPosition = Vector3.Lerp(posicionInicial, posicionFinal, t);
            tope.transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);
            yield return null;
        }

        Destroy(tope);
        animando = false;
    }
}