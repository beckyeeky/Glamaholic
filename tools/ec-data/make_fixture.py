"""Refresh the six-row local-sheet test fixture from checksum-verified EN/CN snapshots."""
import argparse
import json
from pathlib import Path
from generate import load_sources, read_rows, SLOT_COLUMNS, ROOT

parser = argparse.ArgumentParser()
parser.add_argument('--source-dir', type=Path)
args = parser.parse_args()
sources = load_sources(args.source_dir)
cn = {int(r['#']): r for r in read_rows(sources['cn-Item.csv'], cn=True)}
cats = {int(r['#']): r for r in read_rows(sources['EquipSlotCategory.csv'])}
ids = [33988, 11841, 32327, 34021, 37755, 41081]
rows = []
for id in ids:
    row = cn[id]
    category = cats[int(row['EquipSlotCategory'])]
    rows.append({'Id': id, 'Name': row['Name'], 'Slots': [i for i, col in enumerate(SLOT_COLUMNS) if int(category[col]) > 0], 'SoulCrystal': int(category['SoulCrystal']) != 0})
output = ROOT / 'tests/EcResolver.Tests/Fixtures/cn-local-items.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('Wrote six CN names; category flags come from verified EN EquipSlotCategory, using CN category IDs')
