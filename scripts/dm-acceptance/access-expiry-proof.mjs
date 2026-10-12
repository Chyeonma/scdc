import fs from 'node:fs';
import path from 'node:path';
import assert from 'node:assert/strict';
import {fileURLToPath} from 'node:url';
import signalR from '../../clients/WebClient/node_modules/@microsoft/signalr/dist/cjs/index.js';
import {createDmRealtime} from '../../clients/WebClient/src/dmRealtime.js';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..'),run=process.argv[2]||'mvpagentc';
if(!/^[a-z0-9_-]{1,24}$/.test(run))throw new Error('Invalid run');
const folder=path.join(root,'.dm-acceptance/runs',run),read=n=>JSON.parse(fs.readFileSync(path.join(folder,n),'utf8').replace(/^\uFEFF/,''));
const p=read('mvp-fixtures.json').pairs.find(p=>p.case==='C01'),actor=read('manifest.json').accounts.find(u=>u.alias==='A');
const base='http://localhost:15027';let auth,rotations=0,reconnected=0,revoked=0; const states=[];
async function api(url,body){const r=await fetch(base+'/api/v1'+url,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});assert(r.ok,'Expected real auth API success');return r.status===204?null:r.json();}
const startedAt=Date.now();
auth=await api('/auth/login',{login:actor.username,password:'DmDemo2026!Local',deviceName:'MVP-natural-access-expiry'});
const sid=JSON.parse(Buffer.from(auth.accessToken.split('.')[1],'base64url').toString()).sid;
assert(Date.parse(auth.accessTokenExpiresAt)-startedAt<65000,'Use isolated host with AccessTokenMinutes=1');
const hub=new signalR.HubConnectionBuilder().withUrl(base+'/hubs/chat',{accessTokenFactory:async()=>{
 if(Date.parse(auth.accessTokenExpiresAt)-Date.now()<5000){auth={...auth,...await api('/auth/refresh',{refreshToken:auth.refreshToken})};rotations++;}return auth.accessToken;
},transport:signalR.HttpTransportType.WebSockets,skipNegotiation:true}).withAutomaticReconnect([0,500,1000,2000]).configureLogging(signalR.LogLevel.None).build();
const rt=createDmRealtime({connection:hub,isCurrent:()=>true,getToken:async()=>auth.accessToken,
 onState:state=>{states.push(state);if(state==='online')reconnected++;},onRevoked:()=>revoked++});
try{
 await rt.select(p.conversationId,{onMessage:()=>{},loadHistory:async()=>true});
 const end=Date.now()+90000;while(reconnected<2&&Date.now()<end)await new Promise(r=>setTimeout(r,50));
 assert(reconnected>=2);assert(rotations>0);assert.equal(revoked,0,'Access JWT expiry must not revoke the still-valid refresh session');
 assert.equal(JSON.parse(Buffer.from(auth.accessToken.split('.')[1],'base64url').toString()).sid,sid);
 const proof={run,case:'natural-access-expiry',startedAt,observedAt:Date.now(),sid,rotations,onlineTransitions:reconnected,states,revoked,writerRequests:0,result:'PASS real one-minute JWT expiry/refresh/resubscribe via product reconnect controller'};
 fs.writeFileSync(path.join(folder,'natural-access-expiry.json'),JSON.stringify(proof,null,2));console.log(JSON.stringify(proof));
}finally{await rt.stop();await api('/auth/logout',{refreshToken:auth.refreshToken});}
