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
/// </summary>
public class PilaController : MonoBehaviour
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

    [Tooltip("Cantidad máxima de elementos permitidos (deja vacío el push si se supera)")]
    public int capacidadMaxima = 6;

    private List<GameObject> pila = new List<GameObject>();
    private bool animando = false;

    /// <summary>Cantidad actual de elementos en la pila.</summary>
    public int Cantidad => pila.Count;

    /// <summary>Agrega un nuevo elemento a la cima de la pila, animando su caída.</summary>
    public void Push(string valor)
    {
        if (animando)
        {
            Debug.Log("PilaController: espera a que termine la animación actual antes de otro push.");
            return;
        }
        if (pila.Count >= capacidadMaxima)
        {
            Debug.Log("PilaController: la pila alcanzó su capacidad máxima.");
            return;
        }
        StartCoroutine(PushCoroutine(valor));
    }

    /// <summary>Elimina el elemento superior de la pila, animando su salida.</summary>
    public void Pop()
    {
        if (animando)
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
        if (animando) return;
        foreach (var elemento in pila)
        {
            if (elemento != null) Destroy(elemento);
        }
        pila.Clear();
    }

    private IEnumerator PushCoroutine(string valor)
    {
        animando = true;

        GameObject nuevo = Instantiate(elementoPrefab, transform);
        // Respeta la escala del prefab (el marcador mide solo 0.1 m de ancho)
        nuevo.transform.localScale = elementoPrefab.transform.localScale;
        nuevo.transform.localRotation = Quaternion.identity;

        float posicionFinalY = pila.Count * alturaElemento;
        Vector3 posicionInicial = new Vector3(0f, posicionFinalY + alturaCaida, 0f);
        Vector3 posicionFinal = new Vector3(0f, posicionFinalY, 0f);
        nuevo.transform.localPosition = posicionInicial;

        TextMeshPro etiqueta = nuevo.GetComponentInChildren<TextMeshPro>();
        if (etiqueta != null) etiqueta.text = valor;

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
    }

    private IEnumerator PopCoroutine()
    {
        animando = true;

        GameObject tope = pila[pila.Count - 1];
        pila.RemoveAt(pila.Count - 1);

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
