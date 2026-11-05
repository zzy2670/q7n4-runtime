"""Closed revolved vacuum-tool approximation. Millimetres; no CAD install required."""
from pathlib import Path
import json, math, struct, hashlib
from collections import Counter
import numpy as np

ROOT = Path(__file__).resolve().parent
LAYOUT = ROOT.parent / '02_RobotStudio工程' / '开发源文件' / 'layout.json'


def main():
    raw = LAYOUT.read_bytes()
    tool = json.loads(raw.decode('utf-8'))['tool']
    tcp = np.array(tool['tcp_translation_flange_mm'], dtype=float)
    q = np.array(tool['tcp_quaternion_flange_wxyz'], dtype=float)
    assert np.allclose(tcp[:2], 0) and tcp[2] > 0
    assert np.allclose(q, [1, 0, 0, 0]), 'Axial generator needs identity TCP rotation'
    length = float(tcp[2])
    # Radius,z profile: mounting flange, slender shaft, cup neck, bellows and lip.
    # Filled compliant contact membrane closes the tip so TCP lies on material.
    profile = [(30, 0), (30, .0533333333*length),
               (10, .0533333333*length), (10, .80*length),
               (8, .8666666667*length), (11, .8933333333*length),
               (9, .92*length), (18, .9666666667*length), (18, length)]
    n = 96
    vertices = []
    for radius, z in profile:
        for j in range(n):
            a = 2*math.pi*j/n
            vertices.append([radius*math.cos(a), radius*math.sin(a), z])
    bottom, top = len(vertices), len(vertices)+1
    vertices.extend([[0, 0, 0], tcp.tolist()])
    faces = []
    for i in range(len(profile)-1):
        for j in range(n):
            a, b = i*n+j, i*n+(j+1)%n
            c, d = (i+1)*n+j, (i+1)*n+(j+1)%n
            faces.extend([(a, b, d), (a, d, c)])
    for j in range(n):
        k = (j+1)%n
        faces.append((bottom, k, j))
        faces.append((top, (len(profile)-1)*n+j, (len(profile)-1)*n+k))
    v, f = np.array(vertices), np.array(faces, dtype=np.int32)
    edges, directed = Counter(), Counter()
    for tri in f:
        for a,b in zip(tri,np.roll(tri,-1)):
            edges[tuple(sorted((int(a),int(b))))]+=1
            directed[(int(a),int(b))]+=1
    assert set(edges.values()) == {2}
    assert all(directed[(b,a)]==count for (a,b),count in directed.items())
    assert len(v)-len(edges)+len(f)==2
    t=v[f];normals=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0])
    area=np.linalg.norm(normals,axis=1)
    assert np.all(area>0)
    volume=float(np.einsum('ij,ij->i',t[:,0],np.cross(t[:,1],t[:,2])).sum()/6)
    assert volume>0
    assert np.allclose(v[top],tcp)
    path=ROOT/'vacuum_tool_TCP150mm.stl'
    with path.open('wb') as out:
        out.write(b'MA6212 vacuum tool; millimetres; flange origin; +Z toward TCP'.ljust(80,b' '))
        out.write(struct.pack('<I',len(f)))
        for tri,normal in zip(t,normals/area[:,None]):
            out.write(struct.pack('<12fH',*normal,*tri.ravel(),0))
    data=path.read_bytes()
    reread=np.frombuffer(data,offset=84,dtype=np.dtype([('n','<f4',(3,)),('v','<f4',(3,3)),('attr','<u2')]))
    reread_v=reread['v']
    assert len(data)==84+50*len(f) and len(reread)==len(f)
    assert np.allclose(reread_v.max(axis=(0,1)),v.max(0))
    assert np.allclose(reread_v.min(axis=(0,1)),v.min(0))
    manifest={
        'file':path.name,'units':'mm; STL is unitless, choose millimetres when importing',
        'source_layout_relative':'../02_RobotStudio工程/开发源文件/layout.json',
        'source_layout_sha256':hashlib.sha256(raw).hexdigest(),
        'layout_tool':tool,
        'geometry_frame':'Origin at mounting flange plane centre; +Z toward cup contact face. Mount identity at robot flange.',
        'profile_radius_z_mm':profile,
        'tcp_contact_position_flange_mm':tcp.tolist(),
        'contact_face':'Solid compliant contact membrane disk at z=TCP.z, radius18mm; centre is a material vertex, not a hole.',
        'contact_face_outward_normal_flange':[0,0,1],
        'tool_positive_z_flange':[0,0,1],
        'mounting_interface':'Simplified flange disk radius30mm x height8mm at TCP length150mm; screw pattern omitted.',
        'validation':{'vertices':len(v),'triangles':len(f),'bytes':len(data),
            'closed_two_manifold':True,'consistent_winding':True,'euler_characteristic':2,
            'positive_volume_mm3':volume,'bbox_min_mm':v.min(0).tolist(),
            'bbox_max_mm':v.max(0).tolist(),'bbox_size_mm':np.ptp(v,axis=0).tolist(),
            'tcp_material_vertex_exact':True,'binary_stl_reread_passed':True},
        'limitations':[
            'Video-inspired slender tool surrogate; not an exact measured reconstruction.',
            'The compliant contact membrane is a solid simulation approximation; pneumatic passages and real vacuum physics are omitted.',
            'Mass1kg, centre of gravity and inertia are adopted from layout tooldata, not certified or derived from this heterogeneous geometry.',
            'Cup diameter36mm, curvature/contact sealing, robot reach, collision and RobotStudio import remain to be validated.',
            'If TCP or source tooldata changes, rerun this generator and review contact/collision geometry.'
        ]}
    (ROOT/'vacuum_tool_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from mpl_toolkits.mplot3d.art3d import Poly3DCollection
    fig=plt.figure(figsize=(6,8));ax=fig.add_subplot(111,projection='3d')
    colors=['#999999' if tri[:,2].max()<length*.1 else '#405365' if tri[:,2].max()<length*.87 else '#24282b' for tri in t]
    ax.add_collection3d(Poly3DCollection(t,facecolor=colors,edgecolor='none'))
    ax.scatter(*tcp,color='red',s=25);ax.text(*tcp,' TCP',color='red')
    ax.set_xlim(-40,40);ax.set_ylim(-40,40);ax.set_zlim(0,length+10)
    ax.set_box_aspect([80,80,length+10]);ax.view_init(elev=15,azim=40)
    ax.set_xlabel('X mm');ax.set_ylabel('Y mm');ax.set_zlabel('Z mm')
    ax.set_title('Vacuum tool geometry preview\nNot RobotStudio simulation evidence')
    fig.savefig(ROOT/'vacuum_tool_preview.png',dpi=130,bbox_inches='tight');plt.close(fig)
    print(json.dumps(manifest,ensure_ascii=False,indent=2))


if __name__=='__main__':
    main()
