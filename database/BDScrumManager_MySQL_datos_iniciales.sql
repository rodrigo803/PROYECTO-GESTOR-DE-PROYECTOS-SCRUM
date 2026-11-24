/*
SCRUM Manager - Datos iniciales (MySQL / MariaDB)
Creado por Sergio Monge
Fecha: 19/11/2026

Ejecutar en una base RECIÉN CREADA con BDScrumManager_MySQL.sql
(no después del script de pruebas, porque ese también inserta usuarios).
El orden de los insert importa: los Id se generan en el orden en que se insertan.

HU-01: las contraseñas ya quedan con un hash BCrypt real (BCrypt.Net-Next) de la
contraseña de demostración "Scrum2026!" para los 10 usuarios. Si tu base ya existía
con el marcador 'PENDIENTE-HU01', corre database/BDScrumManager_actualizar_contrasenas.sql.
*/

set names utf8mb4;

-- Roles ya insertados por el script de estructura:
-- 1 Administrador, 2 Product Owner, 3 Scrum Master, 4 Miembro del Equipo

-- Contraseña de demostración para los 10 usuarios: Scrum2026!
insert into Usuarios
(Nombre,Apellidos,Correo,ContrasenaHash,IdRol)
values
('Admin','Sistema','admin@scrummanager.test','$2a$11$wtv4ly8.Mo2awZQg7.wSG.Rt2z70dcTtejMwZpzjTkJMCiTIlobf.',1),
('Valeria','Castro','valeria.castro@scrummanager.test','$2a$11$LNcVU15u6Ap4oZFJVmPhhuO7XWRyA/Cv4uAA1HesdGx3A6MtMtGaS',2),
('Rodrigo','Morales','rodrigo.morales@scrummanager.test','$2a$11$zYGhtDfUJm1NMgeQDa9YAukehhf8c0CPiuOVrIJHyZX5W443skFh6',3),
('Carlos','Ruiz','carlos.ruiz@scrummanager.test','$2a$11$jgkAFjwD3cXwUYPsZfx6H.N1jjKAO6/7o.lNqXUF1xSByHD3GjKi.',4),
('Ana','Gómez','ana.gomez@scrummanager.test','$2a$11$K4rwu246i.KwdVEZDjBwsu69D0w01HnZCq9ihgiYV68vJ6IEF.G/e',4),
('Mateo','Silva','mateo.silva@scrummanager.test','$2a$11$sm0Wdfu4ZzoZYCksH8/fOuGFHS.YLXBnx9en/fdu6lpUNzij8uQ3S',4),
('Lucía','Morales','lucia.morales@scrummanager.test','$2a$11$Ewn8yYk9kLnQJBiZOzidLezlYMnenrQLmc7B4FCEzYc5zv4rSYGTe',2),
('Jesús','De Lima','jesus.delima@scrummanager.test','$2a$11$4V4Kfnk23KD8CcaTaH0LSu80UaKx3r2UHyZmINErYimhxsguUW8Ry',3),
('Daniela','Vargas','daniela.vargas@scrummanager.test','$2a$11$QYkqq5B2FtD9JZsqMCMdAOd/Wy1wN12/bqTF7OLxcmNattA18CbCa',4),
('Andrés','Solís','andres.solis@scrummanager.test','$2a$11$XJvNQ4yH2/FE/Xp65SUQ8.qyj/kdeYIcnIGoThPOExOWckYRB.WKq',4);

