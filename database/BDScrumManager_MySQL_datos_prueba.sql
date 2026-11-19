set names utf8mb4;

-- =========================================================
-- SCRUM Manager - Datos de prueba para verificar las correcciones (MySQL / MariaDB)
-- Ejecutar DESPUÉS de BDScrumManager_MySQL.sql, en la misma base de datos recién creada.
-- Cada prueba muestra una fila con su resultado.
-- =========================================================

-- Procedimiento auxiliar: ejecuta una sentencia que DEBE fallar y muestra si falló.
DELIMITER //
create procedure sp_ProbarError(in p_prueba varchar(100), in p_sql text)
begin
declare v_msg text default null;
declare continue handler for sqlexception
begin
get diagnostics condition 1 v_msg = message_text;
end;
set @sentencia = p_sql;
prepare stmt from @sentencia;
execute stmt;
deallocate prepare stmt;
if v_msg is null then
select p_prueba as Prueba, 'ERROR: la operación se permitió' as Resultado;
else
select p_prueba as Prueba, concat('Correcto: ', v_msg) as Resultado;
end if;
end//
DELIMITER ;

insert into Usuarios (Nombre, Apellidos, Correo, ContrasenaHash, IdRol) values
('Valeria', 'Castro', 'valeria@scrum.test', 'hash-de-prueba', 2),
('Rodrigo', 'Morales', 'rodrigo@scrum.test', 'hash-de-prueba', 3),
('Ana', 'Gómez', 'ana@scrum.test', 'hash-de-prueba', 4);

insert into Proyectos (Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado) values
('PRY-001', 'Fintech Mobile App', 'App de pagos y suscripciones', '2026-11-02', '2027-02-26', 'Activo');

insert into HistoriasUsuario (IdProyecto, Codigo, Como, Quiero, Para, Prioridad, OrdenPrioridad, Estimacion, Estado) values
(1, 'HU-04', 'cliente', 'pagar con tarjeta', 'completar mi suscripción', 'Alta', 1, 8, 'En Sprint');

insert into Sprints (IdProyecto, Nombre, Objetivo, FechaInicio, FechaFin, CapacidadPuntos, Estado) values
(1, 'Sprint 1', 'Cobro con tarjeta funcionando', '2026-11-02', '2026-11-16', 38, 'Activo');

insert into SprintHistorias (IdSprint, IdHistoria, Resultado) values (1, 1, 'Comprometida');

-- Tarea A: sencilla (2 puntos). Tarea B: compleja (8 puntos).
insert into Tareas (IdSprintHistoria, Titulo, IdResponsable, PuntosEsfuerzo, HorasEstimadas, FechaEntregaEstimada) values
(1, 'A - Formulario de tarjeta', 3, 2, 4, '2026-11-06'),
(1, 'B - Integración con pasarela de pago', 3, 8, 16, '2026-11-13');

-- Checkpoints de avance (uno por sentencia, como lo haría la aplicación)
insert into AvanceTareas (IdTarea, IdUsuario, Fecha, PorcentajeAnterior, PorcentajeNuevo, HorasTrabajadas, EstadoAnterior, EstadoNuevo)
values (1, 3, '2026-11-04', 0, 50, 2, 'Pendiente', 'En progreso');
insert into AvanceTareas (IdTarea, IdUsuario, Fecha, PorcentajeAnterior, PorcentajeNuevo, HorasTrabajadas, EstadoAnterior, EstadoNuevo)
values (1, 3, '2026-11-10', 50, 100, 2.5, 'En progreso', 'Finalizado');
insert into AvanceTareas (IdTarea, IdUsuario, Fecha, PorcentajeAnterior, PorcentajeNuevo, HorasTrabajadas, EstadoAnterior, EstadoNuevo)
values (2, 3, '2026-11-11', 0, 25, 5, 'Pendiente', 'En progreso');

