#!/usr/bin/env python3
"""Genera un UPDATE MySQL para una contraseña segura ASP.NET Identity V3.
Ejecuta localmente y pega el SQL en phpMyAdmin; nunca subas la salida a Git.
"""
import base64, getpass, hashlib, os, re, struct
correo=input('Correo del usuario existente en Usuarios: ').strip()
if not re.fullmatch(r"[A-Za-z0-9_.+\-]+@[A-Za-z0-9.\-]+", correo):
    raise SystemExit('Correo no válido')
clave=getpass.getpass('Contraseña nueva (mínimo 14 caracteres): ')
confirmacion=getpass.getpass('Confirmar contraseña: ')
if clave!=confirmacion or len(clave)<14:
    raise SystemExit('Las contraseñas no coinciden o tienen menos de 14 caracteres')
salt=os.urandom(16)
subkey=hashlib.pbkdf2_hmac('sha512',clave.encode('utf8'),salt,100_000,dklen=32)
hash_b64=base64.b64encode(b'\x01'+struct.pack('>III',2,100_000,16)+salt+subkey).decode('ascii')
print('\nEjecuta esta sentencia en tu base BDScrumManager (sin subirla al repositorio):\n')
print(f"update Usuarios set ContrasenaHash='{hash_b64}' where Correo='{correo}';")
print('\nEl hash es compatible con PasswordHasher de ASP.NET Core; solo podrás ingresar usando esa contraseña.')
