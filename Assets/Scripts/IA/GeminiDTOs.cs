using System; 
using UnityEngine;

namespace IA.Contratos
{
    // ========================================================
    // 1. CONFIGURACIÓN LOCAL (Carga de Assets/Resources/api_config.json)
    // ========================================================
    [Serializable]
    public class ApiConfig {
        public string apiKey;
        public string model;
        public float temperature;
        public int maxOutputTokens;
        public int timeoutSeg;
    }

    // ========================================================
    // 2. ESTRUCTURA DE PETICIÓN (Enviar JSON a la API de Gemini)
    // ========================================================
    [Serializable]
    public class GeminiRequest {
        public SystemInstruction systemInstruction; 
        public Content[] contents; 
        public GenerationConfig generationConfig; 
        }

        [Serializable]
        public class SystemInstruction {
            public Part[] parts;
        } 
            
        [Serializable]
        public class Content {
            public string role; // "user" o "model"
            public Part[] parts;
        }
                
        [Serializable]
        public class Part {
            public string text;
        }
                    
        [Serializable]
        public class GenerationConfig {
            public float temperature; 
            public int maxOutputTokens; 
        }

    // ======================================================== 
    // 3. ESTRUCTURA DE RESPUESTA (Parsear JSON devuelto por Gemini) 
    // ========================================================
    [Serializable]
    public class GeminiResponse {
        public Candidate[] candidates;
        public UsageMetadata usageMetadata;
        public string modelVersion;
        public string responseId;

        /// <summary> 
        /// Método de extracción segura para evitar excepciones de referencia nula en Unity. 
        /// </summary>
        public bool IntentarObtenerTexto(out string textoRespuesta, out string mensajeError)
        {
            textoRespuesta = string.Empty;
            mensajeError = string.Empty;
            if (candidates == null || candidates.Length == 0)
            { 
                mensajeError = "La API de Gemini no devolvió ningun candidato de respuesta."; 
                return false;
            }
            var primerCandidato = candidates[0];

            if (primerCandidato.content == null || primerCandidato.content.parts == null || primerCandidato.content.parts.Length == 0)
            { 
                mensajeError = "La respuesta del modelo viene vacía o fue filtrada por politicas de seguridad."; 
                return false;
            } 
            textoRespuesta = primerCandidato.content.parts[0].text;
            return !string.IsNullOrEmpty(textoRespuesta);
        }
    }

    [Serializable]
    public class Candidate {
        public Content content; 
        public string finishReason; // "STOP", "MAX_TOKENS", "SAFETY", etc.
        public int index;
    }

    [Serializable]
    public class UsageMetadata {
        public int promptTokenCount;
        public int candidatesTokenCount;
        public int totalTokenCount;
    }

}