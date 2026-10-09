[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Upgrade','Open','Snapshot','Concurrent','FaultOn','FaultOff','Smoke')][string]$Action,
    [string]$Run='baseline', [string]$ActorAlias='A', [string]$PeerAlias='B',
    [switch]$RequireNewPair, [int]$ExpectedStatus=200
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'Common.psm1') -Force -DisableNameChecking
Assert-DmDatabase
if ($Action -notin @('FaultOff','Snapshot')) { Assert-DmApiTarget }

function Invoke-TestSql([string]$Sql) {
    # Writes are restricted to the explicitly named acceptance database.
    $config=Get-DmConfig
    $Sql | & docker compose --project-name scdc-dm-acceptance --env-file (Join-Path (Get-DmPaths).Repo '.env.dm-test') -f (Join-Path (Get-DmPaths).Repo 'compose.dm-test.yaml') exec -T postgres psql -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
    if ($LASTEXITCODE -ne 0) { throw 'Acceptance SQL failed.' }
}

if ($Action -eq 'FaultOff') {
    Invoke-TestSql 'DROP TRIGGER IF EXISTS dm_acceptance_pair_fault ON messaging.direct_conversations; DROP FUNCTION IF EXISTS messaging.dm_acceptance_pair_fault();'
    Write-Host 'Acceptance fault removed.'
    exit
}

if ($Action -eq 'Upgrade') {
    Invoke-TestSql (Get-Content (Join-Path (Get-DmPaths).Repo 'database/postgres/migrations/20261009_dm_p1_open_conversation.sql') -Raw -Encoding UTF8)
    Invoke-DmCompose -Arguments @('build','chat-service','web-client')
    & (Join-Path $PSScriptRoot '../dm-acceptance.ps1') -Action Start -NoBuild
    exit
}

$manifest=Get-DmManifest $Run
$actor=@($manifest.accounts | Where-Object alias -eq $ActorAlias)
$peer=@($manifest.accounts | Where-Object alias -eq $PeerAlias)
if ($actor.Count -ne 1 -or $peer.Count -ne 1) { throw 'Unknown or ambiguous fixture alias.' }
$actorId=([guid]$actor[0].id).ToString(); $peerId=([guid]$peer[0].id).ToString()
# PostgreSQL uuid ordering is network byte order; do not sort Guid.ToByteArray().
$low,$high=@($actorId,$peerId | Sort-Object)
$pairWhere="user_low_id='$low' AND user_high_id='$high'"
function Get-Snapshot {
    $sql=@"
SELECT json_build_object(
 'actorId','$actorId','peerId','$peerId',
 'pairCount',(SELECT count(*) FROM messaging.direct_conversations WHERE $pairWhere),
 'spaceIds',(SELECT coalesce(json_agg(space_id),'[]') FROM messaging.direct_conversations WHERE $pairWhere),
 'memberCount',(SELECT count(*) FROM messaging.space_members WHERE space_id IN (SELECT space_id FROM messaging.direct_conversations WHERE $pairWhere)),
 'actorSpaceCount',(SELECT count(*) FROM messaging.spaces WHERE created_by_user_id='$actorId'),
 'orphanDirectSpaces',(SELECT count(*) FROM messaging.spaces s WHERE space_type=1 AND NOT EXISTS(SELECT 1 FROM messaging.direct_conversations d WHERE d.space_id=s.id)),
 'messageCount',(SELECT count(*) FROM messaging.messages WHERE space_id IN (SELECT space_id FROM messaging.direct_conversations WHERE $pairWhere))
)::text;
"@
    return (Invoke-DmReadSql $sql | ConvertFrom-Json)
}
$before=Get-Snapshot
if ($RequireNewPair -and $before.pairCount -ne 0) { throw 'Pair already exists. Choose an unused PeerAlias; no conversation deleted.' }

