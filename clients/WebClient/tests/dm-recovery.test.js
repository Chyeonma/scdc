import test from 'node:test';
import assert from 'node:assert/strict';
import { recoverDmHistory } from '../src/dmRecovery.js';
import { mergeDmMessages } from '../src/dmHistory.js';
const message=(n,version='1')=>({id:String(n),sequence:String(n),version,author:{id:'a'},clientMessageId:String(n),content:version});
test('101 catchup preserves each merged frontier, reloads visible old window and never advances from live frames',async()=>{
 let cache={rows:[message(1)],resumeCursor:'resume1'},calls=[];
 const read=async q=>{
  calls.push(q);
  if(q.after==='resume1')return{items:Array.from({length:50},(_,i)=>message(i+2)),hasMore:true,nextCursor:'page51',throughSequence:'102'};
  if(q.after==='page51')return{items:Array.from({length:50},(_,i)=>message(i+52)),hasMore:true,nextCursor:'page101',throughSequence:'102'};
  if(q.after==='page101')return{items:[message(102)],hasMore:false,resumeCursor:'resume102',throughSequence:'102'};
  if(!q.before)return{items:Array.from({length:50},(_,i)=>message(i+53)),hasMore:true,nextCursor:'before53',throughSequence:'102'};
  return{items:Array.from({length:52},(_,i)=>message(i+1,i===0?'2':'1')),hasMore:false,nextCursor:null,throughSequence:'102'};
 };
 const merge=items=>{cache.rows=mergeDmMessages(cache.rows,items);};
 const progress=p=>{assert(cache.rows.length>0);Object.assign(cache,p);};
 assert(await recoverDmHistory({cache,read,merge,progress,isCurrent:()=>true}));
 assert.equal(cache.rows.length,102);assert.equal(cache.rows[0].version,'2');assert.equal(cache.resumeCursor,'resume102');assert.equal(cache.nextCursor,null);
 assert.deepEqual(calls.slice(0,3),[{after:'resume1',through:null},{after:'page51',through:'102'},{after:'page101',through:'102'}]);
});
test('interruption keeps last merged page cursor; changed actor never merges a response',async()=>{
 const cache={rows:[],resumeCursor:'start'};let reads=0;
 const options={cache,isCurrent:()=>true,merge:items=>{cache.rows.push(...items);},progress:p=>Object.assign(cache,p),
  read:async()=>{if(++reads===2)throw new Error('offline');return{items:[message(1)],hasMore:true,nextCursor:'next',throughSequence:'101'};}};
 await assert.rejects(recoverDmHistory(options),/offline/);assert.equal(cache.recoveryCursor,'next');assert.equal(cache.rows.length,1);
 assert.equal(await recoverDmHistory({...options,isCurrent:()=>false,read:async()=>({items:[message(2)]})}),false);assert.equal(cache.rows.length,1);
});
