#!/usr/bin/env python3
import sys

MINIMUM_PYTHON = (3, 10)
if sys.version_info < MINIMUM_PYTHON:
    print(f"error: Python {MINIMUM_PYTHON[0]}.{MINIMUM_PYTHON[1]} or newer is required (running {sys.version.split()[0]})", file=sys.stderr)
    sys.exit(1)

import argparse
import gzip
import hashlib
import io
import re
import tarfile
from dataclasses import dataclass
from pathlib import Path, PurePosixPath

META_SUFFIX = ".meta"
GUID_PATTERN = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)
FIXED_MTIME = 0
FILE_MODE = 0o644


class PackageSourceError(Exception):
    pass


@dataclass(frozen=True)
class PackageAsset:
    guid: str
    pathname: str
    meta_path: Path
    content_path: Path | None


@dataclass(frozen=True)
class SourceEntries:
    included: list[Path]
    skipped: list[Path]


def is_ignored_by_unity(name: str) -> bool:
    asset_name = name[: -len(META_SUFFIX)] if name.endswith(META_SUFFIX) else name
    return asset_name.startswith(".") or asset_name.endswith("~")


def scan_source(folder: Path, entries: SourceEntries) -> None:
    for entry in sorted(folder.iterdir()):
        if is_ignored_by_unity(entry.name):
            entries.skipped.append(entry)
            continue
        entries.included.append(entry)
        if entry.is_dir() and not entry.is_symlink():
            scan_source(entry, entries)


def read_guid(meta_path: Path) -> str:
    try:
        text = meta_path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        raise PackageSourceError(f"meta is not valid UTF-8: {meta_path}") from None
    match = GUID_PATTERN.search(text)
    if match is None:
        raise PackageSourceError(f"no guid in meta: {meta_path}")
    return match.group(1)


def to_asset(entry: Path, pathname: str) -> PackageAsset:
    meta_path = entry.with_name(entry.name + META_SUFFIX)
    return PackageAsset(
        guid=read_guid(meta_path),
        pathname=pathname,
        meta_path=meta_path,
        content_path=entry if entry.is_file() else None,
    )


def find_meta_problems(entries: list[Path]) -> list[str]:
    problems = []
    for entry in entries:
        if entry.is_symlink():
            problems.append(f"symbolic links are not supported: {entry}")
        elif entry.name.endswith(META_SUFFIX):
            if not entry.is_file():
                problems.append(f"a folder named *.meta is not supported: {entry}")
            elif not entry.with_name(entry.name[: -len(META_SUFFIX)]).exists():
                problems.append(f"meta without its asset: {entry}")
        elif not entry.with_name(entry.name + META_SUFFIX).is_file():
            problems.append(f"asset without its meta: {entry}")
    return problems


def find_duplicate_guid_problems(assets: list[PackageAsset]) -> list[str]:
    first_pathname_by_guid: dict[str, str] = {}
    problems = []
    for asset in assets:
        first_pathname = first_pathname_by_guid.setdefault(asset.guid, asset.pathname)
        if first_pathname != asset.pathname:
            problems.append(f"duplicate guid {asset.guid}: {first_pathname} and {asset.pathname}")
    return problems


def collect_assets(source: Path, root_pathname: str, entries: SourceEntries) -> list[PackageAsset]:
    if not source.is_dir():
        raise PackageSourceError(f"source folder not found: {source}")
    if not source.with_name(source.name + META_SUFFIX).is_file():
        raise PackageSourceError(f"meta of the source folder itself not found: {source}{META_SUFFIX}")

    scan_source(source, entries)
    problems = find_meta_problems(entries.included)
    if problems:
        raise PackageSourceError("\n".join(problems))

    assets = [to_asset(source, root_pathname)]
    for entry in entries.included:
        if entry.name.endswith(META_SUFFIX):
            continue
        relative = PurePosixPath(entry.relative_to(source).as_posix())
        assets.append(to_asset(entry, str(PurePosixPath(root_pathname) / relative)))

    problems = find_duplicate_guid_problems(assets)
    if problems:
        raise PackageSourceError("\n".join(problems))
    return sorted(assets, key=lambda asset: asset.guid)


