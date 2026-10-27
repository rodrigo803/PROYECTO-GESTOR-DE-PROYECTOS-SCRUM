/**
 * SCRUM Manager - JavaScript de Interacción y Navegación
 * Funcionalidades transversales para la aplicación multi-página
 */

document.addEventListener('DOMContentLoaded', function () {
  // Inicializar toggle del sidebar en dispositivos móviles
  const sidebarToggle = document.getElementById('sidebarToggle');
  const sidebar = document.getElementById('adminSidebar');

  if (sidebarToggle && sidebar) {
    sidebarToggle.addEventListener('click', function () {
      sidebar.classList.toggle('show');
    });
  }
});

/**
 * Actualiza el estilo y texto del dropdown de estado de una tarea
 * @param {HTMLElement} elemento - Elemento del menú que fue clickeado
 * @param {string} nuevoEstado - 'pendiente', 'progreso', 'revision', 'finalizado'
 */
function cambiarEstadoTarea(elemento, nuevoEstado) {
  const dropdownButton = elemento.closest('.dropdown').querySelector('.status-badge-select');
  const textSpan = dropdownButton.querySelector('.status-text');

  // Limpiar clases de estado anteriores
  dropdownButton.classList.remove('status-pendiente', 'status-progreso', 'status-revision', 'status-finalizado');

  if (nuevoEstado === 'pendiente') {
    dropdownButton.classList.add('status-pendiente');
    textSpan.textContent = 'Pendiente';
    mostrarToast('Tarea cambiada a estado "Pendiente"', 'info');
  } else if (nuevoEstado === 'progreso') {
    dropdownButton.classList.add('status-progreso');
    textSpan.textContent = 'En progreso';
    mostrarToast('Tarea en desarrollo ("En progreso")', 'primary');
  } else if (nuevoEstado === 'revision') {
    dropdownButton.classList.add('status-revision');
    textSpan.textContent = 'En revisión';
    mostrarToast('Tarea enviada a ("En revisión / PR")', 'secondary');
  } else if (nuevoEstado === 'finalizado') {
    dropdownButton.classList.add('status-finalizado');
    textSpan.textContent = 'Finalizado';
    mostrarToast('¡Tarea completada con éxito!', 'success');
  }
}

/**
 * Precarga el modal de agregar tarea con el nombre de la Historia de Usuario activa
 * @param {string} nombreHistoria - Nombre o código de la HU
 */
function prepararModalTarea(nombreHistoria) {
  const inputHU = document.getElementById('tareaHuAsociada');
  const subtitulo = document.getElementById('modalTareaSubtitulo');
  if (inputHU) inputHU.value = nombreHistoria;
  if (subtitulo) subtitulo.textContent = `Asociada a: ${nombreHistoria}`;
}

/**
 * Simulación de guardado para el formulario de Creación de Sprint (Requerimiento 4)
 * @param {Event} event - Evento submit del formulario
 */
function guardarNuevoSprint(event) {
  event.preventDefault();
  const nombreSprint = document.getElementById('sprintNombre').value;

  // Cerrar modal con la instancia de Bootstrap
  const modalElement = document.getElementById('modalCrearSprint');
  if (modalElement) {
    const modalInstance = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
    modalInstance.hide();
  }

  // Resetear campos del formulario
  document.getElementById('formCrearSprint').reset();

  // Mostrar notificación al usuario
  mostrarToast(`Sprint "${nombreSprint}" creado y programado exitosamente.`, 'success');
}

/**
 * Simulación de guardado para el formulario de Agregar Tarea (Requerimiento 5)
 * @param {Event} event - Evento submit del formulario
 */
function guardarNuevaTarea(event) {
  event.preventDefault();
  const nombreTarea = document.getElementById('tareaNombre').value;

  // Cerrar modal con la instancia de Bootstrap
  const modalElement = document.getElementById('modalAgregarTarea');
  if (modalElement) {
    const modalInstance = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
    modalInstance.hide();
  }

  // Resetear campos del formulario
  document.getElementById('formAgregarTarea').reset();

  // Mostrar notificación al usuario
  mostrarToast(`Tarea "${nombreTarea}" registrada en el Sprint Backlog.`, 'success');
}

