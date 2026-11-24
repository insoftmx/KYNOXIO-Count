# KYNOXIO Count — diseño del conteo automático

## Decisión de producto

- Modo predeterminado de nuevas sesiones: **Automático**.
- Alternativa siempre disponible: **Manual**.
- El usuario puede cambiar de Automático a Manual antes o después del análisis.
- El resultado detectado por el modelo y el resultado final confirmado por la persona se almacenan por separado.
- Si el modelo no está instalado, falla o su confianza es insuficiente, la app debe indicar claramente que no hay resultado automático y ofrecer captura manual. Nunca debe inventar ni inferir una cantidad.

## Alcance del primer modelo

Un modelo inicial debe contemplar dos familias de escenas:

1. **Piezas individuales agrupadas:** rondanas, tuercas, tapabujes y componentes industriales similares. La salida requerida es una detección por instancia, con clase, caja delimitadora y confianza. La cantidad es el número de instancias válidas después de la supresión de duplicados.
2. **Material apilado:** polines, tablas, hojas de cartón y paneles. La salida debe identificar clase y regiones/bordes visibles de cada unidad o capa. En escenas donde las caras/bordes estén ocultos, el sistema debe marcar el resultado como no concluyente y pedir una toma adicional o conteo manual. No extrapolar unidades ocultas como si fueran observadas.

La primera versión de IA procesa una fotografía confirmada. El conteo continuo al abrir la cámara es una fase posterior que requiere seguimiento temporal de objetos, estabilización y deduplicación entre fotogramas.

## Arquitectura propuesta

- Interfaz de dominio: `IImageCountAnalyzer`.
- Entrada: imagen normalizada, grupo de material y configuración de inferencia.
- Salida: cantidad detectada, clase(s), confianza agregada, detecciones individuales/regiones, versión del modelo y estado (Completed, LowConfidence, UnsupportedScene, ModelUnavailable, Failed).
- Persistencia: mantener `DetectedCount` y `Confidence` como resultado de IA; mantener `ManualCount` como corrección/resultado humano; guardar método y estado sin sobrescribir la evidencia original.
- Inferencia local Android: ONNX Runtime Mobile/ONNX Runtime para .NET MAUI, sin enviar fotografías a la nube por defecto.
- OpenCV puede apoyar normalización, recorte, corrección de perspectiva y análisis geométrico de bordes, pero no reemplaza el modelo entrenado.
- Modelo y runtime deben versionarse independientemente de la app, con manifiesto de versión, clases, tamaño de entrada, normalización y checksum.

## Licencias y dependencias

La preferencia es utilizar componentes con licencias compatibles con distribución comercial propietaria. ONNX Runtime está publicado bajo MIT y soporta escenarios móviles Android/iOS; OpenCV 4.5.0+ usa Apache 2.0. Debe revisarse la licencia de cada modelo, pesos, dataset y dependencia por separado. No adoptar Ultralytics YOLO para una distribución propietaria sin resolver previamente su licencia AGPL-3.0 o contar con un acuerdo comercial aplicable.

Referencias:
- ONNX Runtime Mobile: https://onnxruntime.ai/docs/get-started/with-mobile.html
- ONNX Runtime mobile deployment: https://onnxruntime.ai/docs/tutorials/mobile/
- ONNX Runtime license: https://github.com/microsoft/onnxruntime
- OpenCV license: https://opencv.org/license/
- Ultralytics license: https://www.ultralytics.com/license

## Datos de entrenamiento y validación

Se necesita un conjunto propio de imágenes industriales representativas. Cada imagen debe incluir:
- grupo de escena (individuales/apilados);
- clase/material;
- anotaciones por pieza o borde/capa visible;
- condiciones (iluminación, distancia, ángulo, oclusión, fondo);
- cantidad real verificada físicamente;
- separación por lote/escena entre entrenamiento, validación y prueba para evitar fuga de imágenes casi idénticas.

No basta con imágenes genéricas de internet: el modelo debe evaluarse con las piezas, tamaños, superficies, empaques, iluminación y cámaras que se usarán en operación.

## Primera fase de implementación — base de muestras

La base de datos ya incorpora el contrato separado para muestras de entrenamiento:
- Modelo de dominio `TrainingSample`.
- Repositorio `ITrainingSampleRepository`.
- Tabla SQLite independiente `TrainingSamples`, sin mezclar registros con `CountSessions`.
- Grupos controlados: `IndividualParts` y `StackedMaterials`.
- Clase/material, cantidad verificada físicamente, condiciones de captura JSON, anotaciones JSON, estado y notas.
- Estados `Draft`, `Ready` y `Excluded`. Una muestra `Ready` requiere cantidad física verificada.
- El repositorio se registra en inyección de dependencias y comparte el archivo SQLite local, pero opera sobre su propia tabla.

**Límite de esta fase:** la interfaz para capturar/seleccionar, etiquetar y revisar muestras aún no está implementada. Tampoco se ha agregado la exportación del dataset al ZIP de respaldo. Hasta completar esas partes, las muestras no deben considerarse un dataset portable ni listo para entrenamiento. El formato JSON de anotaciones está reservado para cajas/regiones visibles; guardar solo una cantidad global no basta para entrenar un detector por instancia.

Flujo de usuario que debe implementarse:
1. Abrir el módulo Banco de imágenes de entrenamiento.
2. Tomar una foto o seleccionar una imagen.
3. Elegir Individuales agrupadas o Material apilado.
4. Registrar clase/material y cantidad comprobada físicamente.
5. Registrar condiciones de captura y observaciones; permitir mantener la muestra como borrador.
6. En una fase de anotación, marcar cada pieza individual o los bordes/capas visibles. Solo después de validar los requisitos la muestra podrá pasar a Ready.
7. Revisar, excluir o corregir muestras sin alterar el historial operativo.
8. Exportar el conjunto en un paquete versionado con imágenes, manifiesto y anotaciones; probar importación y restauración antes de usarlo para entrenamiento.

## Criterios de aceptación

Medir por separado:
- error absoluto de cantidad y porcentaje de conteos exactos;
- precisión/recall de detección por clase;
- tasa de escenas rechazadas correctamente por baja confianza;
- latencia en Samsung Galaxy A52 y otros dispositivos objetivo;
- memoria, temperatura y consumo en sesiones prolongadas.

No publicar un porcentaje de precisión hasta medirlo con un conjunto de prueba independiente y cantidades físicas verificadas. Definir umbrales por clase y familia de material. Para escenas ambiguas, priorizar abstenerse y solicitar intervención manual antes que entregar un conteo incorrecto.

## Experiencia de usuario prevista

1. La pantalla de captura muestra Automático como modo seleccionado y Manual como alternativa visible.
2. En foto, el análisis se ejecuta al obtener la imagen; se muestra estado Analizando.
3. Resultado: cantidad detectada, confianza, clase y marcas visuales sobre la imagen.
4. El usuario confirma o corrige; la cantidad final humana no borra el resultado del modelo.
5. Si no existe modelo compatible o la escena no es soportada, la app informa la causa y ofrece Manual.
6. Video en vivo se habilita solo después de integrar tracking y validar que una pieza no se cuente repetidamente entre fotogramas.

## Puerta de salida

No declarar el conteo automático listo para producción hasta disponer de modelos entrenados con datos propios, evaluación independiente, pruebas de rendimiento en Android y validación en campo para ambas familias.
