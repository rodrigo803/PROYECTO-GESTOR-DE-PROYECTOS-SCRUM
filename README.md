# ScrumGestor — aporte jesus de lima

**Proyecto 1:** Prototipo no funcional, curso TI-162, Colegio Universitario de Cartago.  
**Propósito:** agregar estas pantallas al repositorio GitHub compartido **sin reemplazar** los archivos de los compañeros.

## Historias de usuario asignadas

| Historia | Descripción | Archivos propios |
|---|---|---|
| HU-01 | Autenticación y cierre de sesión | `acceso.html`, `cerrar-sesion.html` |
| HU-04 | Product Backlog | `product-backlog.html`, `historia-usuario.html`, `backlog-cliente.html`, `historia-cliente.html` |
| HU-08 | Tablero Kanban | `tablero-kanban.html`, `tablero-cliente.html` |
| HU-11 | Retrospectivas | `retrospectiva.html`, `retrospectiva-cliente.html` |

Se incluyen además `panel-admin-e1.html` y `panel-cliente-e1.html`, dos pantallas de orientación para recorrer este aporte sin depender de las páginas del resto del grupo.

## Estructura para subir al repositorio

```text
/  (raíz del repositorio del grupo)
├── acceso.html
├── cerrar-sesion.html
├── panel-admin-e1.html
├── panel-cliente-e1.html
├── product-backlog.html
├── historia-usuario.html
├── backlog-cliente.html
├── historia-cliente.html
├── tablero-kanban.html
├── tablero-cliente.html
├── retrospectiva.html
├── retrospectiva-cliente.html
├── css/
│   └── estudiante1.css
├── js/
│   └── estudiante1.js
└── README_ESTUDIANTE_1.md
```

*Las páginas de los compañeros (`index.html`, `proyectos.html`, `sprints.html`, `usuarios.html`, `impedimentos.html` y similares) no se modifican ni se incluyen en este ZIP.*

## Cómo subirlo a GitHub (desde el navegador)

1. Descomprime `Aporte_Estudiante_1_GitHub.zip`.
2. Entra al repositorio compartido del grupo y asegúrate de estar en la rama acordada.
3. En `Add file > Upload files`, añade los **archivos HTML del nivel raíz** y `README_ESTUDIANTE_1.md`.
4. Abre la carpeta `css` en GitHub y usa `Add file > Upload files` para agregar **solo** `estudiante1.css`.
5. Abre la carpeta `js` en GitHub y sube **solo** `estudiante1.js`.
6. En cada subida crea un commit con un mensaje como `HU-01 y HU-04: pantallas de Estudiante 1`.
7. Comprueba que `acceso.html` abre y que desde ahí puedes entrar a las dos vistas.

> **No reemplaces** `index.html`, `README.md`, ni los CSS/JS existentes del equipo. Estas rutas y nombres se eligieron precisamente para evitar conflictos.

## Integración desde el `index.html` del grupo

Alguien del equipo puede **añadir** estos enlaces en el menú que ya existe en `index.html` o su barra lateral:

```html
<a href="acceso.html">Iniciar sesión (HU-01)</a>
<a href="product-backlog.html">Administrar Product Backlog (HU-04)</a>
<a href="backlog-cliente.html">Consultar Product Backlog (HU-04)</a>
<a href="tablero-kanban.html">Administrar Kanban (HU-08)</a>
<a href="tablero-cliente.html">Ver tablero Kanban (HU-08)</a>
<a href="retrospectiva.html">Administrar retrospectivas (HU-11)</a>
<a href="retrospectiva-cliente.html">Ver retrospectivas (HU-11)</a>
```

Los nombres de enlaces corresponden a los archivos de este aporte; verifica con el equipo qué elementos del menú final corresponden a administrador o a cliente. No se requiere hacer cambios en los archivos de otros módulos para abrir el aporte independiente.

## Cómo probar antes de subir

Abre `acceso.html` con el navegador. Las opciones **Vista del cliente / equipo** y **Vista administrativa** permiten recorrer todas las páginas de este aporte. No requiere instalación, servidor ni base de datos.

## Alcance del Proyecto 1

- Mockups estáticos para cliente y administración.
- Navegación entre páginas de demostración.
- Menú adaptable a teléfono (único JS, sin lógica de negocio).
- **No hay inicio de sesión real, guardado, creación, actualización, borrado ni back-end.** Los datos son ilustrativos.
- El Planner y la bitácora individual se trabajan por separado y se deben completar con evidencia verdadera.
