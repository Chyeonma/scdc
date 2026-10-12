import test from 'node:test';
import assert from 'node:assert/strict';
import { createDmRealtime } from '../src/dmRealtime.js';
import { mergeDmMessages } from '../src/dmHistory.js';

class Hub {
  state = 'Disconnected'; handlers = new Map(); calls = [];
  on(name, fn) { this.handlers.set(name, fn); }
  onclose(fn) { this.close = fn; }
  async start() { this.calls.push('start'); assert(this.handlers.has('MessageChanged')); this.state = 'Connected'; }
  async invoke(name, p) { this.calls.push([name,p.conversationId]); return { conversationId:p.conversationId, subscribed:name==='SubscribeConversation' }; }
  async stop() { this.state='Disconnected'; this.close?.(); }
  event(e) { this.handlers.get('MessageChanged')(e); }
}
const event = (version='1', conversationId='s') => ({ eventId:'event',conversationId,message:{id:'m',conversationId,author:{id:'a'},clientMessageId:'o',sequence:'9007199254740993',version,content:version==='3'?null:version} });
function setup(hub=new Hub()) {
  let rows=[{author:{id:'a'},clientMessageId:'o',status:'sending'}], actor=true;
  const states=[], revoked=[];
  const rt=createDmRealtime({connection:hub,isCurrent:()=>actor,getToken:()=>'',onState:s=>states.push(s),onRevoked:e=>revoked.push(e)});
  return {rt,hub,states,revoked,onMessage:e=>{rows=mergeDmMessages(rows,[e.message]);},rows:()=>rows,changeActor:()=>{actor=false;}};
}
test('event before history/HTTP reply buffers, correlates pending operation and preserves newest exact bigint version',async()=>{
  const s=setup();let loaded;
  const history=new Promise(resolve=>loaded=resolve);
  const selecting=s.rt.select('s',{onMessage:s.onMessage,loadHistory:()=>history});
  await new Promise(resolve=>setImmediate(resolve));
  s.hub.event(event('9007199254740993'));s.hub.event(event('9007199254740992'));assert.equal(s.rows()[0].status,'sending');
  loaded(true);await selecting;assert.equal(s.rows().length,1);assert.equal(s.rows()[0].version,'9007199254740993');
  const tombstone=event('9007199254740994');tombstone.message.content=null;
  s.hub.event(tombstone);s.hub.event(event('9007199254740993'));s.hub.event(tombstone);
  assert.equal(s.rows().length,1);assert.equal(s.rows()[0].content,null);assert.equal(s.rows()[0].sequence,'9007199254740993');
  await s.rt.stop();
});
test('switch unsubscribe precedes next subscribe; wrong conversation/actor and late frames are ignored',async()=>{
  const s=setup();const options={onMessage:s.onMessage,loadHistory:async()=>true};
  await s.rt.select('s',options);await s.rt.select('other',options);s.hub.event(event());assert.equal(s.rows()[0].status,'sending');
  assert.deepEqual(s.hub.calls,['start',['SubscribeConversation','s'],['UnsubscribeConversation','s'],['SubscribeConversation','other']]);
  s.changeActor();s.hub.event(event('1','other'));assert.equal(s.rows()[0].status,'sending');await s.rt.stop();
});
test('denied subscription does not fetch private history or pretend online; disconnect never replays a mutation',async()=>{
  const s=setup();let reads=0;
  s.hub.invoke=async()=>{throw new Error('denied');};
  await s.rt.select('s',{onMessage:s.onMessage,loadHistory:async()=>{reads++;return true;}});
  assert.equal(reads,0);assert.equal(s.states.at(-1),'offline');
  await s.hub.stop();assert.equal(s.states.at(-1),'offline');assert.equal(reads,0);await s.rt.stop();
});
test('session revoke clears subscription state and stops connection; malformed frames never enter the timeline',async()=>{
  const s=setup();await s.rt.select('s',{onMessage:s.onMessage,loadHistory:async()=>true});
  s.hub.event({...event(),message:{...event().message,version:1}});assert.equal(s.rows()[0].status,'sending');
  s.hub.handlers.get('AccessRevoked')({scope:'session',conversationId:null,reason:'session_revoked'});
  assert.equal(s.revoked.length,1);assert.equal(s.hub.state,'Disconnected');s.hub.event(event());assert.equal(s.rows()[0].status,'sending');await s.rt.stop();
});
