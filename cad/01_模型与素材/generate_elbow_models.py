"""Generate a watertight hollow 30-degree short elbow STL in millimetres.

Uses numpy/scipy. Circular OD215, wall15 and Z-height150 are simultaneous.
STL contains no units; import using mm. No real robot motion is issued.
"""
from pathlib import Path
from collections import Counter
import json
import math
import struct
import numpy as np
from scipy.spatial.transform import Rotation

ROOT = Path(__file__).resolve().parent
NTHETA, NPHI = 48, 96
R, A, WALL = 192.5, 107.5, 15.0
BEND_ANGLE = math.pi / 6


def cross_section(phi, b, inner=False):
    q = np.array([A * np.cos(phi), b * np.sin(phi)])
    if inner:
        normal = np.array([np.cos(phi) / A, np.sin(phi) / b])
        q -= WALL * normal / np.linalg.norm(normal)
    return q


def mesh(b):
    vertices = []
    for inner in [False, True]:
        for i in range(NTHETA + 1):
            theta = BEND_ANGLE * i / NTHETA
            radial = np.array([np.cos(theta), 0.0, -np.sin(theta)])
            center = np.array([R * (1 - np.cos(theta)), 0.0, R * np.sin(theta)])
            for j in range(NPHI):
                rho, z = cross_section(2 * math.pi * j / NPHI, b, inner)
                vertices.append(center + rho * radial + np.array([0, z, 0]))
    v = np.asarray(vertices)
    layer = (NTHETA + 1) * NPHI

    def ix(i, j, inner=0):
        return inner * layer + i * NPHI + j % NPHI

    f = []
    for inner in [0, 1]:
        for i in range(NTHETA):
            for j in range(NPHI):
                a, c, d, e = ix(i, j, inner), ix(i + 1, j, inner), ix(i + 1, j + 1, inner), ix(i, j + 1, inner)
                pairs = [(a, c, d), (a, d, e)]
                f.extend(pairs if inner else [tuple(reversed(t)) for t in pairs])
    for i in [0, NTHETA]:
        theta = BEND_ANGLE * i / NTHETA
        expected = np.array([np.sin(theta), 0.0, np.cos(theta)]) * (-1 if i == 0 else 1)
        for j in range(NPHI):
            a, c, d, e = ix(i, j), ix(i, j + 1), ix(i, j + 1, 1), ix(i, j, 1)
            for tri in [(a, c, d), (a, d, e)]:
                p = v[list(tri)]
                if np.dot(np.cross(p[1] - p[0], p[2] - p[0]), expected) < 0:
                    tri = tuple(reversed(tri))
                f.append(tri)
    return v, np.asarray(f, dtype=np.int32)


def validate(v, f):
    edges = Counter()
    directed = Counter()
    for tri in f:
        for a, b in zip(tri, np.roll(tri, -1)):
            edges[tuple(sorted((int(a), int(b))))] += 1
            directed[(int(a), int(b))] += 1
    assert set(edges.values()) == {2}, "Non-manifold or open boundary"
    assert all(directed[(b, a)] == n for (a, b), n in directed.items()), "Inconsistent winding"
    t = v[f]
    areas = np.linalg.norm(np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0]), axis=1) / 2
    assert np.all(areas > 1e-8)
    volume = float(np.einsum('ij,ij->i', t[:, 0], np.cross(t[:, 1], t[:, 2])).sum() / 6)
    assert volume > 0
    euler = len(v) - len(edges) + len(f)
    assert euler == 0, "Hollow elbow with annular ends must have genus one"
    return {
        "vertices": len(v), "triangles": len(f), "closed_two_manifold": True,
        "consistent_winding": True, "euler_characteristic": euler,
        "minimum_triangle_area_mm2": float(areas.min()),
        "solid_volume_mm3": volume,
        "bbox_min_mm": v.min(axis=0).tolist(), "bbox_max_mm": v.max(axis=0).tolist(),
        "bbox_size_mm": np.ptp(v, axis=0).tolist(),
        "validation_scope": "Topology, winding, positive volume and nondegenerate facets. RobotStudio import and robot reach/collision are not yet tested.",
    }