-- HU-03: proyectos (PRY-004 queda sin Scrum Master para probar la validación)
insert into Proyectos
(Codigo,Nombre,Descripcion,FechaInicio,FechaFin,Estado,MotivoCierre,FechaCierre)
values
('PRY-001','Fintech Mobile App','Aplicación móvil de pagos y suscripciones para clientes bancarios.','2026-10-19','2027-02-26','Activo',null,null),
('PRY-002','Portal de Clientes Web','Autogestión de cuentas, facturas y soporte para clientes empresariales.','2026-10-05','2027-01-29','Activo',null,null),
('PRY-003','App de Inventario','Control de existencias con lector de códigos de barras para bodegas.','2026-11-02','2027-03-26','Activo',null,null),
('PRY-004','Plataforma E-learning','Cursos en línea con evaluaciones y certificados digitales.','2027-01-11','2027-05-28','Planificado',null,null),
('PRY-005','Sistema de Reservas','Reservas de salas y equipos para oficinas corporativas.','2026-06-01','2026-09-25','Cerrado','Proyecto completado','2026-09-25'),
('PRY-006','Migración a la Nube','Traslado de servicios internos a infraestructura en la nube.','2026-04-06','2026-08-28','Cerrado','Proyecto completado','2026-08-28');

insert into ProyectoMiembros
(IdProyecto,IdUsuario,RolProyecto,FechaIngreso)
values
(1,2,'Product Owner','2026-10-19'),
(1,3,'Scrum Master','2026-10-19'),
(1,4,'Developer','2026-10-19'),
(1,5,'Developer','2026-10-19'),
(1,6,'Developer','2026-10-20'),
(1,9,'Developer','2026-10-26'),
(2,7,'Product Owner','2026-10-05'),
(2,8,'Scrum Master','2026-10-05'),
(2,5,'Developer','2026-10-05'),
(2,10,'Developer','2026-10-12'),
(3,2,'Product Owner','2026-11-02'),
(3,3,'Scrum Master','2026-11-02'),
(3,4,'Developer','2026-11-02'),
(3,6,'Developer','2026-11-02'),
(3,10,'Developer','2026-11-02'),
(4,2,'Product Owner','2026-11-10'),
(4,5,'Developer','2026-11-10'),
(5,7,'Product Owner','2026-06-01'),
(5,8,'Scrum Master','2026-06-01'),
(5,4,'Developer','2026-06-01'),
(5,6,'Developer','2026-06-01'),
(6,2,'Product Owner','2026-04-06'),
(6,3,'Scrum Master','2026-04-06'),
(6,4,'Developer','2026-04-06');

-- HU-04: Product Backlog
-- Estados: Terminada (Sprint 0), En Sprint (Sprint 1), Lista (candidatas para HU-06), Pendiente (no elegibles)
insert into HistoriasUsuario
(IdProyecto,Codigo,Como,Quiero,Para,Prioridad,OrdenPrioridad,Estimacion,Estado)
values
(1,'HU-01','cliente','iniciar sesión con doble factor de autenticación','proteger mi cuenta','Alta',1,8,'Terminada'),
(1,'HU-02','cliente','recuperar mi contraseña por correo','no perder el acceso a mi cuenta','Alta',2,5,'Terminada'),
(1,'HU-03','administrador','consultar el registro de accesos','detectar ingresos sospechosos','Media',3,5,'Terminada'),
(1,'HU-04','cliente','pagar con tarjeta de crédito o débito','completar mi suscripción','Alta',4,13,'En Sprint'),
(1,'HU-05','cliente','descargar mis facturas en PDF','llevar el control de mis gastos','Alta',5,8,'En Sprint'),
(1,'HU-06','cliente','consultar el saldo de mi cuenta','saber cuánto dinero tengo disponible','Alta',6,5,'En Sprint'),
(1,'HU-07','cliente','recibir notificaciones push antes de cada cobro','evitar pagos atrasados','Media',7,3,'Lista'),
(1,'HU-08','administrador','crear códigos de descuento','atraer nuevos clientes','Media',8,8,'Lista'),
(1,'HU-09','cliente','exportar mis movimientos a Excel','analizar mis finanzas','Media',9,5,'Lista'),
(1,'HU-10','contador','generar reportes fiscales','cumplir con las obligaciones tributarias','Baja',10,8,'Pendiente'),
(1,'HU-11','cliente','programar pagos recurrentes','no olvidar mis pagos mensuales','Media',11,null,'Pendiente'),
(1,'HU-12','cliente','cambiar entre el plan mensual y el anual','ahorrar con el plan anual','Alta',12,13,'Lista'),
(2,'HU-01','cliente empresarial','consultar mis facturas','revisar lo que debo pagar','Alta',1,5,'Terminada'),
(2,'HU-02','cliente empresarial','abrir tickets de soporte','reportar problemas con el servicio','Alta',2,8,'En Sprint'),
(2,'HU-03','agente de soporte','responder los tickets asignados','resolver los problemas de los clientes','Alta',3,5,'En Sprint'),
(2,'HU-04','cliente empresarial','actualizar mis datos de contacto','recibir las notificaciones correctamente','Media',4,3,'Lista'),
(3,'HU-01','bodeguero','registrar entradas con el lector de códigos de barras','evitar errores de digitación','Alta',1,8,'En Sprint'),
(3,'HU-02','bodeguero','registrar salidas de producto','mantener el inventario al día','Alta',2,5,'En Sprint'),
(3,'HU-03','jefe de bodega','recibir alertas de stock mínimo','reabastecer a tiempo','Media',3,5,'Lista'),
(3,'HU-04','jefe de bodega','consultar el inventario por ubicación','encontrar los productos más rápido','Media',4,null,'Pendiente');

