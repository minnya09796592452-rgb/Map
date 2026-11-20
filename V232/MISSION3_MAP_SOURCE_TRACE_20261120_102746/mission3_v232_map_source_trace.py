root@localhost:/storage/emulated/0/Map Check# cd "/storage/emulated/0/Map Check"
root@localhost:/storage/emulated/0/Map Check#
root@localhost:/storage/emulated/0/Map Check# python3 - <<'PY'
> from pathlib import Path
>
> p = Path("report/mission3_v232_map_source_trace.py")
> src = p.read_text(encoding="utf-8")
>
> old = """            focus_owner = owner in FOCUS_OWNERS
>             focus_stranded = (
> """
>
> new = """            focus_owner = owner in FOCUS_OWNERS
>             focus_compression = (
>                 owner == 'Funlabs.MPExtensions'
>                 and method in {
>                     'ToCompressStable',
>                     'Decompress',
>                     'InitializeCompressStableValues'
>                 }
>             )
>             focus_stranded = (
> """
>
> if src.count(old) != 1:
>     raise SystemExit("STOP: selector insertion point not unique")
>
> src = src.replace(old, new, 1)
>
> old2 = """                or focus_stranded
>                 or calls_core
> """
> new2 = """                or focus_stranded                                             >                 or focus_compression
>                 or calls_core
> """
>
> if src.count(old2) != 1:
>     raise SystemExit("STOP: selection condition not unique")
>
> src = src.replace(old2, new2, 1)
>
> old3 = """            if focus_stranded:
>                 why.append('stranded_world_flow')
> """
> new3 = """            if focus_stranded:
>                 why.append('stranded_world_flow')
>             if focus_compression:
>                 why.append('heightmap_compression_transform')
> """
>
> if src.count(old3) != 1:
>     raise SystemExit("STOP: selection reason insertion point not unique")
>
> src = src.replace(old3, new3, 1)
>
> p.write_text(src, encoding="utf-8")
> print("PATCH OK: compression methods included")
> PY
STOP: selector insertion point not unique
root@localhost:/storage/emulated/0/Map Check#
root@localhost:/storage/emulated/0/Map Check# /root/.venv_mission3/bin/python \
>   report/mission3_v232_map_source_trace.py
Loading Assembly-CSharp.dll (read-only)

=================================================================
MAP SOURCE TRACE COMPLETE
=================================================================
Output: /storage/emulated/0/Map Check/report/MISSION3_MAP_SOURCE_TRACE_20261120_102746
DLL SHA256: d5501ae7a2c09499cdf569c5b52578401f039c2f4aa5e7e9240c22faeef214bc
Scanned methods: 23276
Selected methods: 72
Parse errors: 0
MapEditorData.Id writer methods: 2
INTERNAL_MISSION_3 literal hits: 1
Text parts: 1
JSONL parts: 1
CSV: METHOD_INDEX.csv, TYPE_FIELD_INDEX.csv, STRING_INDEX.csv, TERRAIN_CALL_GRAPH.csv, MANIFEST_RESOURCES.csv
root@localhost:/storage/emulated/0/Map Check#
