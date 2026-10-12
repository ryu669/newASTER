"""Regression checks for preserving identity boundaries in authoring input."""
import importlib.util
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location('context', Path(__file__).with_name('build-heroine-production-context.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ProductionContextTests(unittest.TestCase):
    def setUp(self):
        self.inputs = [module.read(n) for n in ('heroine-production-roster',
                      'heroine-relationships', 'kyoshin-heroine-relationships',
                      'aa-kyoshin-person-timeline')]
        self.bundle = module.compile_context(*self.inputs)
        self.people = {p['planningId']: p for p in self.bundle['people']}

    def test_every_candidate_survives_without_runtime_ids_being_added(self):
        self.assertEqual(len(self.people), 197)
        for p in self.inputs[0]['people']:
            self.assertEqual(self.people[p['planningId']]['implementedFormIds'], p['implementedFormIds'])

    def test_lady_x_remains_two_people(self):
        self.assertEqual(self.people['kyoshin.person.113']['name'], 'レディX(少女)')
        self.assertEqual(self.people['kyoshin.person.114']['name'], 'レディX(大人)')
        self.assertNotEqual(self.people['kyoshin.person.113']['formKeys'],
                            self.people['kyoshin.person.114']['formKeys'])

    def test_cross_work_link_is_candidate_not_merge(self):
        aa = self.people['aa.person.032']
        self.assertTrue(aa['crossWorkCandidates'])
        self.assertIn('kyoshin.person.004', self.people)
        self.assertIn('kyoshin.person.005', self.people)
        self.assertEqual(aa['source'], 'aa')

    def test_both_relationship_endpoints_receive_original_evidence(self):
        r = self.inputs[1]['relationships'][0]
        for pid in (r['fromPersonId'], r['toPersonId']):
            self.assertIn(r, self.people[pid]['observedRelationships'])

    def test_compilation_does_not_mutate_research(self):
        self.bundle['people'][0]['formReferences'][0]['personName'] = 'changed'
        self.assertNotEqual(self.inputs[0]['videoRecords'][0]['personName'], 'changed')

    def test_duplicate_person_is_rejected(self):
        self.inputs[0]['people'].append(self.inputs[0]['people'][0])
        with self.assertRaises(ValueError):
            module.compile_context(*self.inputs)


if __name__ == '__main__':
    unittest.main()
