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
