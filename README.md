# Backdrop Studio for Windows

Aplicación local para Windows que organiza tus imágenes y permite aplicarlas al escritorio y a la pantalla de bloqueo.

## Instalación

Descarga el proyecto desde **Code → Download ZIP**, extrae la carpeta y abre **Instalar.cmd**. El instalador compila el código C# con .NET Framework de Windows y crea un acceso en Inicio para tu usuario. Requiere Windows 10 u 11, Windows PowerShell y .NET Framework 4.x.

## Uso

Arrastra archivos PNG, JPG, JPEG o BMP a la biblioteca. Se importan copias locales. Pulsa F2 para cambiar el nombre de la copia seleccionada.

En **Escritorio**, elige una imagen fija para un monitor o un carrusel aleatorio para todos. En **Bloqueo**, elige la imagen seleccionada o toda la biblioteca con un intervalo independiente. El carrusel de bloqueo inicia un proceso local que rota las imágenes mientras tu sesión está iniciada. Aplicar una imagen fija al bloqueo detiene esa rotación y elimina su inicio automático.

La biblioteca empieza vacía. Los archivos se guardan en la carpeta Cuadros junto al ejecutable, dentro de la carpeta de instalación de tu usuario.
