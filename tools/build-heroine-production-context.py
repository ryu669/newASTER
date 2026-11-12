"""Compile observed research into per-person authoring input, without unlocking actors.

Run without arguments to rebuild the bundle, or --person aa.person.032 to inspect
one person's input. Requires only repository JSON, never the original video drive.
"""
import argparse
import copy
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / 'docs/production'
OUTPUT = DOCS / 'heroine-production-context.json'


def read(name):
    return json.loads((DOCS / (name + '.json')).read_text(encoding='utf-8-sig'))


def unique(rows, key):
    values = [row[key] for row in rows]
    if len(values) != len(set(values)):
        raise ValueError('Duplicate ' + key)


def validate_published_roster(roster, index):
    """Check the published form IDs and person boundaries without the video drive."""
    unique(index['people'], 'personId')
    published = {}
    for person in index['people']:
        for form_id in person['formIds']:
            if form_id in published:
                raise ValueError(f'Duplicate published form: {form_id}')
            published[form_id] = person['personId']

    references = {}
    for row in roster['videoRecords']:
        if row['implementationId']:
            key = (row['source'], row['formKey'])
            form_id = row['implementationId']
            if key in references and references[key] != form_id:
                raise ValueError(f'{key}: video references disagree on the published form')
            references[key] = form_id

    claimed = {}
    groups = {}
    for person in roster['people']:
        planning_id = person['planningId']
        form_ids = person['implementedFormIds']
        keys = [(person['source'], form_key) for form_key in person['formKeys']]
        reference_ids = {references[key] for key in keys if key in references}
        if set(form_ids) != reference_ids:
            raise ValueError(f'{planning_id}: published forms disagree with video references')
        for form_id in form_ids:
            if form_id not in published:
                raise ValueError(f'{planning_id}: unpublished form in roster: {form_id}')
            if form_id in claimed:
                raise ValueError(f'{planning_id}: duplicate roster form: {form_id}')
            claimed[form_id] = planning_id
            groups.setdefault(published[form_id], set()).add(planning_id)
        if len({published[form_id] for form_id in form_ids}) > 1:
            raise ValueError(f'{planning_id}: roster merges distinct published people')

    unclaimed = sorted(set(references.values()) - set(claimed))
    if unclaimed:
        raise ValueError('Published video references missing from roster: ' + ', '.join(unclaimed))
    missing = sorted(set(published) - set(claimed))
    if missing:
        raise ValueError('Published forms missing from roster: ' + ', '.join(missing))
    for person_id, planning_ids in groups.items():
        if len(planning_ids) > 1:
            raise ValueError(f'{person_id}: published person split across roster entries')


def compile_context(roster, aa, kyoshin, timeline):
    roster, aa, kyoshin, timeline = copy.deepcopy((roster, aa, kyoshin, timeline))
    people = copy.deepcopy(roster['people'])
    unique(people, 'planningId')
    relations = aa['relationships'] + kyoshin['relationships']
    unique(relations, 'id')
    unique(timeline['people'], 'id')
    ids = {p['planningId'] for p in people}
    result = []
    for person in people:
        pid = person['planningId']
        personal_relations = [r for r in relations
                              if pid in (r.get('fromPersonId'), r.get('toPersonId'))]
        notes = [n for n in aa.get('identityNotes', []) if n.get('personId') == pid]
        links = [r for r in timeline['people']
                 if pid in r.get('aaPersonIds', []) + r.get('kyoshinPersonIds', [])]
        forms = [v for v in roster['videoRecords']
                 if v['source'] == person['source'] and v['formKey'] in person['formKeys']]
        if not forms:
            raise ValueError('No form evidence: ' + pid)
        result.append(dict(
            **person,
            observedIdentity=notes,
            formReferences=forms,
            observedRelationships=personal_relations,
            crossWorkCandidates=links,
            adoptionStatus='原作調査からの制作入力・本作への採用は人物設計時に決定',
            authoringRequirements=['本作の性格・口調・関係の成長を執筆',
                                   '形態ごとのジョブ・3スキル・特性を確定',
                                   '本文・専用画像・保存・戦闘・画面確認を完了'],
        ))
    # Non-roster figures (gods, beasts and unnamed speakers) remain in source rows.
    # Never create playable characters or merge identities from their mentions.
    external_ids = sorted({r.get(k) for r in relations
                           for k in ('fromPersonId', 'toPersonId')
                           if r.get(k) and r[k] not in ids})
    return dict(schemaVersion=1, identityPolicy=timeline['identityPolicy'],
                sourceFiles=['heroine-production-roster.json', 'heroine-relationships.json',
                             'kyoshin-heroine-relationships.json',
                             'aa-kyoshin-person-timeline.json'],
                people=result, externalResearchPersonIds=external_ids,
                worldReference=copy.deepcopy(aa.get('storySourceReview', {})),
                timeline=dict(events=copy.deepcopy(timeline['events']),
                              ordering=copy.deepcopy(timeline['ordering']),
                              reviewFindings=copy.deepcopy(timeline.get('reviewFindings', []))),
                totals=dict(people=len(result), relationships=len(relations),
                            crossWorkCandidates=len(timeline['people'])))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--person', help='Stable planning ID, e.g. aa.person.032')
    parser.add_argument('--check', action='store_true', help='Fail if the saved bundle is stale')
    args = parser.parse_args()
    roster = read('heroine-production-roster')
    index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8-sig'))
    validate_published_roster(roster, index)
    bundle = compile_context(roster, read('heroine-relationships'),
                             read('kyoshin-heroine-relationships'), read('aa-kyoshin-person-timeline'))
    if args.person:
        selected = next((p for p in bundle['people'] if p['planningId'] == args.person), None)
        if selected is None:
            parser.error('Unknown person: ' + args.person)
        print(json.dumps(selected, ensure_ascii=False, indent=2))
    elif args.check:
        if not OUTPUT.exists() or json.loads(OUTPUT.read_text(encoding='utf-8')) != bundle:
            raise SystemExit('Production context is stale; rebuild it.')
        print('Production context is current:', bundle['totals'])
    else:
        OUTPUT.write_text(json.dumps(bundle, ensure_ascii=False, indent=2) + '\n',
                          encoding='utf-8', newline='\n')
        print('Built production context:', bundle['totals'])


if __name__ == '__main__':
    main()
