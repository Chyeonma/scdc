#!/usr/bin/env python3
"""Prepare isolated Community preview credentials and a public JWT trust key."""
import argparse
import base64
import os
from pathlib import Path
import secrets
import subprocess
import tempfile

FILES = (
    'community_preview_admin_password', 'community_preview_password',
    'community_preview_operation_key', 'identity-preview.pub.pem',
)


def initialize(directory: Path) -> None:
    if directory.is_symlink():
        raise ValueError('Preview secret directory must not be a symlink')
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    if any((directory / name).is_symlink() for name in FILES):
        raise ValueError('Preview secret file must not be a symlink')
    existing = [name for name in FILES if (directory / name).exists()]
    if existing and len(existing) != len(FILES):
        raise ValueError('Partial preview secrets; restore the existing set instead of rotating it')
    if existing:
        for name in FILES[:2]:
            value = (directory / name).read_text().strip()
            if len(value) < 32 or any(character.isspace() for character in value):
                raise ValueError('Invalid existing preview password')
        key = base64.b64decode((directory / FILES[2]).read_text().strip(), validate=True)
        if len(key) != 32:
            raise ValueError('Invalid existing preview operation key')
        subprocess.run(['openssl', 'pkey', '-pubin', '-in', str(directory / FILES[3]), '-noout'],
                       check=True, capture_output=True)
    else:
        with tempfile.TemporaryDirectory(prefix='scdc-preview-key-') as temporary:
            private = Path(temporary) / 'private.pem'
            subprocess.run(['openssl', 'genpkey', '-algorithm', 'RSA', '-pkeyopt', 'rsa_keygen_bits:2048',
                            '-out', str(private)], check=True, capture_output=True)
            public = subprocess.run(['openssl', 'pkey', '-in', str(private), '-pubout'],
                                    check=True, capture_output=True).stdout.decode()
        values = [secrets.token_urlsafe(48) + '\n', secrets.token_urlsafe(48) + '\n',
                  base64.b64encode(secrets.token_bytes(32)).decode() + '\n', public]
        for name, value in zip(FILES, values):
            descriptor = os.open(directory / name, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(descriptor, 'w') as stream:
                stream.write(value)
    # The parent remains private; individual Compose secret mounts are readable by app/PG UIDs.
    for name in FILES:
        (directory / name).chmod(0o444)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--label-for-containers', action='store_true')
    args = parser.parse_args()
    initialize(args.directory)
    if args.label_for_containers and Path('/sys/fs/selinux/enforce').exists():
        subprocess.run(['chcon', '-t', 'container_file_t',
                        *[str(args.directory / name) for name in FILES]], check=True)
    print('Community preview secrets ready; existing credentials preserved. No live Identity keys are used.')