-- Criterios de aceptación (HU-10 del proyecto 1 y HU-04 del proyecto 3 quedan sin criterios a propósito)
insert into CriteriosAceptacion
(IdHistoria,Descripcion)
values
(1,'El sistema envía un código de 6 dígitos que vence en 5 minutos'),
(1,'Tras 3 códigos incorrectos la cuenta se bloquea 15 minutos'),
(2,'El enlace de recuperación vence en 30 minutos'),
(2,'La nueva contraseña debe tener al menos 12 caracteres'),
(3,'Se puede filtrar el registro por rango de fechas'),
(3,'Cada acceso muestra fecha, hora, dirección IP y dispositivo'),
(4,'Se aceptan tarjetas Visa y Mastercard'),
(4,'Un pago rechazado muestra el motivo al cliente'),
(4,'El pago se confirma en menos de 5 segundos'),
(5,'La factura en PDF incluye el desglose de impuestos'),
(5,'Se pueden descargar las facturas de los últimos 12 meses'),
(6,'El saldo se actualiza al instante después de cada pago'),
(7,'La notificación llega 3 días antes del cobro'),
(7,'El cliente puede desactivar las notificaciones'),
(8,'Cada código tiene fecha de vencimiento y límite de usos'),
(8,'Un código vencido muestra un mensaje claro'),
(9,'El archivo incluye fecha, descripción y monto de cada movimiento'),
(11,'El cliente elige el día del mes para el pago'),
(12,'El cambio de plan aplica en el siguiente ciclo de cobro'),
(12,'Se muestra el ahorro del plan anual antes de confirmar'),
(13,'Se muestran las facturas pendientes primero'),
(14,'El ticket permite adjuntar capturas de pantalla'),
(14,'El cliente recibe un número de ticket por correo'),
(15,'El agente ve los tickets ordenados por prioridad'),
(16,'El correo nuevo se confirma con un enlace'),
(17,'La lectura del código agrega el producto en menos de 2 segundos'),
(17,'Un código desconocido muestra una alerta'),
(18,'No se permite una salida mayor al stock disponible'),
(19,'La alerta se envía al jefe de bodega por correo'),
(19,'El stock mínimo se configura por producto');

