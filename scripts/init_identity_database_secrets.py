#!/usr/bin/env python3
"""Provision persistent local Identity database passwords outside the repository."""
import argparse
import os
from pathlib import Path
import secrets
import subprocess

FILES = ('identity_database_admin_password', 'identity_database_password')


def initialize(directory: Path) -> None:
    if directory.is_symlink():
        raise ValueError('Secret directory must not be a symlink')
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    if any((directory / name).is_symlink() for name in FILES):
        raise ValueError('Secret file must not be a symlink')
    existing = [name for name in FILES if (directory / name).exists()]
    if existing and len(existing) != len(FILES):
        raise ValueError('Partial database secrets; restore the existing set instead of rotating it')
    for name in FILES:
        path = directory / name
        if path.exists():
            value = path.read_text().strip()
            if len(value) < 32 or any(c.isspace() for c in value):
                raise ValueError('Invalid existing database secret')
        else:
            descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(descriptor, 'w') as stream:
                stream.write(secrets.token_urlsafe(48) + '\n')
        # Individual secret mounts must be readable by the non-root app UID.
        path.chmod(0o444)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--label-for-containers', action='store_true')
    args = parser.parse_args()
    initialize(args.directory)
    if args.label_for_containers and Path('/sys/fs/selinux/enforce').exists():
        subprocess.run(['chcon', '-t', 'container_file_t', *[str(args.directory / name) for name in FILES]], check=True)
    print('Identity database secrets ready; existing passwords preserved.')
