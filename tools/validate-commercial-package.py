"""Verify a built package without modifying it or starting the game."""
import argparse
import hashlib
import json
from pathlib import Path


def verify(root, version=None):
    root = Path(root).resolve()
    data = json.loads((root / "PACKAGE-MANIFEST.json").read_text(encoding="utf-8-sig"))
    expected = version or data["version"]
    if data["version"] != expected:
        raise ValueError("Package version differs from expected version")
    for name in ("newASTER.exe", "UnityPlayer.dll", "README.txt", "CREDITS.txt", "RESOURCE-INVENTORY.json", "newASTER_Data/Managed/Assembly-CSharp.dll", "ThirdPartyNotices/NotoSansCJKjp/OFL.txt", "ThirdPartyNotices/NotoSansCJKjp/NOTICE.txt"):
        if not (root / name).is_file():
            raise ValueError("Missing required file: " + name)
    inventory = json.loads((root / "RESOURCE-INVENTORY.json").read_text(encoding="utf-8-sig"))
    if inventory["version"] != expected:
        raise ValueError("Resource inventory version differs")
    for name in ("README.txt", "CREDITS.txt"):
        if "Version " + expected not in (root / name).read_text(encoding="utf-8-sig"):
            raise ValueError("Outdated product text: " + name)
    seen = set()
    for record in data["files"]:
        name = record["path"]
        path = (root / name).resolve()
        if name in seen or path == root or not path.is_relative_to(root):
            raise ValueError("Unsafe or duplicate manifest path: " + name)
        seen.add(name)
        if path.stat().st_size != record["bytes"]:
            raise ValueError("File size mismatch: " + name)
        digest = hashlib.sha256()
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                digest.update(chunk)
        if digest.hexdigest() != record["sha256"]:
            raise ValueError("Checksum mismatch: " + name)
    actual = {p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file() and p != root / "PACKAGE-MANIFEST.json"}
    if actual != seen:
        raise ValueError("Manifest does not describe every distribution file")
    print(f"COMMERCIAL_PACKAGE_VERIFY_PASS version={expected} files={len(seen)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory")
    parser.add_argument("--version")
    args = parser.parse_args()
    verify(args.directory, args.version)
