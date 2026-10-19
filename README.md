# SCRUM Manager — Prototipo Frontend de Gestión Ágil
> **Parte 1 — Proyecto Sitios Web**  
> **Aporte Individual — Persona 2: Rodrigo Rene Elias Ramirez**

---

## 📌 1. Resumen Ejecutivo del Aporte

En el marco del desarrollo del frontend para la plataforma **SCRUM Manager** (un sistema de gestión ágil orientado a roles administrativos como *Scrum Master*, *Product Owner* y *Tech Lead*), a la **Persona 2** le correspondió el diseño, maquetación, modularización e interactividad de los **Requerimientos Funcionales 4 y 5**, junto con la arquitectura visual base y pantallas de integración transversal:

* **Requerimiento 4 — Gestión de Sprints (`sprints.html`):** Panel para supervisar, filtrar y crear ciclos de iteración con métricas de capacidad, fechas de entrega, objetivos y selección de historias del backlog.
* **Requerimiento 5 — Sprint Backlog y Gestión de Tareas (`sprint-backlog.html`):** Desglose detallado del sprint activo mediante acordeones de Historias de Usuario, tablas de tareas técnicas, selectores dinámicos de estado y modal de alta de tareas.
* **Módulos de Soporte e Integración:**
  * **Dashboard Principal (`index.html`):** Punto de acceso unificado con resumen de salud del proyecto y accesos directos a los requerimientos.
  * **Gestión de Usuarios (`usuarios.html`):** Directorio del squad de desarrollo con roles, asignación de horas y disponibilidad para garantizar consistencia en la asignación de tareas.
  * **Arquitectura de Estilos (`css/styles.css`):** Sistema de diseño desacoplado con variables CSS, paleta modo oscuro (*slate/indigo*), componentes reutilizables y soporte responsivo.
  * **Lógica Frontend (`js/app.js`):** Controlador en JavaScript puro (ES6+) para manejo de modales, actualización dinámica del DOM, validación simulada y notificaciones flotantes (*Toasts*).

---

## 🚀 2. Requerimientos Asignados y Funcionalidades Desarrolladas

### 🔹 Requerimiento 4: Gestión de Sprints (`sprints.html`)
Diseñado para que el Scrum Master pueda planificar y administrar los ciclos de trabajo del equipo.
* **Tarjetas de Métricas Superiores (KPIs):** Indicadores visuales de *Sprints Totales (3)*, *Sprint Activo (Sprint 1)*, *Puntos Comprometidos (42 SP)* y *Velocidad Promedio (36 pts)*.
* **Tabla de Sprints Responsiva:**
  * Visualización de ciclos en distintos estados: **Activo** (*Sprint 1: Core Auth & Onboarding*), **Planificado** (*Sprint 2: Billing & Subscriptions*) y **Cerrado** (*Sprint 0: Architecture & DevOps*).
  * Columnas con rango de fechas, objetivos de negocio, Story Points comprometidos, horas estimadas y badges de estado con código de colores.
  * Botón de acceso directo hacia el Sprint Backlog del ciclo activo.
* **Buscador y Filtros de Estado:** Barra de búsqueda en vivo y selector de filtro por estado (*Todos, Activo, Planificado, Cerrado*).
* **Modal Interactivo "Crear Nuevo Sprint" (`#modalCrearSprint`):**
  * Formulario con validaciones para: Nombre del Sprint, Fecha de Inicio, Fecha de Cierre, Capacidad en Story Points, Estado inicial y Objetivo del Sprint.
  * Checklist con historias disponibles extraídas del Product Backlog (ej. *HU-04 Checkout con Stripe, HU-05 Descarga de Facturas PDF, HU-06 Consulta de Balances*).
  * Manejo del evento de guardado con reseteo de formulario, cierre automático del modal y feedback mediante Toast.

---

