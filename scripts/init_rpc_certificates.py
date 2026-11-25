#!/usr/bin/env python3
"""Create local mTLS identities without replacing existing keys. Not a production PKI."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

FILES = ['ca.crt', 'ca.key', 'identity-service.crt', 'identity-service.key', 'chat-service.crt', 'chat-service.key']

def initialize(directory: Path, include_community: bool = False) -> None:
    peers = ['identity-service', 'chat-service'] + (['community-service'] if include_community else [])
    files = ['ca.crt', 'ca.key'] + [peer + extension for peer in peers for extension in ['.crt', '.key']]
    if directory.is_symlink():
        raise ValueError('RPC secret directory must not be a symlink')
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    if any((directory/name).is_symlink() for name in files):
        raise ValueError('RPC secret files must not be symlinks')
    existing = [name for name in files if (directory/name).exists()]
    if existing and len(existing) != len(files):
        raise ValueError('Partial RPC identities exist; restore the complete set instead of replacing keys')
    if existing:
        for peer in peers:
            subprocess.run(['openssl','verify','-CAfile',str(directory/'ca.crt'),str(directory/(peer+'.crt'))],check=True,stdout=subprocess.DEVNULL)
            cert = subprocess.check_output(['openssl','x509','-in',str(directory/(peer+'.crt')),'-pubkey','-noout'])
            key = subprocess.check_output(['openssl','pkey','-in',str(directory/(peer+'.key')),'-pubout'])
            if cert != key:
                raise ValueError('RPC certificate and private key do not match')
        return
    with tempfile.TemporaryDirectory(prefix='scdc-rpc-',dir=directory) as temporary:
        stage=Path(temporary)
        def run(*args):
            subprocess.run(['openssl',*args],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
        run('req','-x509','-newkey','rsa:3072','-nodes','-keyout',str(stage/'ca.key'),'-out',str(stage/'ca.crt'),'-days','365','-subj','/CN=SCDC local RPC CA',
            '-addext','basicConstraints=critical,CA:TRUE','-addext','keyUsage=critical,keyCertSign,cRLSign')
        for peer in peers:
            run('req','-newkey','rsa:2048','-nodes','-keyout',str(stage/(peer+'.key')),'-out',str(stage/(peer+'.csr')),'-subj','/CN='+peer)
            (stage/'extensions').write_text('basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth,clientAuth\nsubjectAltName=DNS:'+peer+',DNS:localhost,IP:127.0.0.1\n')
            run('x509','-req','-in',str(stage/(peer+'.csr')),'-CA',str(stage/'ca.crt'),'-CAkey',str(stage/'ca.key'),'-set_serial',str(int.from_bytes(os.urandom(16),'big')),
                '-out',str(stage/(peer+'.crt')),'-days','180','-extfile',str(stage/'extensions'))
        for name in files:
            # Individual Compose mounts must be readable by the app UID; parent stays 0700.
            (stage/name).chmod(0o600 if name=='ca.key' else 0o444)
            shutil.move(stage/name,directory/name)

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('directory',type=Path)
    parser.add_argument('--label-for-containers',action='store_true')
    parser.add_argument('--include-community', action='store_true', help='Create an isolated three-service identity set; existing partial sets are rejected')
    args=parser.parse_args();initialize(args.directory, args.include_community)
    if args.label_for_containers and Path('/sys/fs/selinux/enforce').exists():
        names = FILES + (['community-service.crt', 'community-service.key'] if args.include_community else [])
        subprocess.run(['chcon','-t','container_file_t',*[str(args.directory/name) for name in names if name!='ca.key']],check=True)
    print('RPC identities ready; existing keys preserved.')
