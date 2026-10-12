// Real API/PostgreSQL/SignalR acceptance. Auth/body DTOs remain in RAM, reports contain metadata only.
import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
import signalR from '../../clients/WebClient/node_modules/@microsoft/signalr/dist/cjs/index.js';
const exec=promisify(execFile),root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const [run='mvpbe',id='C01']=process.argv.slice(2);
if(!/^[a-z0-9_-]{1,24}$/.test(run)||!/^C(0[1-8]|10)$/.test(id))throw new Error('Use dedicated run and C01-C08 or C10; C09 is browser input plus DB proof.');
const folder=path.join(root,'.dm-acceptance/runs',run),read=n=>JSON.parse(fs.readFileSync(path.join(folder,n),'utf8').replace(/^\uFEFF/,''));
const users=read('manifest.json').accounts,pair=read('mvp-fixtures.json').pairs.find(p=>p.case===id);
const sessions=[],hubs=[];let paused=false,disabled=false;
async function ps(action,extra=[],file='mvp.ps1'){
 return(await exec('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'scripts/dm-acceptance',file),'-Action',action,'-Run',run,...(file==='mvp.ps1'?['-Case',id]:[]),...extra],{cwd:root,encoding:'utf8',timeout:120000})).stdout;
}
const snapshot=async()=>JSON.parse(await ps('Snapshot'));
async function api(auth,method,url,body){const r=await fetch('http://localhost:15026/api/v1'+url,{method,headers:{'Content-Type':'application/json',...(auth?{Authorization:'Bearer '+auth.accessToken}:{})},body:body?JSON.stringify(body):undefined});return{status:r.status,body:r.status===204?null:await r.json()};}
async function login(alias){const u=users.find(u=>u.alias===alias),r=await api(null,'POST','/auth/login',{login:u.username,password:'DmDemo2026!Local',deviceName:'MVP-BE-'+id});assert.equal(r.status,200);sessions.push(r.body);return r.body;}
function connect(auth){const hub=new signalR.HubConnectionBuilder().withUrl('http://localhost:15026/hubs/chat',{accessTokenFactory:()=>auth.accessToken,transport:signalR.HttpTransportType.WebSockets,skipNegotiation:true}).configureLogging(signalR.LogLevel.None).build();const entry={hub,events:[],revoked:[]};hub.on('MessageChanged',p=>entry.events.push({id:p.message.id,version:p.message.version,sequence:p.message.sequence,eventId:p.eventId,receivedAt:Date.now()}));hub.on('AccessRevoked',p=>entry.revoked.push({scope:p.scope,reason:p.reason,receivedAt:Date.now()}));hubs.push(entry);return entry;}
async function until(fn,ms=10000){const end=Date.now()+ms;while(Date.now()<end){if(await fn())return;await new Promise(r=>setTimeout(r,25));}throw new Error('Expected acceptance state not observed');}
const route=`/direct-conversations/${pair.conversationId}/messages`;
let before,after,details={};
try{
 before=await snapshot();assert.equal(before.messages,0,'Fresh run/case required; no reset performed');
 const aa=await login('A'),bb=await login(pair.peerAlias),cc=await login(id==='C02'?'S23':'C');
 const a=connect(aa),b=connect(bb),c=connect(cc);await Promise.all(hubs.map(e=>e.hub.start()));
 for(const entry of [a,b])assert.equal((await entry.hub.invoke('SubscribeConversation',{conversationId:pair.conversationId})).subscribed,true);
 let denial;try{await c.hub.invoke('SubscribeConversation',{conversationId:pair.conversationId});}catch(e){const t=e.message;denial=JSON.parse(t.slice(t.indexOf('{'),t.lastIndexOf('}')+1));}
 assert.equal(denial.status,404);assert.equal(denial.errorCode,'RESOURCE_NOT_FOUND');
 const send=(body,op=pair.X,auth=aa)=>api(auth,'POST',route,{clientMessageId:op,content:body});
 if(id==='C02'){
  const baseline=await api(bb,'GET',route);let cursor=baseline.body.resumeCursor;
  const ids=[];for(let i=1;i<=101;i++){const r=await send('RC-'+String(i).padStart(3,'0'),crypto.randomUUID());assert.equal(r.status,200);ids.push(r.body.id);}
  const readIds=[],sizes=[],throughs=[];let through=null;
  for(;;){const r=await api(bb,'GET',route+'?after='+encodeURIComponent(cursor)+(through?'&through='+through:''));assert.equal(r.status,200);sizes.push(r.body.items.length);throughs.push(r.body.throughSequence);readIds.push(...r.body.items.map(m=>m.id));if(!r.body.hasMore){assert(r.body.resumeCursor);break;}assert.equal(r.body.resumeCursor,null);cursor=r.body.nextCursor;through=r.body.throughSequence;}
  assert.deepEqual(sizes,[50,50,1]);assert.deepEqual(readIds,ids);assert.deepEqual(throughs,['101','101','101']);details={sizes,idsMatchWriter:true,through:'101'};
 }else if(id==='C08'){
  const a2=await login('A'),second=connect(a2);await second.hub.start();await second.hub.invoke('SubscribeConversation',{conversationId:pair.conversationId});
  const sid=JSON.parse(Buffer.from(a2.accessToken.split('.')[1],'base64url').toString()).sid;
  const startedAt=Date.now();const r=await api(aa,'DELETE','/auth/sessions/'+sid);assert.equal(r.status,204);const commitConfirmedAt=Date.now();await until(()=>second.revoked.length>0,5000);assert(Date.now()-startedAt<=5000);
  assert.equal((await send('AFTER-REVOKE')).status,200);await until(()=>a.events.length>0);assert.equal(second.events.length,0);
  const negotiate=await fetch('http://localhost:15026/hubs/chat/negotiate?negotiateVersion=1',{method:'POST',headers:{Authorization:'Bearer '+a2.accessToken}});assert.equal(negotiate.status,401);details={sid,startedAt,commitConfirmedAt,cutoffConservativeMs:second.revoked[0].receivedAt-startedAt,oldNegotiation:401,oldReceived:0};
 }else{
  if(id==='C06'){await ps('Pause',[],'realtime.ps1');paused=true;}
  const content={C01:'MVP-M01',C03:'EDIT-ORIGINAL',C04:'DELETE-ORIGINAL',C05:'RACE-ORIGINAL',C06:'WORKER-STOP-C06',C07:'PRIVATE-C07',C10:'K-ORIGINAL'}[id];
  const sent=await send(content);assert.equal(sent.status,200);const mid=sent.body.id,url=route+'/'+mid;
  if(id==='C03'){
   for(const[version,text]of[['1','EDIT-V2'],['2','EDIT-V3']]){const r=await api(aa,'PATCH',url,{expectedVersion:version,content:text});assert.equal(r.status,200);}
   const counts=await snapshot();assert.equal((await api(aa,'PATCH',url,{expectedVersion:'3',content:'EDIT-V3'})).status,200);const noop=await snapshot();assert.equal(noop.outbox,counts.outbox);
   assert.equal((await api(aa,'PATCH',url,{expectedVersion:'2',content:'stale'})).body.errorCode,'VERSION_CONFLICT');assert.equal((await api(aa,'PATCH',url,{expectedVersion:'3',content:'a'.repeat(2001)})).status,400);
   assert.equal((await api(bb,'PATCH',url,{expectedVersion:'3',content:'forbidden'})).body.errorCode,'AUTHOR_REQUIRED');details={mid,version:'3',noopOutboxUnchanged:true};
  }else if(id==='C04'){
   const deleted=await api(aa,'DELETE',url+'?expectedVersion=1');assert.equal(deleted.status,200);assert.equal(deleted.body.content,null);
   assert.equal((await api(aa,'DELETE',url+'?expectedVersion=1')).body.version,'2');assert.equal((await api(aa,'PATCH',url,{expectedVersion:'2',content:'resurrect'})).body.errorCode,'MESSAGE_DELETED');
   const replay=await send(content);assert.equal(replay.body.id,mid);assert.equal(replay.body.content,null);details={mid,version:'2',replayCurrentTombstone:true};
  }else if(id==='C05'){
   const results=await Promise.all([api(aa,'PATCH',url,{expectedVersion:'1',content:'PARALLEL-EDIT'}),api(aa,'DELETE',url+'?expectedVersion=1')]);assert.deepEqual(results.map(r=>r.status).sort(),[200,409]);details={mid,statuses:results.map(r=>r.status),deterministicBarrierProof:'Backend MessageMutationTests; this helper sends parallel requests, not a DB barrier.'};
  }else if(id==='C06'){
   assert.equal((await snapshot()).pending,1);assert.equal((await api(bb,'GET',route)).body.items[0].id,mid);assert.equal(b.events.length,0);
   await new Promise(r=>setTimeout(r,30000));assert.equal(b.events.length,0);await ps('Resume',[],'realtime.ps1');paused=false;await until(()=>b.events.some(e=>e.id===mid));details={mid,pauseMs:30000,history200WhilePaused:true};
  }else if(id==='C07'){
   const pending=users.find(u=>u.alias==='U');assert.equal((await api(null,'POST','/auth/login',{login:pending.username,password:'DmDemo2026!Local'})).status,403);
   for(const method of ['GET','POST','PATCH','DELETE']){const u=method==='GET'||method==='POST'?route:url+(method==='DELETE'?'?expectedVersion=1':'');const body=method==='POST'?{clientMessageId:pair.Y,content:'outsider'}:method==='PATCH'?{expectedVersion:'1',content:'outsider'}:undefined;const r=await api(cc,method,u,body);assert.equal(r.status,404);assert.equal(r.body.errorCode,'RESOURCE_NOT_FOUND');}
   assert.equal((await api(bb,'DELETE',url+'?expectedVersion=1')).status,403);details={mid,outsiderAll404:true,memberCannotDeleteAuthor:true,pendingLogin403:true};
  }else if(id==='C10'){
   await ps('State',['-Alias','K','-State','Disabled']);disabled=true;assert.equal((await api(bb,'GET',route)).status,401);assert.equal((await api(aa,'GET',route)).status,200);
   assert.equal((await api(aa,'PATCH',url,{expectedVersion:'1',content:'K-CURRENT'})).status,200);assert.equal((await api(aa,'DELETE',url+'?expectedVersion=2')).status,200);assert.equal((await send(content)).body.content,null);assert.equal((await send('NEW-BLOCKED',pair.Y)).status,404);
   await ps('State',['-Alias','K','-State','Unlock']);disabled=false;assert.equal((await api(bb,'GET','/users/me')).status,401);details={mid,readEditDeleteCurrentAllowed:true,newSend404:true,oldKAfterUnlock401:true};
  }else{
   await until(()=>b.events.some(e=>e.id===mid));const event=b.events.find(e=>e.id===mid);assert.equal(event.version,sent.body.version);assert.equal(event.sequence,sent.body.sequence);details={mid,event};
  }
 }
 await until(async()=>{after=await snapshot();return after.pending===0;},15000);assert.equal(after.bodyCopies,0);assert.equal(after.editHistory,0);assert.equal(after.operations,after.messages);assert.equal(after.createOutbox,after.messages);assert.equal(c.events.length,0);
 if(id!=='C02')assert.equal(after.messages,1);
 const evidence={run,case:id,pair,before,after,details,outsider:{status:404,errorCode:'RESOURCE_NOT_FOUND',frames:0},result:'PASS actual API/WebSocket/PostgreSQL',userResult:'Chua xac nhan'};
 fs.writeFileSync(path.join(folder,'mvp-backend-'+id+'.json'),JSON.stringify(evidence,null,2));console.log(JSON.stringify({run,case:id,result:evidence.result,messages:after.messages,outbox:after.outbox,pending:after.pending}));
}finally{
 if(paused)await ps('Resume',[],'realtime.ps1');if(disabled)await ps('State',['-Alias','K','-State','Unlock']);
 for(const e of hubs)await e.hub.stop();for(const auth of sessions)await api(null,'POST','/auth/logout',{refreshToken:auth.refreshToken});
}
