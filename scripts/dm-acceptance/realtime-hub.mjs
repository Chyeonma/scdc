// Local REST + actual SignalR WebSocket proof; tokens/DTO bodies remain in RAM.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import assert from 'node:assert/strict';
import signalR from '../../clients/WebClient/node_modules/@microsoft/signalr/dist/cjs/index.js';
const exec = promisify(execFile);
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const [run='p4be',id='C01']=process.argv.slice(2);
if(!/^[a-z0-9_-]{1,24}$/.test(run)||!['C01','C02','C03','C04'].includes(id))throw new Error('Use a dedicated run and C01-C04.');
const folder=path.join(root,'.dm-acceptance/runs',run);
const read=name=>JSON.parse(fs.readFileSync(path.join(folder,name),'utf8').replace(/^\uFEFF/,''));
const fixtures=read('realtime-fixtures.json'),pair=fixtures.pairs.find(p=>p.case===id),users=read('manifest.json').accounts;
const connections=[],sessions=[];
async function ps(action,extra=[]){return (await exec('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(root,'scripts/dm-acceptance/realtime.ps1'),'-Action',action,'-Run',run,'-Case',id,...extra],{cwd:root,encoding:'utf8',timeout:120000})).stdout;}
const snapshot=async()=>JSON.parse(await ps('Snapshot'));
async function api(auth,method,url,body){
 const r=await fetch('http://localhost:15026/api/v1'+url,{method,headers:{'Content-Type':'application/json',...(auth?{Authorization:'Bearer '+auth.accessToken}:{})},body:body?JSON.stringify(body):undefined});
 return{status:r.status,body:r.status===204?null:await r.json()};
}
async function actor(alias){
 const user=users.find(u=>u.alias===alias);const r=await api(null,'POST','/auth/login',{login:user.username,password:'DmDemo2026!Local',deviceName:'DM-P4-T01-HubProof'});assert.equal(r.status,200);sessions.push(r.body);return r.body;
}
function connect(auth){
 const hub=new signalR.HubConnectionBuilder().withUrl('http://localhost:15026/hubs/chat',{accessTokenFactory:()=>auth.accessToken,transport:signalR.HttpTransportType.WebSockets,skipNegotiation:true}).configureLogging(signalR.LogLevel.None).build();
 const events=[];hub.on('MessageChanged',payload=>events.push({payload,receivedAt:Date.now()}));const entry={hub,events};connections.push(entry);return entry;
}
async function waitEvent(entry,mid,count=1){
 const until=Date.now()+10000;
 while(Date.now()<until){if(entry.events.filter(e=>e.payload.message.id===mid).length>=count)return entry.events.filter(e=>e.payload.message.id===mid).at(-1);await new Promise(r=>setTimeout(r,25));}
 throw new Error('Expected committed Hub event not observed');
}
async function published(){
 const until=Date.now()+10000;while(Date.now()<until){const s=await snapshot();if(s.pending===0)return s;await new Promise(r=>setTimeout(r,50));}throw new Error('Outbox did not finish');
}
function delta(before,after,n){for(const key of ['messages','operations','outbox'])assert.equal(after[key]-before[key],n);assert.equal(BigInt(after.counter)-BigInt(before.counter),BigInt(n));assert.equal(after.bodyCopies,0);}
function summary(e){const p=e.payload;return{eventId:p.eventId,conversationId:p.conversationId,messageId:p.message.id,clientMessageId:p.message.clientMessageId,version:p.message.version,sequence:p.message.sequence,receivedAt:e.receivedAt};}
let held=false;
try{
 const before=await snapshot();assert.equal(before.messages,0,'Use a fresh case/run; no reset performed');
 const aa=await actor('A'),bb=await actor(pair.peerAlias),cc=await actor(id==='C02'?'S23':'C');
 const a=connect(aa),b=connect(bb),c=connect(cc);await Promise.all(connections.map(e=>e.hub.start()));
 const payload={conversationId:pair.conversationId};
 const aAck=await a.hub.invoke('SubscribeConversation',payload),bAck=await b.hub.invoke('SubscribeConversation',payload);assert.equal(aAck.subscribed,true);assert.equal(bAck.subscribed,true);
 let denial;try{await c.hub.invoke('SubscribeConversation',payload);}catch(e){const text=e.message;denial=JSON.parse(text.slice(text.indexOf('{'),text.lastIndexOf('}')+1));}
 assert.equal(denial.status,404);assert.equal(denial.errorCode,'RESOURCE_NOT_FOUND');
 if(id==='C02'){await ps('Pause');held=true;}
 if(id==='C04'){
  const ack=await b.hub.invoke('UnsubscribeConversation',payload);assert.equal(ack.subscribed,false);
  const peer=users.find(u=>u.alias==='S23');const other=await api(bb,'POST','/direct-conversations',{peerUserId:peer.id});assert.equal(other.status,200);
  await b.hub.invoke('SubscribeConversation',{conversationId:other.body.id});
 }
 const content={C01:'REALTIME-C01',C02:'WORKER-STOP-C02',C03:'EVENT-FIRST-C03',C04:'ROUTE-DENY-C04'}[id];
 const startedAt=Date.now();const sent=await api(aa,'POST',`/direct-conversations/${pair.conversationId}/messages`,{clientMessageId:pair.X,content});const responseAt=Date.now();assert.equal(sent.status,200);
 const mid=sent.body.id;let eventA,eventB;
 if(id==='C02'){
  const pending=await snapshot();delta(before,pending,1);assert.equal(pending.pending,1);
  const history=await api(bb,'GET',`/direct-conversations/${pair.conversationId}/messages`);assert.equal(history.status,200);assert.equal(history.body.items[0].id,mid);
  await b.hub.invoke('SubscribeConversation',payload);assert.equal(b.events.length,0);
  await new Promise(r=>setTimeout(r,30000));await ps('Resume');held=false;
 }
 eventA=await waitEvent(a,mid);
 for(const field of ['id','clientMessageId','sequence','version','conversationId'])assert.equal(eventA.payload.message[field],sent.body[field]);
 assert.equal(eventA.payload.conversationId,pair.conversationId);
 if(id!=='C04')eventB=await waitEvent(b,mid);
 else{
  await b.hub.invoke('UnsubscribeConversation',payload);assert.equal(b.events.length,0);
  assert.equal((await b.hub.invoke('SubscribeConversation',payload)).subscribed,true);
  const history=await api(bb,'GET',`/direct-conversations/${pair.conversationId}/messages`);
  assert.equal(history.status,200);assert.equal(history.body.items[0].id,mid);
 }
 if(eventB)for(const field of ['id','clientMessageId','sequence','version','conversationId'])assert.equal(eventB.payload.message[field],sent.body[field]);
 await c.hub.invoke('UnsubscribeConversation',payload);assert.equal(c.events.length,0);
 let after=await published();delta(before,after,1);const eid=after.events[0].id;
 if(id==='C02'||id==='C03'){
  if(id==='C03')await ps('Current',['-MessageId',mid,'-State','Edited']);
  await ps('Requeue',['-EventId',eid]);await waitEvent(b,mid,2);after=await published();delta(before,after,1);
  assert.equal(b.events.at(-1).payload.message.version,id==='C03'?'2':'1');
  if(id==='C03')assert.equal(b.events.at(-1).payload.message.content,'CURRENT-P4-C03');
 }
 const proof={run,case:id,pair,before,after,status:200,subscribeA:aAck,subscribeB:bAck,outsider:{status:denial.status,errorCode:denial.errorCode,received:0},startedAt,responseAt,
  commitConfirmedBy:'POST200; timestamps do not claim an exact database commit instant',a:summary(eventA),b:eventB?summary(eventB):null,
  republished:id==='C02'||id==='C03',result:'PASS actual REST/WebSocket/PostgreSQL',userResult:'Chua xac nhan'};
 fs.writeFileSync(path.join(folder,'hub-proof-'+id+'.json'),JSON.stringify(proof,null,2));console.log(JSON.stringify({run,case:id,result:proof.result,status:200,messageId:mid,records:after.messages,pending:after.pending}));
}finally{
 if(held)await ps('Resume');for(const e of connections)await e.hub.stop();for(const auth of sessions)await api(null,'POST','/auth/logout',{refreshToken:auth.refreshToken});
}
