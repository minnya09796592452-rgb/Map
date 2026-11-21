import os, re, json, struct
from pathlib import Path

SRC = Path(
    "/storage/emulated/0/Download/"
    "Stranded-Deep-Original/Godot Project"
).resolve()
OUT_DIR = Path("Castaway_Depths_Audit").resolve()

def parse_glb(p):
    info = {
        "size": p.stat().st_size,
        "valid": False,
        "version": 0,
        "nodes": [],
        "meshes": [],
        "materials": [],
        "textures": 0,
        "images": [],
        "skins": 0,
        "animations": [],
        "ext_uris": [],
        "err": None,
    }
    try:
        with open(p, "rb") as f:
            hdr = f.read(12)
            if len(hdr) < 12:
                info["err"] = "hdr<12"
                return info
            mg, ver, tlen = struct.unpack(
                "<4sII", hdr
            )
            if mg != b"glTF":
                info["err"] = f"magic={mg}"
                return info
            info["version"] = ver
            ch0 = f.read(8)
            if len(ch0) < 8:
                info["err"] = "ch0<8"
                return info
            clen, ctype = struct.unpack(
                "<II", ch0
            )
            if ctype != 0x4E4F534A:
                info["err"] = "no_json_chunk"
                return info
            jraw = f.read(clen)
            g = json.loads(
                jraw.decode("utf-8", "replace")
            )
            info["valid"] = True
            info["nodes"] = [
                n.get("name", f"node_{i}")
                for i, n in enumerate(
                    g.get("nodes", [])
                )
            ]
            for i, m in enumerate(
                g.get("meshes", [])
            ):
                prims = len(
                    m.get("primitives", [])
                )
                mn = m.get("name", f"mesh_{i}")
                info["meshes"].append(
                    f"{mn}(prims={prims})"
                )
            for i, mt in enumerate(
                g.get("materials", [])
            ):
                info["materials"].append(
                    mt.get("name", f"mat_{i}")
                )
            info["textures"] = len(
                g.get("textures", [])
            )
            for i, im in enumerate(
                g.get("images", [])
            ):
                uri = im.get("uri", "")
                mime = im.get("mimeType", "")
                bv = im.get("bufferView", -1)
                if uri:
                    info["ext_uris"].append(uri)
                info["images"].append(
                    f"img_{i}(bv={bv},"
                    f"mime={mime},"
                    f"uri={uri[:24]})"
                )
            info["skins"] = len(
                g.get("skins", [])
            )
            info["animations"] = [
                a.get("name", f"anim_{i}")
                for i, a in enumerate(
                    g.get("animations", [])
                )
            ]
    except Exception as ex:
        info["err"] = str(ex)
    return info