/**
 * Muestra un mensaje flotante tipo Toast
 * @param {string} mensaje - Texto informativo
 * @param {string} tipo - Tipo de alerta ('success', 'info', 'primary', 'warning')
 */
function mostrarToast(mensaje, tipo = 'success') {
  const toastEl = document.getElementById('actionToast');
  if (!toastEl) return;

  const toastMessage = document.getElementById('toastMessage');
  const toastIcon = document.getElementById('toastIcon');

  if (toastMessage) toastMessage.textContent = mensaje;

  if (toastIcon) {
    if (tipo === 'success') {
      toastIcon.className = 'bi bi-check-circle-fill text-success fs-5';
    } else if (tipo === 'info') {
      toastIcon.className = 'bi bi-info-circle-fill text-info fs-5';
    } else if (tipo === 'primary') {
      toastIcon.className = 'bi bi-arrow-clockwise text-primary fs-5';
    } else {
      toastIcon.className = 'bi bi-bell-fill text-warning fs-5';
    }
  }

  const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
  toast.show();
}

/**
 * Confirmación simulada para el cierre de sesión
 */
function confirmarCierreSesion() {
  if (confirm('¿Estás seguro de que deseas cerrar sesión en SCRUM Manager?')) {
    alert('Sesión finalizada. Redirigiendo a pantalla de login...');
    window.location.href = 'cerrar-sesion.html';
  }
}

/**
 * =========================================================
 * LÓGICA INTERACTIVA DEL GRÁFICO BURNDOWN (HU-10)
 * =========================================================
 */
const burndownSprint1 = {
  nombre: 'Sprint 1 · Core Auth & Onboarding',
  totalPts: 42,
  totalHrs: 160,
  dias: [
    { dia: 1, fecha: '10 Oct', idealPts: 42.0, realPts: 42, idealHrs: 160, realHrs: 160, quemados: 0, hitos: 'Kick-off del Sprint 1, refinamiento de historias y desglose de tareas técnicas.', estado: 'A tiempo' },
    { dia: 2, fecha: '11 Oct', idealPts: 38.8, realPts: 42, idealHrs: 148, realHrs: 160, quemados: 0, hitos: 'Configuración de entornos de desarrollo, branch gitflow y setup de DB.', estado: 'En inicio' },
    { dia: 3, fecha: '12 Oct', idealPts: 35.5, realPts: 38, idealHrs: 135, realHrs: 145, quemados: 4, hitos: 'Diseño de pantallas de login y UI de 2FA completados (Ana G. / Jesús).', estado: 'A tiempo' },
    { dia: 4, fecha: '13 Oct', idealPts: 32.3, realPts: 35, idealHrs: 123, realHrs: 133, quemados: 3, hitos: 'Modelo de base de datos de usuarios, roles y tabla de logs (Rodrigo M.).', estado: 'A tiempo' },
    { dia: 5, fecha: '14 Oct', idealPts: 29.1, realPts: 31, idealHrs: 111, realHrs: 118, quemados: 4, hitos: 'Endpoints base de autenticación JWT y hashing con Bcrypt (Carlos R.).', estado: 'A tiempo' },
    { dia: 6, fecha: '15 Oct', idealPts: 25.8, realPts: 28, idealHrs: 98,  realHrs: 106, quemados: 3, hitos: 'Generación y envío de códigos OTP vía servicio mock SMS/Email (Mateo S.).', estado: 'A tiempo' },
    { dia: 7, fecha: '16 Oct', idealPts: 22.6, realPts: 22, idealHrs: 86,  realHrs: 84,  quemados: 6, hitos: 'HU-02 Gestión de Usuarios y Permisos completada y fusionada a develop.', estado: 'Adelantado' },
    { dia: 8, fecha: '17 Oct', idealPts: 19.4, realPts: 17, idealHrs: 74,  realHrs: 65,  quemados: 5, hitos: 'Verificación de TOTP integrada con frontend y pruebas de integración.', estado: 'Adelantado (+2.4 pts)' },
    { dia: 9, fecha: '18 Oct (Hoy)', idealPts: 16.2, realPts: 13, idealHrs: 62, realHrs: 50, quemados: 4, hitos: 'Endpoint de perfil completado. Validar backup codes en revisión QA.', estado: 'Adelantado (+3.2 pts)', esHoy: true },
    { dia: 10, fecha: '19 Oct', idealPts: 12.9, realPts: 10, idealHrs: 49, realHrs: 38, proyectado: true, hitos: 'Proyección: Pruebas E2E de recuperación de cuenta y reseteo de claves.', estado: 'Proyección' },
    { dia: 11, fecha: '20 Oct', idealPts: 9.7,  realPts: 7,  idealHrs: 37, realHrs: 26, proyectado: true, hitos: 'Proyección: Ajustes de responsividad móvil y tablet (HU-12).', estado: 'Proyección' },
    { dia: 12, fecha: '21 Oct', idealPts: 6.5,  realPts: 4,  idealHrs: 25, realHrs: 15, proyectado: true, hitos: 'Proyección: Auditoría de seguridad y logs de accesos (HU-03).', estado: 'Proyección' },
    { dia: 13, fecha: '22 Oct', idealPts: 3.2,  realPts: 2,  idealHrs: 12, realHrs: 6,  proyectado: true, hitos: 'Proyección: Code freeze, revisión de PRs pendientes y pruebas de carga.', estado: 'Proyección' },
    { dia: 14, fecha: '24 Oct', idealPts: 0.0,  realPts: 0,  idealHrs: 0,  realHrs: 0,  proyectado: true, hitos: 'Proyección: Sprint Review con cliente y Retrospectiva del equipo (HU-11).', estado: 'Meta final' }
  ]
};

