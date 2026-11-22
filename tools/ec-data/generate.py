#!/usr/bin/env python3
"""Generate EC offline index deterministically, checking every upstream input first."""
import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SLOT_COLUMNS = ['MainHand', 'OffHand', 'Head', 'Body', 'Gloves', 'Legs', 'Feet', 'Ears', 'Neck', 'Wrists', 'FingerR', 'FingerL']
APOSTROPHES = str.maketrans({c: "'" for c in '\u2018\u2019\u02bc\uff07'})

def normalize(value):
    # English source names: matches C# invariant lower/Unicode whitespace and apostrophes.
    return ' '.join(value.translate(APOSTROPHES).lower().split())

def read_rows(data, cn=False):
    reader = csv.reader(io.StringIO(data.decode('utf-8-sig'), newline=''))
    if cn:
        next(reader)  # SaintCoinach field positions
        header = next(reader)
        next(reader)  # field types
    else:
        header = next(reader)
    return [dict(zip(header, row)) for row in reader]

def load_sources(source_dir=None):
    lock = json.loads((HERE / 'sources.lock.json').read_text())
    sources = {}
    for filename, spec in lock['files'].items():
        if source_dir:
            data = (Path(source_dir) / filename).read_bytes()
        else:
            with urllib.request.urlopen(spec['url']) as response:
                data = response.read()
        actual = hashlib.sha256(data).hexdigest()
        if actual != spec['sha256']:
            raise ValueError(f'{filename}: SHA256 mismatch: {actual}')
        sources[filename] = data
    return sources

def validate_sources(sources):
    required = {
        'Item.csv': ['#', 'Name', 'EquipSlotCategory'],
        'Stain.csv': ['#', 'Name'],
        'EquipSlotCategory.csv': ['#', 'SoulCrystal'] + SLOT_COLUMNS,
    }
    for filename, fields in required.items():
        rows = read_rows(sources[filename])
        if not rows or not set(fields).issubset(rows[0]):
            raise ValueError(f'{filename}: missing required fields')
        seen = set()
        for row in rows:
            id = int(row['#'])
            if not 0 <= id <= (255 if filename == 'Stain.csv' else 0xffffffff):
                raise ValueError(f'{filename}: ID out of range: {id}')
            if id in seen:
                raise ValueError(f'{filename}: duplicate/conflicting row ID: {id}')
            seen.add(id)
            if filename == 'Item.csv' and not 0 <= int(row['EquipSlotCategory']) <= 0xffffffff:
                raise ValueError('Item.csv: invalid category ID')
            if filename == 'EquipSlotCategory.csv':
                for field in fields[1:]:
                    if int(row[field]) not in (-1, 0, 1):
                        raise ValueError(f'EquipSlotCategory: invalid flag {field}')

def build_index(sources):
    validate_sources(sources)
    categories = {int(r['#']): r for r in read_rows(sources['EquipSlotCategory.csv'])}
    items = {}
    for row in read_rows(sources['Item.csv']):
        category = categories[int(row['EquipSlotCategory'])]
        name = normalize(row['Name'])
        if not name or int(row['EquipSlotCategory']) == 0 or int(category['SoulCrystal']) != 0:
            continue
        items.setdefault(name, []).append(int(row['#']))
    stains = {}
    for row in read_rows(sources['Stain.csv']):
        name = normalize(row['Name'])
        if name and int(row['#']) != 0:
            stains.setdefault(name, []).append(int(row['#']))
    return {key: {name: sorted(set(ids)) for name, ids in sorted(values.items())}
            for key, values in [('Items', items), ('Stains', stains)]}

def encode(index):
    return (json.dumps(index, ensure_ascii=False, separators=(',', ':')) + '\n').encode()

def validate_index(index):
    if set(index) != {'Items', 'Stains'}:
        raise ValueError('Invalid index sections')
    for section, names in index.items():
        for name, ids in names.items():
            if not name or name != normalize(name) or not ids or ids != sorted(set(ids)):
                raise ValueError(f'Invalid normalized index entry: {name}')
            if any(type(id) is not int or not 0 < id <= (255 if section == 'Stains' else 0xffffffff) for id in ids):
                raise ValueError(f'Invalid index ID: {name}')

def fetch(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'Glamaholic-EC-data-generator'})
    with urllib.request.urlopen(request, timeout=120) as response:
        return response.read()

def update_revision(revision, lock):
    # Resolve even a SHA through GitHub: the manifest always records the server's full commit SHA.
    from urllib.parse import quote
    commit = json.loads(fetch('https://api.github.com/repos/xivapi/ffxiv-datamining/commits/' + quote(revision, safe='')))['sha']
    if len(commit) != 40 or any(c not in '0123456789abcdef' for c in commit):
        raise ValueError('Upstream did not resolve to a full commit SHA')
    sources = {}
    for filename in ('Item.csv', 'Stain.csv', 'EquipSlotCategory.csv'):
        url = f'https://raw.githubusercontent.com/xivapi/ffxiv-datamining/{commit}/csv/en/{filename}'
        data = fetch(url)
        sources[filename] = data
        lock['files'][filename] = {'repository': 'xivapi/ffxiv-datamining', 'commit': commit,
                                   'url': url, 'sha256': hashlib.sha256(data).hexdigest()}
    return sources

def write_outputs(output, data, lock):
    # All network/validation work has completed. Stage both files before replacing either.
    manifest = HERE / 'sources.lock.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    old_data = output.read_bytes() if output.exists() else None
    old_lock = manifest.read_bytes()
    staged_data = output.with_suffix('.json.tmp')
    staged_lock = manifest.with_suffix('.json.tmp')
    try:
        staged_data.write_bytes(data)
        staged_lock.write_text(json.dumps(lock, indent=2) + '\n', encoding='utf-8')
        staged_data.replace(output)
        staged_lock.replace(manifest)
    except Exception:
        if old_data is None:
            output.unlink(missing_ok=True)
        else:
            output.write_bytes(old_data)
        manifest.write_bytes(old_lock)
        raise
    finally:
        staged_data.unlink(missing_ok=True)
        staged_lock.unlink(missing_ok=True)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', type=Path, help='Read checksum-verified source CSVs')
    parser.add_argument('--check', action='store_true', help='Offline index check; --source-dir also checks reproducibility')
    parser.add_argument('--revision', help='Explicitly update EN data to SHA/ref (resolved to full SHA); never automatic')
    args = parser.parse_args()
    if args.revision and (args.check or args.source_dir):
        parser.error('--revision cannot be combined with --check or --source-dir')
    output = ROOT / 'Glamaholic' / 'Resources' / 'EcNameIndex.json'
    lock = json.loads((HERE / 'sources.lock.json').read_text())
    if args.check and not args.source_dir:
        data = output.read_bytes()
        index = json.loads(data)
        if hashlib.sha256(data).hexdigest() != lock['artifact']['sha256']:
            raise ValueError('Checked-in index SHA256 mismatch')
    else:
        sources = update_revision(args.revision, lock) if args.revision else load_sources(args.source_dir)
        index = build_index(sources)
        data = encode(index)
    validate_index(index)
    if args.check:
        if output.read_bytes() != data:
            raise SystemExit('Generated index differs')
    else:
        lock['artifact'] = {'path': 'Glamaholic/Resources/EcNameIndex.json', 'sha256': hashlib.sha256(data).hexdigest()}
        write_outputs(output, data, lock)
    print(f"EC index OK: {len(index['Items'])} item names, {len(index['Stains'])} stain names, {len(data)} bytes, SHA256 {hashlib.sha256(data).hexdigest()}")

if __name__ == '__main__':
    main()