### 🔹 Requerimiento 5: Sprint Backlog y Tareas (`sprint-backlog.html`)
Diseñado para el seguimiento granular del trabajo diario durante el ciclo de desarrollo en curso (*Sprint 1 - Fintech Mobile App*).
* **Cabecera y Avance General:** Indicador del ciclo activo y barra de progreso porcentual del sprint (**68% completado** de 42 Story Points).
* **Tarjetas Resumen de Estados de Tarea:**
  * 🟡 **Pendientes:** 2 tareas (10 hrs estimadas)
  * 🔵 **En progreso:** 3 tareas (24 hrs estimadas)
  * 🟣 **En revisión / PR:** 1 tarea (16 hrs estimadas)
  * 🟢 **Finalizadas:** 2 tareas (13 hrs estimadas)
* **Acordeón Interactivo de Historias de Usuario (`#accordionSprintBacklog`):**
  * **HU-01:** *Iniciar Sesión con Doble Factor de Autenticación (2FA)* — Prioridad Alta, 8 Story Points.
  * **HU-02:** *Gestión de Perfiles y Recuperación de Contraseña* — Prioridad Media, 5 Story Points.
  * **HU-03:** *Auditoría y Registro de Inicios de Sesión Sospechosos* — Prioridad Media, 5 Story Points.
* **Tablas Anidadas de Tareas Técnicas:**
  * Columnas para: Nombre técnico de la tarea, desarrollador responsable con avatar gráfico, horas estimadas y control de estado.
  * **Dropdown Dinámico de Estado:** Botón interactivo que permite cambiar el estado de la tarea en tiempo real entre *Pendiente, En progreso, En revisión* y *Finalizado*, actualizando instantáneamente el color del badge y disparando un mensaje informativo Toast.
* **Modal "Agregar Nueva Tarea" (`#modalAgregarTarea`):**
  * Se abre desde el botón `+ Agregar Tarea` de cada Historia de Usuario, precargando automáticamente el nombre de la HU asociada.
  * Asignación de desarrolladores técnicos reales del squad:
    * Carlos Ruiz (*Backend Lead*)
    * Ana Gómez (*Frontend Dev*)
    * Mateo Silva (*QA Automation*)
    * Lucía Morales (*UI/UX Designer*)
  * Campos de estimación de horas, descripción técnica y selector de estado inicial.

---

### 🔹 Módulos Complementarios y de Experiencia de Usuario (UX)

#### 1. Dashboard Ejecutivo (`index.html`)
* Banner de bienvenida con resumen del estado del proyecto activo (*Fintech Mobile App*).
* Tarjetas de navegación destacadas con enlaces directos hacia **Gestión de Sprints (Req 4)** y **Sprint Backlog (Req 5)**.
* Paneles de rendimiento general del equipo y accesos rápidos.

#### 2. Gestión de Usuarios y Equipo (`usuarios.html`)
* Listado de los integrantes del equipo con roles asignados, correos de contacto, capacidad de horas semanales (40 hrs/sem), tareas activas asignadas y estado de disponibilidad.
* Asegura consistencia en los nombres y avatares utilizados en el Sprint Backlog.

#### 3. Barra de Navegación (Navbar) y Menú Lateral (Sidebar)
* **Navbar Superior:** Modo oscuro (`#0f172a`), badge con el logotipo del sistema, chip en tiempo real con el estado del sprint (*Día 5 de 14*), menú desplegable de notificaciones y perfil de usuario (*Rodrigo Morales - Scrum Master / Admin*).
* **Sidebar Lateral Fijo:** Menú de navegación con resaltado de la página activa (`.active`), indicador de progreso de la fase activa del proyecto y botón de cierre de sesión con diálogo interactivo.
* **Soporte Móvil:** Botón hamburguesa para colapsar/desplegar el menú en dispositivos de pantalla reducida.

---

## 🛠️ 3. Tecnologías y Herramientas Utilizadas

