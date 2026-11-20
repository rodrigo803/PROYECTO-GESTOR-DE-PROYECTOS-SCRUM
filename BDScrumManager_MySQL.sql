-- =========================================================
-- SCRUM Manager - Script de creación de la base de datos (v2)
-- Motor: MySQL 8.0.16+ o MariaDB 10.4+ · Curso TI-162 · CUC 3-2026
-- v2: correcciones del profesor (avance ponderado, checkpoints
--     inmutables, horas y fechas reales, aprobaciones, alertas)
--
-- EN PLESK: crear la base de datos desde el panel (Bases de datos),
-- abrir phpMyAdmin, seleccionar la base e importar este archivo.
-- EN UNA INSTALACIÓN LOCAL: quitar el comentario de las 2 líneas siguientes.
-- =========================================================

-- create database BDScrumManager character set utf8mb4 collate utf8mb4_unicode_ci;
-- use BDScrumManager;

set names utf8mb4;

-- HU-01, HU-02: Roles del sistema que controlan el acceso y lo que cada quien puede ver.
create table Roles
(
IdRol int auto_increment primary key,
Nombre varchar(30) not null unique check (Nombre in ('Administrador', 'Product Owner', 'Scrum Master', 'Miembro del Equipo'))
) engine = InnoDB default charset = utf8mb4;

-- HU-01, HU-02: Personas que inician sesión en el sistema.
create table Usuarios
(
IdUsuario int auto_increment primary key,
Nombre varchar(50) not null,
Apellidos varchar(80) not null,
Correo varchar(120) not null unique,
ContrasenaHash varchar(255) not null,
IdRol int not null,
Activo boolean not null default true,
FechaRegistro datetime not null default current_timestamp,
UltimoAcceso datetime null,
constraint fk_Usuarios_IdRol foreign key (IdRol) references Roles (IdRol)
) engine = InnoDB default charset = utf8mb4;

-- HU-03: Proyectos SCRUM de la organización.
create table Proyectos
(
IdProyecto int auto_increment primary key,
Codigo varchar(10) not null unique,
Nombre varchar(100) not null unique,
Descripcion varchar(500) not null,
FechaInicio date not null,
FechaFin date not null,
Estado varchar(15) not null check (Estado in ('Planificado', 'Activo', 'En pausa', 'Cerrado')),
MotivoCierre varchar(300) null,
FechaCierre date null,
constraint ck_Proyectos_Fechas check (FechaInicio < FechaFin)
) engine = InnoDB default charset = utf8mb4;

