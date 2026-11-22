"""Offline checks; optionally add --source-dir PATH to verify original CSVs and exact regeneration."""
import argparse
import hashlib
import json
from pathlib import Path
import unittest
from unittest.mock import patch
import generate as g

parser = argparse.ArgumentParser()
parser.add_argument('--source-dir', type=Path)
args, remaining = parser.parse_known_args()

class DataTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = (g.ROOT / 'Glamaholic/Resources/EcNameIndex.json').read_bytes()
        cls.index = json.loads(cls.data)

    def test_artifact_hash_and_shape(self):
        lock = json.loads((g.HERE / 'sources.lock.json').read_text())
        self.assertEqual(hashlib.sha256(self.data).hexdigest(), lock['artifact']['sha256'])
        g.validate_index(self.index)
        for spec in lock['files'].values():
            self.assertEqual(len(spec['commit']), 40)
            self.assertIn(spec['commit'], spec['url'])
            self.assertEqual(len(spec['sha256']), 64)

    def test_sample_and_stains(self):
        for name, id in [("Academic's Coat", 33988), ('Welkin Half Sleeves', 11841),
                         ('Edenmorn Chausses of Healing', 32327), ('Atrophy Thighboots', 34021),
                         ('Rinascita Samurai Blade', 37755), ("Azeyma's Earrings", 41081)]:
            self.assertIn(id, self.index['Items'][g.normalize(name)])
        for name, id in [('dalamud red', 10), ('jet black', 102), ('cork brown', 22), ('soot black', 6), ('opo-opo brown', 24)]:
            self.assertEqual(self.index['Stains'][name], [id])

    def test_duplicate_names_retained(self):
        # This snapshot has no collisions; the format and synthetic generator test must still preserve them.
        sample = {'Items': {'same name': [1, 2]}, 'Stains': {}}
        g.validate_index(sample)
        self.assertEqual(json.loads(g.encode(sample))['Items']['same name'], [1, 2])

    def test_normalize(self):
        self.assertEqual(g.normalize('  ACADEMIC’S\u00a0  Coat\t'), "academic's coat")
        self.assertNotEqual(g.normalize('Academic Coat'), "academic's coat")

    def test_synthetic_filter_validation_and_collisions(self):
        sources = {
            'Item.csv': b"#,Name,EquipSlotCategory\n1,Test,1\n2,Test,2\n3,Soul,3\n4,Not gear,0\n",
            'Stain.csv': b'#,Name\n0,No color\n1,White\n',
            'EquipSlotCategory.csv': ('#,' + ','.join(['SoulCrystal'] + g.SLOT_COLUMNS) + '\n' +
                '\n'.join(f'{id},' + ','.join(map(str, [int(id == 3)] + [int(id == 1)] * 12)) for id in range(4)) + '\n').encode(),
        }
        index = g.build_index(sources)
        self.assertEqual(index['Items'], {'test': [1, 2]})
        invalid = dict(sources, **{'Item.csv': sources['Item.csv'] + b'1,Conflict,1\n'})
        with self.assertRaisesRegex(ValueError, 'duplicate/conflicting'):
            g.build_index(invalid)
        with self.assertRaisesRegex(ValueError, 'out of range'):
            g.build_index(dict(sources, **{'Stain.csv': b'#,Name\n256,Overflow\n'}))
        with self.assertRaisesRegex(ValueError, 'missing required'):
            g.build_index(dict(sources, **{'Item.csv': b'#,Bad\n1,Invalid\n'}))
        with self.assertRaises(KeyError):
            g.build_index(dict(sources, **{'Item.csv': b'#,Name,EquipSlotCategory\n1,Missing,999\n'}))

    def test_download_failure_does_not_write(self):
        lock = json.loads((g.HERE / 'sources.lock.json').read_text())
        original = (g.HERE / 'sources.lock.json').read_bytes()
        with patch.object(g, 'fetch', side_effect=RuntimeError('offline')):
            with self.assertRaisesRegex(RuntimeError, 'offline'):
                g.update_revision('master', lock)
        self.assertEqual(original, (g.HERE / 'sources.lock.json').read_bytes())
        self.assertEqual(self.data, (g.ROOT / 'Glamaholic/Resources/EcNameIndex.json').read_bytes())

    def test_revision_ref_resolves_to_full_sha(self):
        lock = json.loads((g.HERE / 'sources.lock.json').read_text())
        original_cn = dict(lock['files']['cn-Item.csv'])
        commit = 'a' * 40
        with patch.object(g, 'fetch', side_effect=[json.dumps({'sha': commit}).encode(), b'items', b'stains', b'categories']) as fetch:
            result = g.update_revision('release/test', lock)
        self.assertEqual(set(result), {'Item.csv', 'Stain.csv', 'EquipSlotCategory.csv'})
        self.assertIn('release%2Ftest', fetch.call_args_list[0].args[0])
        for filename, content in result.items():
            self.assertEqual(lock['files'][filename]['commit'], commit)
            self.assertIn(commit, lock['files'][filename]['url'])
            self.assertEqual(lock['files'][filename]['sha256'], hashlib.sha256(content).hexdigest())
        self.assertEqual(lock['files']['cn-Item.csv'], original_cn)
        before = (g.HERE / 'sources.lock.json').read_bytes()
        with patch.object(g, 'fetch', side_effect=[json.dumps({'sha': commit}).encode(), b'items', RuntimeError('partial download')]):
            with self.assertRaisesRegex(RuntimeError, 'partial download'):
                g.update_revision('master', lock)
        self.assertEqual(before, (g.HERE / 'sources.lock.json').read_bytes())
        self.assertEqual(self.data, (g.ROOT / 'Glamaholic/Resources/EcNameIndex.json').read_bytes())

    @unittest.skipUnless(args.source_dir, 'Optional full source CSV verification')
    def test_full_source_reproducibility_and_soul_exclusion(self):
        sources = g.load_sources(args.source_dir)
        self.assertEqual(g.encode(g.build_index(sources)), self.data)
        categories = {int(r['#']): r for r in g.read_rows(sources['EquipSlotCategory.csv'])}
        ids = {id for values in self.index['Items'].values() for id in values}
        for row in g.read_rows(sources['Item.csv']):
            if int(row['EquipSlotCategory']) == 0 or int(categories[int(row['EquipSlotCategory'])]['SoulCrystal']) != 0:
                self.assertNotIn(int(row['#']), ids)

if __name__ == '__main__':
    unittest.main(argv=['test_data.py'] + remaining)
