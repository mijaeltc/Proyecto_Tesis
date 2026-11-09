using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Vuforia;

/// <summary>
/// Conecta los botones de la interfaz con la estructura cuyo marcador se está viendo en ese
/// momento (pila, cola o árbol), y cambia solo los textos de los botones según la estructura:
///
///   Pila:  [Push]      [Pop]
///   Cola:  [Enqueue]   [Dequeue]
///   Árbol: [Insertar]  [Eliminar]   + botón [Recorrido] (inorden → preorden → postorden)
///
/// CÓMO USARLO:
/// 1. Crea un GameObject vacío en la raíz de la escena llamado "BotonesEstructura" y agrégale
///    este script.
/// 2. Arrastra a sus campos: los tres controladores (PilaContainer, ColaContainer,
///    ArbolContainer) y los tres Image Targets (qr_pila, qr_cola, qr_arbol).
/// 3. BtnPush → On Click: arrastra el objeto "BotonesEstructura" (de la Hierarchy) y elige
///    BotonesEstructura → Agregar. BtnPop → Quitar.
/// 4. (Opcional, para que los botones cambien de nombre) arrastra el texto hijo de cada botón
///    ("Text (TMP)") a "Texto Btn Agregar" y "Texto Btn Quitar".
/// 5. (Opcional, para los recorridos del árbol) duplica BtnPop, llámalo "BtnRecorrido", arrastra
///    el botón a "Btn Recorrido" y su texto a "Texto Btn Recorrido"; en On Click elige
///    BotonesEstructura → Recorrido. El botón solo se ve cuando el marcador del árbol está a la vista.
///    MEJOR AÚN: crea un Dropdown - TextMeshPro (UI → Dropdown - TextMeshPro) y arrástralo a
///    "Dropdown Recorrido": el script le pone solas las opciones (Inorden, Preorden, Postorden),
///    lo muestra solo con el árbol y ejecuta el recorrido elegido sin que tengas que cablear nada.
/// 6. (Opcional) crea un InputField - TextMeshPro (UI) y arrástralo a "Campo Valor": si hay un
///    número escrito, los botones lo usan; si está vacío, usan un número al azar.
/// </summary>
public class BotonesEstructura : MonoBehaviour
{
    [Header("Estructuras")]
    public PilaController pila;
    public ColaController cola;
    public ArbolController arbol;

    [Header("Marcadores (Image Targets)")]
    public ObserverBehaviour marcadorPila;
    public ObserverBehaviour marcadorCola;
    public ObserverBehaviour marcadorArbol;

    [Header("Interfaz (opcional)")]
    [Tooltip("Texto del botón Agregar (cambia a Push / Enqueue / Insertar)")]
    public TMP_Text textoBtnAgregar;
    [Tooltip("Texto del botón Quitar (cambia a Pop / Dequeue / Eliminar)")]
    public TMP_Text textoBtnQuitar;
    [Tooltip("Menú desplegable de recorridos (solo se muestra con el árbol); el script le pone las opciones")]
    public TMP_Dropdown dropdownRecorrido;
    [Tooltip("Botón de recorridos que va cambiando de uno en uno (alternativa al menú desplegable)")]
    public GameObject btnRecorrido;
    public TMP_Text textoBtnRecorrido;
    [Tooltip("Campo donde se escribe el valor; si está vacío se usa un número al azar")]
    public TMP_InputField campoValor;

    [Header("Valores al azar")]
    public int valorMinimo = 1;
    public int valorMaximo = 99;

    private readonly string[] nombresRecorrido = { "Inorden", "Preorden", "Postorden" };
    private int siguienteRecorrido = 0;

    private void Start()
    {
        if (dropdownRecorrido == null) return;
        dropdownRecorrido.ClearOptions();
        dropdownRecorrido.AddOptions(new List<string> { "Recorridos", "Inorden", "Preorden", "Postorden" });
        dropdownRecorrido.SetValueWithoutNotify(0);
        dropdownRecorrido.RefreshShownValue();
        dropdownRecorrido.onValueChanged.AddListener(RecorridoElegido);
    }

    // La primera opción ("Recorridos") es solo el título; al elegir otra se ejecuta y el menú
    // vuelve al título, así se puede elegir de nuevo el mismo recorrido las veces que quieras.
    private void RecorridoElegido(int opcion)
    {
        if (opcion <= 0) return;
        if (EstructuraActiva() == Activa.Arbol)
        {
            switch (opcion)
            {
                case 1: arbol.RecorridoInorden(); break;
                case 2: arbol.RecorridoPreorden(); break;
                default: arbol.RecorridoPostorden(); break;
            }
        }
        dropdownRecorrido.SetValueWithoutNotify(0);
        dropdownRecorrido.RefreshShownValue();
    }

    private enum Activa { Ninguna, Pila, Cola, Arbol }

    private static bool Visible(ObserverBehaviour o)
    {
        return o != null && o.TargetStatus.Status == Status.TRACKED;
    }

    private Activa EstructuraActiva()
    {
        if (Visible(marcadorPila) && pila != null) return Activa.Pila;
        if (Visible(marcadorCola) && cola != null) return Activa.Cola;
        if (Visible(marcadorArbol) && arbol != null) return Activa.Arbol;
        return Activa.Ninguna;
    }

