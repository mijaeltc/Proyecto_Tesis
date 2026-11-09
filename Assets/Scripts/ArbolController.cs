using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla la animación de un Árbol Binario de Búsqueda (ABB) en la escena de RA, SIN límite de
/// nodos: cada valor nuevo se inserta con la regla del ABB (menor a la izquierda, mayor a la
/// derecha) y aparece cayendo sobre su lugar, con una rama que lo une a su padre. También se
/// pueden ELIMINAR valores (con los tres casos del ABB: hoja, un hijo y dos hijos) y mostrar los
/// recorridos inorden, preorden y postorden resaltando los nodos uno por uno.
///
/// El dibujo se reacomoda solo cada vez que cambia el árbol y se achica automáticamente cuando
/// crece, para que siempre quepa sobre el marcador. Los números siempre miran a la cámara.
///
/// CÓMO USARLO:
/// 1. Crea un GameObject vacío como hijo de tu Image Target "qr_arbol" (por ejemplo,
///    llámalo "ArbolContainer", posición 0,0,0) y agrégale este script.
/// 2. Prefab "NodoArbol": un GameObject vacío (escala 1,1,1) con dos hijos:
///    - "Esfera": Sphere con escala 0.03, 0.03, 0.03 y material de URP (un Lit de color).
///    - "Texto": TextMeshPro 3D (no el de UI), alineación centrada y un tamaño de fuente
///      pequeño. Su posición y rotación no importan: el script lo coloca delante de la esfera.
/// 3. Prefab "RamaArbol": un Cube con escala (1,1,1), sin collider y con material de URP.
/// 4. Arrastra "NodoArbol" a "Nodo Prefab" y "RamaArbol" a "Rama Prefab".
/// 5. Llama a Insertar("50"), Eliminar("50") o EliminarAleatorio() desde botones de la UI o desde
///    la lógica del chat con IA. Solo se aceptan números enteros. Para ver los recorridos usa
///    RecorridoInorden(), RecorridoPreorden() o RecorridoPostorden().
/// 6. Para que la demostración salte sola al detectar el QR, agrega MarcadorDemo al Image
///    Target "qr_arbol".
/// </summary>
public class ArbolController : MonoBehaviour, IDemoEstructura
{
    [Header("Prefabs")]
    [Tooltip("Prefab del nodo (esfera + TextMeshPro 3D hijo)")]
    public GameObject nodoPrefab;

    [Tooltip("Prefab de la rama: un Cube de escala (1,1,1) que se estira entre padre e hijo")]
    public GameObject ramaPrefab;

    [Header("Forma del árbol")]
    [Tooltip("Ancho máximo que puede ocupar el dibujo; si el árbol es más ancho, se achica (metros)")]
    public float anchoTotal = 0.12f;

    [Tooltip("Alto máximo que puede ocupar el dibujo; si el árbol es más alto, se achica (metros)")]
    public float altoMaximo = 0.10f;

    [Tooltip("Altura de la raíz sobre el marcador (metros)")]
    public float alturaRaiz = 0.11f;

    [Tooltip("Distancia horizontal entre nodos vecinos (metros)")]
    public float separacionHoja = 0.035f;

    [Tooltip("Distancia vertical entre un nivel y el siguiente (metros)")]
    public float separacionNiveles = 0.04f;

    [Tooltip("Diámetro aproximado de un nodo (metros), para calcular cuánto hay que achicar")]
    public float diametroNodo = 0.03f;

    [Tooltip("Tamaño mínimo al que se puede reducir el árbol (1 = tamaño normal)")]
    public float escalaMinima = 0.15f;

    [Tooltip("Grosor de las ramas (metros)")]
    public float grosorRama = 0.004f;

    [Tooltip("Distancia a la que se coloca el número delante de la esfera, hacia la cámara (metros)")]
    public float separacionTexto = 0.017f;

    [Header("Animación")]
    public float duracionAnimacion = 0.6f;
    [Tooltip("Altura desde la que cae un nodo nuevo")]
    public float alturaCaida = 0.05f;

