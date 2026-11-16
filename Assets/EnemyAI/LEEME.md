# Enemigo de El Retirado

La escena casa01 ya tiene la bestia configurada. Pulsa Play para probarla.

- Patrulla seis puntos conectados en la casa, con animación de caminar.
- Detecta al jugador hasta 14 metros y en un campo visual de 110 grados. Los colliders de las paredes bloquean la visión.
- Corre a 3,6 m/s al perseguir. Si pierde de vista al jugador, recuerda su última posición durante 6 segundos y luego investiga antes de volver a patrullar.
- Ataca a 1,25 metros: 25 de daño cada 1,2 segundos. El jugador tiene 100 de vida. Al morir aparece Reintentar.
- Investiga los pasos (5 m caminando, 12 m corriendo, 1,5 m agachado) y los interruptores (6 m). Los radios son distancias directas, también a través de paredes.
- Usa reposo, caminar y correr. No se añadió una animación de ataque: todavía no hay una específica entre los archivos de la bestia.

## Ajustes en Unity

Selecciona Bestia_Enemigo en Hierarchy para cambiar velocidades, vista, daño y tiempos. Los puntos de la ruta están en Ruta_Patrulla_Bestia: puedes moverlos sobre el suelo transitable.

Si cambias la geometría de la casa, utiliza El Retirado > Configurar enemigo y navegacion para reconstruir la navegación. Es necesario que el suelo y los obstáculos tengan colliders.

Para otro ruido (puertas, objetos que caen, etc.), añade NoiseEmitter al objeto. Asigna su radio y llama a EmitNoise desde el evento de interacción o de animación. También puedes asignar un AudioSource y activar Emit When Audio Starts. Los sonidos ambientales y de interfaz no activan automáticamente a la bestia.

## Copias y archivos

Los archivos nuevos están en Assets/EnemyAI. La escena previa, incluidos los cambios que estaban sin guardar, quedó en Assets/EnemyAI/Backup/casa01_antes_IA.unity. La copia del script de interruptores está en la misma carpeta.

El menú El Retirado > Comprobar comportamiento en Play ejecuta pruebas temporales y deja el resultado en Assets/EnemyAI/validation-report.txt. Los cambios de las pruebas se descartan al salir de Play. Las pruebas mantienen activo el controlador y el collider reales del jugador y usan el daño normal.


Las copias de caminar y correr usadas por la IA se corrigieron para animarse en el sitio. Los FBX de Mixamo originales se conservan. La vida se muestra en pantalla y cada golpe produce un destello rojo; la pantalla de muerte usa un Canvas por encima de la interfaz del juego.