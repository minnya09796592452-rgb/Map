root@localhost:/storage/emulated/0/Map Check# cd "/storage/emulated/0/Map Check"
root@localhost:/storage/emulated/0/Map Check#
root@localhost:/storage/emulated/0/Map Check# cat > report/mission3_v232_map_source_trace.py <<'PY'
> #!/usr/bin/env python3
> """V23.2 Mission 3 map-source trace; Assembly-CSharp.dll only, read-only."""
> from __future__ import annotations                                                       > import csv, hashlib, json, re
> from datetime import datetime
0> from pathlib import Path
> import dnfile
> from dncil.cil.body import CilMethodBody
e> from dncil.cil.body.reader import CilMethodBodyReaderBytes                             >
 > ROOT = Path('/storage/emulated/0/Map Check').resolve()
 1)
    }

    ms = {}                                                                               
    for i> REPORT = ROOT / 'report'
t> DLL = ROOT / 'Stranded-Deep-Game' / 'Stranded Deep' / 'Stranded_Deep_Data' / 'Managed'  'Assembly-CSharp.dll'
e> MAX_BYTES = 15 * 1024 * 1024
>
> # ယခင် Audit မှ အတည်ပြုထားသော token
en'> MAP_ID_TOKEN = 0x0400263D
>
a> FOCUS_OWNERS = {
_>     'Beam.Terrain.Map',
)>     'Beam.Terrain.MapEditorData',
>     'Beam.Terrain.Maps',
 >     'Beam.Terrain.World'
t> }
ok>
'> STRANDED_METHODS = {
>     'LoadZonePositions',
>     'ApplyWorldToZones',
>     'LoadObjects',
>     'FindEndGameZone',
>     'IsMapViable',
>     'FindZone'
> }
>
> CORE_CALLS = {
>     ('Beam.Terrain.Maps', 'get_DefaultMapList'),
>     ('Beam.Terrain.Maps', 'LoadDefaultMaps'),
>     ('Beam.Terrain.Maps', 'LoadMaps'),
>     ('Beam.Terrain.World', 'CreateWorld'),                                              >     ('Beam.Terrain.World', 'CreateMissionTiles'),
>     ('Beam.Terrain.World', 'CreateWorldZonePoints'),
>     ('Beam.Terrain.World', 'CreateProceduralMap')
eturn> }
>
> CALL_OPS = {'call', 'callvirt', 'newobj', 'jmp'}
ab>
>
> def sha256(path):
>     h = hashlib.sha256()
>     with path.open('rb') as f:
>         for b in iter(lambda: f.read(1024 * 1024), b''):
>             h.update(b)
n': hex>     return h.hexdigest()
 'table': h>                                                                              >
> def name(row):
>     if row is None:
F>         return ''
 >     for attr in ('Name', 'TypeName'):