    [Header("Recorridos y eliminación")]
    public Color colorResaltado = new Color(1f, 0.8f, 0.1f);
    [Tooltip("Color con el que se marca el nodo que se va a eliminar")]
    public Color colorEliminar = new Color(0.9f, 0.2f, 0.2f);
    public float pausaRecorrido = 0.6f;
    [Tooltip("(Opcional) Texto donde se va mostrando el orden del recorrido o los avisos")]
    public TMP_Text textoRecorrido;

    [Header("Demostración al detectar el QR")]
    [Tooltip("Valores enteros que se insertan automáticamente cuando se detecta el marcador")]
    public string[] valoresDemo = { "50", "30", "70", "20", "40" };

    [Tooltip("Pausa entre un valor de la demostración y el siguiente (segundos)")]
    public float pausaDemo = 0.8f;

    private class Nodo
    {
        public int valor;
        public int profundidad;
        public int indice;           // posición de izquierda a derecha (recorrido inorden)
        public Nodo izquierdo;
        public Nodo derecho;
        public Nodo padre;
        public GameObject objeto;
        public GameObject rama;
        public TextMeshPro texto;
        public Renderer render;
        public Color colorOriginal;
        public Vector3 posicion;     // posición destino, local al pivote
    }

    private Nodo raiz;
    private int cantidad = 0;
    private bool animando = false;
    private float animFin = 0f;      // seguro: si una animación se queda trabada, se libera sola
    private Coroutine demoRutina;
    private Coroutine recorridoRutina;   // recorrido en curso (se puede interrumpir con otro)
    private readonly List<Nodo> todos = new List<Nodo>();
    private Transform pivote;        // se escala para que el árbol siempre quepa; parte de la raíz
    private int contadorIndice;
    private int alturaArbol;

    /// <summary>Cantidad actual de nodos.</summary>
    public int Cantidad => cantidad;

    public bool Ocupado
    {
        get
        {
            if (animando && Time.time > animFin)
            {
                Debug.LogWarning("ArbolController: una animación se quedó trabada; se libera automáticamente.");
                animando = false;
            }
            return animando;
        }
    }

    private void Awake()
    {
        pivote = new GameObject("PivoteArbol").transform;
        pivote.SetParent(transform, false);
        pivote.localPosition = new Vector3(0f, alturaRaiz, 0f);
        pivote.localRotation = Quaternion.identity;
        pivote.localScale = Vector3.one;
    }

    // Si el objeto se desactiva, las corrutinas se cancelan sin terminar
    private void OnDisable()
    {
        animando = false;
        demoRutina = null;
        recorridoRutina = null;
        foreach (var n in todos)
        {
            if (n.render != null && n.objeto != null) n.render.material.color = n.colorOriginal;
        }
    }

    // Interrumpe el recorrido en curso (si lo hay) y devuelve los nodos a su color normal
    private void CancelarRecorrido()
    {
        if (recorridoRutina == null) return;
        StopCoroutine(recorridoRutina);
        recorridoRutina = null;
        foreach (var n in todos)
        {
            if (n.render != null && n.objeto != null) n.render.material.color = n.colorOriginal;
        }
        if (textoRecorrido != null) textoRecorrido.text = "";
    }

    /// <summary>true si el árbol no tiene nodos (IDemoEstructura).</summary>
    public bool EstaVacia => raiz == null;

    /// <summary>Lista de valores actuales (inorden, de menor a mayor).</summary>
    public List<int> Valores()
    {
        List<int> v = new List<int>();
        foreach (var n in Recolectar(raiz, 0)) v.Add(n.valor);
        return v;
    }

    // ------------------------------------------------------------------ INSERTAR

