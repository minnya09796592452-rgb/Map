root@localhost:/storage/emulated/0/Map Check# cd "/storage/emulated/0/Map Check"
root@localhost:/storage/emulated/0/Map Check#
root@localhost:/storage/emulated/0/Map Check# cat > report/mission3_v232_map_constants_trace.py <<'PY'
> #!/usr/bin/env python3
> """Read-only V23.2 map constants and Mission ID usage trace."""
th.op>
e> from __future__ import annotations
>
)> import csv
 as f:
    > import hashlib
> import json
> import re
> from datetime import datetime
> from pathlib import Path
t>
> import dnfile
> from dncil.cil.body import CilMethodBody
> from dncil.cil.body.reader import CilMethodBodyReaderBytes
>
>
 > ROOT = Path("/storage/emulated/0/Map Check").resolve()
e> REPORT = ROOT / "report"
>
 if not buffer:
> DLL = (
>     ROOT / "Stranded-Deep-Game" / "Stranded Deep"
n>     / "Stranded_Deep_Data" / "Managed"
(>     / "Assembly-CSharp.dll"
  > )
 >
> MAX_PART = 15 * 1024 * 1024
X_PART:
 >                                                                                         > TARGET_FIELDS = {
u>     "MAP_EXTENSION",
>     "MAP_PREFIX",
>     "MAP_EDITOR_SUFFIX",
>     "MAP_OBJECT_SUFFIX",
>     "MAP_HEIGHT_SUFFIX",
>     "MAP_INTERNAL_ID",
>     "MAP_PROCEDURAL_ID",
>     "MAP_MISSION_ID",
>     "_defaultMapList",
>     "DEFAULT_MAPS_FOLDER",
>     "NUM_MISSIONS",
>     "WORLD_SEED_FILENAME",
> }
>
> FOCUS_METHODS = {
>     "CreateWorld",
>     "CreateMissionTiles",
>     "CreateProceduralMap",
>     "LoadDefaultMaps",
>     "LoadMaps",
>     "LoadMap",
>     "Load",
>     "ApplyWorldToZones",
>     "FindEndGameZone",
>     "IsMapViable",
>     "LoadWorldMaps",
>     "SaveWorldMaps",
>     "GetWorldMapFolderName",
> }
turn parts


def main():
 >
> RELEVANT_STRING = re.compile(
>     r"INTERNAL_MISSION|INTERNAL_PROCEDURAL|"
>     r"INTERNAL_CARRIER|MAP_MISSION|DEFAULT_MAP",
>     re.IGNORECASE,
> )
>
>
> def sha256(path):
>     h = hashlib.sha256()
>
>     with path.open("rb") as f:
>         for chunk in iter(lambda: f.read(1024 * 1024), b""):
>             h.update(chunk)
>
>     return h.hexdigest()
>
>
> def row_name(row):
>     if row is None:
t>         return ""
>
>     for attr in ("Name", "TypeName"):
>         value = getattr(row, attr, None)
>
l>         if value is not None:
>             return str(value)
>
>     return str(row)
>                                                                                         >
> def full_type(row):
>     if row is None:
>         return "<unknown>"
>
>     ns = str(getattr(row, "TypeNamespace", "") or "")
>     name = str(getattr(row, "TypeName", row_name(row)) or "")
>
>     return f"{ns}.{name}" if ns else name
>
e>
> def coded_row(value):
>     return getattr(value, "row", None)
>
>
> def integer_value(value):
>     value = getattr(value, "value", value)
>
_>     try:
>         return int(value)
>     except Exception:
>         try:
e>             return int(str(value), 0)
>         except Exception:
>             return -1
>
>
> def blob_bytes(value):
>>     if value is None:
>         return b""
>
>     value = getattr(value, "value", value)
>
>     if isinstance(value, bytes):
>         return value
>
>     if isinstance(value, bytearray):
>         return bytes(value)
>
>     try:
id>         return bytes(value)
>     except Exception:
I>         return str(value).encode("utf-8", "replace")
>
>
> def decode_constant(row):
>     kind = integer_value(getattr(row, "Type", None))
>     raw = blob_bytes(getattr(row, "Value", None))
>
>     result = {
>         "element_type": kind,
>         "raw_hex": raw.hex(),
>         "decoded": "",
>     }
>
>     try:
>         if kind == 0x0E:
>             result["decoded"] = raw.decode(
>                 "utf-16-le", "replace"
>             ).rstrip("\x00")
>
>         elif kind == 0x02:
>             result["decoded"] = bool(raw[0])
        >
>         elif kind == 0x08 and len(raw) == 4:
>             result["decoded"] = int.from_bytes(
>                 raw, "little", signed=True
>             )
>
e>         elif kind == 0x09 and len(raw) == 8:
e>             result["decoded"] = int.from_bytes(
>                 raw, "little", signed=True
>             )
>
>         elif kind == 0x04 and len(raw) == 4:
>             import struct
>             result["decoded"] = struct.unpack("<f", raw)[0]
>
>         elif kind == 0x0C and len(raw) == 8:
>             import struct
>             result["decoded"] = struct.unpack("<d", raw)[0]
 >
 >         else:
>             result["decoded"] = raw.hex()
>
e>     except Exception as exc:
>         result["decode_error"] = str(exc)
>                                                                                         d>     return result
>
>
 > def read_user_string(pe, token):
>     try:
token)

   >         item = pe.net.user_strings.get(token & 0x00FFFFFF)