def fixed_tar_info(name: str, size: int) -> tarfile.TarInfo:
    info = tarfile.TarInfo(name)
    info.size = size
    info.mode = FILE_MODE
    info.mtime = FIXED_MTIME
    info.uid = 0
    info.gid = 0
    info.uname = ""
    info.gname = ""
    return info


def add_bytes(tar: tarfile.TarFile, name: str, data: bytes) -> None:
    tar.addfile(fixed_tar_info(name, len(data)), io.BytesIO(data))


def add_asset(tar: tarfile.TarFile, asset: PackageAsset) -> None:
    if asset.content_path is not None:
        add_bytes(tar, f"{asset.guid}/asset", asset.content_path.read_bytes())
    add_bytes(tar, f"{asset.guid}/asset.meta", asset.meta_path.read_bytes())
    add_bytes(tar, f"{asset.guid}/pathname", asset.pathname.encode("utf-8"))


def build_tar(assets: list[PackageAsset]) -> bytes:
    buffer = io.BytesIO()
    with tarfile.open(fileobj=buffer, mode="w", format=tarfile.USTAR_FORMAT) as tar:
        for asset in assets:
            add_asset(tar, asset)
    return buffer.getvalue()


def gzip_fixed(data: bytes) -> bytes:
    buffer = io.BytesIO()
    with gzip.GzipFile(filename="", mode="wb", fileobj=buffer, mtime=FIXED_MTIME, compresslevel=9) as gz:
        gz.write(data)
    return buffer.getvalue()


def normalize_root_pathname(root: str) -> str:
    normalized = root.replace("\\", "/").strip("/")
    if not normalized.startswith("Assets/"):
        raise PackageSourceError(f"root must start with Assets/: {root}")
    if any(part in ("", ".", "..") for part in normalized.split("/")):
        raise PackageSourceError(f"root must not contain empty, . or .. segments: {root}")
    return normalized


def write_package(output: Path, data: bytes) -> None:
    if output.is_dir():
        raise PackageSourceError(f"output is a folder: {output}")
    try:
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_bytes(data)
    except OSError as error:
        raise PackageSourceError(f"cannot write output {output}: {error.strerror or error}") from None


def report_skipped(source: Path, skipped: list[Path]) -> None:
    if not skipped:
        return
    names = ", ".join(entry.relative_to(source).as_posix() for entry in skipped)
    print(f"skipped {len(skipped)} entries that Unity ignores: {names}", file=sys.stderr)


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build a .unitypackage without Unity.")
    parser.add_argument("--source", required=True, type=Path, help="folder of assets; <folder>.meta must sit next to it")
    parser.add_argument("--root", required=True, help="path the folder is imported to, e.g. Assets/AmenoneDialogue")
    parser.add_argument("--output", required=True, type=Path, help="path of the .unitypackage to write")
    return parser.parse_args()


def main() -> int:
    arguments = parse_arguments()
    source = arguments.source.resolve()
    entries = SourceEntries(included=[], skipped=[])
    try:
        assets = collect_assets(source, normalize_root_pathname(arguments.root), entries)
        tar_bytes = build_tar(assets)
        package_bytes = gzip_fixed(tar_bytes)
        write_package(arguments.output, package_bytes)
    except PackageSourceError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1
    except OSError as error:
        print(f"error: {error}", file=sys.stderr)
        return 1
    finally:
        report_skipped(source, entries.skipped)

    print(f"assets: {len(assets)}")
    print(f"tar sha256: {hashlib.sha256(tar_bytes).hexdigest()}")
    print(f"unitypackage sha256: {hashlib.sha256(package_bytes).hexdigest()}")
    print(f"output: {arguments.output}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
