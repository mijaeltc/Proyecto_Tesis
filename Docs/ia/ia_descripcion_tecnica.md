# Integración Técnica del Asistente Virtual basado en Inteligencia Artificial

## 1. Selección del Proveedor y Modelo de IA 
Para la implementación del asistente virtual conversacional se seleccionó el servicio de la **API de Google Gemini** (gestionado a través de Google AI Studio). La elección de la arquitectura de LLM (*Large Language Model*) en la nube sobre modelos locales responde a las restricciones de hardware del dispositivo móvil de pruebas (Xiaomi Redmi Note 13 4G) y a la necesidad de mantener el rendimiento continuo del renderizado 3D en Realidad Aumentada sin sobrecargar la GPU/CPU del teléfono. 

Dentro de la familia de modelos de Gemini, se eligió **`gemini-3.5-flash-lite`**. Esta elección se justifica técnicamente bajo tres criterios esenciales para el diseño cuasiexperimental: 
1. **Minimización de latencia:** Ofrece tiempos de respuesta ultrabajos, indispensables para interactuar en tiempo real mientras el estudiante mantiene la cámara enfocando el marcador 3D. 2. **Alta disponibilidad y tolerancia a concurrencia:** Durante las evaluaciones experimentales, 15 estudiantes del Grupo Experimental realizan consultas de manera simultánea. Las pruebas preliminares de conectividad demostraron que la versión *Flash-Lite* reduce drásticamente la tasa de errores de saturación de servidor (HTTP 503) frente a versiones de mayor tamaño. 
3. **Control de costos y cuotas:** Permite operar con fluidez dentro del nivel de uso académico de Google AI Studio, manteniendo un consumo eficiente de tokens.

## 2. Arquitectura y Flujo de Comunicación (Unity ↔ API REST) 
La comunicación entre la aplicación móvil desarrollada en Unity 6 (6000.5.10f1) y la API de Gemini se realiza mediante una arquitectura cliente-servidor directa sobre protocolo **HTTPS POST**, utilizando el módulo asíncrono `UnityWebRequest` de C#. 

El componente principal encargado de esta integración es el script `AsistenteIAClient.cs`, ubicado en `Assets/Scripts/IA/`. El flujo de procesamiento de cada consulta sigue la siguiente secuencia: 
1. **Captura y filtrado local:** El estudiante ingresa una consulta desde la interfaz gráfica de chat. El cliente en C# valida que el texto no esté vacío y limita automáticamente la longitud máxima a **200 caracteres**, previniendo un consumo innecesario de tokens.
2. **Inyección de contexto de Realidad Aumentada:** El cliente consulta el estado del motor Vuforia Engine. Identifica el marcador que se encuentra en estado *TRACKED* y asigna el valor correspondiente a la variable `ESTRUCTURA_ACTIVA` (`PILA`, `COLA`, `ARBOL` o `NINGUNA`). 
3. **Construcción del payload JSON:** Mediante las clases auxiliares de transferencia de datos (`GeminiDTOs.cs`), se estructura la petición POST incluyendo las instrucciones del sistema (*System Prompt*), la variable `ESTRUCTURA_ACTIVA`, el historial reciente de conversación y la pregunta del usuario. 
4. **Envío y control de timeout:** La solicitud HTTP POST se transmite hacia el endpoint `v1beta/models/gemini-3.5-flash-lite:generateContent`, autenticada mediante el encabezado `x-goog-api-key`. Se establece un *timeout* estricto de **15 segundos**. 
5. **Recepción y deserialización:** Al recibir la respuesta (código HTTP 200 OK), `JsonUtility` deserializa el objeto JSON, extrae el texto del candidato (`candidates[0].content.parts[0].text`), registra las métricas de consumo de tokens (`usageMetadata`) en la consola de depuración y entrega la respuesta a la interfaz de usuario.