-- HU-05: Sprints (hoy: Sprint 1 del proyecto 1 en curso)
insert into Sprints
(IdProyecto,Nombre,Objetivo,FechaInicio,FechaFin,CapacidadPuntos,Estado)
values
(1,'Sprint 0','Base del sistema y autenticación segura','2026-10-26','2026-11-08',30,'Cerrado'),
(1,'Sprint 1','Pagos con tarjeta y facturación','2026-11-09','2026-11-22',34,'Activo'),
(1,'Sprint 2','Suscripciones, descuentos y notificaciones','2026-11-23','2026-12-06',38,'Planificado'),
(2,'Sprint 1','Consulta de facturas empresariales','2026-10-26','2026-11-08',20,'Cerrado'),
(2,'Sprint 2','Soporte con tickets','2026-11-09','2026-11-22',21,'Activo'),
(3,'Sprint 1','Entradas y salidas de bodega','2026-11-09','2026-11-22',18,'Activo');

-- HU-06: Sprint Backlog (el Sprint 2 del proyecto 1 queda sin compromisos para planificarlo con HU-06)
insert into SprintHistorias
(IdSprint,IdHistoria,FechaCompromiso,Resultado)
values
(1,1,'2026-10-26 09:00:00','Terminada'),
(1,2,'2026-10-26 09:00:00','Terminada'),
(1,3,'2026-10-26 09:00:00','Terminada'),
(2,4,'2026-11-09 09:00:00','Comprometida'),
(2,5,'2026-11-09 09:00:00','Comprometida'),
(2,6,'2026-11-09 09:00:00','Comprometida'),
(4,13,'2026-10-26 09:00:00','Terminada'),
(5,14,'2026-11-09 09:00:00','Comprometida'),
(5,15,'2026-11-09 09:00:00','Comprometida'),
(6,17,'2026-11-09 09:00:00','Comprometida'),
(6,18,'2026-11-09 09:00:00','Comprometida');

-- HU-07: Tareas (se crean en Pendiente y 0 %; el avance llega por AvanceTareas)
insert into Tareas
(IdSprintHistoria,Titulo,Descripcion,IdResponsable,PuntosEsfuerzo,HorasEstimadas,FechaEntregaEstimada,FechaRegistro)
values
(1,'Configurar el proveedor de códigos 2FA',null,4,3,8,'2026-10-30','2026-10-26 08:00:00'),
(1,'Pantalla de verificación del código',null,5,2,6,'2026-11-02','2026-10-26 08:00:00'),
(2,'Envío del correo de recuperación',null,6,3,6,'2026-11-04','2026-10-26 08:00:00'),
(2,'Formulario de nueva contraseña',null,9,2,4,'2026-11-05','2026-10-26 08:00:00'),
(3,'Tabla y consulta del registro de accesos',null,4,3,8,'2026-11-07','2026-10-26 08:00:00'),
(4,'Integración con la pasarela de pago','Conexión con la API del procesador de tarjetas',4,8,16,'2026-11-18','2026-11-09 08:00:00'),
(4,'Formulario de datos de la tarjeta',null,5,2,6,'2026-11-13','2026-11-09 08:00:00'),
(4,'Pruebas de pagos rechazados',null,6,3,6,'2026-11-20','2026-11-09 08:00:00'),
(5,'Generar el PDF de la factura',null,9,5,10,'2026-11-17','2026-11-09 08:00:00'),
(5,'Listado de facturas del cliente',null,5,3,6,'2026-11-19','2026-11-09 08:00:00'),
(6,'Consulta de saldo en la API',null,4,3,5,'2026-11-21','2026-11-09 08:00:00'),
(6,'Pantalla de saldo',null,5,2,4,'2026-11-22','2026-11-09 08:00:00'),
(7,'Listado de facturas empresariales',null,10,3,8,'2026-11-06','2026-10-26 08:00:00'),
(8,'Formulario de ticket de soporte',null,10,3,6,'2026-11-14','2026-11-09 08:00:00'),
(8,'Adjuntar archivos al ticket',null,5,5,8,'2026-11-20','2026-11-09 08:00:00'),
(9,'Bandeja de tickets del agente',null,10,5,10,'2026-11-21','2026-11-09 08:00:00'),
(10,'Lectura del código de barras',null,6,5,12,'2026-11-18','2026-11-09 08:00:00'),
(10,'Registro de la entrada en inventario',null,4,3,6,'2026-11-20','2026-11-09 08:00:00'),
(11,'Registro de salida de producto',null,10,3,6,'2026-11-21','2026-11-09 08:00:00'),
(11,'Validar el stock disponible',null,6,2,4,'2026-11-22','2026-11-09 08:00:00');