let metricaBurndownActiva = 'pts'; // 'pts' | 'hrs'

/**
 * Actualiza el panel interactivo de información al seleccionar o pasar el cursor por un día
 * @param {number} numeroDia - Número del día (1 al 14)
 */
function actualizarDetalleBurndown(numeroDia) {
  const dato = burndownSprint1.dias.find(d => d.dia === numeroDia);
  if (!dato) return;

  const panel = document.getElementById('burndownDetallePanel');
  if (!panel) return;

  const unidad = metricaBurndownActiva === 'pts' ? 'Story Points' : 'Horas Hombre';
  const unidadCorta = metricaBurndownActiva === 'pts' ? 'pts' : 'hrs';
  const valReal = metricaBurndownActiva === 'pts' ? dato.realPts : dato.realHrs;
  const valIdeal = metricaBurndownActiva === 'pts' ? dato.idealPts : dato.idealHrs;
  const valQuemado = metricaBurndownActiva === 'pts' ? dato.quemados : Math.round(dato.quemados * (160 / 42));

  let badgeColor = 'bg-primary-subtle text-primary border border-primary-subtle';
  if (dato.esHoy) badgeColor = 'bg-warning-subtle text-warning-emphasis border border-warning-subtle';
  else if (dato.proyectado) badgeColor = 'bg-info-subtle text-info border border-info-subtle';

  panel.innerHTML = `
    <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-2">
      <div>
        <div class="d-flex align-items-center gap-2 mb-1">
          <span class="badge ${badgeColor} px-2 py-1 fs-7">
            ${dato.esHoy ? '<i class="bi bi-clock-history me-1"></i>' : ''}Día ${dato.dia} · ${dato.fecha}
          </span>
          <span class="fw-bold text-dark small">${dato.proyectado ? 'Proyección de Trabajo' : 'Avance Registrado'}</span>
          <span class="badge bg-light text-secondary border small">${dato.estado}</span>
        </div>
        <p class="small text-muted mb-0">
          <strong>Hitos / Tareas:</strong> ${dato.hitos}
        </p>
      </div>
      <div class="d-flex gap-3 text-nowrap align-self-start align-self-md-center border-start ps-md-3">
        <div>
          <div class="small text-muted" style="font-size: 0.75rem;">Restante Real</div>
          <div class="fw-bold fs-6 ${dato.proyectado ? 'text-info' : 'text-primary'}">${valReal} ${unidadCorta}</div>
        </div>
        <div>
          <div class="small text-muted" style="font-size: 0.75rem;">Línea Ideal</div>
          <div class="fw-bold fs-6 text-secondary">${valIdeal} ${unidadCorta}</div>
        </div>
        ${!dato.proyectado ? `
        <div>
          <div class="small text-muted" style="font-size: 0.75rem;">Quemados</div>
          <div class="fw-bold fs-6 text-success">+${valQuemado} ${unidadCorta}</div>
        </div>` : ''}
      </div>
    </div>
  `;

  // Resaltar fila en la tabla si está visible
  const filas = document.querySelectorAll('.burndown-table-row');
  filas.forEach(f => f.classList.remove('table-primary'));
  const filaActiva = document.getElementById(`burndownRow-${numeroDia}`);
  if (filaActiva) filaActiva.classList.add('table-primary');
}

