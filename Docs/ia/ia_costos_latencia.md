# Reporte Técnico: Análisis de Latencia, Rendimiento y Consumo de Tokens

## 1. Análisis de Latencia de Respuesta
Las mediciones temporales registradas mediante `System.Diagnostics.Stopwatch` en el dispositivo móvil Xiaomi Redmi Note 13 4G evidencian una respuesta ágil y adaptada a dinámicas de clase presenciales:

* **Latencia Promedio:** **1.76 segundos** (1,762 ms).
* **Latencia Mínima:** **1.03 segundos** (P09, respuesta de rechazo directo).
* **Latencia Máxima:** **2.25 segundos** (P01, primera consulta de apertura con carga de contexto).
* **Cumplimiento Operativo:** El 100% de las consultas se resolvió muy por debajo del límite de timeout (15 s), permitiendo al estudiante recibir retroalimentación conceptual inmediata mientras manipula las estructuras en Realidad Aumentada.

## 2. Consumo de Tokens por Consulta
* **Tokens de Entrada Promedio:** **961 tokens** (mínimo: 913 tokens; máximo: 1082 tokens). La oscilación refleja la inclusión dinámica del historial conversacional inmediato.
* **Tokens de Salida Promedio:** **116 tokens** (mínimo: 73 tokens; máximo: 146 tokens). Se cumple el objetivo de mantener la respuesta por debajo de las 120 palabras requeridas para la visualización en pantalla vertical (1080×1920).
* **Consumo Total Promedio:** **1,077 tokens** por interacción completa.

## 3. Comportamiento en Red y Tolerancia a Fallos

1. **Modo Fuera de Línea:** Al interrumpirse los datos móviles o Wi-Fi, la aplicación no colapsa ni expone errores no controlados. El sistema de captura de Vuforia y el renderizado 3D continúan operando localmente, mientras que la UI despliega un mensaje amigable: *"Sin conexión: el asistente no está disponible, pero puedes seguir viendo la estructura en Realidad Aumentada."*
2. **Estabilidad Térmica y de Cuadros por Segundo (FPS):** Las pruebas continuas de 15 minutos en el dispositivo móvil no registraron fugas de memoria, caídas perceptibles de framerate ni sobrecalentamiento.
