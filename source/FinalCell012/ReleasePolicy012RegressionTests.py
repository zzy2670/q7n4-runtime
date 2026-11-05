"""Offline regression against actual final release bytes and the production monitor gate."""
from pathlib import Path
import json,hashlib
p=Path(__file__).resolve().parent
source=(p/'DemoMonitor012.cs').read_text(encoding='utf-8')
assert 'r.GetProperty("status").GetString()!="FROZEN_FOR_VIRTUAL_EXECUTION"' in source
assert 'r.GetProperty("canonical_layout_sha256").GetString().ToLowerInvariant()!=layoutHash' in source
assert '!frozen.GetBoolean()' in source
new=json.loads((p/'release_final012/virtual_cell_policy.FROZEN_012.json').read_bytes())
old=json.loads((p/'history0132/release_final012/virtual_cell_policy.FROZEN_012.json').read_bytes())
c=json.loads((p/'release_final012/ActorConfig012.json').read_bytes())
layout=hashlib.sha256(c['LayoutJson'].encode()).hexdigest()
def gate(x):return x['frozen_for_virtual_execution'] is True and x['canonical_layout_sha256'].lower()==layout and x['status']=='FROZEN_FOR_VIRTUAL_EXECUTION'
assert not gate(old), 'Actual0132 failure must be reproduced'
assert gate(new), 'Released production policy must satisfy frozen/status/canonical gate'
assert {k:v for k,v in old.items() if k!='status'}=={k:v for k,v in new.items() if k!='status'}
assert c['PolicyJson'].encode()==(p/'release_final012/virtual_cell_policy.FROZEN_012.json').read_bytes()
print('PRODUCTION_RELEASE_POLICY_REGRESSION_PASS: actual0132 rejected; corrected0133 accepted; all scientific fields exact')
