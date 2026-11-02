using System; 
using System.Collections; 
using System.Collections.Generic; 
using System.Text; 
using UnityEngine; 
using UnityEngine.Networking; 
using IA.Contratos;

namespace IA
{
    /// <summary> 
    /// Cliente principal para la comunicación asíncrona entre Unity y la API de Gemini. 
    /// Encargado del control de solicitudes, manejo de errores, historial y tiempo de espera. 
    /// </summary>
    public class AsistenteIAClient : MonoBehaviour
    {
        [Header("Archivos de Configuración (Assets/Resources/)")]
        [SerializeField] private string nombreConfiguracion = "api_config";
        [SerializeField] private string nombreSystemPrompt = "system_prompt_v1"; 
        private ApiConfig config;
        private string systemPromptBase = string.Empty; 
        private bool ocupado = false; 
        private string ultimaEstructuraActiva = string.Empty;

        // Historial corto: almacena hasta las últimas 3 interacciones (6 mensajes) 
        private readonly List<Content> historialMensajes = new List<Content>();

        private void Awake()
        {
            CargarConfiguracion();
            CargarSystemPrompt();
        }

        /// <summary>
        /// Lee el archivo JSON local Assets/Resources/api_config.json
        /// </summary>
        private void CargarConfiguracion() 
        { TextAsset configFile = Resources.Load<TextAsset>(nombreConfiguracion);
        if (configFile != null)
        {
            config = JsonUtility.FromJson<ApiConfig>(configFile.text);
            Debug.Log($"[AsistenteIAClient] Configuración cargada con éxito. Modelo: {config.model}");
        } 
        else
            {
                Debug.LogError($"[AsistenteIAClient] No se encontró el archivo '{nombreConfiguracion}.json' en Assets/Resources/.");
            }
        }

        /// <summary> 
        /// Lee el archivo System Prompt desde Assets/Resources/system_prompt.txt o system_prompt.md 
        /// </summary> 
        private void CargarSystemPrompt()
        {
            TextAsset promptFile = Resources.Load<TextAsset>(nombreSystemPrompt);
            if (promptFile != null) 
            { 
                systemPromptBase = promptFile.text; 
                Debug.Log("[AsistenteIAClient] System Prompt cargado correctamente desde Resources."); 
            }
            else
            {
                // Prompt base de respaldo si aún no existe el asset en Resources 
                systemPromptBase = "Eres un tutor virtual educativo en español, experto en Estructuras de Datos (Pilas, Colas y Árboles Binarios). Responde de forma breve (máximo 120 palabras), clara y didáctica."; 
                Debug.LogWarning("[AsistenteIAClient] No se encontró system_prompt en Resources. Usando prompt de respaldo."); 
            }
        }

        /// <summary> 
        /// Método público principal para realizar consultas desde la UI de la app. 
        /// Cumple con la firma exigida en el contrato de desarrollo. 
        /// </summary>
        public void Consultar( 
            string pregunta, 
            string estructuraActiva, 
            Action<string> onRespuesta,
            Action<string> onError)
        {
            // 1. Prevenir consultas múltiples en simultáneo 
            if (ocupado) 
            { 
                onError?.Invoke("Espera la respuesta anterior."); 
                return; 
            }

            // 2. Validar consulta vacía 
            if (string.IsNullOrWhiteSpace(pregunta)) 
            { 
                onError?.Invoke("Por favor, escribe una pregunta antes de enviar."); 
                return; 
            }

            // 3. Limitar la longitud máxima (200 caracteres para ahorrar tokens) 
            string preguntaProcesada = pregunta.Trim(); 
            if (preguntaProcesada.Length > 200) 
            { 
                preguntaProcesada = preguntaProcesada.Substring(0, 200); 
            }

            // 4. Reiniciar historial al cambiar de marcador/estructura 
            string estructuraNormalizada = string.IsNullOrEmpty(estructuraActiva) ? "ninguna" : estructuraActiva.ToLower().Trim(); 
            if (estructuraNormalizada != ultimaEstructuraActiva) 
            { historialMensajes.Clear(); 
            ultimaEstructuraActiva = estructuraNormalizada; 
            Debug.Log($"[AsistenteIAClient] Cambio de estructura a '{estructuraNormalizada}'. Historial reiniciado."); 
            }

            // 5. Iniciar la corrutina asíncrona 
            StartCoroutine(EnviarConsultaCoroutine(preguntaProcesada, estructuraNormalizada, onRespuesta, onError));
        }

