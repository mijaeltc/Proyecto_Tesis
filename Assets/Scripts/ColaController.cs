using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla la animación de una Cola (Queue, FIFO) en la escena de RA. Los elementos se
/// colocan en fila sobre el eje X del marcador: ENQUEUE entra por la derecha (el final de
/// la cola) y DEQUEUE sale por la izquierda (el frente), y los demás avanzan un lugar.
///
/// CÓMO USARLO:
/// 1. Crea un GameObject vacío como hijo de tu Image Target "qr_cola" (por ejemplo,
///    llámalo "ColaContainer", posición 0,0,0) y agrégale este script.
/// 2. Crea un Prefab "ElementoCola": un GameObject vacío (escala 1,1,1) con dos hijos:
///    - "Cubo": Cube con escala 0.03, 0.03, 0.03 y posición local (0, 0.015, 0).
///    - "Texto": TextMeshPro 3D (no el de UI) con posición local (0, 0.0305, 0), rotación
///      (90, 0, 0), alineación centrada y un tamaño de fuente pequeño (empieza en 0.3 y
///      ajústalo hasta que el número quepa en la cara superior del cubo).
///    Usa un material de URP (por ejemplo, un Lit de color) en el cubo.
/// 3. Arrastra ese Prefab al campo "Elemento Prefab".
/// 4. (Opcional) Crea dos TextMeshPro 3D hijos de ColaContainer con los textos "FRENTE" y
///    "FINAL" y arrástralos a "Etiqueta Frente" y "Etiqueta Final"; el script los mueve
///    sobre el primer y el último elemento.
/// 5. Llama a colaController.Enqueue("10") o colaController.Dequeue() desde botones de la
///    UI o desde la lógica del chat con IA.
/// 6. Para que la demostración salte sola al detectar el QR, agrega MarcadorDemo al Image
///    Target "qr_cola".
/// </summary>
public class ColaController : MonoBehaviour, IDemoEstructura
{
    [Header("Configuración de la Cola")]
    [Tooltip("Prefab del elemento de la cola (con un TextMeshPro hijo)")]
    public GameObject elementoPrefab;

    [Tooltip("Distancia entre un elemento y el siguiente en el eje X (metros)")]
    public float separacion = 0.035f;

    [Tooltip("Posición X del frente de la cola (el primer lugar)")]
    public float xInicial = -0.07f;

    [Tooltip("Duración de cada animación en segundos")]
    public float duracionAnimacion = 0.5f;

    [Tooltip("Cantidad máxima de elementos. 0 = sin límite (la cola se va achicando para caber en el marcador)")]
    public int limiteElementos = 0;

    [Tooltip("Cantidad de elementos que caben a tamaño normal; al pasar de ahí, toda la cola se reduce")]
    public int elementosVisibles = 5;

    [Tooltip("Tamaño mínimo al que se puede reducir la cola (1 = tamaño normal)")]
    public float escalaMinima = 0.15f;

    [Header("Etiquetas (opcional)")]
    public TextMeshPro etiquetaFrente;
    public TextMeshPro etiquetaFinal;

    [Header("Demostración al detectar el QR")]
    [Tooltip("Valores que se encolan automáticamente cuando se detecta el marcador")]
    public string[] valoresDemo = { "10", "20", "30" };

    [Tooltip("Pausa entre un valor de la demostración y el siguiente (segundos)")]
    public float pausaDemo = 0.8f;

    private readonly List<GameObject> cola = new List<GameObject>();
    private bool animando = false;
    private float animFin = 0f;      // seguro: si una animación se queda trabada, se libera sola
    private Coroutine demoRutina;

    /// <summary>Cantidad actual de elementos en la cola.</summary>
    public int Cantidad => cola.Count;

    public bool Ocupado
    {
        get
        {
            if (animando && Time.time > animFin)
            {
                Debug.LogWarning("ColaController: una animación se quedó trabada; se libera automáticamente.");
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
        // El frente de la cola se queda en su sitio mientras todo se reduce hacia la derecha
        transform.localPosition = posicionBase + transform.localRotation * new Vector3(escalaBase.x * xInicial * (1f - factor), 0f, 0f);
    }

    // Si el objeto se desactiva, las corrutinas se cancelan sin terminar
    private void OnDisable()
    {
        animando = false;
        demoRutina = null;
    }

    /// <summary>true si la cola no tiene elementos (IDemoEstructura).</summary>
    public bool EstaVacia => cola.Count == 0;

    private void Start()
    {
        ActualizarEtiquetas();
    }

    /// <summary>Agrega un elemento al final de la cola; entra deslizándose desde la derecha.</summary>
    public void Enqueue(string valor)
    {
        if (Ocupado)
        {
            Debug.Log("ColaController: espera a que termine la animación actual antes de otro enqueue.");
            return;
        }
        if (limiteElementos > 0 && cola.Count >= limiteElementos)
        {
            Debug.Log("ColaController: la cola alcanzó el límite de elementos configurado.");
            return;
        }
        StartCoroutine(EnqueueCoroutine(valor));
    }

    /// <summary>Quita el elemento del frente de la cola; los demás avanzan un lugar.</summary>
    public void Dequeue()
    {
        if (Ocupado)
        {
            Debug.Log("ColaController: espera a que termine la animación actual antes de otro dequeue.");
            return;
        }
        if (cola.Count == 0)
        {
            Debug.Log("ColaController: la cola está vacía, no se puede hacer dequeue.");
            return;
        }
        StartCoroutine(DequeueCoroutine());
    }

    /// <summary>Elimina todos los elementos de golpe (útil para reiniciar la demostración).</summary>
    public void LimpiarCola()
    {
        if (Ocupado) return;
        foreach (var elemento in cola)
        {
            if (elemento != null) Destroy(elemento);
        }
        cola.Clear();
        FijarObjetivo(0);
        ActualizarEtiquetas();
    }

    /// <summary>Encola automáticamente los valores de demostración (IDemoEstructura).</summary>
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
            Enqueue(valor);
            yield return new WaitForSeconds(pausaDemo);
        }
        demoRutina = null;
    }