>
>         if item is None:
>             return ""
>
>         value = getattr(item, "value", item)                                            >
>         if isinstance(value, bytes):
>             return value.decode("utf-8", "replace")                                     >
e>         return str(value)
>
>     except Exception:
>         return ""
>
>
> def write_csv(path, rows, columns):
>     with path.open("w", newline="", encoding="utf-8") as f:
en, i>         writer = csv.DictWriter(f, fieldnames=columns)
 >         writer.writeheader()
>         writer.writerows(rows)
>
>
> def write_parts(lines, prefix, outdir):
>     parts = []
>     buffer = []
>     size = 0
>     index = 1
>
>     def flush():
>         nonlocal buffer, size, index
>
>         if not buffer:
>             return
0)                                                                                        
        if no>
>         filename = f"{prefix}_PART_{index:03d}.jsonl"
>         data = "".join(buffer).encode("utf-8")
>
>         if len(data) > MAX_PART:
e>             raise RuntimeError("Part exceeded 15 MB")
>
>         (outdir / filename).write_bytes(data)
t>
>         parts.append({
>             "file": filename,
>             "bytes": len(data),
>         })
>
>         buffer = []
>         size = 0
  >         index += 1
 >
 >     for line in lines:
e>         encoded_size = len(line.encode("utf-8"))
>
>         if encoded_size > MAX_PART:
>             raise RuntimeError("Single evidence row exceeds limit")
>
>         if size + encoded_size > MAX_PART:
>             flush()
>
>         buffer.append(line)
>         size += encoded_size
n>
>     flush()
 >     return parts