-- PRUEBA 1: avance ponderado de la historia
-- Esperado: AvancePonderado = 40.0 y AvanceSimple = 62.5
select 'Prueba 1' as Prueba, Codigo, TotalTareas, PuntosTotales, AvancePonderado, AvanceSimple, HorasEstimadas, HorasReales from vw_AvanceHistoria;

-- PRUEBA 2: comparativo semanal
-- Esperado: semana 1 = 10.0 % acumulado; semana 2 = 40.0 % acumulado
select 'Prueba 2' as Prueba, IdSprint, Semana, PuntosGanadosSemana, AvanceAcumulado from vw_AvanceSemanal order by Semana;

-- PRUEBA 3: el avance no puede bajar sin justificación (esperado: Correcto)
call sp_ProbarError('Prueba 3: bajar avance sin justificación',
'insert into AvanceTareas (IdTarea, IdUsuario, PorcentajeAnterior, PorcentajeNuevo, EstadoAnterior, EstadoNuevo) values (2, 3, 25, 10, ''En progreso'', ''En progreso'')');

-- PRUEBA 4: con justificación sí se permite (esperado: la tarea B queda en 10 %)
insert into AvanceTareas (IdTarea, IdUsuario, PorcentajeAnterior, PorcentajeNuevo, EstadoAnterior, EstadoNuevo, Justificacion)
values (2, 3, 25, 10, 'En progreso', 'En progreso', 'La pasarela cambió su API; se rehace la integración');
select 'Prueba 4' as Prueba, IdTarea, Titulo, PorcentajeAvance, HorasReales, Estado, FechaEntregaReal from Tareas;

-- PRUEBA 5: los checkpoints no se pueden editar ni borrar (esperado: Correcto, Correcto)
call sp_ProbarError('Prueba 5a: editar un checkpoint', 'update AvanceTareas set PorcentajeNuevo = 90 where IdAvance = 1');
call sp_ProbarError('Prueba 5b: borrar un checkpoint', 'delete from AvanceTareas where IdAvance = 1');

-- PRUEBA 6: no se puede saltar un checkpoint ni finalizar sin llegar a 100 % (esperado: Correcto, Correcto)
call sp_ProbarError('Prueba 6a: porcentaje anterior falso',
'insert into AvanceTareas (IdTarea, IdUsuario, PorcentajeAnterior, PorcentajeNuevo, EstadoAnterior, EstadoNuevo) values (2, 3, 50, 80, ''En progreso'', ''En progreso'')');
call sp_ProbarError('Prueba 6b: finalizar sin 100 %',
'insert into AvanceTareas (IdTarea, IdUsuario, PorcentajeAnterior, PorcentajeNuevo, EstadoAnterior, EstadoNuevo) values (2, 3, 10, 90, ''En progreso'', ''Finalizado'')');

-- PRUEBA 7: aprobación formal; un rechazo exige comentario (esperado: Correcto)
insert into CriteriosAceptacion (IdHistoria, Descripcion) values (1, 'El pago se confirma en menos de 5 segundos');
insert into Aprobaciones (IdHistoria, IdTarea, IdAprobador, Resultado) values (1, 1, 2, 'Aprobado');
call sp_ProbarError('Prueba 7: rechazar sin comentario',
'insert into Aprobaciones (IdHistoria, IdCriterio, IdAprobador, Resultado) values (1, 1, 1, ''Rechazado'')');

-- PRUEBA 8: las llaves foráneas funcionan (esperado: Correcto)
call sp_ProbarError('Prueba 8: tarea con historia inexistente',
'insert into Tareas (IdSprintHistoria, Titulo, PuntosEsfuerzo, HorasEstimadas, FechaEntregaEstimada) values (999, ''X'', 1, 1, ''2026-11-20'')');

-- PRUEBA 9: alertas (la tarea B sigue en curso)
select 'Prueba 9' as Prueba, IdTarea, Titulo, DiasSinMovimiento, Bloqueada, Atrasada from vw_AlertasTareas;

drop procedure sp_ProbarError;
