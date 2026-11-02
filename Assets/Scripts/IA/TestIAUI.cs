using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IA;

namespace IA
{
    /// <summary> 
    /// Controlador de UI para la escena de pruebas Test_IA.unity. 
    /// Registra la latencia automáticamente en la Consola de Unity sin alterar el texto visual de la respuesta.
    /// </summary>
    public class TestIAUI: MonoBehaviour
    {
        [Header("Referencias del cliente IA")]
        [SerializeField] private AsistenteIAClient clienteIA;

        [Header("Componentes de la Interfaz (UI)")]
        [SerializeField] private TMP_InputField inputPregunta;
        [SerializeField] private TMP_Dropdown dropdownEstructura;
        [SerializeField] private Button botonEnviar;
        [SerializeField] private TextMeshProUGUI textRespuesta;

        [Header("Opciones de UX")]
        [Tooltip("Si se activa, borra el campo de texto después de enviar una consulta exitosa.")]
        [SerializeField] private bool limpiarInputAlEnviar = false;

        private void Start() {
            if(botonEnviar != null)
            {
                botonEnviar.onClick.AddListener(OnEnviarConsultaClicked);
            }

            if(textRespuesta != null)
            {
                textRespuesta.text = "Escribe una pregunta y presiona 'Enviar' para probar la API y registrar métricas.";
            }
        }

        public void OnEnviarConsultaClicked()
        {
            if(clienteIA == null)
            {
                Debug.LogError("[TestIAUI] No se ha asignado la referencia de AsistenteIAClient.");
                return;
            }

            string pregunta = inputPregunta != null ? inputPregunta.text : string.Empty;
            string estructura = dropdownEstructura != null ? dropdownEstructura.options[dropdownEstructura.value].text : "pila";

            if (string.IsNullOrWhiteSpace(pregunta))
            {
                if (textRespuesta != null)
                {
                    textRespuesta.text = "<color=red>Por favor, ingresa una pregunta antes de enviar.</color>";
                }
            }

            if(textRespuesta != null)
            {
                textRespuesta.text = "<color=yellow>Procesando consulta con Gemini 3.5 Flash-Lite...</color>";
            }

            if(botonEnviar != null)
            {
                botonEnviar.interactable = false;
            }

            // Iniciar cronómetro de alta precisión
            System.Diagnostics.Stopwatch cronometro = System.Diagnostics.Stopwatch.StartNew();

            clienteIA.Consultar(
                pregunta,
                estructura,
                onRespuesta: (respuesta) =>
                {
                    // Detener cronómetro y registrar tiempo de respuesta
                    cronometro.Stop();
                    float latenciaSegundos = (float)cronometro.Elapsed.TotalSeconds;

                    // Mostrar ÚNICAMENTE la respuesta del asistente en la interfaz visual
                    if (textRespuesta != null)
                    {
                        textRespuesta.text = respuesta;
                    }

                    // Registrar el tiempo y datos métricos de la tesis exclusivamente en la Consola
                    Debug.Log($"[METRICA TESIS] Latencia: {latenciaSegundos:F2}s | Estructura: {estructura} | Pregunta: \"{pregunta}\"");

                    if (botonEnviar != null)
                    {
                        botonEnviar.interactable = true;
                    }

                    if (limpiarInputAlEnviar && inputPregunta != null)
                    {
                        inputPregunta.text = string.Empty;
                    }
                },
                onError: (mensajeError) =>
                {
                    cronometro.Stop();
                    float latenciaSegundos = (float)cronometro.Elapsed.TotalSeconds;
                    
                    if (textRespuesta != null)
                    {
                        textRespuesta.text = $"<color=red>{mensajeError}</color>";
                    }

                    Debug.LogWarning($"[METRICA TESIS ERROR] Mensaje: \"{mensajeError}\" | Tiempo: {latenciaSegundos:F2}s");

                    if (botonEnviar != null)
                    {
                        botonEnviar.interactable = true;
                    }
                }
            );
        }
    }
}