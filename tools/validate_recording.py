"""Validate a completed run recording against its controller and recorder results.
The source video is decoded for inspection and is never re-encoded.
Usage: python validate_recording.py --run-root <root> --evidence-dir <newdir>
"""
from pathlib import Path
import argparse, json, os, subprocess, shutil, hashlib
import xml.etree.ElementTree as ET
from fractions import Fraction

PASS='BATCH20_TRUE_UNIQUE_C2_FEED_CLEAR_CSV_GEOMETRY_READY_VERIFIED_SIM_PAUSED'
def new(path,raw):
    with path.open('xb') as f:f.write(raw);f.flush();os.fsync(f.fileno())
def xml(path):return ET.fromstring(path.read_bytes())
def main():
    a=argparse.ArgumentParser(description=__doc__);a.add_argument('--run-root',type=Path,required=True);a.add_argument('--evidence-dir',type=Path,required=True);v=a.parse_args()
    root=v.run_root.resolve();out=v.evidence_dir.resolve()
    button=root/'button_result.xml';record=root/'recording_result.xml'
    b=xml(button);r=xml(record)
    if b.findtext('Status')!=PASS:raise ValueError('Actual motion/packing terminal PASS required before video inspection')
    if (r.findtext('NativeStopped') or '').lower()!='true' or (r.findtext('RecordingFailure') or '').strip() or (r.findtext('StopError') or '').strip():raise ValueError('Native recorder must be stopped without capture/stop errors')
    video=Path(r.findtext('VideoPath') or '').resolve();side=Path(r.findtext('FrameSidecarPath') or '').resolve()
    if not video.is_relative_to(root) or not side.is_relative_to(root) or video!=root/'demo10005_owned_view.mp4' or side!=Path(str(video)+'.frames.jsonl'):raise ValueError('Only this run owned-view video and sidecar accepted')
    if not video.is_file() or not side.is_file() or out.exists():raise ValueError('Completed source files and fresh evidence directory required')
    probe=shutil.which('ffprobe');decoder=shutil.which('ffmpeg')
    if not probe or not decoder:raise FileNotFoundError('Installed ffprobe and ffmpeg required')
    # XML terminal checks precede all reads of video/sidecar or process invocation.
    out.mkdir(parents=True);intent={'video':str(video),'videoBytes':video.stat().st_size,'sidecar':str(side),'buttonResultSHA256':hashlib.sha256(button.read_bytes()).hexdigest().upper(),'recordingResultSHA256':hashlib.sha256(record.read_bytes()).hexdigest().upper(),'operation':'metadata probe then one full decode with selected PNG output; original video unchanged'}
    new(out/'intent.json',(json.dumps(intent,indent=2)+'\n').encode())
    result={'status':'FAILED_OR_INCOMPLETE_RETAINED','visualReviewPassed':False,'intent':intent}
    try:
        kw={'creationflags':getattr(subprocess,'CREATE_NO_WINDOW',0),'stdin':subprocess.DEVNULL}
        cmd=[probe,'-v','error','-select_streams','v:0','-show_entries','stream=codec_name,width,height,r_frame_rate,avg_frame_rate,nb_frames,duration:format=duration','-of','json',str(video)]
        pr=subprocess.run(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,**kw)
        new(out/'ffprobe.stdout.json',pr.stdout);new(out/'ffprobe.stderr.txt',pr.stderr)
        if pr.returncode:raise ValueError('ffprobe failed: '+str(pr.returncode))
        metadata=json.loads(pr.stdout);streams=metadata.get('streams',[])
        if len(streams)!=1:raise ValueError('Exactly one selected video stream required')
        st=streams[0];n=int(st['nb_frames']);fps=Fraction(st['avg_frame_rate'])
        if (int(st['width']),int(st['height']))!=(864,692) or fps!=8 or n<=0:raise ValueError('Expected actual864x692/8fps/nonempty frame metadata')
        result['metadata']=metadata
        first=last=previous=None;frame_rows=0;kinds={};caller_jumps=[];native_jumps=[];max_capture=0;max_sim_gap=0;max_wall_gap=0;dimensions=set();native_values=set();counter_last={"callerMissed":0,"nativeDroppedFrames":0};stopped=False
        with side.open('rb') as f:
            for lineno,line in enumerate(f,1):
                if not line.endswith(b'\n'):raise ValueError('Stopped sidecar has incomplete final record')
                row=json.loads(line);kind=row.get('kind');kinds[kind]=kinds.get(kind,0)+1
                if kind in ('capture_failure','stop_failure'):raise ValueError('Recorder failure event in sidecar')
                for key,dest in [('callerMissed',caller_jumps),('nativeDroppedFrames',native_jumps)]:
                    if key not in row:continue
                    value=int(row[key]);delta=value-counter_last[key]
                    if delta<0:raise ValueError('Nonmonotonic recorder counter '+key)
                    if delta:dest.append({'kind':kind,'idx':row.get('idx'),'from':counter_last[key],'to':value,'delta':delta,'simulationMs':row.get('simulationBeginMs',row.get('simulationMs')),'wallMs':row.get('wallBeginMs',row.get('wallMs'))})
                    counter_last[key]=value
                    if key=='nativeDroppedFrames':native_values.add(value)
                if kind=='stopped':stopped=True
                if kind!='frame_submitted':continue
                frame_rows+=1
                if int(row['idx'])!=frame_rows:raise ValueError('Submitted frame index gap or duplicate')
                dims=(int(row['bitmapWidth']),int(row['bitmapHeight']));dimensions.add(dims)
                if dims!=(864,692):raise ValueError('Submitted bitmap dimensions changed')
                max_capture=max(max_capture,float(row['wallEndMs'])-float(row['wallBeginMs']))
                native_values.add(int(row['nativeDroppedFrames']))
                if previous:
                    sg=float(row['simulationBeginMs'])-float(previous['simulationBeginMs']);wg=float(row['wallBeginMs'])-float(previous['wallBeginMs'])
                    if sg<0 or wg<0:raise ValueError('Frame simulation/wall time rewound')
                    max_sim_gap=max(max_sim_gap,sg);max_wall_gap=max(max_wall_gap,wg)
                if first is None:first=row
                last=row;previous=row
        if kinds.get('start')!=1 or kinds.get('stopped')!=1 or not stopped or frame_rows==0:raise ValueError('Complete unique recorder start/stopped lifecycle required')
        result['timing']={'submittedFrameRows':frame_rows,'encodedMetadataFrameCount':n,'submittedAndEncodedCountEqual':frame_rows==n,'firstFrame':first,'lastFrame':last,'callerMissedFinal':counter_last['callerMissed'],'allCallerMissedJumps':caller_jumps,'nativeDroppedFinalObserved':counter_last['nativeDroppedFrames'],'allNativeDroppedJumps':native_jumps,'nativeDroppedAlwaysObservedZero':native_values=={0},'maximumCaptureDurationMs':max_capture,'maximumAdjacentSimulationBeginGapMs':max_sim_gap,'maximumAdjacentWallBeginGapMs':max_wall_gap,'bitmapDimensions':sorted(dimensions),'clockNote':'Caller missed scheduling periods and native encoder dropped frames are distinct counters. MP4 duration at8fps is not simulation elapsed or wall elapsed.'}
        indices=sorted(set([round(i*(n-1)/11) for i in range(12)]+[min(n-1,8),min(n-1,16),min(n-1,24)]))
        if len(indices)>15:raise AssertionError('At most fifteen review PNGs')
        select='+'.join('eq(n\\,'+str(i)+')' for i in indices)
        cmd=[decoder,'-v','error','-xerror','-err_detect','explode','-i',str(video),'-map','0:v:0','-an','-sn','-dn','-vf','select='+select,'-fps_mode','vfr',str(out/'review_%02d.png')]
        new(out/'decode_command.json',(json.dumps(cmd,indent=2)+'\n').encode())
        with (out/'ffmpeg.stderr.txt').open('xb') as err,(out/'ffmpeg.stdout.txt').open('xb') as stdout:
            decoded=subprocess.run(cmd,stdout=stdout,stderr=err,**kw)
        result['decodeReturnCode']=decoded.returncode
        if decoded.returncode:raise ValueError('Full decode failed; no video acceptance')
        pngs=sorted(out.glob('review_*.png'))
        if len(pngs)!=len(indices):raise ValueError('Selected review image count differs from planned indices')
        result['reviewImages']=[{'file':f.name,'frameIndexZeroBased':i,'encodedTimeSeconds':float(Fraction(i,1)/fps)} for f,i in zip(pngs,indices)]
        result['status']='DECODE_AND_TIMING_ONLY_VISUAL_REVIEW_PENDING'
        result['visualReviewRequirement']='Root and independent reviewer must inspect start/middle/end PNGs and full-run coverage before final video PASS. No automatic claim about stripes, black frames, CAD content or full-motion coverage.'
    except Exception as e:
        result['error']=str(e)
    finally:new(out/'result.json',(json.dumps(result,indent=2)+'\n').encode())
    print(result['status'],out/'result.json')
    if result['status']!='DECODE_AND_TIMING_ONLY_VISUAL_REVIEW_PENDING':raise SystemExit(1)
if __name__=='__main__':main()