switch ($Action) {
    Snapshot { $before | ConvertTo-Json -Depth 6 }
    FaultOn {
        if ($before.pairCount -ne 0) { throw 'Fault requires an unused pair. Choose S11/S12/etc.' }
        Invoke-TestSql @"
CREATE OR REPLACE FUNCTION messaging.dm_acceptance_pair_fault() RETURNS trigger LANGUAGE plpgsql AS `$`$
BEGIN
 IF current_database()<>'scdc_dm_acceptance_test' THEN RAISE EXCEPTION 'Test database required'; END IF;
 IF NEW.user_low_id='$low' AND NEW.user_high_id='$high' THEN
  RAISE EXCEPTION USING ERRCODE='58000', MESSAGE='Acceptance pair insertion fault';
 END IF; RETURN NEW;
END; `$`$;
DROP TRIGGER IF EXISTS dm_acceptance_pair_fault ON messaging.direct_conversations;
CREATE TRIGGER dm_acceptance_pair_fault BEFORE INSERT ON messaging.direct_conversations
FOR EACH ROW EXECUTE FUNCTION messaging.dm_acceptance_pair_fault();
"@
        Write-Host "Fault enabled only for $ActorAlias/$PeerAlias in acceptance DB. Run FaultOff even if testing fails."
    }
    Open {
        $session=Connect-DmActor $ActorAlias $Run 'DM-P1-T02-manual'
        try {
            $response=Invoke-DmRequest POST /direct-conversations @{peerUserId=$peerId} -AccessToken $session.accessToken
            Assert-DmStatus $response $ExpectedStatus 'Open DM'
            $response | ConvertTo-Json -Depth 7
            $after=Get-Snapshot
            if ($ExpectedStatus -eq 200) {
                if ($after.pairCount -ne 1 -or $after.memberCount -ne 2 -or $after.orphanDirectSpaces -ne 0) { throw 'Pair/member/orphan invariant failed.' }
            } elseif (($before | ConvertTo-Json -Compress) -cne ($after | ConvertTo-Json -Compress)) { throw 'Rejected request changed Messaging state.' }
            $after | ConvertTo-Json -Depth 6
        } finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
    }
    Concurrent {
        if (-not ('DmOpenBarrier' -as [type])) {
            Add-Type -ReferencedAssemblies System.Net.Http -TypeDefinition @'
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
public static class DmOpenBarrier {
 public static async Task<string[]> Run(string root,string aToken,string bToken,string aId,string bId) {
  ServicePointManager.DefaultConnectionLimit=64;
  using(var client=new HttpClient()) {
   client.Timeout=TimeSpan.FromSeconds(90);
   var barrier=new TaskCompletionSource<bool>(); var tasks=new Task<string>[40];
   for(int i=0;i<40;i++) { var reverse=i%2==1; tasks[i]=Send(client,barrier.Task,root,reverse?bToken:aToken,reverse?aId:bId); }
   barrier.SetResult(true); return await Task.WhenAll(tasks);
  }
 }
 static async Task<string> Send(HttpClient client,Task barrier,string root,string token,string peer) {
  await barrier; using(var request=new HttpRequestMessage(HttpMethod.Post,root+"/direct-conversations")) {
   request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
   request.Content=new StringContent("{\"peerUserId\":\""+peer+"\"}",System.Text.Encoding.UTF8,"application/json");
   using(var response=await client.SendAsync(request)) {
    if((int)response.StatusCode!=200) throw new Exception("Open status "+(int)response.StatusCode);
    return await response.Content.ReadAsStringAsync();
   }
  }
 }
}
'@
        }
        $a=Connect-DmActor $ActorAlias $Run 'DM-P1-T02-barrier-A'
        $b=Connect-DmActor $PeerAlias $Run 'DM-P1-T02-barrier-B'
        try {
            $urls=Get-DmUrls
            $json=[DmOpenBarrier]::Run($urls.Api,$a.accessToken,$b.accessToken,$actorId,$peerId).GetAwaiter().GetResult()
            $ids=@($json | ForEach-Object { ($_ | ConvertFrom-Json).id } | Sort-Object -Unique)
            $after=Get-Snapshot
            if ($ids.Count -ne 1 -or $after.pairCount -ne 1 -or $after.memberCount -ne 2 -or $after.orphanDirectSpaces -ne 0) { throw 'Concurrent invariant failed.' }
            $proof=@{task='DM-P1-T02';test='C02';requests=40;directions=@(20,20);conversationId=$ids[0];before=$before;after=$after;userResult='Chưa xác nhận'}
            Write-DmJson (Join-Path (Get-DmRunPath $Run) "p1-t02-concurrent-$ActorAlias-$PeerAlias.json") $proof
            $proof | ConvertTo-Json -Depth 7
        } finally { foreach ($session in $a,$b) { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} } }
    }
    Smoke {
        & $PSCommandPath -Action Open -Run $Run -ActorAlias A -PeerAlias B
        & $PSCommandPath -Action Open -Run $Run -ActorAlias B -PeerAlias A
        & $PSCommandPath -Action Open -Run $Run -ActorAlias A -PeerAlias A -ExpectedStatus 400
        & $PSCommandPath -Action Open -Run $Run -ActorAlias A -PeerAlias U -ExpectedStatus 404
    }
}