    /// <summary>Inserta un número entero siguiendo la regla del ABB.</summary>
    public void Insertar(string valorTexto)
    {
        if (nodoPrefab == null || ramaPrefab == null)
        {
            Debug.LogError("ArbolController: falta asignar 'Nodo Prefab' y/o 'Rama Prefab' en el Inspector de ArbolContainer.");
            return;
        }
        if (Ocupado)
        {
            Debug.Log("ArbolController: espera a que termine la animación actual.");
            return;
        }
        if (!int.TryParse(valorTexto, out int valor))
        {
            Debug.Log("ArbolController: solo se aceptan números enteros (recibido: \"" + valorTexto + "\").");
            Avisar("Solo se aceptan números enteros");
            return;
        }

        // Busca el lugar que le corresponde según la regla del ABB
        Nodo padre = null;
        Nodo actual = raiz;
        bool aLaIzquierda = false;
        while (actual != null)
        {
            if (valor == actual.valor)
            {
                Debug.Log("ArbolController: el valor " + valor + " ya está en el árbol.");
                Avisar("El valor " + valor + " ya está en el árbol");
                return;
            }
            padre = actual;
            aLaIzquierda = valor < actual.valor;
            actual = aLaIzquierda ? actual.izquierdo : actual.derecho;
        }

        Nodo nuevo = new Nodo { valor = valor, padre = padre };
        if (padre != null)
        {
            if (aLaIzquierda) padre.izquierdo = nuevo; else padre.derecho = nuevo;
        }
        else
        {
            raiz = nuevo;
        }
        CancelarRecorrido();
        StartCoroutine(InsertarCoroutine(nuevo));
    }