    private void Update()
    {
        Activa a = EstructuraActiva();
        if (textoBtnAgregar != null)
            textoBtnAgregar.text = a == Activa.Pila ? "Push" : a == Activa.Cola ? "Enqueue" : a == Activa.Arbol ? "Insertar" : "Agregar";
        if (textoBtnQuitar != null)
            textoBtnQuitar.text = a == Activa.Pila ? "Pop" : a == Activa.Cola ? "Dequeue" : a == Activa.Arbol ? "Eliminar" : "Quitar";
        // El texto del recorrido (Inorden: 20 → 30…) solo se muestra con el árbol
        if (arbol != null && arbol.textoRecorrido != null)
            arbol.textoRecorrido.gameObject.SetActive(a == Activa.Arbol);
        if (btnRecorrido != null) btnRecorrido.SetActive(a == Activa.Arbol);
        if (dropdownRecorrido != null) dropdownRecorrido.gameObject.SetActive(a == Activa.Arbol);
        if (textoBtnRecorrido != null) textoBtnRecorrido.text = nombresRecorrido[siguienteRecorrido];
    }

    // Lo escrito en el campo (sin espacios) o null si está vacío
    private string ValorEscrito()
    {
        if (campoValor == null) return null;
        string t = campoValor.text == null ? "" : campoValor.text.Trim();
        return t.Length == 0 ? null : t;
    }

    private void LimpiarCampo()
    {
        if (campoValor != null) campoValor.text = "";
    }

    private string ValorAzar()
    {
        return Random.Range(valorMinimo, valorMaximo + 1).ToString();
    }

    // Un valor al azar que todavía no esté en el árbol
    private string ValorAzarNuevoParaArbol()
    {
        List<int> existentes = arbol.Valores();
        for (int i = 0; i < 200; i++)
        {
            int v = Random.Range(valorMinimo, valorMaximo + 1);
            if (!existentes.Contains(v)) return v.ToString();
        }
        return ValorAzar();
    }

    /// <summary>Botón principal: push / enqueue / insertar según el marcador visible.</summary>
    public void Agregar()
    {
        switch (EstructuraActiva())
        {
            case Activa.Pila: pila.Push(ValorEscrito() ?? ValorAzar()); break;
            case Activa.Cola: cola.Enqueue(ValorEscrito() ?? ValorAzar()); break;
            case Activa.Arbol: arbol.Insertar(ValorEscrito() ?? ValorAzarNuevoParaArbol()); break;
            default:
                Debug.Log("BotonesEstructura: apunta la cámara a un marcador antes de usar los botones.");
                return;
        }
        LimpiarCampo();
    }

    /// <summary>Botón secundario: pop / dequeue / eliminar según el marcador visible.</summary>
    public void Quitar()
    {
        switch (EstructuraActiva())
        {
            case Activa.Pila: pila.Pop(); break;
            case Activa.Cola: cola.Dequeue(); break;
            case Activa.Arbol:
                string v = ValorEscrito();
                if (v != null) arbol.Eliminar(v); else arbol.EliminarAleatorio();
                break;
            default:
                Debug.Log("BotonesEstructura: apunta la cámara a un marcador antes de usar los botones.");
                return;
        }
        LimpiarCampo();
    }

    /// <summary>Borra todos los datos de la estructura que se está viendo (lo usa BotonQuitarMantener).</summary>
    public void LimpiarTodo()
    {
        Activa a = EstructuraActiva();
        if (a == Activa.Ninguna)
        {
            Debug.Log("BotonesEstructura: apunta la cámara a un marcador antes de borrar todo.");
            return;
        }
        StartCoroutine(LimpiarCuandoLibre(a));
        LimpiarCampo();
    }

    private IEnumerator LimpiarCuandoLibre(Activa a)
    {
        // Primero se detiene la demostración automática para que no vuelva a agregar datos
        if (a == Activa.Pila) pila.DetenerDemo();
        else if (a == Activa.Cola) cola.DetenerDemo();
        else arbol.DetenerDemo();

        // Se espera (máximo 3 s) a que termine la animación que esté en curso
        float limite = Time.time + 3f;
        while (Time.time < limite &&
               ((a == Activa.Pila && pila.Ocupado) || (a == Activa.Cola && cola.Ocupado) || (a == Activa.Arbol && arbol.Ocupado)))
            yield return null;

        if (a == Activa.Pila) pila.LimpiarPila();
        else if (a == Activa.Cola) cola.LimpiarCola();
        else arbol.LimpiarArbol();
    }

    /// <summary>Botón de recorridos del árbol: cada pulsación muestra el siguiente recorrido.</summary>
    public void Recorrido()
    {
        if (EstructuraActiva() != Activa.Arbol) return;
        switch (siguienteRecorrido)
        {
            case 0: arbol.RecorridoInorden(); break;
            case 1: arbol.RecorridoPreorden(); break;
            default: arbol.RecorridoPostorden(); break;
        }
        siguienteRecorrido = (siguienteRecorrido + 1) % nombresRecorrido.Length;
    }
}