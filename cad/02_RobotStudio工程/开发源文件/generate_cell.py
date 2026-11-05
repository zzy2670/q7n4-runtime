"""Generate layout-dependent RAPID and scene geometry; does not run RobotStudio.
Canonical editable input: layout.json. CAD and contact data: linked manifest.
"""
import hashlib,json,math,re,struct
from pathlib import Path
import numpy as np
import xml.etree.ElementTree as ET
HERE=Path(__file__).resolve().parent

def qmat(q):
    q=np.array(q,dtype=float); q=q/np.linalg.norm(q); w,x,y,z=q
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],
      [2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],
      [2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])

def matq(r):
    # Symmetric eigenproblem is stable for 180-degree rotations.
    K=np.array([[r[0,0]-r[1,1]-r[2,2],r[0,1]+r[1,0],r[0,2]+r[2,0],r[2,1]-r[1,2]],
      [r[0,1]+r[1,0],r[1,1]-r[0,0]-r[2,2],r[1,2]+r[2,1],r[0,2]-r[2,0]],
      [r[0,2]+r[2,0],r[1,2]+r[2,1],r[2,2]-r[0,0]-r[1,1],r[1,0]-r[0,1]],
      [r[2,1]-r[1,2],r[0,2]-r[2,0],r[1,0]-r[0,1],np.trace(r)]])/3
    _,v=np.linalg.eigh(K); a=v[:,-1]; q=np.array([a[3],a[0],a[1],a[2]])
    if q[0]<0:q=-q
    assert np.allclose(qmat(q),r,atol=1e-7)
    return q

def pose(p):
    T=np.eye(4);T[:3,:3]=qmat(p['quaternion_wxyz']);T[:3,3]=p['position_mm'];return T

def contact(c):
    return pose({'position_mm':c['position_part_mm'],'quaternion_wxyz':c['tcp_quaternion_wxyz_part']})

def mesh_triangles(path):
    raw=path.read_bytes()
    if len(raw)>=84:
        n=struct.unpack_from('<I',raw,80)[0]
        if len(raw)==84+n*50:
            dt=np.dtype([('n','<f4',(3,)),('v','<f4',(3,3)),('a','<u2')])
            return np.frombuffer(raw,dtype=dt,offset=84,count=n)['v'].astype(float)
    text=raw.decode('ascii'); vertices=re.findall(r'vertex\s+([-+\d.eE]+)\s+([-+\d.eE]+)\s+([-+\d.eE]+)',text)
    return np.array(vertices,dtype=float).reshape(-1,3,3)

def mass_properties(tris,mass):
    a,b,c=tris[:,0],tris[:,1],tris[:,2]
    vol=np.einsum('ij,ij->i',a,np.cross(b,c))/6
    V=vol.sum(); assert V>0
    S=a+b+c; com=(vol[:,None]*S/4).sum(axis=0)/V
    second=(vol[:,None,None]*(np.einsum('ni,nj->nij',S,S)+
       np.einsum('nki,nkj->nij',tris,tris))/20).sum(axis=0)/V*mass
    J=np.trace(second)*np.eye(3)-second-mass*(np.dot(com,com)*np.eye(3)-np.outer(com,com))
    return com,J/1e6,V

def fmt(x):
    if abs(float(x))<1e-10:x=0
    return f'{float(x):.9g}'

def arr(a):return '['+','.join(fmt(x) for x in a)+']'