-- HU-09: Impedimentos
insert into Impedimentos
(IdSprint,IdHistoria,IdTarea,Titulo,Descripcion,Severidad,Estado,IdReportadoPor,IdResponsable,FechaReporte,CausaRaiz,Solucion,FechaResolucion)
values
(2,4,6,'La pasarela de pago cambió la versión de su API','Las llamadas de autorización devuelven error 410 desde el 16/11.','Crítica','En atención',4,3,'2026-11-16 09:00:00',null,null,null),
(2,5,10,'Falta la plantilla oficial de la factura electrónica','Contabilidad no ha enviado el formato aprobado.','Alta','Abierto',5,null,'2026-11-14 11:00:00',null,null,null),
(1,2,3,'El servidor de correo bloqueaba los envíos','Los correos de recuperación llegaban como spam o no llegaban.','Media','Resuelto',6,3,'2026-11-03 08:00:00','El dominio no tenía registro SPF','Se configuró el registro SPF y se usó el servidor SMTP institucional','2026-11-03 15:00:00'),
(6,17,17,'Los lectores de código de barras no han llegado','El proveedor atrasó la entrega de los equipos.','Alta','Abierto',6,null,'2026-11-12 10:00:00',null,null,null),
(5,15,null,'Acceso a la API de tickets pendiente de aprobación','TI no ha creado las credenciales del ambiente de pruebas.','Media','En atención',10,8,'2026-11-17 14:00:00',null,null,null);

insert into SeguimientoImpedimentos
(IdImpedimento,IdUsuario,Comentario,EstadoNuevo,Fecha)
values
(1,4,'Se reporta el cambio de versión de la API de la pasarela.','Abierto','2026-11-16 09:05:00'),
(1,3,'Se contactó al proveedor y se asigna el seguimiento.','En atención','2026-11-16 10:30:00'),
(3,3,'Se revisa la configuración del dominio de correo.','En atención','2026-11-03 09:00:00'),
(3,3,'Se configuró SPF y los envíos funcionan.','Resuelto','2026-11-03 15:00:00'),
(5,8,'Se solicitó el acceso al área de TI.','En atención','2026-11-17 15:00:00');

-- Checkpoints de avance (cada uno parte del porcentaje actual de la tarea)
insert into AvanceTareas
(IdTarea,IdUsuario,Fecha,PorcentajeAnterior,PorcentajeNuevo,HorasTrabajadas,EstadoAnterior,EstadoNuevo,Justificacion,IdImpedimento)
values
(1,4,'2026-10-28 10:00:00',0,50,4,'Pendiente','En progreso',null,null),
(1,4,'2026-10-30 16:00:00',50,100,5,'En progreso','Finalizado',null,null),
(2,5,'2026-10-29 11:00:00',0,60,4,'Pendiente','En progreso',null,null),
(2,5,'2026-11-02 15:00:00',60,100,3,'En progreso','Finalizado',null,null),
(3,6,'2026-11-03 16:00:00',0,100,7,'Pendiente','Finalizado',null,null),
(4,9,'2026-11-04 14:00:00',0,100,4,'Pendiente','Finalizado',null,null),
(5,4,'2026-11-05 10:00:00',0,70,6,'Pendiente','En progreso',null,null),
(5,4,'2026-11-07 12:00:00',70,100,3,'En progreso','Finalizado',null,null),
(6,4,'2026-11-11 10:00:00',0,25,5,'Pendiente','En progreso',null,null),
(6,4,'2026-11-14 16:00:00',25,50,6,'En progreso','En progreso',null,null),
(6,4,'2026-11-17 09:00:00',50,40,2,'En progreso','En progreso','La pasarela cambió la versión de su API; se rehace la autorización',1),
(7,5,'2026-11-12 15:00:00',0,100,6,'Pendiente','Finalizado',null,null),
(9,9,'2026-11-12 11:00:00',0,40,4,'Pendiente','En progreso',null,null),
(9,9,'2026-11-16 17:00:00',40,80,4,'En progreso','En revisión',null,null),
(10,5,'2026-11-13 10:00:00',0,30,2,'Pendiente','En progreso',null,null),
(13,10,'2026-10-30 16:00:00',0,100,9,'Pendiente','Finalizado',null,null),
(14,10,'2026-11-12 14:00:00',0,100,5,'Pendiente','Finalizado',null,null),
(15,5,'2026-11-16 11:00:00',0,50,4,'Pendiente','En progreso',null,null),
(16,10,'2026-11-18 10:00:00',0,20,3,'Pendiente','En progreso',null,null),
(17,6,'2026-11-11 09:00:00',0,60,8,'Pendiente','En progreso',null,null),
(19,10,'2026-11-17 15:00:00',0,100,5,'Pendiente','Finalizado',null,null);

