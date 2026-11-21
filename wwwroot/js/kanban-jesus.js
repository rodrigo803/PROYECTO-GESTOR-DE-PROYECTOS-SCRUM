// HU-08: acciones reales con formulario POST y token antifalsificación.
(() => {
 const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
 async function enviar(url,data){const fd = new FormData();Object.entries(data).forEach(([k,v])=>fd.append(k,v??''));fd.append('__RequestVerificationToken',token||'');
   const r=await fetch(url,{method:'POST',body:fd,credentials:'same-origin',headers:{'X-Requested-With':'XMLHttpRequest'}});
   let payload={};try{payload=await r.json();}catch{};
   if(!r.ok||!payload.ok)throw Error(payload.mensaje||'No se pudo guardar la operación.');
   location.reload();
 }
 function error(e){alert(e.message||'No se pudo guardar.');}
 window.asignarTarea=async id=>{try{await enviar(window.kanbanUrls.asignar,{IdTarea:id,IdResponsable:document.getElementById(`responsable-${id}`).value});}catch(e){error(e)}};
 window.guardarAvance=async id=>{try{await enviar(window.kanbanUrls.mover,{IdTarea:id,EstadoNuevo:document.getElementById(`estado-${id}`).value,PorcentajeNuevo:document.getElementById(`porcentaje-${id}`).value,HorasTrabajadas:document.getElementById(`horas-${id}`).value,Justificacion:document.getElementById(`justificacion-${id}`).value});}catch(e){error(e)}};
 window.marcarTarea=async (id,checked,old)=>{
   const razon=!checked?prompt('Motivo para reabrir la tarea y registrar el retroceso:'):'';
   if(!checked&&!razon){location.reload();return;}
   try{await enviar(window.kanbanUrls.mover,{IdTarea:id,EstadoNuevo:checked?'Finalizado':'En progreso',PorcentajeNuevo:checked?100:Math.min(old,90),HorasTrabajadas:0,Justificacion:razon});}catch(e){error(e);location.reload();}
 };
 let dragged=null;
 document.querySelectorAll('.kanban-task').forEach(card=>{
   card.addEventListener('dragstart',e=>{dragged=card;e.dataTransfer.setData('text/plain',card.dataset.task);e.dataTransfer.effectAllowed='move';});
   card.addEventListener('dragend',()=>{dragged=null;document.querySelectorAll('.kanban-dropzone').forEach(x=>x.classList.remove('kanban-hover'));});
   card.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();bootstrap.Modal.getOrCreateInstance(document.querySelector(card.dataset.bsTarget)).show();}});
 });
 document.querySelectorAll('.kanban-dropzone').forEach(zone=>{
   zone.addEventListener('dragover',e=>{e.preventDefault();e.dataTransfer.dropEffect='move';zone.classList.add('kanban-hover');});
   zone.addEventListener('dragleave',()=>zone.classList.remove('kanban-hover'));
   zone.addEventListener('drop',async e=>{e.preventDefault();zone.classList.remove('kanban-hover');const card=dragged;dragged=null;if(!card)return;
      const nuevo=zone.dataset.drop, anterior=card.dataset.estado;if(nuevo===anterior)return;
      const previo=Number(card.dataset.avance);
      const porcentaje=nuevo==='Finalizado'?100:anterior==='Finalizado'?Math.min(previo,90):previo;
      let justificacion='';if(porcentaje<previo){justificacion=prompt('Motivo de retroceso de avance:')||'';if(!justificacion)return;}
      try{await enviar(window.kanbanUrls.mover,{IdTarea:card.dataset.task,EstadoNuevo:nuevo,PorcentajeNuevo:porcentaje,HorasTrabajadas:0,Justificacion:justificacion});}catch(err){error(err)}
   });
 });
 document.getElementById('filtroResponsable')?.addEventListener('change',e=>{let value=e.target.value;document.querySelectorAll('.kanban-task').forEach(c=>c.hidden=!!value&&c.dataset.responsable!==value);});
})();
