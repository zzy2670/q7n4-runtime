"""Offline official-format add-in distribution only; never installs/loads/runs it.
Not a project/source ZIP and not the mandatory native Pack & Go.
Root package structure follows ABB DP_Structure and Packing-and-Distributing-an-Add-In.
"""
from pathlib import Path
import zipfile
P=Path(__file__).resolve().parent
out=P/'MA6212.FinalCell012-PREPARED-0.12.6.rspak'
files={'manifest.xml':P/'manifest.xml','RobotStudio/Add-In-net10.0/MA6212_FinalCell012.rsaddin':P/'MA6212_FinalCell012.rsaddin','RobotStudio/Add-In-net10.0/MA6212_FinalCell012.dll':P/'bin/addin/MA6212_FinalCell012.dll','RobotStudio/Add-In-net10.0/MA6212_ActorDemo012.dll':P/'bin/actor/MA6212_ActorDemo012.dll'}
with zipfile.ZipFile(out,'x',compression=zipfile.ZIP_DEFLATED) as z:
 for name,path in files.items():z.write(path,'MA6212.FinalCell012-0.12.6/'+name)
print('PREPARED_UNRELEASED_NOT_INSTALLED',out)
