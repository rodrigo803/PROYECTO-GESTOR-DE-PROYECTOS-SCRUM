# 🚀 SCRUM Manager - Aplicación Web para Gestión de Proyectos

Este repositorio contiene el código fuente, la documentación y los prototipos del proyecto "Desarrollo de una Aplicación Web para la Gestión de Proyectos bajo la Metodología SCRUM", desarrollado para el curso de **Administración y Programación de Sitios WEB - TI-162**.

## 📋 Descripción General
El objetivo del sistema es centralizar la gestión operativa de proyectos SCRUM en una aplicación web responsiva. Permite a las organizaciones tecnológicas abandonar el uso de hojas de cálculo dispersas, brindando una plataforma donde se puede organizar el Product Backlog, planificar Sprints, gestionar historias de usuario y tareas, y visualizar el avance mediante tableros Kanban y Dashboards.

## 👥 Equipo de Desarrollo
*   **Persona 1:** Jesus Alejandro De Lima, Product Backlog, Tablero Kanban y Retrospectiva[cite: 1].
*   **Persona 2:** Rodrigo Rene Elias Ramirez - Encargado de Gestión de Usuarios, Gestión de Sprints, Gestión de Tareas y Dashboard[cite: 1].
*   **Persona 3:** Sergio Alejandro Monge Moya - Encargado de Gestión de Proyectos, Sprint Backlog, Impedimentos y Responsividad[cite: 1].

## 👨‍🏫 Profesor / Facilitador
*   **Nombre:** Roberto Soto Morales

## 🛠️ Tecnologías y Arquitectura
El proyecto sigue una arquitectura Cliente/Servidor[cite: 1].

*   **Front-end:** HTML5, CSS3, JavaScript, Framework CSS (ej. Bootstrap 5)[cite: 1].
*   **Back-end:** C#, .Net Core 10.0.
*   **Base de Datos:** MySQL, DBMaria, PHPMyAdmin.
*   **Control de Versiones:** Git & GitHub.
*   **Despliegue:** Plesk.
*   **Gestión del Proyecto:** Microsoft Planner (Control de HUs y Checklists).

## 🚀 Fases del Proyecto y Entregables

### 📌 Proyecto 1: Prototipo No Funcional (Mockups) - 20/10/2026
*   Desarrollo de las interfaces gráficas (HTML/CSS/JS) para la Vista de Cliente y la Vista Administrativa[cite: 1].
*   Diseño responsivo para Escritorio, Tableta y Móvil[cite: 1].
*   Documento PDF de justificación y Bitácoras individuales de trabajo[cite: 1].

### 📌 Proyecto 2: Incremento Funcional - 24/11/2026
*   Implementación de base de datos y backend[cite: 1].
*   Funcionalidad de Autenticación, Usuarios, Proyectos, Product Backlog, Sprints y Tareas[cite: 1].
*   **Requisito crítico:** Despliegue en Plesk e inyección de al menos 50 registros de la parte sustantiva del negocio[cite: 1].

### 📌 Proyecto Final: Sistema Completo - 15/12/2026
*   Implementación del Tablero Kanban interactivo, registro de impedimentos, dashboard de indicadores (Burndown chart) y retrospectivas[cite: 1].
*   Validación final de responsividad y despliegue definitivo[cite: 1].

## ⚙️ Instalación y Ejecución (Entorno de Desarrollo)

### Para el Prototipo (Fase 1)
1. Clona este repositorio en tu máquina local:
   ```bash
   git clone [https://github.com/tu-usuario/scrum-manager.git](https://github.com/tu-usuario/scrum-manager.git)
   ```

### Para el Backend (Fase 2)
La cadena de conexión a MySQL (`ConnectionStrings:ScrumDb`) **no se guarda con contraseña en el repositorio**. `appsettings.Development.json` trae la cadena con la contraseña vacía solo como plantilla; cada integrante guarda su propia contraseña local con **User Secrets** de .NET, que vive fuera del repo y nunca se sube a GitHub.

La primera vez, desde la carpeta `Gestor-proyectos-scrum/`, configura tu contraseña local con:
```bash
dotnet user-secrets set "ConnectionStrings:ScrumDb" "Server=localhost;Port=3306;Database=BDScrumManager;User=root;Password=TU_CONTRASEÑA;"
```

Reemplaza `TU_CONTRASEÑA` por la contraseña de tu MySQL local. Esto sobreescribe (solo en tu máquina) la cadena de `appsettings.Development.json` al correr el proyecto en modo Development.

## 🌐 Publicar en Plesk

La Api (`Gestor-proyectos-scrum.Api`) está montada dentro del propio MVC (`Gestor-proyectos-scrum`), así que en Plesk solo se publica **un** sitio.

1. **Generar el publish (self-contained win-x64)**, desde la carpeta `Gestor-proyectos-scrum/`:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -o C:\publicar\scrum-publicar
   ```
   Self-contained porque el servidor de Plesk no tiene instalado el runtime de .NET 10; así el publish trae su propio `dotnet` embebido (el `.exe` generado ya no depende de nada instalado en el servidor).

2. **Subir el contenido** de `C:\publicar\scrum-publicar` (no la carpeta en sí, su contenido) a `httpdocs/scrum` del dominio en Plesk, vía el administrador de archivos de Plesk o FTP/SFTP.

3. **Crear el directorio virtual** en Plesk: Sitios web y dominios → tu dominio → Hosting Node.js/IIS → "Agregar directorio" (o "Crear aplicación" según la versión del panel) con ruta `/scrumapp` apuntando a `httpdocs/scrum`. Plesk detecta el `web.config` incluido en el publish y configura el `AspNetCoreModuleV2` automáticamente.

4. **`appsettings.Production.json`**: no viaja en el repo ni en el publish (está en `.gitignore`). Se sube **a mano** directamente a `httpdocs/scrum` en el servidor, copiando `appsettings.Production.example.json` como base y completando ahí mismo la contraseña real de MySQL — nunca se escribe esa contraseña en Git ni en el publish local.

5. **Logs**: `web.config` escribe el log de arranque de Kestrel/IIS en `httpdocs/scrum/logs/stdout*.log` (la carpeta `logs/` debe existir y tener permisos de escritura; si no existe, créala vacía al subir). Son el primer lugar donde mirar si el sitio da 502/503 después de publicar.

6. **Actualizar una versión ya publicada**: subir un `.dll` mientras el proceso de IIS lo tiene abierto falla ("file in use"). Antes de sobrescribir:
   - Sube un archivo `app_offline.htm` a `httpdocs/scrum` (cualquier HTML; IIS lo detecta y detiene la app, mostrando ese archivo a los visitantes).
   - Sobrescribe los archivos actualizados del publish.
   - Borra `app_offline.htm` para que IIS vuelva a arrancar la app con el código nuevo.
