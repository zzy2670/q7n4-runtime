"""Offline exact-byte PolicyJson embedding; no runtime use or approval change."""
from pathlib import Path
import json, hashlib, re
p=Path(__file__).resolve().parent
h=lambda b:hashlib.sha256(b).hexdigest().upper()
cp=p/'release_prepared012/ActorConfig012.PREPARED.json'
rp=p/'release_prepared012/FinalRelease012.PREPARED.json'
policy=p/'release_prepared012/virtual_cell_policy.FROZEN_012.json'
c=json.loads(cp.read_bytes())
assert h(policy.read_bytes())==c['PolicyHash']
c['PolicyJson']=policy.read_bytes().decode('utf-8')
for stem in ('Layout','Scene','Contacts','Policy'):
 key=stem+('Xml' if stem=='Scene' else 'Json')
 assert h(c[key].encode('utf-8'))==c[stem+'Hash'], stem
cp.write_bytes((json.dumps(c,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
r=json.loads(rp.read_bytes()); assert r['executionApproved'] is False
r['actorConfigSHA256']=h(cp.read_bytes())
rp.write_bytes((json.dumps(r,ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
f=p/'FinalFreeze012.cs'
f.write_bytes(re.sub(rb'ReleaseManifestHash="[A-F0-9]+"',('ReleaseManifestHash="'+h(rp.read_bytes())+'"').encode(),f.read_bytes()))
print('EXACT_EMBEDDED_TEXT_HASHES_PASS_EXECUTION_APPROVAL_UNCHANGED')