-- Aprobaciones formales (HU-03 del proyecto 1 se rechazó una vez y luego se aprobó)
insert into Aprobaciones
(IdHistoria,IdCriterio,IdTarea,IdAprobador,Resultado,Comentario,Fecha)
values
(1,null,null,2,'Aprobado','Cumple los criterios de seguridad','2026-11-08 16:00:00'),
(2,null,null,2,'Aprobado',null,'2026-11-08 16:10:00'),
(3,5,null,2,'Rechazado','Falta filtrar el registro por rango de fechas','2026-11-07 15:00:00'),
(3,null,null,2,'Aprobado','Se agregó el filtro por fechas','2026-11-08 16:20:00'),
(4,null,7,3,'Aprobado','Formulario revisado en la daily','2026-11-13 09:30:00'),
(13,null,null,7,'Aprobado',null,'2026-11-08 15:00:00');

-- HU-11: Retrospectivas de los sprints cerrados
insert into Retrospectivas
(IdSprint,Fecha,IdFacilitador)
values
(1,'2026-11-08',3),
(4,'2026-11-08',8);

insert into ElementosRetrospectiva
(IdRetrospectiva,Tipo,Descripcion,IdResponsable,FechaCompromiso,Estado)
values
(1,'Positivo','Se completaron todas las historias comprometidas',null,null,null),
(1,'Mejora','Las tareas de correo se estimaron por debajo de lo real',null,null,null),
(1,'Acción','Revisar las dependencias externas durante la planificación',3,'2026-11-12','Cumplida'),
(2,'Positivo','Buena comunicación con el cliente durante la revisión',null,null,null),
(2,'Acción','Solicitar los accesos de TI antes de iniciar el sprint',8,'2026-11-10','Pendiente');

-- Auditoría de cambios relevantes
insert into Auditoria
(IdUsuario,Accion,Entidad,IdRegistro,Detalle,Fecha)
values
(7,'Cerrar','Proyectos',5,'Proyecto completado','2026-09-25 17:00:00'),
(2,'Crear','Proyectos',1,'Proyecto PRY-001 creado','2026-10-19 09:00:00'),
(2,'Crear','Proyectos',3,'Proyecto PRY-003 creado','2026-11-02 09:00:00'),
(3,'Cerrar','Sprints',1,'Sprint 0 del proyecto PRY-001 cerrado','2026-11-08 17:00:00'),
(3,'Crear','Sprints',3,'Sprint 2 del proyecto PRY-001 planificado','2026-11-10 10:00:00'),
(2,'Crear','Proyectos',4,'Proyecto PRY-004 creado sin Scrum Master','2026-11-10 11:00:00');
