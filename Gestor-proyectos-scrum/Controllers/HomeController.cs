using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Gestor_proyectos_scrum.Controllers
{
    public class HomeController : Controller
    {
        private readonly ConexionDb _conexionDb;
        private readonly IWebHostEnvironment _entorno;

        public HomeController(ConexionDb conexionDb, IWebHostEnvironment entorno)
        {
            _conexionDb = conexionDb;
            _entorno = entorno;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult InicioCliente()
        {
            return View();
        }

        /// <summary>
        /// Diagnóstico de la conexión a la base de datos. Solo disponible en Development.
        /// </summary>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> EstadoBd()
        {
            if (!_entorno.IsDevelopment())
            {
                return NotFound();
            }

            var modelo = new EstadoBdViewModel();

            try
            {
                using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
                modelo.Conectado = true;

                modelo.Tablas = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.tables " +
                    "WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE';");

                modelo.Vistas = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.views WHERE table_schema = DATABASE();");

                modelo.Triggers = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = DATABASE();");

                modelo.RegistrosProyectos = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Proyectos;");
            }
            catch (Exception ex)
            {
                modelo.Conectado = false;
                modelo.Error = ex.Message;
            }

            return View(modelo);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