| Tecnología | Versión / Fuente | Propósito en el Proyecto |
| :--- | :--- | :--- |
| **HTML5** | Estándar W3C | Estructura semántica (`<header>`, `<nav>`, `<aside>`, `<main>`, `<section>`, formularios y tablas). |
| **CSS3 (Vanilla)** | Estándar | Estilos centralizados en `css/styles.css`, variables `:root`, sombras suaves y diseño responsivo. |
| **JavaScript (ES6+)** | Nativo del navegador | Manipulación dinámica del DOM, gestión de estados, eventos de modales y Toasts en `js/app.js`. |
| **Bootstrap** | v5.3.3 (CDN) | Sistema de rejilla flexible (Grid), modales, dropdowns, acordeones y componentes de interfaz. |
| **Bootstrap Icons** | v1.11.3 (CDN) | Iconografía vectorial SVG para enriquecer la interfaz visual y la usabilidad. |
| **Google Fonts** | Inter | Tipografía moderna de alta legibilidad para dashboards administrativos. |

---

## 📂 4. Estructura de Archivos del Aporte

```text
Persona 2/
├── README.md                  <- Documento de entrega y especificación del aporte (este archivo)
├── DOCUMENTACION_PROYECTO.txt <- Documentación técnica extendida del prototipo
├── index.html                 <- Dashboard Principal con accesos a los requerimientos
├── sprints.html               <- REQUERIMIENTO 4: Gestión de Sprints y Modal de Creación
├── sprint-backlog.html        <- REQUERIMIENTO 5: Sprint Backlog, Acordeón de HUs y Tareas
├── usuarios.html              <- Módulo de apoyo: Gestión de Miembros del Squad
├── css/
│   └── styles.css             <- Hojas de estilos desacopladas (variables, sidebar, navbar, badges)
└── js/
    └── app.js                 <- Lógica interactiva desacoplada (cambio de estados, modales, toasts)
```

---

## 💡 5. Decisiones de Diseño y Buenas Prácticas Aplicadas

1. **Separación de Responsabilidades (Clean Code):**
   * El código HTML se encuentra limpio de estilos en línea y de scripts embebidos. Todo el diseño reside en `css/styles.css` y el comportamiento en `js/app.js`.
2. **Contexto de Negocio Realista (Mock Data):**
   * No se utilizaron textos de relleno (*Lorem Ipsum*). Todas las historias, tareas y estimaciones corresponden a un desarrollo de software verosímil (*Fintech Mobile App: 2FA, Stripe, JWT, logs de auditoría*).
3. **Interactividad sin Backend:**
   * Las interacciones (creación de sprint, alta de tareas, cambio de estado de tarea en tiempo real) se ejecutan 100% en el cliente, proporcionando confirmación visual inmediata mediante alertas flotantes (*Toasts* de Bootstrap).
4. **Diseño Responsivo:**
   * Adaptado para pantallas de escritorio, laptops, tablets y móviles gracias al grid de Bootstrap y media queries en el sidebar.

---

## 💻 6. Instrucciones de Ejecución

El proyecto es **100% estático y autónomo**, por lo que **no requiere instalación de Node.js, servidores web, compiladores ni dependencias locales**:

1. Descargar o clonar la carpeta `Persona 2/`.
2. Abrir el archivo `index.html` con doble clic o clic derecho -> *Abrir con* en cualquier navegador web moderno (**Google Chrome**, **Mozilla Firefox**, **Microsoft Edge**, **Safari**).
3. Todas las dependencias visuales (Bootstrap y fuentes de Google) se cargan automáticamente vía CDN con conexión a Internet.
4. Navegar entre las opciones del menú lateral para explorar:
   * **Gestión de Sprints:** `sprints.html`
   * **Sprint Backlog:** `sprint-backlog.html`
   * **Gestión de Usuarios:** `usuarios.html`
   * **Dashboard:** `index.html`

---

## 👤 Información del Autor

* **Estudiante:** Rodrigo Rene Elias Ramirez
* **Rol / Asignación:** Persona 2 — Requerimientos 4 y 5
* **Proyecto:** Prototipo Frontend de Administración SCRUM
* **Curso:** Proyecto Sitios Web — Parte 1
