# Tabla 5: Especificación de Hiperparámetros y Restricciones del Asistente Virtual

Para garantizar la estabilidad y reproducibilidad de la variable independiente (asistente virtual con IA) a lo largo de la intervención cuasiexperimental, se establecieron y congelaron los siguientes parámetros técnicos de inferencia y comunicación:

## 1. Tabla de Parámetros Calibrados
| Parámetro | Valor Calibrado | Justificación Metodológica y Técnica |
|:---|:---:|:---|
| **Modelo Fundacional** | `gemini-3.5-flash-lite` | Modelo de baja latencia optimizado para dispositivos móviles y diálogos educativos interactivos en tiempo real. |
| **Temperatura** | `0.3` | Nivel bajo de estocasticidad para asegurar respuestas deterministas, rigurosas conceptualmente y sin alucinaciones técnicas. |
| **Longitud Máxima de Consulta** | `200` caracteres | Filtro en el cliente móvil (`InputField_Pregunta`) para evitar saturación de la UI y prevenir ataques de inyección de contexto extenso. |
| **Tokens Máximos de Salida (`maxOutputTokens`)** | `300` tokens | Restricción configurada para forzar respuestas concisas (≤ 120 palabras) legibles en el `ScrollView` sin sobrecargar cognitivamente al alumno. |
| **Memoria Conversacional Corta** | Últimas 3 interacciones | Inyección de historial breve para mantener coherencia en preguntas encadenadas sin disparar el costo de procesamiento por consulta. |
| **Timeout de Conexión HTTP** | `15.0` segundos | Límite de espera de red en `UnityWebRequest`. Si la red no responde en este lapso, aborta la petición y muestra mensaje de desconexión. |
| **Estructura del System Prompt** | Versión 1.0 Definitiva | Directiva pedagógica basada en el método socrático con delimitación estricta de alcance (Pilas, Colas y Árboles Binarios). |