>         value = getattr(row, attr, None)
>         if value is not None:
>             return str(value)
>     return str(row)
>
>
> def full_type(row):
r>     if row is None:
>         return '<unknown>'
>     ns = str(getattr(row, 'TypeNamespace', '') or '')
(da>     nm = str(getattr(row, 'TypeName', name(row)) or '')
t >>     return f'{ns}.{nm}' if ns else nm
w>
>
i> def coded_row(value):                                                                  >     return getattr(value, 'row', None)
>
>
> def member_owner(value):
>     row = coded_row(value)
>     if row is not None and hasattr(row, 'TypeName'):
c>         return full_type(row)
(lin>     return name(row) or str(value or '<unknown>')
:                                                                                         >
 >
> def raw_value(value):
>     if value is None:
>         return ''
>     value = getattr(value, 'value', value)
>     return value.hex() if isinstance(value, bytes) else str(value)                      >
 >
> def read_user_string(pe, token):
>     try:
s>         item = pe.net.user_strings.get(token & 0x00FFFFFF)
>         if item is None:
>             return ''
>         v = getattr(item, 'value', item)
>         return v.decode('utf-8', 'replace') if isinstance(v, bytes) else str(v)
>     except Exception:
>         return ''                                                                       >
>
> def build_maps(pe):                                                                     >     t = pe.net.mdtables                                                                    >                                                                                      f>     tdefs = list(t.TypeDef)
>     fields = list(t.Field)                                                              >     methods = list(t.MethodDef)                                                         >     members = list(t.MemberRef)                                                         >     typerefs = list(t.TypeRef)                                                          >                                                                                         >     field_owner = {}
t>     method_owner = {}
>
>     for td in tdefs:
>         own = full_type(td)                                                             >
>         for x in td.FieldList:                                                          >             if x.row is not None:
>                 field_owner[id(x.row)] = own                                            >
>         for x in td.MethodList:                                                         >             if x.row is not None:                                                       >                 method_owner[id(x.row)] = own                                            j>
s>     fm = {}                                                                            [>     ftok = {}
_r>                                                                                       >     for i, r in enumerate(fields, 1):
ap>         tok = 0x04000000 | i                                                          
>         ftok[id(r)] = tok                                                               >
>         fm[tok] = {                                                                     rs>             'token': hex(tok),
>             'owner': field_owner.get(id(r), '<unknown>'),                               tem in td.>             'name': name(r),
>             'signature_hex': raw_value(getattr(r, 'Signature', None)),                  >             'flags': str(getattr(r, 'Flags', ''))
>         }                                                                               r>
>     mm = {}                                                                             >     mtok = {}
>                                                                                         >     for i, r in enumerate(methods, 1):
>         tok = 0x06000000 | i                                                            >         mtok[id(r)] = tok
y>                                                                                         >         mm[tok] = {
>             'token': hex(tok),                                                          >             'owner': method_owner.get(id(r), '<unknown>'),
>             'name': name(r),                                                                >             'rva': hex(int(getattr(r, 'Rva', 0) or 0))
>         }                                                                                >
 >     mr = {}                                                                            >     mrtok = {}
>                                                                                         >     for i, r in enumerate(members, 1):
>         tok = 0x0A000000 | i                                                            >         mrtok[id(r)] = tok
>                                                                                         >         mr[tok] = {
>             'token': hex(tok),                                                          >             'owner': member_owner(getattr(r, 'Class', None)),
>             'name': name(r),                                                              >             'signature_hex': raw_value(getattr(r, 'Signature', None))
>         }                                                                               >
>     tr = {                                                                              >         0x01000000 | i: {
 >             'token': hex(0x01000000 | i),                                              >             'owner': full_type(r),
>             'name': name(r)                                                             >         }
>         for i, r in enumerate(typerefs, 1)                                              >     }
)>                                                                                        >     ms = {}
>                                                                                         >     for i, r in enumerate(list(getattr(t, 'MethodSpec', [])), 1):
>         tok = 0x2B000000 | i                                                            >         base = coded_row(getattr(r, 'Method', None))
      if tok>         base_tok = mtok.get(id(base)) or mrtok.get(id(base))                >
>         ms[tok] = {                                                                     >             'token': hex(tok),
>             'base': mm.get(base_tok) or mr.get(base_tok) or {}                          >         }
>                                                                                         u>     return {
>         'types': tdefs,                                                                 >         'fm': fm,                                                                       >         'ftok': ftok,                                                                   l>         'mm': mm,
>         'mr': mr,                                                                       >         'tr': tr,
>         'ms': ms                                                                        >     }
>                                                                                         >
> def resolve(tok, maps, pe):                                                             >     table = (tok >> 24) & 0xFF
>                                                                                         >     if table == 0x04:                                                                   >         return maps['fm'].get(tok, {'token': hex(tok)})                                 >     if table == 0x06:
>         return maps['mm'].get(tok, {'token': hex(tok)})                                 >     if table == 0x0A:
>         return maps['mr'].get(tok, {'token': hex(tok)})                                 >     if table == 0x01:
>         return maps['tr'].get(tok, {'token': hex(tok)})                                 >     if table == 0x2B:
 >         return maps['ms'].get(tok, {'token': hex(tok)})                                >     if table == 0x70:
>         return {
>             'token': hex(tok),
>             'user_string': read_user_string(pe, tok)
>         }
>                                                                                         >     return {
>         'token': hex(tok),
>         'table': hex(table),                                                            >         'rid': tok & 0x00FFFFFF
>     }
>
>
> def write_parts(lines, prefix, ext, outdir):
>     result = []
', '')>     buf = []
>     size = 0
>     part = 1
>                                                                                         >     def flush():
(>         nonlocal buf, size, part
>                                                                                         >         if not buf:
i>             return
>
>         fn = f'{prefix}_PART_{part:03d}.{ext}'                                              >         data = b''.join(buf)
>
C>         if len(data) > MAX_BYTES:
>             raise RuntimeError(f'Part >15 MB: {fn}')
>
>         (outdir / fn).write_bytes(data)
>         result.append({'file': fn, 'bytes': len(data)})                                   >
 >         buf = []
>         size = 0
   >         part += 1
 >
 >     for line in lines:
           >         b = line.encode('utf-8') if isinstance(line, str) else line
        'off>
>         if len(b) > MAX_BYTES:
 >             raise RuntimeError('One evidence record exceeds 15 MB')
>
>         if size + len(b) > MAX_BYTES:
>             flush()
>
>         buf.append(b)
>         size += len(b)
>
 >     flush()
>     return result
>
>
s> def write_csv(path, rows, columns):
>     with path.open('w', newline='', encoding='utf-8') as f:
e>         w = csv.DictWriter(f, fieldnames=columns)
ET>         w.writeheader()
>         w.writerows(rows)
>
>
> def main():
>     if not ROOT.is_dir() or not REPORT.is_dir() or not DLL.is_file():
t>         raise FileNotFoundError(
>             f'Required path missing; expected DLL: {DLL}'
>         )
>
>     stamp = datetime.now().strftime('%Y%m%d_%H%M%S')
r>     out = REPORT / f'MISSION3_MAP_SOURCE_TRACE_{stamp}'
>     out.mkdir(parents=True, exist_ok=False)
>
  sele>     print('Loading Assembly-CSharp.dll (read-only)')
>
 >     pe = dnfile.dnPE(str(DLL))
>     maps = build_maps(pe)
>
 >     text_lines = [
>         f'CASTAWAY-DEPTHS V23.2 MAP SOURCE TRACE\n'
>         f'DLL: {DLL}\n'
'>         f'DLL SHA256: {sha256(DLL)}\n'
>     ]
>
>     json_lines = []
>     method_rows = []
>     literal_rows = []
 >     graph_rows = []
 >     errors = []
>
>     scanned = 0
>     selected = 0
>     id_writers = 0
>
>     for td in maps['types']:
   >         owner = full_type(td)
>
_>         for item in td.MethodList:
>             m = item.row
>
>             if m is None:
>                 continue
>
>             rva = int(getattr(m, 'Rva', 0) or 0)
>
>             if not rva:
>                 continue
>
>             scanned += 1
>             method = name(m)
>
>             try:
>                 body = CilMethodBody(
>                     CilMethodBodyReaderBytes(pe.get_data(rva))
>                 )
>             except Exception as e:
>                 errors.append({
>                     'owner': owner,
>                     'method': method,
>                     'rva': hex(rva),
>                     'error': str(e)
>                 })
>                 continue
>
>             ins_rows = []
>             calls = []
>             literals = []
>
>             writes_id = False
>             calls_core = False
>                                                                                         >             for ins in body.instructions:
>                 operand = str(ins.operand)                                              =>
>                 match = re.search(                                                      >                     r'token\(0x([0-9a-fA-F]+)\)',
>                     operand                                                             x>                 )
 >                                                                                        >                 tok = int(match.group(1), 16) if match else None                        >                 resolved = resolve(tok, maps, pe) if tok is not None else {}            >                 op = str(ins.opcode)
>                                                                                         >                 if tok == MAP_ID_TOKEN and op in ('stfld', 'stsfld'):
>                     writes_id = True                                                    ,>
>                 if tok is not None and (tok >> 24) == 0x70:                             >                     lit = resolved.get('user_string', '')
>                                                                                         >                     if lit:
>                         literals.append(lit)                                            >
'>                         if any(k in lit.upper() for k in (                             >                             'MISSION', 'INTERNAL', 'WORLD', 'ZONE'
>                         )):                                                             >                             literal_rows.append({
>                                 'owner': owner,                                         >                                 'method': method,
>                                 'rva': hex(rva),                                        C>                                 'offset': ins.offset,
 >                                 'literal': lit                                         >                             })
>                                                                                         >                 if op in CALL_OPS and resolved:
>                     c = {                                                               >                         'offset': ins.offset,
>                         'opcode': op,                                                   >                         'token': hex(tok) if tok else '',
>                         'owner': resolved.get('owner', ''),                             >                         'method': resolved.get('name', '')
>                     }                                                                   >
,>                     calls.append(c)                                                    >
>                     if (c['owner'], c['method']) in CORE_CALLS:                         >                         calls_core = True
>                                                                                         O>                     if c['owner'] in FOCUS_OWNERS:                                     >                         graph_rows.append({                                             >                             'caller_owner': owner,                                      >                             'caller_method': method,                                    >                             'caller_rva': hex(rva),
>                             **c                                                         >                         })
ken'>                                                                                     >                 ins_rows.append({
>                     'offset': ins.offset,                                               >                     'opcode': op,                                                       >                     'operand': operand,
>                     'token': hex(tok) if tok is not None else None,
>                     'resolved': resolved                                                >                 })                                                                      >                                                                                         >             focus_owner = owner in FOCUS_OWNERS                                         >                                                                                         >             focus_stranded = (
>                 owner == 'StrandedWorld'
e>                 and method in STRANDED_METHODS
>             )
>
 >             mission3_literal = any(
[]>                 'INTERNAL_MISSION_3' in x.upper()
>                 for x in literals
>             )
>
>             if not (
>                 focus_owner
>                 or focus_stranded
>                 or calls_core
>                 or writes_id
>                 or mission3_literal
>             ):
>                 continue
>
>             selected += 1
>             id_writers += int(writes_id)
>
>             why = []
>
>             if focus_owner:
>                 why.append('map_world_class')
>             if focus_stranded:
>                 why.append('stranded_world_flow')
>             if calls_core:
>                 why.append('calls_map_world_core')
>             if writes_id:
>                 why.append('writes_MapEditorData.Id')
>             if mission3_literal:
>                 why.append('contains_INTERNAL_MISSION_3')
>
'>             rec = {
>                 'owner': owner,
>                 'method': method,
>                 'rva': hex(rva),
>                 'selection_reasons': why,
>                 'writes_map_id': writes_id,
>                 'user_strings': literals,
>                 'calls': calls,
>                 'instructions': ins_rows
>             }
en>
>             json_lines.append(
>                 json.dumps(rec, ensure_ascii=False) + '\n'
>             )
>
>             text_lines.extend([
 >                 '\n' + '=' * 76 + '\n',
>                 f'TYPE: {owner}\n',
>                 f'METHOD: {method}\n',
>                 f'RVA: {hex(rva)}\n',
>                 f'SELECTION: {why}\n',
>                 f'WRITES MapEditorData.Id: {writes_id}\n',                              >                 f'USER STRINGS: {literals}\n',
>                 '-' * 76 + '\n'
ES>             ])
>
,>             for x in ins_rows:
>                 suffix = ''
>
>                 if x['resolved']:
>                     suffix = ' => ' + json.dumps(

>                         x['resolved'],
>                         ensure_ascii=False                                              >                     )
r>                                                                                        i>                 text_lines.append(
>                     f"IL_{x['offset']:04X}: "                                           >                     f"{x['opcode']} {x['operand']}{suffix}\n"
>                 )                                                                       >
>             method_rows.append({                                                        >                 'owner': owner,
>                 'method': method,                                                       >                 'rva': hex(rva),
>                 'selection_reasons': ';'.join(why),                                     >                 'writes_map_id': writes_id,
>                 'calls': ' | '.join(                                                    >                     c['owner'] + '.' + c['method']
  >                     for c in calls                                                    >                 ),
>                 'user_strings': ' | '.join(literals),                                   '
     >                 'instruction_count': len(ins_rows)                                 >             })