def contact(theta, phi, b, name):
    radial = np.array([np.cos(theta), 0.0, -np.sin(theta)])
    center = np.array([R * (1 - np.cos(theta)), 0.0, R * np.sin(theta)])
    rho, z = cross_section(phi, b)
    p = center + rho * radial + np.array([0, z, 0])
    n = (np.cos(phi) / A) * radial + np.array([0, np.sin(phi) / b, 0])
    n /= np.linalg.norm(n)
    x = np.array([np.sin(theta), 0.0, np.cos(theta)])
    tcp_z = -n
    y = np.cross(tcp_z, x)
    matrix = np.column_stack([x, y, tcp_z])
    assert np.allclose(matrix.T @ matrix, np.eye(3))
    assert np.isclose(np.linalg.det(matrix), 1)
    qxyzw = Rotation.from_matrix(matrix).as_quat()
    qwxyz = np.roll(qxyzw, 1)
    if qwxyz[0] < 0:
        qwxyz *= -1
    return {
        "name": name, "surface": "outer material surface, away from open ends",
        "theta_deg": math.degrees(theta), "phi_deg": math.degrees(phi),
        "position_part_mm": p.tolist(), "outward_surface_normal_part": n.tolist(),
        "tcp_positive_z_part": tcp_z.tolist(), "tcp_quaternion_wxyz_part": qwxyz.tolist(),
        "approach_rule": "approach_position = contact_position + clearance_mm * outward_surface_normal; move toward contact along -normal",
        "qualification": "Geometric contact candidate only. Cup diameter, curvature, interference and vacuum performance require validation.",
    }


def write_stl(path, v, f):
    with path.open('wb') as out:
        out.write(b'MA6212 hollow elbow; units mm; generated approximation'.ljust(80, b' '))
        out.write(struct.pack('<I', len(f)))
        for ids in f:
            p = v[ids]
            n = np.cross(p[1] - p[0], p[2] - p[0])
            n /= np.linalg.norm(n)
            out.write(struct.pack('<12fH', *n, *p.ravel(), 0))
    assert path.stat().st_size == 84 + 50 * len(f)


def preview(v, f, path):
    # Static geometry inspection aid, not a RobotStudio simulation result.
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    from mpl_toolkits.mplot3d.art3d import Poly3DCollection
    fig = plt.figure(figsize=(8, 7))
    ax = fig.add_subplot(111, projection='3d')
    ax.add_collection3d(Poly3DCollection(v[f], facecolor='#454d59', edgecolor='none', alpha=1))
    mn, mx = v.min(0), v.max(0)
    center, span = (mn + mx) / 2, max(mx - mn)
    ax.set_xlim(center[0] - span / 2, center[0] + span / 2)
    ax.set_ylim(center[1] - span / 2, center[1] + span / 2)
    ax.set_zlim(center[2] - span / 2, center[2] + span / 2)
    ax.set_box_aspect([1, 1, 1])
    ax.view_init(elev=55, azim=45)
    ax.set_xlabel('X (mm)'); ax.set_ylabel('Y (mm)'); ax.set_zlabel('Z (mm)')
    ax.set_title(path.stem + '\nGeometry preview; not simulation evidence')
    fig.savefig(path, dpi=130, bbox_inches='tight')
    plt.close(fig)


def main():
    result = {
        "units": "mm (STL is unitless; select millimetres on import)",
        "part_frame": "First opening centre is origin, first opening in XY plane. Centreline c(t)=[R*(1-cos(t)),0,R*sin(t)], t=0..30deg. Z upward.",
        "bend_angle_deg": 30.0,
        "centerline_radius_mm": R, "nominal_max_outer_diameter_mm": 2 * A,
        "normal_wall_thickness_mm": WALL, "nominal_workpiece_mass_kg": 2.5,
        "mass_note": "Mass is supplied separately for controller load data; density/material are not inferred from STL volume.",
        "models": [],
    }
    for stem, b, note in [
        ('elbow_short30_OD215_H150_W15_R192p5', 107.5, 'Circular OD215, inner diameter185, wall15, Z-height150. A 30deg bend and centerline radius192.5 are explicit design assumptions, not measured from video. Height=(R+r)*sin(30deg)=150. No ellipse or flattened cross-section.'),
    ]:
        v, f = mesh(b)
        entry = {"file": stem + '.stl', "model_assumption": note, "validation": validate(v, f)}
        assert np.isclose(np.ptp(v, axis=0)[2], 150.0)
        entry["contacts"] = [contact(math.radians(10), math.pi / 2, b, 'C_pick1_side'), contact(math.radians(20), math.pi, b, 'C_pick2_outer_bend')]
        write_stl(ROOT / entry['file'], v, f)
        preview(v, f, ROOT / (stem + '_preview.png'))
        result['models'].append(entry)
    target = ROOT / 'elbow_geometry_manifest.json'
    target.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
