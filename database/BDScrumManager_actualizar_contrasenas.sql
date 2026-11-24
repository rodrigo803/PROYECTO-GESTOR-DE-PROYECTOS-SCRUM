/*
SCRUM Manager - Actualiza ContrasenaHash de los 10 usuarios iniciales (HU-01)
Creado por Sergio Monge
Fecha: 22/11/2026

Los 10 usuarios se insertaron con ContrasenaHash = 'PENDIENTE-HU01' (marcador temporal)
porque HU-01 (login) no estaba lista. Este script lo reemplaza por un hash BCrypt real
(BCrypt.Net-Next, el mismo algoritmo con el que HU-02 guarda y actualiza ContrasenaHash)
de la contraseña de demostración "Scrum2026!" para los 10 usuarios.

Ejecutar en Plesk (phpMyAdmin, pestaña SQL) y en tu base local, UNA SOLA VEZ, sobre
una base que ya tenga esos 10 correos (BDScrumManager_MySQL_datos_iniciales.sql y/o
BDScrumManager_MySQL_datos_prueba.sql ya importados).

Contraseña de demostración para los 10 usuarios: Scrum2026!
*/

set names utf8mb4;

update Usuarios set ContrasenaHash = '$2a$11$wtv4ly8.Mo2awZQg7.wSG.Rt2z70dcTtejMwZpzjTkJMCiTIlobf.' where Correo = 'admin@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$LNcVU15u6Ap4oZFJVmPhhuO7XWRyA/Cv4uAA1HesdGx3A6MtMtGaS' where Correo = 'valeria.castro@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$zYGhtDfUJm1NMgeQDa9YAukehhf8c0CPiuOVrIJHyZX5W443skFh6' where Correo = 'rodrigo.morales@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$jgkAFjwD3cXwUYPsZfx6H.N1jjKAO6/7o.lNqXUF1xSByHD3GjKi.' where Correo = 'carlos.ruiz@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$K4rwu246i.KwdVEZDjBwsu69D0w01HnZCq9ihgiYV68vJ6IEF.G/e' where Correo = 'ana.gomez@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$sm0Wdfu4ZzoZYCksH8/fOuGFHS.YLXBnx9en/fdu6lpUNzij8uQ3S' where Correo = 'mateo.silva@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$Ewn8yYk9kLnQJBiZOzidLezlYMnenrQLmc7B4FCEzYc5zv4rSYGTe' where Correo = 'lucia.morales@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$4V4Kfnk23KD8CcaTaH0LSu80UaKx3r2UHyZmINErYimhxsguUW8Ry' where Correo = 'jesus.delima@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$QYkqq5B2FtD9JZsqMCMdAOd/Wy1wN12/bqTF7OLxcmNattA18CbCa' where Correo = 'daniela.vargas@scrummanager.test';
update Usuarios set ContrasenaHash = '$2a$11$XJvNQ4yH2/FE/Xp65SUQ8.qyj/kdeYIcnIGoThPOExOWckYRB.WKq' where Correo = 'andres.solis@scrummanager.test';
