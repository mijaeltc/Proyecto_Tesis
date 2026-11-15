using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IA;

namespace IA
{
    /// <summary> 
    /// Controlador de UI para el Asistente IA. 
    /// Controla la apertura del chat, la animación de la burbuja flotante y registra métricas.
    /// </summary>
    public class TestIAUI: MonoBehaviour
    {
        [Header("Contenedor Principal")]
        [SerializeField] private GameObject panelChat;

        [Header("Burbuja Flotante (Toggle)")]
        [Tooltip("RectTransform del botón circular Boton_ToggleChat")]
        [SerializeField] private RectTransform rectBotonToggle;
        [Tooltip("Texto hijo 'IA' dentro del botón")]
        [SerializeField] private TextMeshProUGUI textBotonToggle;
        [Tooltip("Pos X cuando el chat está cerrado (pegado al borde derecho)")]
        [SerializeField] private float posXNormal = -15f;
        [Tooltip("Pos X cuando el chat está abierto (oculta más de la mitad fuera de pantalla)")]
        [SerializeField] private float posXChatAbierto = 65f;
        
        [Header("Referencias del cliente IA")]
        [SerializeField] private AsistenteIAClient clienteIA;

        [Header("Componentes de la Interfaz (UI)")]
        [SerializeField] private TMP_InputField inputPregunta;
        [SerializeField] private TMP_Dropdown dropdownEstructura;
        [SerializeField] private Button botonEnviar;
        [SerializeField] private TextMeshProUGUI textRespuesta;

        [Header("Opciones de UX")]
        [SerializeField] private bool limpiarInputAlEnviar = false;

        private void Start() {
            if(botonEnviar != null)
            {
                botonEnviar.onClick.AddListener(OnEnviarConsultaClicked);
            }

            if(textRespuesta != null)
            {
                textRespuesta.text = "Escribe una pregunta y presiona 'Enviar' para consultar al tutor.";
            }

            // Sincronizar la posición inicial de la burbuja según si el panel arranca oculto o no
            if (panelChat != null)
            {
                ActualizarEstadoBurbuja(panelChat.activeSelf);
            }
        }

        /// <summary>
        /// Alterna la visibilidad del chat y desplaza la esfera al borde de la pantalla.
        /// </summary>

        public void AlternarPanelChat()
        {
            if (panelChat != null)
            {
                bool nuevoEstado = !panelChat.activeSelf;
                panelChat.SetActive(nuevoEstado);
                ActualizarEstadoBurbuja(nuevoEstado);
            }
        }

        private void ActualizarEstadoBurbuja(bool chatAbierto)
        {
            if (rectBotonToggle != null)
            {
                Vector2 pos = rectBotonToggle.anchoredPosition;
                pos.x = chatAbierto ? posXChatAbierto : posXNormal;
                rectBotonToggle.anchoredPosition = pos;
            }

            // Mover el texto 'IA' hacia la izquierda del semicírculo para que no quede cortado al ocultar la mitad
            if (textBotonToggle != null)
            {
                RectTransform rectText = textBotonToggle.rectTransform;
                rectText.anchoredPosition = new Vector2(chatAbierto ? -25f : 0f, 0f);
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

            string estructura = (dropdownEstructura != null && dropdownEstructura.gameObject.activeInHierarchy)
                ? dropdownEstructura.options[dropdownEstructura.value].text
                : string.Empty;

            if (string.IsNullOrWhiteSpace(pregunta))
            {
                if (textRespuesta != null)
                {
                    textRespuesta.text = "<color=red>Por favor, ingresa una pregunta antes de enviar.</color>";
                }
            }

            if(textRespuesta != null)
            {
                textRespuesta.text = "<color=yellow>Consultando al tutor virtual...</color>";
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
                    Debug.Log($"[METRICA TESIS] Latencia: {latenciaSegundos:F2}s | Pregunta: \"{pregunta}\"");

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