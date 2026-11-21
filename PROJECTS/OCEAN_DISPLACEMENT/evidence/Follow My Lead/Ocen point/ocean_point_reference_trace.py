
from pathlib import Path
import hashlib
import traceback
import UnityPy

ROOT = Path("/storage/emulated/0/Download/Stranded-Deep-Original")
OUT = ROOT / "ocean_point_reference_trace.txt"

DATA = ROOT / "Stranded Deep" / "Stranded_Deep_Data"

if not DATA.exists():
    matches = list(ROOT.rglob("Stranded_Deep_Data"))
    if matches:
        DATA = matches[0]

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

def vec(v):
    if v is None:
        return "UNAVAILABLE"
    return (
        getv(v, "x", "?"),
        getv(v, "y", "?"),
        getv(v, "z", "?"),
    )

lines = []

def log(s=""):
    lines.append(str(s))
    print(s)

log("OCEAN POINT REFERENCE TRACE")
log("MODE: READ ONLY")
log(f"ROOT: {ROOT}")
log(f"DATA: {DATA}")

if not DATA.exists():
    raise SystemExit("ERROR: Stranded_Deep_Data not found")

dll = DATA / "Managed" / "Assembly-CSharp.dll"
if dll.exists():
    digest = hashlib.sha256(dll.read_bytes()).hexdigest()
    log(f"DLL SHA256: {digest}")

log("\n[1] LOADING ALL DATA FILES INTO ONE ENVIRONMENT")

env = UnityPy.load(str(DATA))
objects = env.objects

log(f"TOTAL OBJECTS: {len(objects)}")

log("\n[2] RESOLVING MONOSCRIPT REFERENCES")

found = 0
script_errors = 0

for obj in objects:
    if getattr(obj.type, "name", "") != "MonoBehaviour":
        continue

    try:
        head = obj.parse_monobehaviour_head()
        script_ptr = getv(head, "m_Script")
        script_obj = deref(script_ptr)

        if script_obj is None:
            continue

        script = script_obj.parse_as_object()

        namespace = getv(script, "m_Namespace", "")
        classname = getv(script, "m_ClassName", "")
        assembly = getv(script, "m_AssemblyName", "")

        fullname = (
            f"{namespace}.{classname}"
            if namespace else classname
        )

        if fullname.lower() != "ceto.ocean":
            continue

        found += 1

        log("\n" + "=" * 65)
        log(f"OCEAN COMPONENT #{found}")
        log(f"ASSEMBLY: {assembly}")
        log(f"CLASS: {fullname}")
        log(f"MONOBEHAVIOUR PATH ID: {obj.path_id}")
        log(f"FILE: {obj.assets_file.name}")

        # Resolve owning GameObject
        go_ptr = getv(head, "m_GameObject")
        go_obj = deref(go_ptr)

        if go_obj is None:
            log("GAMEOBJECT: UNRESOLVED")
            continue

        go_tree = read_tree(go_obj)
        go_name = getv(go_tree, "m_Name", "UNKNOWN")

        log(f"GAMEOBJECT: {go_name}")
        log(f"GAMEOBJECT PATH ID: {go_obj.path_id}")

        # Resolve Transform from GameObject components
        components = getv(go_tree, "m_Component", []) or []
        transform_found = False

        for entry in components:
            component_ptr = getv(entry, "component")
            component_obj = deref(component_ptr)

            if component_obj is None:
                continue

            type_name = getattr(
                getattr(component_obj, "type", None),
                "name", ""
            )

            if type_name not in ("Transform", "RectTransform"):
                continue

            transform_found = True
            transform_tree = read_tree(component_obj)

            log(f"TRANSFORM TYPE: {type_name}")
            log(f"TRANSFORM PATH ID: {component_obj.path_id}")

            log(
                "LOCAL POSITION: "
                + str(vec(getv(transform_tree, "m_LocalPosition")))
            )

            log(
                "LOCAL ROTATION: "
                + str(getv(transform_tree, "m_LocalRotation"))
            )

            log(
                "LOCAL SCALE: "
                + str(vec(getv(transform_tree, "m_LocalScale")))
            )

            father = getv(transform_tree, "m_Father")
            log(
                "PARENT PATH ID: "
                + str(getv(father, "m_PathID", "UNKNOWN"))
            )

        if not transform_found:
            log("TRANSFORM: NOT RESOLVED")

        # Try to read serialized Ceto.Ocean fields
        log("\n--- SERIALIZED OCEAN DATA ---")

        tree = read_tree(obj)

        if isinstance(tree, dict):
            log(f"SERIALIZED FIELDS: {list(tree.keys())}")

            if "level" in tree:
                log(f"OCEAN LEVEL: {tree['level']}")
            else:
                log("OCEAN LEVEL: NOT IN AVAILABLE TYPE TREE")
        else:
            log("OCEAN TYPE TREE: UNAVAILABLE")

        # Record raw data for later field-layout analysis
        try:
            raw = obj.get_raw_data()
            log(f"RAW OBJECT SIZE: {len(raw)} bytes")
            log(f"RAW OBJECT HEX: {raw.hex()}")
        except Exception as e:
            log(f"RAW DATA ERROR: {e}")

    except Exception as e:
        script_errors += 1
        log(f"REFERENCE ERROR: {type(e).__name__}: {e}")

log("\n" + "=" * 65)
log("SUMMARY")
log(f"TOTAL OBJECTS: {len(objects)}")
log(f"CETO.OCEAN MATCHES: {found}")
log(f"REFERENCE ERRORS: {script_errors}")

if found:
    log("RESULT: CETO.OCEAN COMPONENT LOCATED")
else:
    log("RESULT: CETO.OCEAN COMPONENT NOT RESOLVED")
    log("NOTE: This does not prove the Ocean object is absent.")

OUT.write_text("\n".join(lines), encoding="utf-8")

log(f"\nSAVED: {OUT}")
log("TRACE COMPLETE")