        private IEnumerator EnviarConsultaCoroutine( 
            string pregunta, 
            string estructuraActiva, 
            Action<string> onRespuesta,
            Action<string> onError)
        {
            ocupado=true;

            if (config == null || string.IsNullOrEmpty(config.apiKey)) 
            { 
                Debug.LogError("[AsistenteIAClient] La clave de la API no está cargada o falta el archivo api_config.json."); 
                onError?.Invoke("Error de configuración local: No se encontró la clave de acceso."); 
                ocupado = false; 
                yield break; 
            }

            // A. Combinar el System Prompt con la estructura activa del marcador 3D 
            string systemPromptCompleto = $"{systemPromptBase}\\n\\nESTRUCTURA_ACTIVA: {estructuraActiva.ToUpper()}"; 
            var systemInstructionObj = new SystemInstruction 
            { 
                parts = new Part[] { new Part { text = systemPromptCompleto } } 
            };

            // B. Construir el arreglo de contenidos (Historial acumulado + Pregunta actual)
            List<Content> contentsList=new List<Content>(historialMensajes);

            var userContent=new Content 
            { 
                role = "user", 
                parts = new Part[] { new Part { text = pregunta } } 
            };
            contentsList.Add(userContent);

            // C. Armar el objeto DTO de la petición
            var peticionObj = new GeminiRequest
            {
                systemInstruction = systemInstructionObj,
                contents = contentsList.ToArray(),
                generationConfig = new GenerationConfig
                { temperature = config.temperature > 0 ? config.temperature : 0.3f, 
                maxOutputTokens = config.maxOutputTokens > 0 ? config.maxOutputTokens : 300 
                }
            };

            string jsonBody = JsonUtility.ToJson(peticionObj);

            // D. Configurar el Endpoint objetivo
            string modeloElegido = string.IsNullOrEmpty(config.model) ? "gemini-3.5-flash-lite" : config.model;
            string urlEndpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modeloElegido}:generateContent";

            // E. Crear y configurar la solicitud HTTP UnityWebRequest
            byte[] rawData = Encoding.UTF8.GetBytes(jsonBody);
            using (UnityWebRequest request = new UnityWebRequest(urlEndpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(rawData);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", config.apiKey);

                // Configurar Timeout estricto de 15 segundos
                request.timeout = config.timeoutSeg > 0 ? config.timeoutSeg : 15;

                Debug.Log($"[AsistenteIAClient] Enviando consulta sobre '{estructuraActiva}' usando {modeloElegido}...");

                yield return request.SendWebRequest();

                // F. Manejo robusto de respuestas y errores
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    string errorMsg = request.error != null ? request.error.ToLower() : "";
                    if (errorMsg.Contains("timeout") || errorMsg.Contains("timed out"))
                    {
                        Debug.LogError("[AsistenteIAClient] Timeout excedido (&gt; 15s).");
                        onError?.Invoke("El asistente tardó demasiado en responder. Intenta de nuevo.");
                    }
                    else
                    {
                        Debug.LogError($"[AsistenteIAClient] Sin red: {request.error}");
                        onError?.Invoke("Sin conexión: el asistente no está disponible, pero puedes seguir viendo la estructura en Realidad Aumentada.");
                    }
                }
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    long statusCode = request.responseCode;
                    Debug.LogError($"[AsistenteIAClient] Error HTTP {statusCode}: {request.downloadHandler.text}");

                    if (statusCode == 401 || statusCode == 403)
                    {
                        onError?.Invoke("Error de autenticación. Notifica al administrador.");
                    }
                    else if (statusCode == 429)
                    {
                        onError?.Invoke("El servidor recibe muchas consultas. Espera unos segundos e intenta nuevamente.");
                    }
                    else if (statusCode >= 500)
                    {
                        onError?.Invoke("El servicio de IA está temporalmente saturado. Intenta en unos instantes.");
                    }
                    else
                    {
                        onError?.Invoke("No se pudo obtener respuesta del asistente. Intenta de nuevo.");
                    }
                }
                else if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonRespuesta = request.downloadHandler.text;
                    GeminiResponse respuestaObj = JsonUtility.FromJson<GeminiResponse>(jsonRespuesta);
                    
                    // Inicialización previa de variables de salida para evitar el error CS0165
                    string errorParseo = "No se pudo interpretar el JSON recibido.";
                    string textoRespuesta = string.Empty;

                    if (respuestaObj != null && respuestaObj.IntentarObtenerTexto(out textoRespuesta, out errorParseo))
                    {
                        // Registrar métricas de consumo en la consola para la tesis
                        if (respuestaObj.usageMetadata != null)
                        {
                            Debug.Log($"[AsistenteIAClient] Tokens Usados -> Entrada: {respuestaObj.usageMetadata.promptTokenCount}, " +
                                $"Salida: {respuestaObj.usageMetadata.candidatesTokenCount}, " +
                                $"Total: {respuestaObj.usageMetadata.totalTokenCount}");
                        }

                        // Guardar en el historial local (User + Model)
                        historialMensajes.Add(userContent);
                        historialMensajes.Add(new Content
                        {
                            role = "model",
                            parts = new Part[] { new Part { text = textoRespuesta } }
                        });

                        // Mantener únicamente las últimas 3 interacciones (6 mensajes)
                        while (historialMensajes.Count > 6)
                        {
                            historialMensajes.RemoveAt(0);
                        }

                        onRespuesta?.Invoke(textoRespuesta);
                    }
                    else
                    {
                        Debug.LogWarning($"[AsistenteIAClient] Respuesta no válida: {errorParseo}");
                        onError?.Invoke("El asistente no pudo generar una respuesta adecuada. Intenta reformular tu pregunta.");
                    }
                }
            }
            ocupado = false;
        }

        /// <summary>
        /// Limpia manualmente el historial de chat si la interfaz de usuario lo requiere.
        /// </summary>
        public void LimpiarHistorial()
        {
            historialMensajes.Clear();
            Debug.Log("[AsistenteIAClient] Historial de chat reiniciado manualmente.");
        }
    }
    
}