/**
 * Cambia la métrica del gráfico entre Story Points y Horas Hombre
 * @param {string} tipo - 'pts' o 'hrs'
 */
function cambiarMetricaBurndown(tipo) {
  metricaBurndownActiva = tipo;
  const btnPts = document.getElementById('btnBurndownPts');
  const btnHrs = document.getElementById('btnBurndownHrs');

  if (tipo === 'pts') {
    if (btnPts) { btnPts.classList.add('btn-primary'); btnPts.classList.remove('btn-outline-secondary'); }
    if (btnHrs) { btnHrs.classList.remove('btn-primary'); btnHrs.classList.add('btn-outline-secondary'); }

    // Cambiar etiquetas en el gráfico SVG
    actualizarEjesSVG('pts');
    actualizarMetricasKPI('pts');
    mostrarToast('Vista cambiada a Story Points (42 pts)', 'info');
  } else {
    if (btnPts) { btnPts.classList.remove('btn-primary'); btnPts.classList.add('btn-outline-secondary'); }
    if (btnHrs) { btnHrs.classList.add('btn-primary'); btnHrs.classList.remove('btn-outline-secondary'); }

    // Cambiar etiquetas en el gráfico SVG
    actualizarEjesSVG('hrs');
    actualizarMetricasKPI('hrs');
    mostrarToast('Vista cambiada a Horas Hombre estimadas (160 hrs)', 'info');
  }

  // Refrescar detalle al día de hoy
  actualizarDetalleBurndown(9);
}

function actualizarEjesSVG(tipo) {
  const lblY4 = document.getElementById('burndownLblY4');
  const lblY3 = document.getElementById('burndownLblY3');
  const lblY2 = document.getElementById('burndownLblY2');
  const lblY1 = document.getElementById('burndownLblY1');
  const lblY0 = document.getElementById('burndownLblY0');
  const ejeTitulo = document.getElementById('burndownEjeYTitulo');

  if (tipo === 'pts') {
    if (ejeTitulo) ejeTitulo.textContent = 'STORY POINTS RESTANTES';
    if (lblY4) lblY4.textContent = '40 pts';
    if (lblY3) lblY3.textContent = '30 pts';
    if (lblY2) lblY2.textContent = '20 pts';
    if (lblY1) lblY1.textContent = '10 pts';
    if (lblY0) lblY0.textContent = '0 pts';
  } else {
    if (ejeTitulo) ejeTitulo.textContent = 'HORAS RESTANTES ESTIMADAS';
    if (lblY4) lblY4.textContent = '160 h';
    if (lblY3) lblY3.textContent = '120 h';
    if (lblY2) lblY2.textContent = '80 h';
    if (lblY1) lblY1.textContent = '40 h';
    if (lblY0) lblY0.textContent = '0 h';
  }
}

