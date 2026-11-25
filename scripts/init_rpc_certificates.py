#!/usr/bin/env python3
"""Create local mTLS identities without replacing existing keys. Not a production PKI."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

FILES = ['ca.crt', 'ca.key', 'identity-service.crt', 'identity-service.key', 'chat-service.crt', 'chat-service.key']

def initialize(directory: Path) -> None:
    if directory.is_symlink():
        raise ValueError('RPC secret directory must not be a symlink')
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    if any((directory/name).is_symlink() for name in FILES):
        raise ValueError('RPC secret files must not be symlinks')
    existing = [name for name in FILES if (directory/name).exists()]
    if existing and len(existing) != len(FILES):
        raise ValueError('Partial RPC identities exist; restore the complete set instead of replacing keys')
    if existing:
        for peer in ['identity-service', 'chat-service']:
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
        for peer in ['identity-service','chat-service']:
            run('req','-newkey','rsa:2048','-nodes','-keyout',str(stage/(peer+'.key')),'-out',str(stage/(peer+'.csr')),'-subj','/CN='+peer)
            (stage/'extensions').write_text('basicConstraints=critical,CA:FALSE\nkeyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth,clientAuth\nsubjectAltName=DNS:'+peer+',DNS:localhost,IP:127.0.0.1\n')
            run('x509','-req','-in',str(stage/(peer+'.csr')),'-CA',str(stage/'ca.crt'),'-CAkey',str(stage/'ca.key'),'-set_serial',str(int.from_bytes(os.urandom(16),'big')),
                '-out',str(stage/(peer+'.crt')),'-days','180','-extfile',str(stage/'extensions'))
        for name in FILES:
            # Individual Compose mounts must be readable by the app UID; parent stays 0700.
            (stage/name).chmod(0o600 if name=='ca.key' else 0o444)
            shutil.move(stage/name,directory/name)

if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('directory',type=Path)
    parser.add_argument('--label-for-containers',action='store_true')
    args=parser.parse_args();initialize(args.directory)
    if args.label_for_containers and Path('/sys/fs/selinux/enforce').exists():
        subprocess.run(['chcon','-t','container_file_t',*[str(args.directory/name) for name in FILES if name!='ca.key']],check=True)
    print('RPC identities ready; existing keys preserved.')