-- HU-03: Integrantes de cada proyecto y su rol SCRUM dentro de él.
create table ProyectoMiembros
(
IdProyectoMiembro int auto_increment primary key,
IdProyecto int not null,
IdUsuario int not null,
RolProyecto varchar(20) not null check (RolProyecto in ('Product Owner', 'Scrum Master', 'Developer', 'Stakeholder')),
FechaIngreso date not null default (curdate()),
unique (IdProyecto, IdUsuario),
constraint fk_ProyectoMiembros_IdProyecto foreign key (IdProyecto) references Proyectos (IdProyecto),
constraint fk_ProyectoMiembros_IdUsuario foreign key (IdUsuario) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-04: Product Backlog: historias con formato Como / Quiero / Para.
create table HistoriasUsuario
(
IdHistoria int auto_increment primary key,
IdProyecto int not null,
Codigo varchar(10) not null,
Como varchar(100) not null,
Quiero varchar(300) not null,
Para varchar(300) not null,
Prioridad varchar(10) not null check (Prioridad in ('Alta', 'Media', 'Baja')),
OrdenPrioridad int not null,
Estimacion int null check (Estimacion > 0),
Estado varchar(20) not null check (Estado in ('Pendiente', 'Lista', 'En Sprint', 'Terminada')),
unique (IdProyecto, Codigo),
constraint fk_HistoriasUsuario_IdProyecto foreign key (IdProyecto) references Proyectos (IdProyecto)
) engine = InnoDB default charset = utf8mb4;

-- HU-04: Criterios de aceptación de cada historia. Se dan por cumplidos con una aprobación.
create table CriteriosAceptacion
(
IdCriterio int auto_increment primary key,
IdHistoria int not null,
Descripcion varchar(300) not null,
constraint fk_CriteriosAceptacion_IdHistoria foreign key (IdHistoria) references HistoriasUsuario (IdHistoria)
) engine = InnoDB default charset = utf8mb4;

-- HU-05: Iteraciones de cada proyecto.
create table Sprints
(
IdSprint int auto_increment primary key,
IdProyecto int not null,
Nombre varchar(60) not null,
Objetivo varchar(300) not null,
FechaInicio date not null,
FechaFin date not null,
CapacidadPuntos int not null check (CapacidadPuntos > 0),
Estado varchar(15) not null check (Estado in ('Planificado', 'Activo', 'Cerrado')),
constraint ck_Sprints_Fechas check (FechaInicio < FechaFin),
constraint fk_Sprints_IdProyecto foreign key (IdProyecto) references Proyectos (IdProyecto)
) engine = InnoDB default charset = utf8mb4;

-- HU-06: Sprint Backlog: historias comprometidas en cada Sprint.
create table SprintHistorias
(
IdSprintHistoria int auto_increment primary key,
IdSprint int not null,
IdHistoria int not null,
FechaCompromiso datetime not null default current_timestamp,
Resultado varchar(15) not null check (Resultado in ('Comprometida', 'Terminada', 'Devuelta', 'Replanificada')),
unique (IdSprint, IdHistoria),
constraint fk_SprintHistorias_IdSprint foreign key (IdSprint) references Sprints (IdSprint),
constraint fk_SprintHistorias_IdHistoria foreign key (IdHistoria) references HistoriasUsuario (IdHistoria)
) engine = InnoDB default charset = utf8mb4;

-- HU-07, HU-08: Tareas de cada historia comprometida, con esfuerzo, horas y avance.
create table Tareas
(
IdTarea int auto_increment primary key,
IdSprintHistoria int not null,
Titulo varchar(150) not null,
Descripcion varchar(500) null,
IdResponsable int null,
PuntosEsfuerzo int not null check (PuntosEsfuerzo > 0),
HorasEstimadas decimal(5,1) not null check (HorasEstimadas > 0),
HorasReales decimal(6,1) not null default 0 check (HorasReales >= 0),
PorcentajeAvance int not null default 0 check (PorcentajeAvance between 0 and 100),
Estado varchar(15) not null default 'Pendiente' check (Estado in ('Pendiente', 'En progreso', 'En revisión', 'Finalizado')),
FechaRegistro datetime not null default current_timestamp,
FechaEntregaEstimada date not null,
FechaEntregaReal datetime null,
constraint ck_Tareas_FinalizadaAl100 check (Estado <> 'Finalizado' or PorcentajeAvance = 100),
constraint fk_Tareas_IdSprintHistoria foreign key (IdSprintHistoria) references SprintHistorias (IdSprintHistoria),
constraint fk_Tareas_IdResponsable foreign key (IdResponsable) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-09: Bloqueos que afectan el avance del Sprint.
create table Impedimentos
(
IdImpedimento int auto_increment primary key,
IdSprint int not null,
IdHistoria int null,
IdTarea int null,
Titulo varchar(150) not null,
Descripcion varchar(500) not null,
Severidad varchar(10) not null check (Severidad in ('Crítica', 'Alta', 'Media', 'Baja')),
Estado varchar(15) not null check (Estado in ('Abierto', 'En atención', 'Resuelto')),
IdReportadoPor int not null,
IdResponsable int null,
FechaReporte datetime not null default current_timestamp,
CausaRaiz varchar(300) null,
Solucion varchar(500) null,
FechaResolucion datetime null,
constraint ck_Impedimentos_ResueltoDocumentado check (Estado <> 'Resuelto' or (CausaRaiz is not null and Solucion is not null and FechaResolucion is not null)),
constraint ck_Impedimentos_ConResponsable check (Estado = 'Abierto' or IdResponsable is not null),
constraint fk_Impedimentos_IdSprint foreign key (IdSprint) references Sprints (IdSprint),
constraint fk_Impedimentos_IdHistoria foreign key (IdHistoria) references HistoriasUsuario (IdHistoria),
constraint fk_Impedimentos_IdTarea foreign key (IdTarea) references Tareas (IdTarea),
constraint fk_Impedimentos_IdReportadoPor foreign key (IdReportadoPor) references Usuarios (IdUsuario),
constraint fk_Impedimentos_IdResponsable foreign key (IdResponsable) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-07, HU-08, HU-10 (NUEVA): Checkpoints de avance: cada reporte queda guardado y no se puede editar ni borrar.
create table AvanceTareas
(
IdAvance int auto_increment primary key,
IdTarea int not null,
IdUsuario int not null,
Fecha datetime not null default current_timestamp,
PorcentajeAnterior int not null check (PorcentajeAnterior between 0 and 100),
PorcentajeNuevo int not null check (PorcentajeNuevo between 0 and 100),
HorasTrabajadas decimal(5,1) not null default 0 check (HorasTrabajadas >= 0),
EstadoAnterior varchar(15) null check (EstadoAnterior in ('Pendiente', 'En progreso', 'En revisión', 'Finalizado')),
EstadoNuevo varchar(15) not null check (EstadoNuevo in ('Pendiente', 'En progreso', 'En revisión', 'Finalizado')),
Justificacion varchar(300) null,
IdImpedimento int null,
constraint ck_AvanceTareas_RetrocesoJustificado check (PorcentajeNuevo >= PorcentajeAnterior or Justificacion is not null or IdImpedimento is not null),
constraint fk_AvanceTareas_IdTarea foreign key (IdTarea) references Tareas (IdTarea),
constraint fk_AvanceTareas_IdUsuario foreign key (IdUsuario) references Usuarios (IdUsuario),
constraint fk_AvanceTareas_IdImpedimento foreign key (IdImpedimento) references Impedimentos (IdImpedimento)
) engine = InnoDB default charset = utf8mb4;

-- HU-09: Línea de tiempo del seguimiento de cada impedimento.
create table SeguimientoImpedimentos
(
IdSeguimiento int auto_increment primary key,
IdImpedimento int not null,
IdUsuario int not null,
Comentario varchar(500) not null,
EstadoNuevo varchar(15) null check (EstadoNuevo in ('Abierto', 'En atención', 'Resuelto')),
Fecha datetime not null default current_timestamp,
constraint fk_SeguimientoImpedimentos_IdImpedimento foreign key (IdImpedimento) references Impedimentos (IdImpedimento),
constraint fk_SeguimientoImpedimentos_IdUsuario foreign key (IdUsuario) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-04, HU-07 (NUEVA): Aprobación formal de historias, criterios y tareas: quién aprueba, cuándo y por qué.
create table Aprobaciones
(
IdAprobacion int auto_increment primary key,
IdHistoria int not null,
IdCriterio int null,
IdTarea int null,
IdAprobador int not null,
Resultado varchar(10) not null check (Resultado in ('Aprobado', 'Rechazado')),
Comentario varchar(500) null,
Fecha datetime not null default current_timestamp,
constraint ck_Aprobaciones_RechazoConComentario check (Resultado = 'Aprobado' or Comentario is not null),
constraint fk_Aprobaciones_IdHistoria foreign key (IdHistoria) references HistoriasUsuario (IdHistoria),
constraint fk_Aprobaciones_IdCriterio foreign key (IdCriterio) references CriteriosAceptacion (IdCriterio),
constraint fk_Aprobaciones_IdTarea foreign key (IdTarea) references Tareas (IdTarea),
constraint fk_Aprobaciones_IdAprobador foreign key (IdAprobador) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-11: Retrospectiva realizada al cerrar cada Sprint.
create table Retrospectivas
(
IdRetrospectiva int auto_increment primary key,
IdSprint int not null unique,
Fecha date not null,
IdFacilitador int not null,
constraint fk_Retrospectivas_IdSprint foreign key (IdSprint) references Sprints (IdSprint),
constraint fk_Retrospectivas_IdFacilitador foreign key (IdFacilitador) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-11: Aspectos positivos, oportunidades de mejora y acciones acordadas.
create table ElementosRetrospectiva
(
IdElemento int auto_increment primary key,
IdRetrospectiva int not null,
Tipo varchar(10) not null check (Tipo in ('Positivo', 'Mejora', 'Acción')),
Descripcion varchar(300) not null,
IdResponsable int null,
FechaCompromiso date null,
Estado varchar(15) null check (Estado in ('Pendiente', 'En curso', 'Cumplida')),
constraint fk_ElementosRetrospectiva_IdRetrospectiva foreign key (IdRetrospectiva) references Retrospectivas (IdRetrospectiva),
constraint fk_ElementosRetrospectiva_IdResponsable foreign key (IdResponsable) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- HU-01, Regla de negocio: Registro de los cambios relevantes y de quién los hizo.
create table Auditoria
(
IdAuditoria int auto_increment primary key,
IdUsuario int not null,
Accion varchar(30) not null,
Entidad varchar(40) not null,
IdRegistro int null,
Detalle varchar(500) null,
Fecha datetime not null default current_timestamp,
constraint fk_Auditoria_IdUsuario foreign key (IdUsuario) references Usuarios (IdUsuario)
) engine = InnoDB default charset = utf8mb4;

-- Datos base: roles del sistema
insert into Roles (Nombre) values ('Administrador'), ('Product Owner'), ('Scrum Master'), ('Miembro del Equipo');

-- =========================================================
-- Checkpoints de avance (corrección del profesor)
-- AvanceTareas solo admite inserciones: un avance reportado
-- no se puede editar ni borrar. Si hay un error, se registra
-- un avance nuevo con su justificación.
-- =========================================================

DELIMITER //

create trigger tr_AvanceTareas_NoEditar
before update on AvanceTareas
for each row
begin
signal sqlstate '45000' set message_text = 'Los registros de avance no se pueden modificar ni eliminar. Registre un nuevo avance con su justificacion.';
end//

create trigger tr_AvanceTareas_NoBorrar
before delete on AvanceTareas
for each row
begin
signal sqlstate '45000' set message_text = 'Los registros de avance no se pueden modificar ni eliminar. Registre un nuevo avance con su justificacion.';
end//

-- El porcentaje anterior debe coincidir con el avance actual de la tarea,
-- para que nadie salte o invente checkpoints.
create trigger tr_AvanceTareas_Validar
before insert on AvanceTareas
for each row
begin
if new.PorcentajeAnterior <> (select PorcentajeAvance from Tareas where IdTarea = new.IdTarea) then
signal sqlstate '45000' set message_text = 'El porcentaje anterior no coincide con el avance actual de la tarea.';
end if;
end//

-- Al registrar un avance se actualiza la tarea: porcentaje, estado,
-- horas reales y fecha real de entrega.
create trigger tr_AvanceTareas_Registrar
after insert on AvanceTareas
for each row
begin
update Tareas
set PorcentajeAvance = new.PorcentajeNuevo,
Estado = new.EstadoNuevo,
HorasReales = HorasReales + new.HorasTrabajadas,
FechaEntregaReal = case when new.PorcentajeNuevo = 100 then new.Fecha else null end
where IdTarea = new.IdTarea;
end//

DELIMITER ;

-- =========================================================
-- Vistas para el dashboard (HU-10). El avance se calcula,
-- nunca se digita. El peso de cada tarea es PuntosEsfuerzo.
-- =========================================================

-- Avance de cada historia del Sprint: ponderado por esfuerzo y simple (para comparar)
create view vw_AvanceHistoria as
select sh.IdSprintHistoria, sh.IdSprint, h.IdHistoria, h.Codigo, h.Estimacion,
count(t.IdTarea) as TotalTareas,
coalesce(sum(t.PuntosEsfuerzo), 0) as PuntosTotales,
cast(case when coalesce(sum(t.PuntosEsfuerzo), 0) = 0 then 0
else sum(t.PuntosEsfuerzo * t.PorcentajeAvance) / sum(t.PuntosEsfuerzo) end as decimal(5,1)) as AvancePonderado,
cast(coalesce(avg(t.PorcentajeAvance), 0) as decimal(5,1)) as AvanceSimple,
coalesce(sum(t.HorasEstimadas), 0) as HorasEstimadas,
coalesce(sum(t.HorasReales), 0) as HorasReales
from SprintHistorias sh
join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
left join Tareas t on t.IdSprintHistoria = sh.IdSprintHistoria
group by sh.IdSprintHistoria, sh.IdSprint, h.IdHistoria, h.Codigo, h.Estimacion;

-- Avance total del Sprint y conteo de tareas por estado
create view vw_AvanceSprint as
select s.IdSprint, s.Nombre, s.Estado, s.CapacidadPuntos,
count(t.IdTarea) as TotalTareas,
sum(case when t.Estado = 'Pendiente' then 1 else 0 end) as Pendientes,
sum(case when t.Estado = 'En progreso' then 1 else 0 end) as EnProgreso,
sum(case when t.Estado = 'En revisión' then 1 else 0 end) as EnRevision,
sum(case when t.Estado = 'Finalizado' then 1 else 0 end) as Finalizadas,
cast(case when coalesce(sum(t.PuntosEsfuerzo), 0) = 0 then 0
else sum(t.PuntosEsfuerzo * t.PorcentajeAvance) / sum(t.PuntosEsfuerzo) end as decimal(5,1)) as AvancePonderado,
coalesce(sum(t.HorasEstimadas), 0) as HorasEstimadas,
coalesce(sum(t.HorasReales), 0) as HorasReales
from Sprints s
left join SprintHistorias sh on sh.IdSprint = s.IdSprint
left join Tareas t on t.IdSprintHistoria = sh.IdSprintHistoria
group by s.IdSprint, s.Nombre, s.Estado, s.CapacidadPuntos;

-- Avance acumulado por semana del Sprint, a partir de los checkpoints (comparativo semanal)
create view vw_AvanceSemanal as
select x.IdSprint, x.Semana, x.TotalPuntos,
cast(x.PuntosGanados as decimal(7,2)) as PuntosGanadosSemana,
cast(100 * sum(x.PuntosGanados) over (partition by x.IdSprint order by x.Semana rows unbounded preceding)
/ nullif(x.TotalPuntos, 0) as decimal(5,1)) as AvanceAcumulado
from (
select sh.IdSprint, datediff(a.Fecha, s.FechaInicio) div 7 + 1 as Semana, tp.TotalPuntos,
sum(t.PuntosEsfuerzo * (a.PorcentajeNuevo - a.PorcentajeAnterior) / 100) as PuntosGanados
from AvanceTareas a
join Tareas t on t.IdTarea = a.IdTarea
join SprintHistorias sh on sh.IdSprintHistoria = t.IdSprintHistoria
join Sprints s on s.IdSprint = sh.IdSprint
join (select sh2.IdSprint, sum(t2.PuntosEsfuerzo) as TotalPuntos
from Tareas t2 join SprintHistorias sh2 on sh2.IdSprintHistoria = t2.IdSprintHistoria
group by sh2.IdSprint) tp on tp.IdSprint = sh.IdSprint
group by sh.IdSprint, datediff(a.Fecha, s.FechaInicio) div 7 + 1, tp.TotalPuntos
) as x;

-- Alertas para Scrum Master y Product Owner: tareas sin movimiento, bloqueadas o atrasadas
create view vw_AlertasTareas as
select t.IdTarea, t.Titulo, sh.IdSprint, t.IdResponsable, t.Estado, t.PorcentajeAvance,
coalesce(ua.UltimoAvance, t.FechaRegistro) as UltimoMovimiento,
datediff(now(), coalesce(ua.UltimoAvance, t.FechaRegistro)) as DiasSinMovimiento,
case when exists (select 1 from Impedimentos i where i.IdTarea = t.IdTarea and i.Estado <> 'Resuelto') then 1 else 0 end as Bloqueada,
case when t.FechaEntregaEstimada < curdate() then 1 else 0 end as Atrasada
from Tareas t
join SprintHistorias sh on sh.IdSprintHistoria = t.IdSprintHistoria
join Sprints s on s.IdSprint = sh.IdSprint
left join (select IdTarea, max(Fecha) as UltimoAvance from AvanceTareas group by IdTarea) ua on ua.IdTarea = t.IdTarea
where s.Estado = 'Activo' and t.Estado <> 'Finalizado';
