#!/usr/bin/env python3
"""Provision local Compose secret files; the enclosing directory is private (0700)."""
import argparse
import hashlib
import os
from pathlib import Path
import secrets
import subprocess


def initialize(directory: Path) -> None:
    if directory.is_symlink():
        raise ValueError("Redis secret directory must not be a symlink")
    directory.mkdir(parents=True, exist_ok=True, mode=0o700)
    directory.chmod(0o700)
    passwords = {}
    for role in ("reader", "writer"):
        path = directory / f"{role}.password"
        if path.is_symlink():
            raise ValueError("Redis secret file must not be a symlink")
        if not path.exists():
            descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o400)
            with os.fdopen(descriptor, "w") as output:
                output.write(secrets.token_hex(32) + "\n")
        password = path.read_text().strip()
        if len(password) != 64 or any(c not in "0123456789abcdef" for c in password):
            raise ValueError("Redis credential must be 32 random bytes encoded as hex")
        # Compose mounts each file separately. 0444 works with the non-root app/Redis UIDs;
        # other host accounts cannot traverse the 0700 parent directory.
        path.chmod(0o444)
        passwords[role] = hashlib.sha256(password.encode()).hexdigest()
    acl = directory / "users.acl"
    if acl.is_symlink():
        raise ValueError("Redis ACL file must not be a symlink")
    text = (
        "user default off\n"
        f"user scdc_reader on #{passwords['reader']} ~scdc:auth:* +ping +echo +hello +client|setname +client|setinfo +eval +evalsha +script|load +info +hmget\n"
        f"user scdc_writer on #{passwords['writer']} ~scdc:auth:* +ping +echo +hello +client|setname +client|setinfo +eval +evalsha +script|load +info +hget +hmget +hset +hmset +pexpire\n"
    )
    if not acl.exists() or acl.read_text() != text:
        temporary = directory / "users.acl.new"
        descriptor = os.open(temporary, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o400)
        with os.fdopen(descriptor, "w") as output:
            output.write(text)
        temporary.chmod(0o444)
        temporary.replace(acl)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--label-for-containers", action="store_true")
    arguments = parser.parse_args()
    initialize(arguments.directory)
    if arguments.label_for_containers and Path("/sys/fs/selinux/enforce").exists():
        # Compose file-backed secrets are bind mounts without a :z relabel option.
        # The 0700 directory still prevents other host accounts reading the files.
        subprocess.run(["chcon", "-t", "container_file_t", *[
            str(arguments.directory / name)
            for name in ("users.acl", "reader.password", "writer.password")
        ]], check=True)
