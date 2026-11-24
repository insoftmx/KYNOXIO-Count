# KYNOXIO Count — Validación preproducción 0.2.0

## Alcance funcional de esta entrega

Esta versión es una primera versión operativa de captura y registro local asistido:
- Captura de fotografía con cámara y selección desde galería.
- Conservación de la fotografía original en el almacenamiento privado de la aplicación.
- Registro de sesiones en SQLite local.
- Historial, miniaturas y detalle de cada sesión.
- Registro manual de descripción, cantidad y observaciones.
- Migración aditiva del esquema SQLite para conservar las sesiones creadas con versiones anteriores.

La detección automática mediante IA y el conteo por video en tiempo real todavía no forman parte de esta versión. La interfaz no debe presentar cantidades automáticas hasta que exista un modelo entrenado, validado y ejecutado en el dispositivo.

## Pruebas de aceptación en Android

### A. Captura y almacenamiento
1. Tomar una fotografía y confirmar el guardado.
2. Elegir una fotografía de galería y confirmar el guardado.
3. Cancelar cámara, galería y vista previa; no debe crearse una sesión.
4. Repetir la captura varias veces; cada sesión debe tener folio único.
5. Confirmar que el archivo original se abre desde el detalle.
6. Cerrar y volver a abrir la app; historial y fotografías deben persistir.
7. Ejecutar en modo avión; captura, registro, historial y detalle deben seguir funcionando.

### B. Registro manual
1. Abrir una sesión pendiente.
2. Capturar descripción del material o componente, cantidad entera y observaciones.
3. Guardar y comprobar estado `CompletedManual`.
4. Volver al historial y verificar descripción y cantidad.
5. Abrir de nuevo la sesión, cambiar cantidad/observaciones y guardar.
6. Probar descripción vacía, cantidad vacía, cantidad negativa y texto superior a los límites; no debe guardarse información inválida.
7. Comprobar que el resultado se identifica claramente como manual, nunca como resultado de IA.

### C. Persistencia y migración
1. Instalar sobre una versión que ya tenga sesiones de prueba.
2. Verificar que la migración agrega los campos manuales sin borrar folios ni rutas de imágenes.
3. Reiniciar el teléfono y volver a abrir el historial.
4. Confirmar que las fotografías faltantes se muestran como evidencia no disponible, sin cerrar la aplicación.

### D. Robustez
1. Denegar el permiso de cámara y comprobar el mensaje de recuperación.
2. Cancelar el selector del sistema.
3. Probar una imagen grande y una imagen vertical/horizontal.
4. Probar almacenamiento bajo; debe mostrarse un error comprensible y no registrar una sesión incompleta.
5. Probar uso repetido del botón Guardar; un toque no debe crear sesiones duplicadas.

## Comandos de validación local

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet test .\KYNOXIO.Count.Tests\KYNOXIO.Count.Tests.csproj --configuration Release
```

```powershell
dotnet build .\KYNOXIO.Count.App\KYNOXIO.Count.App.csproj -f net8.0-android --configuration Release
```

Para instalar en un dispositivo Android conectado y autorizado:

```powershell
dotnet build .\KYNOXIO.Count.App\KYNOXIO.Count.App.csproj -f net8.0-android -t:Run
```

## Criterios antes de producción

- Pruebas automatizadas SQLite aprobadas.
- Compilación Android Release sin errores.
- Matriz de pruebas físicas aprobada en al menos un dispositivo real.
- Migración verificada con datos existentes.
- Icono, nombre, versión y firma de distribución revisados.
- Política de privacidad y canal de soporte publicados si se distribuirá públicamente.
- Copia/exportación de datos definida antes de uso operativo prolongado.

## Limitación conocida

Los datos y las fotografías se almacenan localmente en el dispositivo. Si el usuario desinstala la aplicación o Android borra sus datos, el historial puede perderse. La aplicación ya permite exportar y restaurar un archivo ZIP manualmente, pero no sincroniza datos en la nube ni crea respaldos automáticos. La recuperación debe probarse en Android antes de considerar esta versión apta para uso operativo prolongado.

## E. Respaldo y recuperación

1. Crear al menos tres sesiones: una pendiente y dos con cantidades manuales diferentes.
2. Abrir Inicio > Respaldo y configuración > Crear y compartir respaldo.
3. Guardar el ZIP en una ubicación externa al almacenamiento privado de la app (por ejemplo, Drive o una computadora).
4. Comprobar que el archivo ZIP existe y no está vacío.
5. En el teléfono, borrar una sesión de prueba solo si se cuenta con otro respaldo y se acepta perderla; alternativamente, realizar la recuperación en un segundo dispositivo o instalación limpia.
6. Seleccionar el ZIP desde Restaurar respaldo.
7. Confirmar que las sesiones y cantidades reaparecen y que las fotografías disponibles se abren.
8. Restaurar el mismo ZIP una segunda vez: no deben duplicarse sesiones; las sesiones con el mismo ID se actualizan.
9. Crear una sesión nueva después de la restauración y comprobar que se guarda normalmente.
10. Probar un archivo que no sea respaldo ZIP: debe mostrarse un error sin alterar el historial.
11. Probar un ZIP con versión no compatible o entradas inválidas: debe rechazarse sin extraer rutas fuera del almacenamiento de la app.

El respaldo es un archivo portable de sesiones y fotografías, no una copia binaria de SQLite. La restauración agrega sesiones faltantes y actualiza por ID; nunca borra sesiones locales que no estén incluidas en el ZIP.