function actualizarMetricasKPI(tipo) {
  const kpiTotal = document.getElementById('burndownKpiTotal');
  const kpiPendiente = document.getElementById('burndownKpiPendiente');
  const kpiQuemado = document.getElementById('burndownKpiQuemado');
  const kpiVelocidad = document.getElementById('burndownKpiVelocidad');

  if (tipo === 'pts') {
    if (kpiTotal) kpiTotal.textContent = '42 pts';
    if (kpiPendiente) kpiPendiente.textContent = '13 pts';
    if (kpiQuemado) kpiQuemado.textContent = '29 pts';
    if (kpiVelocidad) kpiVelocidad.textContent = '3.2 pts/día';
  } else {
    if (kpiTotal) kpiTotal.textContent = '160 hrs';
    if (kpiPendiente) kpiPendiente.textContent = '50 hrs';
    if (kpiQuemado) kpiQuemado.textContent = '110 hrs';
    if (kpiVelocidad) kpiVelocidad.textContent = '12.2 h/día';
  }
}

/**
 * Permite cambiar el Sprint visualizado en el Burndown
 * @param {string} sprintId
 */
function cambiarSprintBurndown(sprintId) {
  if (sprintId === 'sprint-1') {
    mostrarToast('Cargando datos de Sprint 1 (Activo)...', 'primary');
    actualizarDetalleBurndown(9);
  } else if (sprintId === 'sprint-0') {
    mostrarToast('Sprint 0 · Cerrado: 35 de 35 Story Points completados al 100%.', 'info');
  } else if (sprintId === 'sprint-2') {
    mostrarToast('Sprint 2 · Planificado: 38 Story Points listos para iniciar.', 'warning');
  }
}

/* ========================================================= */
/* FUNCIONES DE GESTIÓN DE USUARIOS Y ROLES (HU-02 ADMIN)     */
/* ========================================================= */

/**
 * Filtra los usuarios en la tabla según el texto de búsqueda, rol y estado.
 */
function filtrarUsuarios() {
  const texto = (document.getElementById('filtroUsuarioTexto')?.value || '').toLowerCase().trim();
  const rol = document.getElementById('filtroUsuarioRol')?.value || 'todos';
  const estado = document.getElementById('filtroUsuarioEstado')?.value || 'todos';

  const filas = document.querySelectorAll('#tablaUsuarios tbody tr');
  let visibles = 0;

  filas.forEach(fila => {
    if (fila.id === 'filaSinResultados') return;

    const nombre = fila.getAttribute('data-nombre')?.toLowerCase() || '';
    const email = fila.getAttribute('data-email')?.toLowerCase() || '';
    const id = fila.getAttribute('data-id')?.toLowerCase() || '';
    const rolFila = fila.getAttribute('data-rol') || '';
    const estadoFila = fila.getAttribute('data-estado') || '';

    const coincideTexto = !texto || nombre.includes(texto) || email.includes(texto) || id.includes(texto);
    const coincideRol = (rol === 'todos') || (rolFila === rol);
    const coincideEstado = (estado === 'todos') || (estadoFila === estado);

    if (coincideTexto && coincideRol && coincideEstado) {
      fila.style.display = '';
      visibles++;
    } else {
      fila.style.display = 'none';
    }
  });

  const contador = document.getElementById('contadorUsuarios');
  if (contador) {
    contador.textContent = `Mostrando ${visibles} usuario${visibles === 1 ? '' : 's'}`;
  }

  const filaSinResultados = document.getElementById('filaSinResultados');
  if (filaSinResultados) {
    filaSinResultados.style.display = visibles === 0 ? '' : 'none';
  }
}

/**
 * Guarda un nuevo usuario o invitación desde el modal
 */
function guardarNuevoUsuario(event) {
  event.preventDefault();
  const nombre = document.getElementById('nuevoUsuarioNombre')?.value;
  const email = document.getElementById('nuevoUsuarioEmail')?.value;
  const rol = document.getElementById('nuevoUsuarioRol')?.value;

  const modalEl = document.getElementById('modalNuevoUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.hide();
  }

  document.getElementById('formNuevoUsuario')?.reset();
  mostrarToast(`Invitación enviada exitosamente a ${nombre} (${email}) con rol de ${rol}.`, 'success');
}

/**
 * Prepara el modal de edición con los datos del usuario seleccionado
 */