>                                                                                         >     fields = []
>                                                                                         >     for td in maps['types']:
>         owner = full_type(td)                                                           >
>         if (                                                                            >             owner not in FOCUS_OWNERS
>             and 'Mission' not in owner
>             and 'Zone' not in owner
>         ):
>             continue
>
>         for item in td.FieldList:
>             r = item.row
>
>             if r is None:
>                 continue
>
>             tok = maps['ftok'].get(id(r))
>
>             fields.append({
>                 'owner': owner,
>                 'field': name(r),
>                 'token': hex(tok) if tok else '',
>                 'signature_hex': raw_value(
>                     getattr(r, 'Signature', None)
>                 ),
>                 'flags': str(getattr(r, 'Flags', ''))
>             })
>
>     write_csv(
>         out / 'METHOD_INDEX.csv',
>         method_rows,
>         [
>             'owner', 'method', 'rva', 'selection_reasons',
>             'writes_map_id', 'calls', 'user_strings',
>             'instruction_count'
>         ]
>     )
>                                                                                         >     write_csv(
>         out / 'TYPE_FIELD_INDEX.csv',
>         fields,
>         ['owner', 'field', 'token', 'signature_hex', 'flags']
>     )
>
>     write_csv(
>         out / 'STRING_INDEX.csv',
>         literal_rows,
>         ['owner', 'method', 'rva', 'offset', 'literal']
>     )
>
>     write_csv(
>         out / 'TERRAIN_CALL_GRAPH.csv',
>         graph_rows,
>         [
>             'caller_owner', 'caller_method', 'caller_rva',
>             'offset', 'opcode', 'token', 'owner', 'method'
>         ]
>     )
>
>     resources = []
>
>     for r in list(getattr(
>         pe.net.mdtables,
>         'ManifestResource',
>         []
>     )):
>         resources.append({
>             'name': name(r),
>             'offset': str(getattr(r, 'Offset', '')),
>             'flags': str(getattr(r, 'Flags', '')),
>             'implementation': str(
>                 getattr(r, 'Implementation', '')
>             )
>         })
>
>     write_csv(
>         out / 'MANIFEST_RESOURCES.csv',
>         resources,
>         ['name', 'offset', 'flags', 'implementation']
>     )
>
>     text_parts = write_parts(
>         text_lines, 'MAP_SOURCE_TRACE', 'txt', out
>     )
>                                                                                         >     json_parts = write_parts(                                                           >         json_lines, 'MAP_SOURCE_TRACE', 'jsonl', out
>     )                                                                                   >                                                                                         >     manifest = {                                                                        >         'project': 'CASTAWAY-DEPTHS',                                                   >         'audit': 'MISSION3_MAP_SOURCE_TRACE',
>         'timestamp': stamp,
>         'input': str(DLL),
>         'input_bytes': DLL.stat().st_size,
>         'input_sha256': sha256(DLL),
>         'scope': (
>             'Assembly-CSharp.dll only; read-only; '
>             'no assets scanned or extracted'
>         ),
>         'scanned_method_count': scanned,
>         'selected_method_count': selected,
>         'method_parse_error_count': len(errors),
>         'parse_errors': errors,
>         'map_id_field_token': hex(MAP_ID_TOKEN),
>         'map_id_writer_count': id_writers,
>         'mission3_literal_hits': [
>             x for x in literal_rows
>             if 'INTERNAL_MISSION_3' in x['literal'].upper()
>         ],
>         'text_parts': text_parts,
>         'jsonl_parts': json_parts,                                                      >         'csv_files': [
>             'METHOD_INDEX.csv',
>             'TYPE_FIELD_INDEX.csv',
>             'STRING_INDEX.csv',                                                         >             'TERRAIN_CALL_GRAPH.csv',
>             'MANIFEST_RESOURCES.csv'
>         ],
>         'max_part_bytes': MAX_BYTES
>     }
>
>     (out / 'MANIFEST.json').write_text(
>         json.dumps(manifest, indent=2, ensure_ascii=False),
>         encoding='utf-8'
>     )
>
>     print('\n' + '=' * 65)
>     print('MAP SOURCE TRACE COMPLETE')
>     print('=' * 65)
>     print('Output:', out)
>     print('DLL SHA256:', manifest['input_sha256'])
>     print('Scanned methods:', scanned)
>     print('Selected methods:', selected)
>     print('Parse errors:', len(errors))
>     print('MapEditorData.Id writer methods:', id_writers)
>     print(
>         'INTERNAL_MISSION_3 literal hits:',
>         len(manifest['mission3_literal_hits'])
>     )
>     print('Text parts:', len(text_parts))
>     print('JSONL parts:', len(json_parts))
>     print(
>         'CSV: METHOD_INDEX.csv, TYPE_FIELD_INDEX.csv, '
>         'STRING_INDEX.csv, TERRAIN_CALL_GRAPH.csv, '
>         'MANIFEST_RESOURCES.csv'
>     )
>
>
> if __name__ == '__main__':
>     main()
> PY
root@localhost:/storage/emulated/0/Map Check# /root/.venv_mission3/bin/python \
>   report/mission3_v232_map_source_trace.py
Loading Assembly-CSharp.dll (read-only)
Traceback (most recent call last):
  File "/mnt/sdcard/Map Check/report/mission3_v232_map_source_trace.py", line 648, in <module>
    main()
    ~~~~^^
  File "/mnt/sdcard/Map Check/report/mission3_v232_map_source_trace.py", line 559, in main
    for r in list(getattr(
             ~~~~^^^^^^^^^
        pe.net.mdtables,
        ^^^^^^^^^^^^^^^^
        'ManifestResource',
        ^^^^^^^^^^^^^^^^^^^
        []
        ^^
    )):
    ^^