def scan():
    if not SRC.exists():
        print(f"MISSING_SRC: {SRC}")
        return

    all_files = []
    ext_counts = {}
    res_paths = set()
    for root, dirs, files in os.walk(SRC):
        dirs.sort()
        files.sort()
        rp = Path(root)
        for fn in files:
            fp = rp / fn
            rel = fp.relative_to(SRC).as_posix()
            res_paths.add("res://" + rel)
            ext = fp.suffix.lower() or "<none>"
            ext_counts[ext] = (
                ext_counts.get(ext, 0) + 1
            )
            all_files.append((rel, fp, ext))

    proj_txt = ""
    pg = SRC / "project.godot"
    if pg.exists():
        proj_txt = pg.read_text(
            encoding="utf-8", errors="replace"
        )

    deps = {}
    broken = []
    scenes = {}
    scripts = {}
    glbs = {}

    pat_res = re.compile(
        r'res://[^"\'\s\)]+'
    )

    for rel, fp, ext in all_files:
        ruri = "res://" + rel
        if ext in (
            ".tscn", ".tres", ".gd", ".gdshader"
        ):
            txt = fp.read_text(
                encoding="utf-8",
                errors="replace",
            )
            refs = sorted(
                set(pat_res.findall(txt))
            )
            deps[ruri] = refs
            for rf in refs:
                if rf not in res_paths:
                    broken.append((ruri, rf))
            if ext == ".tscn":
                scenes[ruri] = txt
            elif ext in (".gd", ".gdshader"):
                scripts[ruri] = txt
        elif ext == ".glb":
            glbs[ruri] = parse_glb(fp)

    rep = OUT_DIR / (
        "CASTAWAY_DEPTHS_INSPECTION_REPORT.txt"
    )
    with open(rep, "w", encoding="utf-8") as f:
        f.write(
            "CASTAWAY-DEPTHS — "
            "GODOT PROJECT INSPECTION\n"
            + "=" * 60 + "\n"
            f"Source (READ-ONLY): {SRC}\n"
            f"Total Files: {len(all_files)}\n"
            f"Extensions: {ext_counts}\n\n"
        )
        f.write(
            "1. PROJECT FOLDER STRUCTURE\n"
            + "-" * 40 + "\n"
        )
        for rel, fp, ext in all_files:
            sz = fp.stat().st_size
            f.write(
                f"  res://{rel} ({sz} B)\n"
            )

        f.write(
            "\n2. PROJECT.GODOT CONFIG\n"
            + "-" * 40 + "\n"
        )
        for ln in proj_txt.splitlines():
            if any(
                k in ln for k in (
                    "config/name",
                    "run/main_scene",
                    "config/features",
                    "rendering/",
                )
            ):
                f.write(f"  {ln}\n")

        f.write(
            "\n3. SCENE & RESOURCE DEPENDENCIES\n"
            + "-" * 40 + "\n"
        )
        for k, v in sorted(deps.items()):
            f.write(f"  {k}\n")
            for rf in v:
                ok = (
                    "OK" if rf in res_paths
                    else "MISSING"
                )
                f.write(
                    f"    -> [{ok}] {rf}\n"
                )

        f.write(
            "\n4. SCENE HIERARCHY & NODES\n"
            + "-" * 40 + "\n"
        )
        for sc, txt in sorted(scenes.items()):
            f.write(f"  === {sc} ===\n")
            for ln in txt.splitlines():
                if ln.startswith(
                    ("[gd_scene", "[ext_resource",
                     "[sub_resource", "[node ")
                ):
                    f.write(f"    {ln}\n")

        f.write(
            "\n5. SCRIPTS & SHADERS SUMMARY\n"
            + "-" * 40 + "\n"
        )
        for sc, txt in sorted(scripts.items()):
            lns = txt.splitlines()
            f.write(
                f"  === {sc} "
                f"({len(lns)} lines) ===\n"
            )
            for ln in lns[:25]:
                f.write(f"    {ln}\n")

        f.write(
            "\n6. GLB ASSET ANALYSIS\n"
            + "-" * 40 + "\n"
        )
        for guri, gi in sorted(glbs.items()):
            f.write(
                f"  === {guri} "
                f"({gi['size']} B) ===\n"
                f"    valid={gi['valid']} "
                f"v={gi['version']} "
                f"err={gi['err']}\n"
                f"    nodes({len(gi['nodes'])}): "
                f"{gi['nodes'][:10]}\n"
                f"    meshes({len(gi['meshes'])}): "
                f"{gi['meshes'][:8]}\n"
                f"    mats({len(gi['materials'])}): "
                f"{gi['materials'][:8]}\n"
                f"    textures={gi['textures']} "
                f"skins={gi['skins']} "
                f"anims={gi['animations']}\n"
                f"    images={gi['images'][:6]}\n"
                f"    ext_uris={gi['ext_uris']}\n"
            )

        f.write(
            "\n7. BROKEN / MISSING REFS\n"
            + "-" * 40 + "\n"
        )
        if not broken:
            f.write("  NONE (0 broken refs)\n")
        else:
            for src_r, dst_r in broken:
                f.write(
                    f"  {src_r} -> {dst_r}\n"
                )

    print(
        f"INSPECT_DONE | FILES={len(all_files)}"
        f" | SCENES={len(scenes)}"
        f" | GLBS={len(glbs)}"
        f" | BROKEN={len(broken)}"
    )

if __name__ == "__main__":
    scan()
