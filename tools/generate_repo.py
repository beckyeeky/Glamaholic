#!/usr/bin/env python3
"""从 Dalamud 构建清单生成自定义插件库 repo.json（仅使用 Python 标准库）。"""

import argparse
import json
import os
from pathlib import Path
import re
import sys
import tempfile
import time
from urllib.parse import quote

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "Glamaholic/bin/Release/GlamaholicCN/GlamaholicCN.json"
REPOSITORY = "https://github.com/beckyeeky/Glamaholic"
FIELDS = (
    "Author", "Name", "InternalName", "AssemblyVersion", "Description", "Punchline",
    "ApplicableVersion", "RepoUrl", "DalamudApiLevel", "LoadRequiredState", "LoadSync",
    "CanUnloadAsync", "LoadPriority", "AcceptsFeedback",
)
INTEGER_FIELDS = ("DalamudApiLevel", "LoadRequiredState", "LoadPriority")
BOOLEAN_FIELDS = ("LoadSync", "CanUnloadAsync", "AcceptsFeedback")


def generate(manifest, tag, timestamp):
    """保留构建清单的元数据；只补充库索引字段，不改原作者或插件标识。"""
    if not isinstance(manifest, dict):
        raise ValueError("manifest 必须是 JSON 对象")
    missing = [key for key in FIELDS if key not in manifest]
    if missing:
        raise ValueError("manifest 缺少字段：" + ", ".join(missing))
    for key in FIELDS:
        expected = int if key in INTEGER_FIELDS else bool if key in BOOLEAN_FIELDS else str
        if type(manifest[key]) is not expected:
            raise ValueError(f"manifest 字段 {key} 类型错误，应为 {expected.__name__}")
    if manifest["InternalName"] != "GlamaholicCN":
        raise ValueError("InternalName 必须保持为 GlamaholicCN")
    if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", manifest["AssemblyVersion"]):
        raise ValueError("AssemblyVersion 必须是四段数字版本")
    if not tag or tag.strip() != tag or any(ord(c) < 32 for c in tag):
        raise ValueError("release tag 不能为空或包含首尾空白、控制字符")
    if type(timestamp) is not int or timestamp < 0:
        raise ValueError("LastUpdate 必须是非负整数 Unix 秒级时间戳")
    entry = {key: manifest[key] for key in FIELDS}
    url = f"{REPOSITORY}/releases/download/{quote(tag, safe='')}/latest.zip"
    for key in ("DownloadLinkInstall", "DownloadLinkUpdate", "DownloadLinkTesting"):
        entry[key] = url
    entry.update({
        "LastUpdate": timestamp,
        "IconUrl": "https://raw.githubusercontent.com/goatcorp/PluginDistD17/main/stable/Glamaholic/images/icon.png",
        "Tags": ["glamour", "glamour plates", "cn", "幻化"],
        "CategoryTags": ["UI", "Glamour"],
    })
    return [entry]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("release_tag", nargs="?", help="GitHub Release 标签，例如 v1.15.2.1")
    parser.add_argument("--tag", help="同 release_tag；两种写法只能选择一种")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST,
                        help="构建生成的 Glamaholic.json 路径")
    parser.add_argument("--output", type=Path, default=ROOT / "repo.json", help="输出路径")
    parser.add_argument("--last-update", type=int, help="显式指定 Unix 秒级时间戳，默认使用当前时间")
    args = parser.parse_args(argv)
    if bool(args.release_tag) == bool(args.tag):
        parser.error("请提供一个 release tag（位置参数或 --tag），不能同时提供")
    temporary = None
    try:
        manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
        result = generate(manifest, args.tag or args.release_tag,
                          args.last_update if args.last_update is not None else int(time.time()))
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", newline="\n",
                                         dir=args.output.parent, prefix=".repo-", suffix=".tmp",
                                         delete=False) as handle:
            temporary = Path(handle.name)
            json.dump(result, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary, args.output)
    except (OSError, ValueError) as exc:
        print(f"生成失败：{exc}", file=sys.stderr)
        return 1
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()
    print(f"已生成 {args.output}（{result[0]['AssemblyVersion']}，{args.tag or args.release_tag}）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
