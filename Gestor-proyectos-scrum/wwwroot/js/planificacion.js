(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var botonesRechazar = document.querySelectorAll('.js-btn-rechazar');
        var rechazarIdHistoria = document.getElementById('rechazarIdHistoria');
        var rechazarCodigo = document.getElementById('rechazarCodigo');

        botonesRechazar.forEach(function (boton) {
            boton.addEventListener('click', function () {
                if (rechazarIdHistoria) {
                    rechazarIdHistoria.value = boton.dataset.idhistoria;
                }
                if (rechazarCodigo) {
                    rechazarCodigo.textContent = boton.dataset.codigo;
                }
            });
        });

        var panel = document.getElementById('panelCompromiso');
        if (!panel) {
            return;
        }

        var capacidad = parseInt(panel.dataset.capacidad, 10) || 0;
        var comprometidos = parseInt(panel.dataset.comprometidos, 10) || 0;
        var disponiblesBase = capacidad - comprometidos;

        var checkboxes = document.querySelectorAll('.js-historia-checkbox');
        var valorEl = document.getElementById('capacidadValor');
        var fillEl = document.getElementById('capacidadFill');
        var notaEl = document.getElementById('capacidadNota');
        var botonAbrirModal = document.getElementById('btnAbrirModalCompromiso');
        var modalLista = document.getElementById('modalListaSeleccionadas');
        var modalPuntos = document.getElementById('modalResumenPuntos');
        var modalRestante = document.getElementById('modalResumenCapacidad');
        var confirmoCheckbox = document.getElementById('inputConfirmo');
        var botonEnviar = document.getElementById('btnEnviarCompromiso');

        function seleccionActual() {
            return Array.prototype.filter.call(checkboxes, function (c) { return c.checked; });
        }

        function actualizarVista() {
            var seleccionadas = seleccionActual();
            var puntosSeleccionados = seleccionadas.reduce(function (acumulado, c) {
                return acumulado + (parseInt(c.dataset.puntos, 10) || 0);
            }, 0);

            var total = comprometidos + puntosSeleccionados;
            var excedido = total > capacidad;
            var porcentaje = capacidad > 0 ? Math.min(100, Math.round((total * 100) / capacidad)) : 0;
            var restante = disponiblesBase - puntosSeleccionados;

            if (valorEl) {
                valorEl.textContent = total + ' / ' + capacidad + ' pts';
            }

            if (fillEl) {
                fillEl.style.width = porcentaje + '%';
                fillEl.classList.remove('capacity-ok', 'capacity-warning', 'capacity-over');
                fillEl.classList.add(excedido ? 'capacity-over' : (porcentaje >= 80 ? 'capacity-warning' : 'capacity-ok'));
            }

            if (notaEl) {
                if (excedido) {
                    notaEl.innerHTML = '<i class="bi bi-exclamation-triangle-fill me-1"></i>Superas la capacidad del Sprint por ' + (total - capacidad) + ' pts.';
                } else {
                    notaEl.innerHTML = '<i class="bi bi-info-circle me-1"></i>' + porcentaje + '% de la capacidad utilizada. Quedan ' + restante + ' pts disponibles.';
                }
                notaEl.classList.toggle('text-danger', excedido);
                notaEl.classList.toggle('fw-semibold', excedido);
            }

            if (botonAbrirModal) {
                botonAbrirModal.disabled = seleccionadas.length === 0;
            }

            if (modalLista) {
                modalLista.innerHTML = '';
                if (seleccionadas.length === 0) {
                    modalLista.innerHTML = '<li class="text-muted small">No has seleccionado historias.</li>';
                } else {
                    seleccionadas.forEach(function (c) {
                        var li = document.createElement('li');
                        li.className = 'd-flex justify-content-between align-items-center py-1 border-bottom small';
                        li.innerHTML = '<span><strong>' + c.dataset.codigo + '</strong> ' + c.dataset.titulo + '</span>' +
                            '<span class="badge bg-primary-subtle text-primary">' + c.dataset.puntos + ' pts</span>';
                        modalLista.appendChild(li);
                    });
                }
            }

            if (modalPuntos) {
                modalPuntos.textContent = puntosSeleccionados + ' pts';
            }

            if (modalRestante) {
                modalRestante.textContent = restante + ' pts';
                modalRestante.classList.toggle('text-danger', restante < 0);
            }
        }

        checkboxes.forEach(function (c) {
            c.addEventListener('change', actualizarVista);
        });

        if (confirmoCheckbox && botonEnviar) {
            confirmoCheckbox.addEventListener('change', function () {
                botonEnviar.disabled = !confirmoCheckbox.checked;
            });
        }

        actualizarVista();
    });
})();