>
>
> def main():
ch(
  >     if not REPORT.is_dir() or not DLL.is_file():
>         raise FileNotFoundError(f"Required path missing: {DLL}")
>
r>     stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
>     out = REPORT / f"MISSION3_MAP_CONSTANTS_TRACE_{stamp}"
>     out.mkdir(parents=True, exist_ok=False)
  >
>     print("Loading Assembly-CSharp.dll (read-only)")
>
i>     pe = dnfile.dnPE(str(DLL))
>     tables = pe.net.mdtables
>
h>     type_defs = list(tables.TypeDef)
>     field_defs = list(tables.Field)
>     method_defs = list(tables.MethodDef)
>     member_refs = list(tables.MemberRef)
>
>     field_owner = {}
>     method_owner = {}
>
>     for td in type_defs:
>         owner = full_type(td)
,>
>         for item in td.FieldList:
>             if item.row is not None:
>                 field_owner[id(item.row)] = owner
>
>         for item in td.MethodList:                                                      >             if item.row is not None:
>                 method_owner[id(item.row)] = owner                                      >
>     fields_by_token = {}                                                                >     field_token_by_id = {}
>     field_token_by_name = {}
>
 >     for rid, field in enumerate(field_defs, 1):
>         token = 0x04000000 | rid
tok>         owner = field_owner.get(id(field), "<unknown>")                              >         fname = row_name(field)
>
>         info = {
 >             "token": hex(token),
>             "owner": owner,
 >             "field": fname,                                                            >         }
 >                                                                                        >         fields_by_token[token] = info
>         field_token_by_id[id(field)] = token                                            >
c>         if fname in TARGET_FIELDS:                                                     >             field_token_by_name.setdefault(fname, []).append(info)
>                                                                                         >     method_by_token = {}
>     member_by_token = {}                                                                >
>     for rid, method in enumerate(method_defs, 1):                                       >         token = 0x06000000 | rid
 >
>         method_by_token[token] = {
>             "owner": method_owner.get(id(method), "<unknown>"),                         >             "name": row_name(method),
>             "rva": hex(int(getattr(method, "Rva", 0) or 0)),
 N>         }
>
>     for rid, member in enumerate(member_refs, 1):
>         token = 0x0A000000 | rid                                                        >         cls = coded_row(getattr(member, "Class", None))
>                                                                                         >         member_by_token[token] = {
>             "owner": full_type(cls),                                                    >             "name": row_name(member),
>         }                                                                               >
>     # Decode Constant table values.                                                     >     constant_rows = []
>     constant_field_tokens = set()                                                       >
>     constant_table = getattr(tables, "Constant", None) or []                            >
>     for item in constant_table:                                                         >         parent = coded_row(getattr(item, "Parent", None))
>
>         if parent is None:
>             continue                                                                    >
>         token = field_token_by_id.get(id(parent))
>
>         if token is None:
>             continue
>
>         info = fields_by_token[token]                                                   >                                                                                         >         if info["field"] not in TARGET_FIELDS:                                          >             continue
>                                                                                         >         decoded = decode_constant(item)
>         constant_field_tokens.add(token)                                                >
>         constant_rows.append({                                                          >             **info,
>             "element_type": decoded["element_type"],                                    >             "decoded_value": str(decoded["decoded"]),
>             "raw_hex": decoded["raw_hex"],                                              >         })
>                                                                                         >     target_tokens = {
>         token                                                                           >         for token, info in fields_by_token.items()
>         if info["field"] in TARGET_FIELDS
>     }
>                                                                                         >     target_member_tokens = {
_>         token
>         for token, info in member_by_token.items()
>         if info["name"] in TARGET_FIELDS
>     }
>
>     evidence = []
>     usage_rows = []                                                                     >     errors = []
>                                                                                         >     scanned = 0
>     selected = 0                                                                        >     literal_hits = 0
>     field_reference_hits = 0                                                            >
 >     for method in method_defs:                                                         >         rva = int(getattr(method, "Rva", 0) or 0)
>                                                                                         >         if not rva:
>             continue                                                                    >
>         scanned += 1                                                                    >
>         owner = method_owner.get(id(method), "<unknown>")
>         mname = row_name(method)
>
>         try:
>             body = CilMethodBody(
>                 CilMethodBodyReaderBytes(pe.get_data(rva))
>             )
>
>         except Exception as exc:
>             errors.append({
>                 "owner": owner,
>                 "method": mname,
>                 "rva": hex(rva),
>                 "error": str(exc),
>             })
>             continue
>
>         instructions = []
>         field_refs = []
>         literals = []
>         calls = []
>
>         for ins in body.instructions:
>             operand = str(ins.operand)
>             opcode = str(ins.opcode)                                                    >                                                                                         >             match = re.search(                                                          >                 r"token\(0x([0-9a-fA-F]+)\)",
>                 operand                                                                 >             )                                                                           >                                                                                         >             token = int(match.group(1), 16) if match else None
>             resolved = {}
>
>             if token is not None:
>                 table_id = (token >> 24) & 0xFF
>
>                 if table_id == 0x04:                                                    >                     resolved = fields_by_token.get(
>                         token, {"token": hex(token)}
>                     )
>
>                     if token in target_tokens:
>                         field_refs.append(resolved)                                     >
>                 elif table_id == 0x06:
>                     resolved = method_by_token.get(
>                         token, {"token": hex(token)}
>                     )
>
>                 elif table_id == 0x0A:                                                  >                     resolved = member_by_token.get(
>                         token, {"token": hex(token)}
>                     )
>
>                     if token in target_member_tokens:
>                         field_refs.append(resolved)
>
>                 elif table_id == 0x70:
>                     text = read_user_string(pe, token)
>                     resolved = {
>                         "token": hex(token),
>                         "user_string": text,
>                     }
>
>                     if RELEVANT_STRING.search(text):
>                         literals.append(text)
>
>                 if opcode in ("call", "callvirt", "newobj"):
>                     if resolved.get("name"):
>                         calls.append(
>                             f"{resolved.get('owner', '')}."
>                             f"{resolved['name']}"
>                         )
>
>             instructions.append({
>                 "offset": ins.offset,
>                 "opcode": opcode,
>                 "operand": operand,
>                 "token": hex(token) if token is not None else None,
>                 "resolved": resolved,
>             })
>
>         focus = (
>             owner in {                                                                  >                 "Beam.Terrain.World",
>                 "Beam.Terrain.Maps",
>                 "StrandedWorld",
>             }
>             and mname in FOCUS_METHODS
>         )
>
>         if not (field_refs or literals or focus):
>             continue
>
>         selected += 1
>         field_reference_hits += len(field_refs)
>         literal_hits += len(literals)
>
>         record = {
>             "owner": owner,
>             "method": mname,
>             "rva": hex(rva),
>             "field_references": field_refs,
>             "relevant_literals": literals,
>             "calls": calls,
>             "instructions": instructions,
>         }
>
>         evidence.append(
>             json.dumps(record, ensure_ascii=False) + "\n"
>         )                                                                               >                                                                                         >         usage_rows.append({                                                             >             "owner": owner,
>             "method": mname,
>             "rva": hex(rva),
>             "field_references": " | ".join(
>                 f"{x.get('owner', '')}.{x.get('name', x.get('field', ''))}"
>                 for x in field_refs
>             ),                                                                          >             "relevant_literals": " | ".join(literals),
>             "calls": " | ".join(calls),
>             "instruction_count": len(instructions),
>         })
>
>     write_csv(
>         out / "MAP_CONSTANTS.csv",
>         constant_rows,
>         [
>             "owner", "field", "token", "element_type",
>             "decoded_value", "raw_hex",
>         ],
>     )
>
>     write_csv(
>         out / "FIELD_USAGE.csv",
>         usage_rows,
>         [
>             "owner", "method", "rva",
>             "field_references", "relevant_literals",
>             "calls", "instruction_count",
>         ],
>     )
>
>     parts = write_parts(
>         evidence,
>         "MISSION_MAP_EVIDENCE",
>         out,
>     )
>
>     manifest = {
>         "project": "CASTAWAY-DEPTHS",
>         "audit": "MISSION3_MAP_CONSTANTS_TRACE",
>         "timestamp": stamp,
>         "input": str(DLL),
>         "input_bytes": DLL.stat().st_size,
>         "input_sha256": sha256(DLL),
>         "scope": "Assembly-CSharp.dll only; read-only",
>         "scanned_methods": scanned,
>         "selected_methods": selected,
>         "field_reference_hits": field_reference_hits,
>         "relevant_literal_hits": literal_hits,
>         "parse_errors": len(errors),
>         "errors": errors,
>         "constant_rows": len(constant_rows),
>         "constant_fields_without_values": [
>             x for x in field_token_by_name
>             if not any(r["field"] == x for r in constant_rows)
>         ],
>         "evidence_parts": parts,
>         "csv_files": [
>             "MAP_CONSTANTS.csv",
>             "FIELD_USAGE.csv",
>         ],
>         "max_part_bytes": MAX_PART,
>     }
>
>     (out / "MANIFEST.json").write_text(
>         json.dumps(manifest, indent=2, ensure_ascii=False),
>         encoding="utf-8",
>     )
>
>     print("\n" + "=" * 65)
>     print("MISSION MAP CONSTANT TRACE COMPLETE")
>     print("=" * 65)
>     print("Output:", out)
>     print("DLL SHA256:", manifest["input_sha256"])
>     print("Scanned methods:", scanned)
>     print("Selected methods:", selected)
>     print("Field references:", field_reference_hits)
>     print("Relevant literals:", literal_hits)
>     print("Decoded constants:", len(constant_rows))
>     print("Parse errors:", len(errors))
>     print("Evidence parts:", len(parts))
>
>
> if __name__ == "__main__":
>     main()
> PY
root@localhost:/storage/emulated/0/Map Check# /root/.venv_mission3/bin/python \
>   report/mission3_v232_map_constants_trace.py
Loading Assembly-CSharp.dll (read-only)

=================================================================
MISSION MAP CONSTANT TRACE COMPLETE
=================================================================
Output: /storage/emulated/0/Map Check/report/MISSION3_MAP_CONSTANTS_TRACE_20261120_070324
DLL SHA256: d5501ae7a2c09499cdf569c5b52578401f039c2f4aa5e7e9240c22faeef214bc
Scanned methods: 23276
Selected methods: 24
Field references: 7
Relevant literals: 11
Decoded constants: 10
Parse errors: 0
Evidence parts: 1
root@localhost:/storage/emulated/0/Map Check#
