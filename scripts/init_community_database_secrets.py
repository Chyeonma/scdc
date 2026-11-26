#!/usr/bin/env python3
"""Provision Community's new runtime password and adopt the existing operation key."""
import argparse
import base64
import os
from pathlib import Path
import secrets
import subprocess

FILES = ('community_database_password', 'community_operation_key')


def initialize(directory: Path, operation_key: str) -> None:
    try:
        if len(base64.b64decode(operation_key, validate=True)) < 32:
            raise ValueError('Operation key must contain at least 32 bytes')
    except (ValueError, TypeError) as error:
        raise ValueError('A valid existing Community operation key is required') from error
    if directory.is_symlink():
        raise ValueError('Secret directory must not be a symlink')
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    if any((directory/name).is_symlink() for name in FILES):
        raise ValueError('Secret files must not be symlinks')
    present = [name for name in FILES if (directory/name).exists()]
    if present and len(present) != len(FILES):
        raise ValueError('Partial Community secrets; restore the existing set')
    if present:
        if (directory/FILES[1]).read_text().strip() != operation_key:
            raise ValueError('Operation key differs from the retained Community key; no keys replaced')
        if len((directory/FILES[0]).read_text().strip()) < 32:
            raise ValueError('Invalid existing database password')
    else:
        for name, value in zip(FILES, (secrets.token_urlsafe(48), operation_key)):
            fd = os.open(directory/name, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o444)
            with os.fdopen(fd, 'w') as stream:
                stream.write(value + '\n')
    for name in FILES:
        (directory/name).chmod(0o444)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('--env-file', required=True, type=Path)
    parser.add_argument('--label-for-containers', action='store_true')
    args = parser.parse_args()
    values = dict(line.split('=', 1) for line in args.env_file.read_text().splitlines() if line and not line.startswith('#') and '=' in line)
    initialize(args.directory, values.get('COMMUNITY_OPERATION_KEY', '').strip().strip('\"\''))
    if args.label_for_containers and Path('/sys/fs/selinux/enforce').exists():
        subprocess.run(['chcon', '-t', 'container_file_t', *[str(args.directory/name) for name in FILES]], check=True)
    print('Community secrets ready; database credential and existing operation key preserved.')