function prepararEditarUsuario(id, nombre, email, rol, capacidad, estado) {
  if (document.getElementById('editUsuarioId')) document.getElementById('editUsuarioId').value = id;
  if (document.getElementById('editUsuarioNombre')) document.getElementById('editUsuarioNombre').value = nombre;
  if (document.getElementById('editUsuarioEmail')) document.getElementById('editUsuarioEmail').value = email;
  if (document.getElementById('editUsuarioRol')) document.getElementById('editUsuarioRol').value = rol;
  if (document.getElementById('editUsuarioCapacidad')) document.getElementById('editUsuarioCapacidad').value = capacidad;
  if (document.getElementById('editUsuarioEstado')) document.getElementById('editUsuarioEstado').value = estado;

  const modalEl = document.getElementById('modalEditarUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.show();
  }
}

/**
 * Guarda los cambios editados del usuario
 */
function guardarEdicionUsuario(event) {
  event.preventDefault();
  const nombre = document.getElementById('editUsuarioNombre')?.value || 'Usuario';

  const modalEl = document.getElementById('modalEditarUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.hide();
  }

  mostrarToast(`Los datos y capacidad de ${nombre} han sido actualizados.`, 'info');
}

/**
 * Muestra el modal con el detalle completo y auditoría del usuario
 */
function verDetalleUsuario(id, nombre, email, rol, especialidad, capacidad, asignadas, completadas, estado, iniciales, colorClase) {
  if (document.getElementById('detUsuarioNombre')) document.getElementById('detUsuarioNombre').textContent = nombre;
  if (document.getElementById('detUsuarioEmail')) document.getElementById('detUsuarioEmail').textContent = email;
  if (document.getElementById('detUsuarioId')) document.getElementById('detUsuarioId').textContent = id;
  if (document.getElementById('detUsuarioRol')) document.getElementById('detUsuarioRol').textContent = rol;
  if (document.getElementById('detUsuarioEspecialidad')) document.getElementById('detUsuarioEspecialidad').textContent = especialidad;
  if (document.getElementById('detUsuarioCapacidad')) document.getElementById('detUsuarioCapacidad').textContent = `${capacidad} hrs / semana`;
  if (document.getElementById('detUsuarioAsignadas')) document.getElementById('detUsuarioAsignadas').textContent = asignadas;
  if (document.getElementById('detUsuarioCompletadas')) document.getElementById('detUsuarioCompletadas').textContent = completadas;
  if (document.getElementById('detUsuarioEstado')) document.getElementById('detUsuarioEstado').textContent = estado;

  const avatar = document.getElementById('detUsuarioAvatar');
  if (avatar) {
    avatar.textContent = iniciales;
    avatar.className = `rounded-circle text-white d-flex align-items-center justify-content-center fw-bold fs-4 ${colorClase}`;
    avatar.style.width = '64px';
    avatar.style.height = '64px';
  }

  const modalEl = document.getElementById('modalDetalleUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.show();
  }
}

/**
 * Prepara el modal para confirmar desactivación de un usuario
 */
function prepararDesactivarUsuario(id, nombre) {
  if (document.getElementById('desactivarUsuarioId')) document.getElementById('desactivarUsuarioId').value = id;
  if (document.getElementById('desactivarUsuarioNombre')) document.getElementById('desactivarUsuarioNombre').textContent = nombre;

  const modalEl = document.getElementById('modalDesactivarUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.show();
  }
}

/**
 * Ejecuta la desactivación simulada
 */
function ejecutarDesactivarUsuario() {
  const nombre = document.getElementById('desactivarUsuarioNombre')?.textContent || 'El usuario';
  const modalEl = document.getElementById('modalDesactivarUsuario');
  if (modalEl) {
    const modalInst = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
    modalInst.hide();
  }
  mostrarToast(`${nombre} ha sido dado de baja temporalmente del squad.`, 'warning');
}

/**
 * Exporta el directorio de usuarios
 */
function exportarUsuarios(formato = 'CSV') {
  mostrarToast(`Directorio exportado exitosamente en formato ${formato.toUpperCase()}. Descarga iniciada.`, 'primary');
}
