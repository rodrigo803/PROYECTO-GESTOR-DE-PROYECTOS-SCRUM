/* HU-12: ajuste de navegación del prototipo en pantallas pequeñas.
   NO implementa login, registros, permisos ni persistencia. */
document.addEventListener('DOMContentLoaded', () => {
  const boton = document.querySelector('.menu-mobile');
  const menu = document.querySelector('#menu-lateral');
  if (!boton || !menu) return;
  boton.addEventListener('click', () => {
    const abierto = menu.classList.toggle('is-open');
    boton.setAttribute('aria-expanded', String(abierto));
    boton.setAttribute('aria-label', abierto ? 'Cerrar menú de navegación' : 'Abrir menú de navegación');
  });
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
      menu.classList.remove('is-open');
      boton.setAttribute('aria-expanded', 'false');
      boton.setAttribute('aria-label', 'Abrir menú de navegación');
    }
  });
});