def main():
    layoutPath=HERE/'layout.json'; L=json.loads(layoutPath.read_text(encoding='utf8'))
    manifestPath=(HERE/L['model_manifest']).resolve(); M=json.loads(manifestPath.read_text(encoding='utf8'))
    model=M['models'][L['model_index']]; tris=mesh_triangles(manifestPath.parent/model['file'])
    contacts={c['name']:c for c in model['contacts']}
    C1,C2=[contact(contacts[n]) for n in L['model_contact_names']]
    mass=M['nominal_workpiece_mass_kg']; com,inertia,V=mass_properties(tris,mass)
    Tsrc,Tfix=[pose(L['part_poses'][n]) for n in ['source','fixture']]
    pack=L['packing']; ang=math.radians(pack['part_rotation_y_deg'])
    box_approach=pack['wall_height_mm']+L['motion']['box_entry_clearance_above_rim_mm']
    Rpack=qmat([math.cos(ang/2),0,math.sin(ang/2),0])
    verts=tris.reshape(-1,3); vp=verts@Rpack.T; bmin=vp.min(axis=0);bmax=vp.max(axis=0);size=bmax-bmin
    bboxcenter=(bmin+bmax)/2; nx,ny=pack['columns'],pack['rows'];gap=pack['part_gap_mm']
    pitchx,pitchy=size[0]+gap,size[1]+gap
    floor=np.array(pack['floor_center_mm'],float); placements=[]
    for row in range(ny):
      for col in range(nx):
        T=np.eye(4);T[:3,:3]=Rpack
        T[:3,3]=[floor[0]+(col-(nx-1)/2)*pitchx-bboxcenter[0],
          floor[1]+(row-(ny-1)/2)*pitchy-bboxcenter[1],floor[2]-bmin[2]]
        placements.append(T)
    targets={'pHome':pose(L['motion']['ready_tcp_world']),'pSource':Tsrc@C1,'pFixturePlace':Tfix@C1,'pFixturePick':Tfix@C2}
    assert L['motion']['source_pick_retract_mode']=='world_z'
    assert L['motion']['fixture_place_entry_mode']=='world_z'
    for original,lift in [('pSource','pSourceLift'),('pFixturePlace','pFixturePlaceLift')]:
        targets[lift]=targets[original].copy()
        targets[lift][2,3]+=L['motion']['contact_approach_mm']
    for k,T in enumerate(placements): targets['pBox_'+str(k+1)]=T@C2
    configs=L['commissioning']['target_configurations']
    def rt(name):
        T=targets[name]; cf=configs.get(name,[0,0,0,0])
        return '['+arr(T[:3,3])+','+arr(matq(T[:3,:3]))+','+arr(cf)+',[9E9,9E9,9E9,9E9,9E9,9E9]]'
    decl=['    PERS robtarget '+n+':='+rt(n)+';' for n in ['pHome','pSource','pSourceLift','pFixturePlace','pFixturePlaceLift','pFixturePick']]
    decl+=['    PERS robtarget pBoxTargets{'+str(len(placements))+'}:=['+','.join(rt('pBox_'+str(k+1)) for k in range(len(placements)))+'];']
    payload=[]
    for name,C in [('elbowLoadAtC1',C1),('elbowLoadAtC2',C2)]:
        rc=C[:3,:3]; cg=rc.T@(com-C[:3,3]); it=rc.T@inertia@rc
        values,axes=np.linalg.eigh(it)
        if np.linalg.det(axes)<0: axes[:,0]*=-1
        assert np.all(values>0)
        payload+=['    PERS loaddata '+name+':=['+fmt(mass)+','+arr(cg)+','+arr(matq(axes))+','+','.join(fmt(v) for v in values)+'];']
    template=(HERE/'MA6212_ElbowCell.mod.template').read_text()
    code=template.replace('@@TARGETS@@','\n'.join(decl)).replace('@@PAYLOADS@@','\n'.join(payload))
    code=code.replace('PERS bool layoutGenerated:=FALSE','PERS bool layoutGenerated:=TRUE')
    # Commissioning remains FALSE until actual runtime checks and target configs exist.
    code=code.replace('PERS num boxCapacity:=6','PERS num boxCapacity:='+str(nx*ny))
    code=re.sub(r'    PERS num gridColumns:.*?;\n|    PERS num gridPitchX:.*?;\n|    PERS num gridPitchY:.*?;\n','',code)
    code=code.replace('            column:=slot MOD gridColumns;\n            row:=Trunc(slot/gridColumns);\n','')
    code=code.replace('pPlace:=Offs(pBoxFirst,column*gridPitchX,row*gridPitchY,0);','pPlace:=pBoxTargets{slot+1};')
    code=code.replace('PERS num clearance:=120','PERS num clearance:='+fmt(L['motion']['contact_approach_mm']))
    code=code.replace('PERS num boxClearance:=340','PERS num boxClearance:='+fmt(box_approach))
    code=code.replace('PERS num fixtureDwell:=0.5','PERS num fixtureDwell:='+fmt(L['motion']['fixture_dwell_s']))
    code=code.replace('PERS speeddata vTravel:=[200,','PERS speeddata vTravel:=['+fmt(L['motion']['transfer_speed_mm_s'])+',')
    code=code.replace('PERS speeddata vApproach:=[80,','PERS speeddata vApproach:=['+fmt(L['motion']['approach_speed_mm_s'])+',')
    tool=L['tool']; tooldecl='    PERS tooldata tVacuum:=[TRUE,['+arr(tool['tcp_translation_flange_mm'])+','+arr(tool['tcp_quaternion_flange_wxyz'])+'],['+fmt(tool['mass_kg'])+','+arr(tool['cog_flange_mm'])+',[1,0,0,0],'+','.join(fmt(x) for x in tool['inertia_kg_m2'])+']];'
    # Sole global tool is the actually commissioned MA6212_Tooling module.
    # Do not redeclare tVacuum when this process module is loaded beside it.
    code=re.sub(r'    PERS tooldata tVacuum:.*?;','    ! Uses unique global MA6212_Tooling/tVacuum; load and verify Tooling first.',code)
    offsets=[[0,0]]*L['tests']['nominal_cycles']+L['tests']['offsets_world_xy_mm']
    for axis,n in [(0,'X'),(1,'Y')]:
        code=re.sub(r'CONST num offset'+n+r'\{20\}:=\[.*?\];','CONST num offset'+n+'{20}:='+arr([p[axis] for p in offsets])+';',code)
    assert len(offsets)==20 and L['tests']['nominal_cycles']==10 and L['tests']['offset_cycles']==10
    (HERE/'MA6212_ElbowCell_DRAFT.mod').write_text(code,encoding='ascii')
    # Scene output consumed by CellBuilder: all dimensions/positions derive here.
    scene=ET.Element('SceneGeometry',{'status':'UNCOMMISSIONED','units':'mm'})
    def box(name,pos,sz,color):
        ET.SubElement(scene,'Box',{'Name':name,'Color':color,**{k:fmt(v) for k,v in zip(['x','y','z'],pos)},**{k:fmt(v) for k,v in zip(['sx','sy','sz'],sz)}})
    box('Floor',[-600,-1000,-40],[2200,2200,40],'DarkSlateGray')
    conveyor=pose(L['equipment_poses']['conveyor_support'])
    assert np.allclose(conveyor[:3,:3],np.eye(3)), 'Conveyor generator currently supports identity orientation only'
    src=conveyor[:3,3];box('ConveyorFrame',[src[0]-250,src[1]-300,src[2]-200],[500,600,180],'Silver')
    box('ConveyorSurface',[src[0]-250,src[1]-300,src[2]-20],[500,600,20],'WhiteSmoke')
    fx=Tfix[:3,3];box('InterimFixture',[fx[0]-150,fx[1]-150,0],[300,300,fx[2]],'SteelBlue')
    innerx=(nx-1)*pitchx+size[0]+2*pack['edge_clearance_mm'];innery=(ny-1)*pitchy+size[1]+2*pack['edge_clearance_mm']
    xmin,ymin=floor[:2]-np.array([innerx,innery])/2;t=pack['wall_thickness_mm'];h=pack['wall_height_mm'];ft=pack['floor_thickness_mm']
    box('PackingBoxFloor',[xmin-t,ymin-t,floor[2]-ft],[innerx+2*t,innery+2*t,ft],'Tan')
    box('PackingBoxWall1',[xmin-t,ymin-t,floor[2]],[innerx+2*t,t,h],'BurlyWood')
    box('PackingBoxWall2',[xmin-t,ymin+innery,floor[2]],[innerx+2*t,t,h],'BurlyWood')
    box('PackingBoxWall3',[xmin-t,ymin,floor[2]],[t,innery,h],'BurlyWood')
    box('PackingBoxWall4',[xmin+innerx,ymin,floor[2]],[t,innery,h],'BurlyWood')
    for n,T in [('SourcePart',Tsrc),('FixturePart',Tfix)]+[('BoxPart_'+str(i+1),T) for i,T in enumerate(placements)]:
        ET.SubElement(scene,'PartPose',{'Name':n,'position_mm':','.join(fmt(x) for x in T[:3,3]),'quaternion_wxyz':','.join(fmt(x) for x in matq(T[:3,:3]))})
    ET.indent(scene);ET.ElementTree(scene).write(HERE/'scene_geometry.generated.xml',encoding='utf-8',xml_declaration=True)
    audit={'status':'GEOMETRIC_GENERATION_ONLY_NOT_ROBOT_VALIDATION','layout_sha256':hashlib.sha256(layoutPath.read_bytes()).hexdigest(),'model_manifest_sha256':hashlib.sha256(manifestPath.read_bytes()).hexdigest(),'packing_rotated_bbox_mm':{'min':bmin.tolist(),'max':bmax.tolist(),'size':size.tolist()},'packing_pitch_mm':[pitchx,pitchy],'inner_box_size_mm':[innerx,innery,h],'uniform_shell_load':{'mass_kg':mass,'volume_mm3':V,'com_part_mm':com.tolist(),'inertia_com_part_kg_m2':inertia.tolist()},'targets':{}}
    for n,T in targets.items():
        distance=0 if n.endswith('Lift') else box_approach if n.startswith('pBox_') else L['motion']['contact_approach_mm']
        approach=T.copy(); approach[:3,3]-=T[:3,2]*distance
        audit['targets'][n]={'tcp_world_mm':T[:3,3].tolist(),'tcp_q_wxyz':matq(T[:3,:3]).tolist(),'approach_world_mm':approach[:3,3].tolist(),'approach_distance_mm':distance,'robconf':configs.get(n),'reachable':'NOT_TESTED'}
    audit['box_entry_strategy']={'retract_distance_mm':box_approach,'above_rim_margin_mm':L['motion']['box_entry_clearance_above_rim_mm'],'rim_world_z_mm':float(floor[2]+h),'part_bottom_at_approach_world_z_mm':float(floor[2]+box_approach),'path_clearance':'NOT_TESTED_REQUIRES_NEW_IK_AND_SEGMENT_COLLISION_CHECKS'}
    # Independent geometric identities: target * inverse(contact) restores intended part pose.
    assert np.allclose(targets['pSource']@np.linalg.inv(C1),Tsrc)
    assert np.allclose(targets['pFixturePlace']@np.linalg.inv(C1),Tfix)
    assert np.allclose(targets['pFixturePick']@np.linalg.inv(C2),Tfix)
    for i,T in enumerate(placements): assert np.allclose(targets['pBox_'+str(i+1)]@np.linalg.inv(C2),T)
    assert abs((Rpack@C2[:3,2])[2]+1)<1e-6
    (HERE/'layout_generation_audit.json').write_text(json.dumps(audit,indent=2)+'\n',encoding='utf8')
    print(json.dumps({'targets':len(targets),'box_inner_mm':audit['inner_box_size_mm'],'part_rotated_size_mm':size.tolist(),'controller_compile':'NOT_RUN'},indent=2))

if __name__=='__main__':main()