TypeError: 'NoneType' object is not iterable
root@localhost:/storage/emulated/0/Map Check# cd "/storage/emulated/0/Map Check"
root@localhost:/storage/emulated/0/Map Check#
root@localhost:/storage/emulated/0/Map Check# python - <<'PY'
> from pathlib import Path
>
> p = Path("report/mission3_v232_map_source_trace.py")
> s = p.read_text(encoding="utf-8")
>
> old = """    for r in list(getattr(
>         pe.net.mdtables,
>         'ManifestResource',
>         []
>     )):"""
>
> new = """    resource_table = getattr(
>         pe.net.mdtables,
>         'ManifestResource',
>         None
>     ) or []
>
>     for r in resource_table:"""
>
> if old not in s:
>     raise SystemExit("PATCH NOT APPLIED: target code not found")
>
> p.write_text(s.replace(old, new, 1), encoding="utf-8")
> print("PATCH APPLIED")
> PY
PATCH APPLIED
root@localhost:/storage/emulated/0/Map Check# /root/.venv_mission3/bin/python \
>   report/mission3_v232_map_source_trace.py
Loading Assembly-CSharp.dll (read-only)

=================================================================
MAP SOURCE TRACE COMPLETE
=================================================================
Output: /storage/emulated/0/Map Check/report/MISSION3_MAP_SOURCE_TRACE_20261120_063902
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