┌──────────────────────────────┐ HTTPS POST (JSON) ┌──────────────────────────────┐ │ App Android en Unity │ ────────────────────────────> │ API de Google Gemini │ │ - Vuforia (Tracking 3D) │ │ (gemini-3.5-flash-lite) │ │ - Componente Chat UI │ <──────────────────────────── │ │ │ - Script AsistenteIAClient │ Respuesta JSON (Text) └──────────────────────────────┘ └──────────────────────────────┘

## 3. Gestión del Historial y Contexto Conversacional 
Para garantizar la continuidad pedagógica sin saturar la ventana de contexto ni incrementar la latencia, el cliente mantiene un **historial corto de hasta 3 interacciones (6 mensajes en total: 3 del estudiante y 3 del modelo)**. 
Para preservar la coherencia y evitar interferencias conceptuales, **el historial de chat se reinicia automáticamente** cada vez que el estudiante cambia de marcador visualizado (por ejemplo, al dejar de enfocar el marcador de Pila y comenzar a enfocar el de Cola). 

## 4. Reglas del System Prompt e Integridad Académica
El comportamiento del asistente está regulado por el archivo de directivas `system_prompt\_v1.md`. Este prompt configura al modelo como un tutor universitario especializado, sujeto a las siguientes reglas operativas: 
* **Alcance temático restringido:** Únicamente responde sobre **Pilas (LIFO)**, **Colas (FIFO)** y **Árboles Binarios**. Cualquier consulta fuera de este dominio (otras estructuras, otros cursos o temas ajenos) es rechazada cordialmente, reconduciendo al estudiante al temario. 
* **Extensión máxima:** Respuestas breves (idealmente ≤ 120 palabras), adaptadas a la pantalla del dispositivo móvil. 
* **Parámetros de generación:** Se fija una temperatura de **`0.3`** para garantizar respuestas deterministas y técnicamente precisas, y un límite de salida de **`maxOutputTokens: 300`**. 
* **Integridad de la evaluación:** El asistente está expresamente instruido para **no resolver ejercicios de evaluación ni dar respuestas directas a exámenes**. Su función es guiar mediante pistas conceptuales y explicaciones cualitativas. 
* **Resistencia a inyección de prompts:** El prompt incluye instrucciones explícitas para ignorar intentos del usuario de alterar su rol o eludir las restricciones pedagógicas. 

## 5. Seguridad de Credenciales y Repositorio
Para proteger la API Key y evitar su exposición en el repositorio remoto público/privado de GitHub: 
1. Las credenciales se almacenan en el archivo local `Assets/Resources/api_config.json`, el cual está estrictamente registrado dentro del archivo `.gitignore`. 
2. Se mantiene versionada únicamente la plantilla pública `Assets/Resources/api_config.example.json` con valores vacíos. 
3. El archivo `api_config.json` se carga dinámicamente en tiempo de ejecución mediante `Resources.Load<TextAsset>()`.

## 6. Limitaciones Identificadas del Prototipo 
Se reconocen formalmente las siguientes limitaciones en el módulo de IA del sistema: 
* **Dependencia de conectividad a internet:** El asistente conversacional requiere una conexión activa a red (Wi-Fi o datos móviles). En caso de pérdida de señal, el cliente muestra un mensaje informativo amigable indicando la indisponibilidad del asistente, manteniendo plenamente funcional la visualización y manipulación 3D en Realidad Aumentada (la cual opera de forma local). 
* **Seguridad del paquete APK:** Al tratarse de un prototipo de investigación sin servidor intermedio (*proxy*), la API Key queda empaquetada dentro del binario APK. Esta condición se asume como una limitación aceptable para el ámbito controlado del experimento. 
* **Naturaleza probabilística del LLM:** A pesar del bajo valor de temperatura (`0.3`) y el rigor del *System Prompt*, existe un margen residual implícito de imprecisión en la generación de lenguaje natural, el cual es monitoreado mediante la batería de pruebas de verificación.