    private IEnumerator InsertarCoroutine(Nodo nuevo)
    {
        animando = true;
        animFin = Time.time + duracionAnimacion + 1f;

        if (!CrearVisual(nuevo))
        {
            // Deshace el enlace para no dejar un nodo fantasma en el árbol
            if (nuevo.padre == null) raiz = null;
            else if (nuevo.padre.izquierdo == nuevo) nuevo.padre.izquierdo = null;
            else nuevo.padre.derecho = null;
            animando = false;
            yield break;
        }

        todos.Add(nuevo);
        cantidad++;
        Vector3 escalaFinal = nodoPrefab.transform.localScale;
        nuevo.objeto.transform.localScale = Vector3.zero;

        float fInicial = pivote.localScale.x;
        float fObjetivo = CalcularDisposicion();

        // Cada nodo parte de donde está; el nuevo cae desde arriba de su lugar
        Vector3[] inicios = new Vector3[todos.Count];
        for (int i = 0; i < todos.Count; i++)
            inicios[i] = todos[i] == nuevo ? nuevo.posicion + new Vector3(0f, alturaCaida, 0f)
                                           : todos[i].objeto.transform.localPosition;

        float tiempo = 0f;
        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacion);
            float suave = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < todos.Count; i++)
                todos[i].objeto.transform.localPosition = Vector3.Lerp(inicios[i], todos[i].posicion, suave);
            nuevo.objeto.transform.localScale = escalaFinal * suave;
            pivote.localScale = Vector3.one * Mathf.Lerp(fInicial, fObjetivo, suave);
            ActualizarRamas();
            yield return null;
        }
        for (int i = 0; i < todos.Count; i++)
            todos[i].objeto.transform.localPosition = todos[i].posicion;
        nuevo.objeto.transform.localScale = escalaFinal;
        pivote.localScale = Vector3.one * fObjetivo;
        ActualizarRamas();

        animando = false;
    }

    // Crea la esfera, el número y la rama de un nodo. Devuelve false (y avisa en rojo) si algo falla.
    private bool CrearVisual(Nodo nuevo)
    {
        try
        {
            nuevo.objeto = Instantiate(nodoPrefab, pivote);
            nuevo.objeto.transform.localRotation = Quaternion.identity;
            nuevo.texto = nuevo.objeto.GetComponentInChildren<TextMeshPro>();
            if (nuevo.texto != null) nuevo.texto.text = nuevo.valor.ToString();
            nuevo.render = BuscarRenderDeLaEsfera(nuevo.objeto);
            if (nuevo.render != null) nuevo.colorOriginal = nuevo.render.material.color;
            nuevo.objeto.transform.localPosition = nuevo.padre != null && nuevo.padre.objeto != null
                ? nuevo.padre.objeto.transform.localPosition : Vector3.zero;
            if (nuevo.padre != null) nuevo.rama = Instantiate(ramaPrefab, pivote);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("ArbolController: falló al crear el nodo. " + e.GetType().Name + ": " + e.Message);
            if (nuevo.objeto != null) Destroy(nuevo.objeto);
            if (nuevo.rama != null) Destroy(nuevo.rama);
            nuevo.objeto = null;
            nuevo.rama = null;
            return false;
        }
    }

    // ------------------------------------------------------------------ ELIMINAR

    /// <summary>Elimina un valor del árbol (hoja, un hijo o dos hijos) y reacomoda el dibujo.</summary>
    public void Eliminar(string valorTexto)
    {
        if (Ocupado)
        {
            Debug.Log("ArbolController: espera a que termine la animación actual.");
            return;
        }
        if (!int.TryParse(valorTexto, out int valor))
        {
            Debug.Log("ArbolController: solo se aceptan números enteros (recibido: \"" + valorTexto + "\").");
            Avisar("Solo se aceptan números enteros");
            return;
        }
        Nodo objetivo = raiz;
        while (objetivo != null && objetivo.valor != valor)
            objetivo = valor < objetivo.valor ? objetivo.izquierdo : objetivo.derecho;
        if (objetivo == null)
        {
            Debug.Log("ArbolController: el valor " + valor + " no está en el árbol.");
            Avisar("El valor " + valor + " no está en el árbol");
            return;
        }
        CancelarRecorrido();
        StartCoroutine(EliminarCoroutine(objetivo));
    }

    /// <summary>Elimina un valor cualquiera del árbol (útil para la demostración con un botón).</summary>
    public void EliminarAleatorio()
    {
        if (raiz == null)
        {
            Debug.Log("ArbolController: el árbol está vacío, no hay nada que eliminar.");
            Avisar("El árbol está vacío");
            return;
        }
        List<Nodo> enArbol = Recolectar(raiz, 0);
        Eliminar(enArbol[Random.Range(0, enArbol.Count)].valor.ToString());
    }

    private IEnumerator EliminarCoroutine(Nodo objetivo)
    {
        animando = true;
        animFin = Time.time + duracionAnimacion + 4f;

        // 1) Se marca el nodo que se va a eliminar
        if (objetivo.render != null) objetivo.render.material.color = colorEliminar;
        yield return new WaitForSeconds(0.5f);

        // 2) Se reestructura el árbol según el caso del ABB
        Nodo padreViejo = objetivo.padre;
        Nodo reemplazo;
        if (objetivo.izquierdo == null) reemplazo = objetivo.derecho;
        else if (objetivo.derecho == null) reemplazo = objetivo.izquierdo;
        else
        {
            // Dos hijos: el sucesor (el menor del subárbol derecho) ocupa su lugar
            Nodo sucesor = objetivo.derecho;
            while (sucesor.izquierdo != null) sucesor = sucesor.izquierdo;
            if (sucesor.padre != objetivo)
            {
                sucesor.padre.izquierdo = sucesor.derecho;
                if (sucesor.derecho != null) sucesor.derecho.padre = sucesor.padre;
                sucesor.derecho = objetivo.derecho;
                objetivo.derecho.padre = sucesor;
            }
            sucesor.izquierdo = objetivo.izquierdo;
            objetivo.izquierdo.padre = sucesor;
            reemplazo = sucesor;
        }
        if (reemplazo != null) reemplazo.padre = padreViejo;
        if (padreViejo == null) raiz = reemplazo;
        else if (padreViejo.izquierdo == objetivo) padreViejo.izquierdo = reemplazo;
        else padreViejo.derecho = reemplazo;
        cantidad--;

        // Las ramas deben coincidir con la nueva estructura (la raíz no tiene rama)
        foreach (var n in Recolectar(raiz, 0))
        {
            if (n.padre == null && n.rama != null) { Destroy(n.rama); n.rama = null; }
            else if (n.padre != null && n.rama == null) n.rama = Instantiate(ramaPrefab, pivote);
        }

        // 3) Los demás nodos se reacomodan mientras el eliminado se encoge
        float fInicial = pivote.localScale.x;
        float fObjetivo = CalcularDisposicion();
        Vector3 escalaInicial = objetivo.objeto.transform.localScale;
        Vector3[] inicios = new Vector3[todos.Count];
        for (int i = 0; i < todos.Count; i++) inicios[i] = todos[i].objeto.transform.localPosition;

        float tiempo = 0f;
        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAnimacion);
            float suave = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < todos.Count; i++)
            {
                if (todos[i] == objetivo) continue;
                todos[i].objeto.transform.localPosition = Vector3.Lerp(inicios[i], todos[i].posicion, suave);
            }
            objetivo.objeto.transform.localScale = escalaInicial * (1f - t);
            pivote.localScale = Vector3.one * Mathf.Lerp(fInicial, fObjetivo, suave);
            ActualizarRamas();
            if (objetivo.rama != null) objetivo.rama.transform.localScale *= (1f - t);
            yield return null;
        }

        for (int i = 0; i < todos.Count; i++)
        {
            if (todos[i] != objetivo) todos[i].objeto.transform.localPosition = todos[i].posicion;
        }
        pivote.localScale = Vector3.one * fObjetivo;
        todos.Remove(objetivo);
        if (objetivo.objeto != null) Destroy(objetivo.objeto);
        if (objetivo.rama != null) Destroy(objetivo.rama);
        ActualizarRamas();

        animando = false;
    }

    // ------------------------------------------------------------------ DISPOSICIÓN

    // Calcula el lugar de cada nodo (en orden de izquierda a derecha, un nivel por profundidad) y
    // devuelve el factor de tamaño con el que todo el árbol cabe sobre el marcador.
    private float CalcularDisposicion()
    {
        contadorIndice = 0;
        alturaArbol = 0;
        AsignarIndices(raiz, 0);

        if (contadorIndice == 0) return 1f;
        AsignarPosiciones(raiz);

        float ancho = (contadorIndice - 1) * separacionHoja + diametroNodo;
        float alto = (alturaArbol - 1) * separacionNiveles + diametroNodo;
        float f = Mathf.Min(1f, Mathf.Min(anchoTotal / ancho, altoMaximo / alto));
        return Mathf.Clamp(f, escalaMinima, 1f);
    }

    private void AsignarIndices(Nodo n, int profundidad)
    {
        if (n == null) return;
        AsignarIndices(n.izquierdo, profundidad + 1);
        n.indice = contadorIndice++;
        n.profundidad = profundidad;
        if (profundidad + 1 > alturaArbol) alturaArbol = profundidad + 1;
        AsignarIndices(n.derecho, profundidad + 1);
    }

    private void AsignarPosiciones(Nodo n)
    {
        if (n == null) return;
        float x = (n.indice - (contadorIndice - 1) * 0.5f) * separacionHoja;
        float y = -n.profundidad * separacionNiveles;
        n.posicion = new Vector3(x, y, 0f);
        AsignarPosiciones(n.izquierdo);
        AsignarPosiciones(n.derecho);
    }

    // Une cada nodo con su padre, usando sus posiciones actuales
    private void ActualizarRamas()
    {
        foreach (var n in todos)
        {
            if (n.rama == null || n.padre == null || n.objeto == null || n.padre.objeto == null) continue;
            PonerRama(n.rama, n.padre.objeto.transform.localPosition, n.objeto.transform.localPosition);
        }
    }

    // ------------------------------------------------------------------ OTROS

    /// <summary>Elimina todos los nodos de golpe (útil para reiniciar la demostración).</summary>
    public void LimpiarArbol()
    {
        if (Ocupado) return;
        CancelarRecorrido();
        foreach (var n in todos)
        {
            if (n.objeto != null) Destroy(n.objeto);
            if (n.rama != null) Destroy(n.rama);
        }
        todos.Clear();
        raiz = null;
        cantidad = 0;
        if (pivote != null) pivote.localScale = Vector3.one;
        if (textoRecorrido != null) textoRecorrido.text = "";
    }

    private void Avisar(string mensaje)
    {
        if (textoRecorrido != null) textoRecorrido.text = mensaje;
    }

    public void RecorridoInorden() { IniciarRecorrido("Inorden", Recolectar(raiz, 0)); }
    public void RecorridoPreorden() { IniciarRecorrido("Preorden", Recolectar(raiz, 1)); }
    public void RecorridoPostorden() { IniciarRecorrido("Postorden", Recolectar(raiz, 2)); }

    /// <summary>Inserta automáticamente los valores de demostración (IDemoEstructura).</summary>
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
            Insertar(valor);
            yield return new WaitForSeconds(pausaDemo);
        }
        demoRutina = null;
    }

    // Los números de cada nodo miran siempre hacia la cámara
    private void LateUpdate()
    {
        Camera camara = Camera.main;
        if (camara == null) return;
        float escala = pivote != null ? pivote.localScale.x : 1f;
        foreach (var n in todos)
        {
            if (n.texto == null || n.objeto == null) continue;
            // El número se coloca delante de la esfera (hacia la cámara) y mira hacia ella
            n.texto.transform.position = n.objeto.transform.position - camara.transform.forward * (separacionTexto * escala);
            n.texto.transform.rotation = camara.transform.rotation;
        }
    }

    // Estira y orienta la rama (un cubo de escala 1) para que vaya del punto a al punto b
    private void PonerRama(GameObject rama, Vector3 a, Vector3 b)
    {
        Vector3 direccion = b - a;
        float largo = direccion.magnitude;
        if (largo < 0.0001f)
        {
            rama.transform.localScale = Vector3.zero;
            return;
        }
        rama.transform.localPosition = (a + b) * 0.5f;
        rama.transform.localRotation = Quaternion.LookRotation(direccion);
        rama.transform.localScale = new Vector3(grosorRama, grosorRama, largo);
    }

    // El Renderer de la esfera (no el del texto, que también es un Renderer)
    private Renderer BuscarRenderDeLaEsfera(GameObject nodo)
    {
        foreach (var r in nodo.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.GetComponent<TextMeshPro>() == null) return r;
        }
        return null;
    }

    // tipo: 0 = inorden, 1 = preorden, 2 = postorden
    private List<Nodo> Recolectar(Nodo nodo, int tipo)
    {
        List<Nodo> resultado = new List<Nodo>();
        RecolectarRec(nodo, tipo, resultado);
        return resultado;
    }

    private void RecolectarRec(Nodo nodo, int tipo, List<Nodo> salida)
    {
        if (nodo == null) return;
        if (tipo == 1) salida.Add(nodo);
        RecolectarRec(nodo.izquierdo, tipo, salida);
        if (tipo == 0) salida.Add(nodo);
        RecolectarRec(nodo.derecho, tipo, salida);
        if (tipo == 2) salida.Add(nodo);
    }

    private void IniciarRecorrido(string nombre, List<Nodo> orden)
    {
        if (Ocupado)
        {
            Debug.Log("ArbolController: espera a que termine la animación actual.");
            return;
        }
        if (orden.Count == 0)
        {
            Debug.Log("ArbolController: el árbol está vacío, no hay nada que recorrer.");
            Avisar("El árbol está vacío");
            return;
        }
        // Si ya había un recorrido iluminando nodos, se interrumpe y se muestra el nuevo
        CancelarRecorrido();
        recorridoRutina = StartCoroutine(RecorridoCoroutine(nombre, orden));
    }

    private IEnumerator RecorridoCoroutine(string nombre, List<Nodo> orden)
    {
        string avance = nombre + ": ";
        if (textoRecorrido != null) textoRecorrido.text = avance;

        for (int i = 0; i < orden.Count; i++)
        {
            Nodo n = orden[i];
            if (n.render != null) n.render.material.color = colorResaltado;
            avance += (i > 0 ? " → " : "") + n.valor;
            if (textoRecorrido != null) textoRecorrido.text = avance;
            yield return new WaitForSeconds(pausaRecorrido);
        }

        yield return new WaitForSeconds(pausaRecorrido);
        foreach (var n in orden)
        {
            if (n.render != null) n.render.material.color = n.colorOriginal;
        }
        recorridoRutina = null;
    }
}