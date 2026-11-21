
from pathlib import Path
import UnityPy

ROOT = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
DATA = ROOT / "Stranded Deep" / "Stranded_Deep_Data"
OUT = ROOT / "ocean_transform_owner_trace.txt"

def getv(obj, key, default=None):
    if isinstance(obj, dict):
        return obj.get(key, default)
    return getattr(obj, key, default)

def deref(ptr):
    if ptr is None:
        return None
    try:
        return ptr.deref()
    except Exception:
        try:
            return ptr.read()
        except Exception:
            return None

def read_tree(obj):
    try:
        return obj.read_typetree()
    except Exception:
        try:
            return obj.read().read_typetree()
        except Exception:
            return None

def file_name(obj):
    return getattr(
        getattr(obj, "assets_file", None),
        "name", "UNKNOWN"
    )

def ptr_ids(ptr):
    if ptr is None:
        return None, None

    fid = getv(ptr, "m_FileID",
               getv(ptr, "file_id", 0))
    pid = getv(ptr, "m_PathID",
               getv(ptr, "path_id", None))
    return fid, pid

def vec(v):
    if v is None:
        return "UNAVAILABLE"
    return (
        getv(v, "x", "?"),
        getv(v, "y", "?"),
        getv(v, "z", "?")
    )

lines = []

def log(s=""):
    lines.append(str(s))
    print(s)

log("OCEAN TRANSFORM OWNER TRACE")
log("MODE: READ ONLY")
log(f"ROOT: {ROOT}")
log(f"DATA: {DATA}")

if not DATA.exists():
    raise SystemExit("ERROR: DATA FOLDER NOT FOUND")

env = UnityPy.load(str(DATA))
objects = env.objects

log(f"TOTAL OBJECTS: {len(objects)}")

# Locate Ceto.Ocean components and their GameObjects.
oceans = []

for obj in objects:
    if getattr(obj.type, "name", "") != "MonoBehaviour":
        continue

    try:
        head = obj.parse_monobehaviour_head()
        script = deref(getv(head, "m_Script"))

        if script is None:
            continue

        st = script.parse_as_object()
        namespace = getv(st, "m_Namespace", "")
        classname = getv(st, "m_ClassName", "")

        fullname = (
            f"{namespace}.{classname}"
            if namespace else classname
        )

        if fullname != "Ceto.Ocean":
            continue

        go = deref(getv(head, "m_GameObject"))

        if go is None:
            continue

        oceans.append({
            "ocean": obj,
            "go": go,
            "go_id": go.path_id,
            "file": file_name(obj)
        })

    except Exception:
        pass

log(f"CETO.OCEAN FOUND: {len(oceans)}")

# Scan Transform objects directly.
transform_objects = [
    obj for obj in objects
    if getattr(obj.type, "name", "") in (
        "Transform", "RectTransform"
    )
]

log(f"TRANSFORM OBJECTS: {len(transform_objects)}")

for index, item in enumerate(oceans, 1):
    ocean = item["ocean"]
    go = item["go"]
    go_id = item["go_id"]
    target_file = item["file"]

    go_tree = read_tree(go)

    log("\n" + "=" * 60)
    log(f"OCEAN #{index}")
    log(f"FILE: {target_file}")
    log(f"OCEAN PATH ID: {ocean.path_id}")
    log(f"GAMEOBJECT: {getv(go_tree, 'm_Name', 'UNKNOWN')}")
    log(f"GAMEOBJECT PATH ID: {go_id}")

    components = getv(go_tree, "m_Component", None)

    if components is None:
        log("GAMEOBJECT COMPONENT LIST: UNAVAILABLE")
    else:
        log(f"GAMEOBJECT COMPONENT COUNT: {len(components)}")

    matches = 0

    for tr in transform_objects:
        if file_name(tr) != target_file:
            continue

        tree = read_tree(tr)

        if not isinstance(tree, dict):
            continue

        owner = getv(tree, "m_GameObject")
        fid, pid = ptr_ids(owner)

        if pid != go_id or fid not in (0, None):
            continue

        matches += 1

        parent = getv(tree, "m_Father")
        parent_fid, parent_pid = ptr_ids(parent)

        log("\n--- MATCHED TRANSFORM ---")
        log(f"TYPE: {tr.type.name}")
        log(f"TRANSFORM PATH ID: {tr.path_id}")
        log(f"OWNER PATH ID: {pid}")
        log(f"LOCAL POSITION: {vec(getv(tree, 'm_LocalPosition'))}")
        log(f"LOCAL ROTATION: {getv(tree, 'm_LocalRotation')}")
        log(f"LOCAL SCALE: {vec(getv(tree, 'm_LocalScale'))}")
        log(f"PARENT FILE ID: {parent_fid}")
        log(f"PARENT PATH ID: {parent_pid}")

    if matches == 0:
        log("TRANSFORM MATCH: NOT FOUND")
    else:
        log(f"TRANSFORM MATCHES: {matches}")

log("\n" + "=" * 60)
log("TRACE SUMMARY")
log(f"CETO.OCEAN: {len(oceans)}")
log(f"TRANSFORM OBJECTS SCANNED: {len(transform_objects)}")

OUT.write_text("\n".join(lines), encoding="utf-8")

log(f"SAVED: {OUT}")
log("TRACE COMPLETE")
