/* =============================================================
   MODULOS-RODRIGO.JS
   Validador unificado Bootstrap 5 en español, contadores de
   caracteres, visor de contraseña y reglas de negocio en tiempo
   real para HU-02 (Usuarios), HU-05 (Sprints) y HU-07 (Tareas).
   ============================================================= */

(function () {
    'use strict';

    // 1. Traducir mensajes por defecto de jQuery Validation si está cargado en _Layout
    if (window.jQuery && window.jQuery.validator) {
        window.jQuery.extend(window.jQuery.validator.messages, {
            required: 'Este campo es obligatorio.',
            remote: 'Por favor, corrija este campo.',
            email: 'Ingrese un correo electrónico con formato válido.',
            url: 'Ingrese una URL válida.',
            date: 'Ingrese una fecha válida.',
            dateISO: 'Ingrese una fecha (ISO) válida.',
            number: 'Ingrese un valor numérico válido.',
            digits: 'Ingrese solo dígitos.',
            equalTo: 'Ingrese el mismo valor nuevamente.',
            maxlength: window.jQuery.validator.format('No ingrese más de {0} caracteres.'),
            minlength: window.jQuery.validator.format('Ingrese al menos {0} caracteres.'),
            rangelength: window.jQuery.validator.format('Ingrese un valor entre {0} y {1} caracteres.'),
            range: window.jQuery.validator.format('Ingrese un valor entre {0} y {1}.'),
            max: window.jQuery.validator.format('Ingrese un valor menor o igual a {0}.'),
            min: window.jQuery.validator.format('Ingrese un valor mayor o igual a {0}.'),
            step: window.jQuery.validator.format('Ingrese un múltiplo de {0}.')
        });
    }

    // 2. Utilidades para mostrar / limpiar errores Bootstrap 5 en español
    function obtenerContenedorCampo(campo) {
        return campo.closest('.input-group') || campo;
    }

    function obtenerElementoFeedback(campo) {
        const contenedor = obtenerContenedorCampo(campo);
        const parent = contenedor.parentElement;
        if (!parent) return null;

        let feedback = parent.querySelector('.invalid-feedback.scrum-feedback');
        if (!feedback) {
            feedback = document.createElement('div');
            feedback.className = 'invalid-feedback scrum-feedback';
            contenedor.insertAdjacentElement('afterend', feedback);
        }
        return feedback;
    }

    function marcarInvalido(campo, mensaje) {
        campo.classList.add('is-invalid');
        const grupo = campo.closest('.input-group');
        if (grupo) {
            grupo.classList.add('is-invalid');
        }
        const feedback = obtenerElementoFeedback(campo);
        if (feedback) {
            feedback.textContent = mensaje;
            feedback.classList.add('d-block');
        }
    }

    function limpiarInvalido(campo) {
        campo.classList.remove('is-invalid');
        const grupo = campo.closest('.input-group');
        if (grupo) {
            grupo.classList.remove('is-invalid');
        }
        const contenedor = obtenerContenedorCampo(campo);
        const parent = contenedor.parentElement;
        if (parent) {
            const feedback = parent.querySelector('.invalid-feedback.scrum-feedback');
            if (feedback) {
                feedback.textContent = '';
                feedback.classList.remove('d-block');
            }
        }
    }

    function limpiarFormulario(form) {
        const campos = form.querySelectorAll('input, select, textarea');
        campos.forEach(limpiarInvalido);
        const alertas = form.querySelectorAll('.alerta-validacion-scrum');
        alertas.forEach(function (a) {
            a.classList.add('d-none');
            a.innerHTML = '';
        });
    }

    function formatearFechaCorta(isoDate) {
        if (!isoDate || isoDate.length < 10) return isoDate || '';
        const partes = isoDate.substring(0, 10).split('-');
        if (partes.length !== 3) return isoDate;
        return partes[2] + '/' + partes[1] + '/' + partes[0];
    }

    // 3. Validación individual de un campo
    function validarCampo(campo) {
        if (campo.disabled || campo.type === 'hidden' || campo.readOnly) {
            return true;
        }

        const valorRaw = campo.value || '';
        const valor = valorRaw.trim();
        const esObligatorio = campo.hasAttribute('required') || campo.getAttribute('data-required') === 'true';
        const etiqueta = campo.getAttribute('data-label') || campo.getAttribute('name') || 'Este campo';

        // Obligatorio
        if (esObligatorio) {
            if (campo.tagName === 'SELECT' && (valor === '' || valor === '0')) {
                marcarInvalido(campo, campo.getAttribute('data-msg-required') || ('Seleccione una opción para ' + etiqueta + '.'));
                return false;
            }
            if (valor === '') {
                marcarInvalido(campo, campo.getAttribute('data-msg-required') || ('El campo ' + etiqueta + ' es obligatorio.'));
                return false;
            }
        }

        // Si no es obligatorio y está vacío, es válido
        if (valor === '') {
            limpiarInvalido(campo);
            return true;
        }

        // Correo electrónico
        if (campo.type === 'email' || campo.getAttribute('data-type') === 'email') {
            const regexCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
            if (!regexCorreo.test(valor)) {
                marcarInvalido(campo, campo.getAttribute('data-msg-email') || 'Ingrese un correo electrónico con formato válido (ej. usuario@dominio.com).');
                return false;
            }
        }

        // Longitud mínima
        const minLenAttr = campo.getAttribute('minlength') || campo.getAttribute('data-minlength');
        if (minLenAttr) {
            const minLen = parseInt(minLenAttr, 10);
            if (!isNaN(minLen) && valor.length < minLen) {
                marcarInvalido(campo, campo.getAttribute('data-msg-minlength') || ('Debe ingresar al menos ' + minLen + ' caracteres.'));
                return false;
            }
        }

        // Longitud máxima
        const maxLenAttr = campo.getAttribute('maxlength') || campo.getAttribute('data-maxlength');
        if (maxLenAttr) {
            const maxLen = parseInt(maxLenAttr, 10);
            if (!isNaN(maxLen) && valor.length > maxLen) {
                marcarInvalido(campo, 'No debe exceder los ' + maxLen + ' caracteres.');
                return false;
            }
        }

        // Campos numéricos
        if (campo.type === 'number') {
            const num = parseFloat(valor);
            if (isNaN(num)) {
                marcarInvalido(campo, 'Ingrese un valor numérico válido.');
                return false;
            }

            const minAttr = campo.getAttribute('min');
            if (minAttr !== null && minAttr !== '') {
                const minVal = parseFloat(minAttr);
                if (!isNaN(minVal) && num < minVal) {
                    marcarInvalido(campo, campo.getAttribute('data-msg-min') || ('El valor debe ser mayor o igual a ' + minVal + '.'));
                    return false;
                }
            }

            const maxAttr = campo.getAttribute('max');
            if (maxAttr !== null && maxAttr !== '') {
                const maxVal = parseFloat(maxAttr);
                if (!isNaN(maxVal) && num > maxVal) {
                    marcarInvalido(campo, campo.getAttribute('data-msg-max') || ('El valor no debe superar ' + maxVal + '.'));
                    return false;
                }
            }
        }

        limpiarInvalido(campo);
        return true;
    }

    // 4. Reglas de negocio cruzadas por tipo de formulario
    function validarReglasNegocioFormulario(form) {
        let valido = true;
        const tipoForm = form.getAttribute('data-scrum-form') || '';

        // A) Formulario de Sprint (Crear / Editar): validar rango de fechas y vigencia del proyecto
        if (tipoForm === 'sprint') {
            const inputInicio = form.querySelector('[name="FechaInicio"]');
            const inputFin = form.querySelector('[name="FechaFin"]');
            const selectProyecto = form.querySelector('[name="IdProyecto"]');

            if (inputInicio && inputFin && inputInicio.value && inputFin.value) {
                if (inputFin.value <= inputInicio.value) {
                    marcarInvalido(inputFin, 'La fecha de finalización debe ser posterior a la fecha de inicio.');
                    valido = false;
                } else {
                    let proyInicio = form.getAttribute('data-proyecto-inicio') || '';
                    let proyFin = form.getAttribute('data-proyecto-fin') || '';

                    if (selectProyecto && selectProyecto.tagName === 'SELECT' && selectProyecto.selectedIndex >= 0) {
                        const opt = selectProyecto.options[selectProyecto.selectedIndex];
                        proyInicio = opt.getAttribute('data-inicio') || proyInicio;
                        proyFin = opt.getAttribute('data-fin') || proyFin;
                    }

                    if (proyInicio && proyFin) {
                        const rangoTexto = formatearFechaCorta(proyInicio) + ' al ' + formatearFechaCorta(proyFin);
                        if (inputInicio.value < proyInicio || inputInicio.value > proyFin) {
                            marcarInvalido(inputInicio, 'La fecha de inicio debe estar dentro de la vigencia del proyecto (' + rangoTexto + ').');
                            valido = false;
                        }
                        if (inputFin.value < proyInicio || inputFin.value > proyFin) {
                            marcarInvalido(inputFin, 'La fecha de finalización debe estar dentro de la vigencia del proyecto (' + rangoTexto + ').');
                            valido = false;
                        }
                    }
                }
            }
        }

        // B) Formulario de Checkpoint Inmutable (AvanceTareas)
        if (tipoForm === 'checkpoint') {
            const inputPct = form.querySelector('[name="PorcentajeNuevo"]');
            const selectEstado = form.querySelector('[name="EstadoNuevo"]');
            const inputJust = form.querySelector('[name="Justificacion"]');
            const selectImp = form.querySelector('[name="IdImpedimento"]');
            const pctActual = parseInt(form.getAttribute('data-pct-actual') || (inputPct ? inputPct.getAttribute('data-actual') : '0') || '0', 10);

            if (inputPct && selectEstado) {
                const nuevoPct = parseInt(inputPct.value || '0', 10);
                const estado = selectEstado.value;

                if (estado === 'Finalizado' && nuevoPct !== 100) {
                    marcarInvalido(inputPct, 'Para marcar la tarea como Finalizado, el porcentaje de avance debe ser exactamente 100%.');
                    valido = false;
                }

                if (!isNaN(nuevoPct) && nuevoPct < pctActual) {
                    const justTexto = inputJust ? (inputJust.value || '').trim() : '';
                    const impVal = selectImp ? (selectImp.value || '').trim() : '';

                    if (justTexto === '' && impVal === '') {
                        const msgRetroceso = 'Al disminuir el porcentaje de avance (' + pctActual + '% → ' + nuevoPct + '%), es obligatorio ingresar una justificación o vincular un impedimento.';
                        if (inputJust) marcarInvalido(inputJust, msgRetroceso);
                        if (selectImp) marcarInvalido(selectImp, 'Seleccione un impedimento activo o escriba una justificación arriba.');
                        valido = false;
                    }
                }
            }
        }

        // C) Formulario de Aprobaciones / Rechazos (Aprobaciones)
        if (tipoForm === 'aprobacion') {
            const radioRechazado = form.querySelector('input[name="Resultado"][value="Rechazado"]');
            const inputComentario = form.querySelector('[name="Comentario"]');
            if (radioRechazado && radioRechazado.checked && inputComentario) {
                if ((inputComentario.value || '').trim() === '') {
                    marcarInvalido(inputComentario, 'El comentario es obligatorio cuando se rechaza una tarea.');
                    valido = false;
                }
            }
        }

        return valido;
    }

    // 5. Inicializar contadores de caracteres
    function inicializarContadores() {
        const camposConContador = document.querySelectorAll('[data-char-counter]');
        camposConContador.forEach(function (campo) {
            const selector = campo.getAttribute('data-char-counter');
            const max = parseInt(campo.getAttribute('maxlength') || '300', 10);
            const badge = selector ? document.querySelector(selector) : null;
            if (!badge) return;

            function actualizarContador() {
                const len = (campo.value || '').length;
                badge.textContent = len + ' / ' + max;
                badge.classList.remove('char-limit-near', 'char-limit-reached');
                if (len >= max) {
                    badge.classList.add('char-limit-reached');
                } else if (len >= max * 0.85) {
                    badge.classList.add('char-limit-near');
                }
            }

            campo.addEventListener('input', actualizarContador);
            campo.addEventListener('change', actualizarContador);
            actualizarContador();
        });
    }

    // 6. Inicializar botones de mostrar/ocultar contraseña
    function inicializarBotonesPassword() {
        const botones = document.querySelectorAll('.btn-toggle-password');
        botones.forEach(function (btn) {
            btn.addEventListener('click', function () {
                const targetId = btn.getAttribute('data-target');
                const input = targetId ? document.getElementById(targetId) : btn.closest('.input-group')?.querySelector('input');
                if (!input) return;

                const icon = btn.querySelector('i');
                if (input.type === 'password') {
                    input.type = 'text';
                    if (icon) {
                        icon.classList.remove('bi-eye');
                        icon.classList.add('bi-eye-slash');
                    }
                } else {
                    input.type = 'password';
                    if (icon) {
                        icon.classList.remove('bi-eye-slash');
                        icon.classList.add('bi-eye');
                    }
                }
            });
        });
    }

    // 7. Inicializar validación en todos los formularios .form-validar-scrum
    function inicializarFormulariosScrum() {
        const formularios = document.querySelectorAll('form.form-validar-scrum');

        formularios.forEach(function (form) {
            form.setAttribute('novalidate', 'novalidate');

            const campos = form.querySelectorAll('input, select, textarea');
            campos.forEach(function (campo) {
                campo.addEventListener('input', function () {
                    if (campo.classList.contains('is-invalid')) {
                        validarCampo(campo);
                    }
                    // Si es justificación o impedimento en checkpoint, limpiar el par
                    if (form.getAttribute('data-scrum-form') === 'checkpoint') {
                        const just = form.querySelector('[name="Justificacion"]');
                        const imp = form.querySelector('[name="IdImpedimento"]');
                        if ((just && (just.value || '').trim() !== '') || (imp && (imp.value || '') !== '')) {
                            if (just) limpiarInvalido(just);
                            if (imp) limpiarInvalido(imp);
                        }
                    }
                });

                campo.addEventListener('change', function () {
                    if (campo.classList.contains('is-invalid')) {
                        validarCampo(campo);
                    }
                    if (form.getAttribute('data-scrum-form') === 'checkpoint') {
                        const just = form.querySelector('[name="Justificacion"]');
                        const imp = form.querySelector('[name="IdImpedimento"]');
                        if ((just && (just.value || '').trim() !== '') || (imp && (imp.value || '') !== '')) {
                            if (just) limpiarInvalido(just);
                            if (imp) limpiarInvalido(imp);
                        }
                    }
                });
            });

            form.addEventListener('submit', function (e) {
                let todoValido = true;

                campos.forEach(function (campo) {
                    if (!validarCampo(campo)) {
                        todoValido = false;
                    }
                });

                if (!validarReglasNegocioFormulario(form)) {
                    todoValido = false;
                }

                if (!todoValido) {
                    e.preventDefault();
                    e.stopPropagation();
                    const primerInvalido = form.querySelector('.is-invalid');
                    if (primerInvalido && typeof primerInvalido.focus === 'function') {
                        primerInvalido.focus();
                    }
                    return;
                }

                // Deshabilitar botón de envío para evitar doble clic
                const btnSubmit = form.querySelector('button[type="submit"]');
                if (btnSubmit && !btnSubmit.disabled) {
                    btnSubmit.disabled = true;
                    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span><span>Guardando...</span>';
                }
            });
        });

        // Limpiar estados de error al abrir o cerrar cualquier modal Bootstrap
        const modales = document.querySelectorAll('.modal');
        modales.forEach(function (modal) {
            modal.addEventListener('show.bs.modal', function () {
                const forms = modal.querySelectorAll('form.form-validar-scrum');
                forms.forEach(limpiarFormulario);
                // Actualizar contadores dentro del modal
                setTimeout(function () {
                    modal.querySelectorAll('[data-char-counter]').forEach(function (c) {
                        c.dispatchEvent(new Event('input'));
                    });
                }, 40);
            });

            modal.addEventListener('hidden.bs.modal', function () {
                const forms = modal.querySelectorAll('form.form-validar-scrum');
                forms.forEach(limpiarFormulario);
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        inicializarFormulariosScrum();
        inicializarContadores();
        inicializarBotonesPassword();
    });

    window.ScrumValidador = {
        limpiarFormulario: limpiarFormulario,
        marcarInvalido: marcarInvalido,
        limpiarInvalido: limpiarInvalido
    };
})();