    private Vector3 PosicionDelLugar(int indice)
    {
        return new Vector3(xInicial + indice * separacion, 0f, 0f);
    }

    private IEnumerator EnqueueCoroutine(string valor)
    {
        animando = true;
        animFin = Time.time + duracionAnimacion + 1f;
        FijarObjetivo(cola.Count + 1);

        GameObject nuevo = Instantiate(elementoPrefab, transform);
        nuevo.transform.localScale = elementoPrefab.transform.localScale;
        nuevo.transform.localRotation = Quaternion.identity;

        Vector3 posicionFinal = PosicionDelLugar(cola.Count);
        // Entra desde la derecha, un poco más allá del último lugar posible
        Vector3 posicionInicial = posicionFinal + new Vector3(0.06f, 0f, 0f);
        nuevo.transform.localPosition = posicionInicial;

        TextMeshPro etiqueta = nuevo.GetComponentInChildren<TextMeshPro>();
        if (etiqueta != null) etiqueta.text = valor;

        float tiempo = 0f;
        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacion);
            float tSuavizado = 1f - Mathf.Pow(1f - t, 3f);
            nuevo.transform.localPosition = Vector3.Lerp(posicionInicial, posicionFinal, tSuavizado);
            yield return null;
        }
        nuevo.transform.localPosition = posicionFinal;

        cola.Add(nuevo);
        ActualizarEtiquetas();
        animando = false;
    }

    private IEnumerator DequeueCoroutine()
    {
        animando = true;
        animFin = Time.time + duracionAnimacion * 2f + 1f;

        // 1) El elemento del frente sale hacia la izquierda y se encoge
        GameObject frente = cola[0];
        cola.RemoveAt(0);
        FijarObjetivo(cola.Count);

        Vector3 inicioSalida = frente.transform.localPosition;
        Vector3 finSalida = inicioSalida + new Vector3(-0.05f, 0f, 0f);
        Vector3 escalaInicial = frente.transform.localScale;

        float tiempo = 0f;
        float duracionSalida = duracionAnimacion * 0.8f;
        while (tiempo < duracionSalida)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionSalida);
            frente.transform.localPosition = Vector3.Lerp(inicioSalida, finSalida, t);
            frente.transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t);
            yield return null;
        }
        Destroy(frente);

        // 2) Los demás avanzan un lugar hacia el frente
        if (cola.Count > 0)
        {
            Vector3[] inicios = new Vector3[cola.Count];
            for (int i = 0; i < cola.Count; i++) inicios[i] = cola[i].transform.localPosition;

            tiempo = 0f;
            float duracionAvance = duracionAnimacion * 0.6f;
            while (tiempo < duracionAvance)
            {
                tiempo += Time.deltaTime;
                float t = Mathf.Clamp01(tiempo / duracionAvance);
                float tSuavizado = 1f - Mathf.Pow(1f - t, 3f);
                for (int i = 0; i < cola.Count; i++)
                {
                    cola[i].transform.localPosition = Vector3.Lerp(inicios[i], PosicionDelLugar(i), tSuavizado);
                }
                yield return null;
            }
            for (int i = 0; i < cola.Count; i++) cola[i].transform.localPosition = PosicionDelLugar(i);
        }

        ActualizarEtiquetas();
        animando = false;
    }

    private void ActualizarEtiquetas()
    {
        if (etiquetaFrente != null)
        {
            etiquetaFrente.gameObject.SetActive(cola.Count > 0);
            if (cola.Count > 0) ColocarEtiqueta(etiquetaFrente, PosicionDelLugar(0));
        }
        if (etiquetaFinal != null)
        {
            // Con un solo elemento, "FRENTE" y "FINAL" caerían en el mismo sitio: se muestra solo "FRENTE"
            etiquetaFinal.gameObject.SetActive(cola.Count > 1);
            if (cola.Count > 1) ColocarEtiqueta(etiquetaFinal, PosicionDelLugar(cola.Count - 1));
        }
    }

    private void ColocarEtiqueta(TextMeshPro etiqueta, Vector3 posicionElemento)
    {
        // Sobre el elemento (adelante en Z), tendida sobre el marcador para leerla desde arriba
        etiqueta.transform.localPosition = posicionElemento + new Vector3(0f, 0.002f, 0.03f);
        etiqueta.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}