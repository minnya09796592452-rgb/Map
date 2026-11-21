root@localhost:/storage/emulated/0/Map Check# cd "/storage/emulated/0/Map Check"          root@localhost:/storage/emulated/0/Map Check#                                             froot@localhost:/storage/emulated/0/Map Check# cat > report/mission3_v232_default_map_pathtrace.py <<'PY'
> #!/usr/bin/env python3
 > """Resolve FilePath.DEFAULT_MAPS_FOLDER using targeted metadata tracing."""
>
s> from __future__ import annotations
>
> import csv
> import hashlib
> import json
e> from datetime import datetime                                                           > from pathlib import Path
>
> import dnfile
i> from dncil.cil.body import CilMethodBody
d> from dncil.cil.body.reader import CilMethodBodyReaderBytes
>
w>
> ROOT = Path("/storage/emulated/0/Map Check").resolve()
> REPORT = ROOT / "report"
m>
> DLL = (
>     ROOT / "Stranded-Deep-Game" / "Stranded Deep"
 >     / "Stranded_Deep_Data" / "Managed"                                                 >     / "Assembly-CSharp.dll"
> )
>
> TARGET_FIELD = "DEFAULT_MAPS_FOLDER"
> TARGET_TYPE = "FilePath"
TARG> MAX_PART = 15 * 1024 * 1024
>
>
 > def sha256(path):
k>     h = hashlib.sha256()
,>
>     with path.open("rb") as f:                                                           >         for chunk in iter(lambda: f.read(1024 * 1024), b""):
>             h.update(chunk)
>
=>     return h.hexdigest()
  >
 >
c> def row_of(value):
>     return getattr(value, "row", None)
d_to>
k>
> def text_name(value):
>     if value is None:
>         return ""
 >
>     for attr in ("Name", "TypeName"):
 >         result = getattr(value, attr, None)
>
>         if result is not None:
  "sig>             return str(result)
>
 >     return str(value)
>
>
> def full_type(value):
>     if value is None:
,
>         return ""
>
 >     namespace = str(
 >         getattr(value, "TypeNamespace", "") or ""
>     )
>
>     name = str(
f>         getattr(value, "TypeName", text_name(value)) or ""                             o>     )
>
s>     return f"{namespace}.{name}" if namespace else name
>
[>
> def get_scope_info(type_ref):
w>     scope = getattr(type_ref, "ResolutionScope", None)                                  i>     row = row_of(scope)                                                                cont>
i>     if row is None:
>         return {
>             "scope_kind": str(type(scope).__name__),
>             "scope_name": "",
     i>             "assembly_name": "",
>         }
>
>     kind = type(row).__name__
>     name = text_name(getattr(row, "Name", None))                                        >
>     assembly_name = ""
>
         >     if "AssemblyRef" in kind:                                                  >         assembly_name = name
>
>     return {
>         "scope_kind": kind,
>         "scope_name": name,
i>         "assembly_name": assembly_name,                                                >     }
>
>
o> def constant_value(row):
 >     element_type = int(
 >         getattr(getattr(row, "Type", None), "value", 0) or 0
>     )
>
>     value = getattr(row, "Value", None)
>     raw = getattr(value, "value", value)                                                r>
(>     if not isinstance(raw, bytes):
>         try:
lue[>             raw = bytes(raw)
#>         except Exception:
elo>             raw = b""
>                                                                                         t>     decoded = raw.hex()                                                                 >
 >     if element_type == 0x0E:
>         decoded = raw.decode(
>             "utf-16-le", "replace"
M>         ).rstrip("\x00")
>
>     elif element_type == 0x02 and raw:
>         decoded = bool(raw[0])
tinu>
e>     elif element_type == 0x08 and len(raw) == 4:                                       )>         decoded = int.from_bytes(
>             raw, "little", signed=True
et>         )
>                                                                                         >     return {                                                                            >         "element_type": element_type,                                                     >         "raw_hex": raw.hex(),
 >         "decoded": decoded,
>     }
>
>
> def read_user_string(pe, token):
     >     try:
>         item = pe.net.user_strings.get(token & 0x00FFFFFF)
>                                                                                         o>         if item is None:
           >             return ""
>
>         value = getattr(item, "value", item)
>
>         if isinstance(value, bytes):
>             return value.decode("utf-8", "replace")
 >
>         return str(value)
>
>     except Exception:
>         return ""
>
>
> def inspect_assembly(path):
o>     print("Inspecting:", path.name)
(in>
>     pe = dnfile.dnPE(str(path))
>     tables = pe.net.mdtables
>
t>     type_refs = []
   >     target_type_refs = []
 >
 >     for ref in list(getattr(tables, "TypeRef", []) or []):
>         if text_name(ref) != TARGET_TYPE:
 >             continue

    >
>         info = {
>             "full_type": full_type(ref),
  >             **get_scope_info(ref),
>         }
>
>         type_refs.append(info)
>         target_type_refs.append(ref)
>                                                                                         >     member_refs = []
>
>     for ref in list(getattr(tables, "MemberRef", []) or []):
>         parent = row_of(getattr(ref, "Class", None))
>
>         if parent is None:
>             continue
>
 >         if text_name(parent) != TARGET_TYPE:
>             continue
eli>
>         member_refs.append({

             >             "owner": full_type(parent),
>             "member": text_name(ref),
>             "signature_hex": bytes(
 >                 getattr(
>                     getattr(ref, "Signature", None),
>                     "value",
>                     b""
>                 ) or b""
>             ).hex(),
>         })
>
>     type_defs = list(tables.TypeDef)
>     field_defs = list(tables.Field)
>     method_defs = list(tables.MethodDef)
o>
>     field_owner = {}
>     method_owner = {}
>

>     for td in type_defs:
>         owner = full_type(td)
>
 >         for item in td.FieldList:
>             if item.row is not None:
>                 field_owner[id(item.row)] = owner
 >
>         for item in td.MethodList:                                                      >             if item.row is not None:
>                 method_owner[id(item.row)] = owner
>                                                                                          >     target_defs = [
>         td for td in type_defs
  >         if text_name(td) == TARGET_TYPE
  >     ]
>
>     field_rows = []
>     field_token_map = {}
>
 >     for rid, field in enumerate(field_defs, 1):
>         owner = field_owner.get(id(field), "")
},>
>         if owner.rsplit(".", 1)[-1] != TARGET_TYPE:
>             continue
>
>         token = 0x04000000 | rid
>         field_token_map[token] = text_name(field)
>
>         field_rows.append({
>             "owner": owner,
>             "field": text_name(field),
>             "token": hex(token),
>             "signature_hex": bytes(
>                 getattr(
>                     getattr(field, "Signature", None),
  "assembly": path.name,
      >                     "value",
>                     b""
>                 ) or b""
>             ).hex(),
>             "flags": str(getattr(field, "Flags", "")),
>         })
>
_>     constants = []
>
>     for item in list(getattr(tables, "Constant", []) or []):
>         parent = row_of(getattr(item, "Parent", None))
>
ors": parse_errors,>         if parent is None:
>             continue
>
>         token = next(
>             (
>                 tok for tok, field in field_token_map.items()
a>                 if field == TARGET_FIELD
>                 and any(
>                     id(parent) == id(f)
:>                     for f in field_defs
>                     if 0x04000000 | (field_defs.index(f) + 1) == tok
>                 )
>             ),
>             None
>         )
>
>         if token is None:
>             continue
>
e>         value = constant_value(item)
a>
>         constants.append({
= "".join(buf).encode("utf->             "field": TARGET_FIELD,
>             "token": hex(token),                                                        or>             "element_type": value["element_type"],
>             "decoded_value": str(value["decoded"]),
>             "raw_hex": value["raw_hex"],
>         })
>
>     # Only inspect methods belonging to the FilePath type.
>     evidence = []
>     parse_errors = []
>
>     method_rows = []
>
>     for td in target_defs:                                                              >         for item in td.MethodList:                                                      >             method = item.row                                                            >
 >             if method is None:
>                 continue
>
 >             rva = int(getattr(method, "Rva", 0) or 0)
>
>             if not rva:
f>                 continue
>
>             method_name = text_name(method)
>
>             try:
 >                 body = CilMethodBody(
>                     CilMethodBodyReaderBytes(pe.get_data(rva))
>                 )
_>
>             except Exception as exc:
>                 parse_errors.append({
>                     "method": method_name,
>                     "rva": hex(rva),
>                     "error": str(exc),
tem>                 })
>                 continue
>
>             instructions = []
>             contains_target = False
= D>
L>             for ins in body.instructions:
>                 operand = str(ins.operand)
>                 token = None
>
>                 import re
>                 match = re.search(
 n>                     r"token\\(0x([0-9a-fA-F]+)\\)",
>                     operand
>                 )
>
>                 if match:
>                     token = int(match.group(1), 16)                                     >                                                                                         >                 resolved = {}                                                           A>
R>                 if token is not None:
>                     table = (token >> 24) & 0xFF
>
t>                     if table == 0x70:
>                         resolved = {
>                             "user_string": read_user_string(
>                                 pe, token
>                             )
>                         }
>
>                     elif table == 0x04:                                                 >                         resolved = {                                                    >                             "field": field_token_map.get(                               >                                 token, ""
>                             )
>                         }
>
>                         if resolved["field"] == TARGET_FIELD:
 >                             contains_target = True
>
>                 instructions.append({
>                     "offset": ins.offset,
s>                     "opcode": str(ins.opcode),
>                     "operand": operand,
>                     "resolved": resolved,
n>                 })
>
>             if (
>                 method_name == ".cctor"
>                 or TARGET_FIELD in method_name
>                 or contains_target
>             ):
>                 evidence.append(
   >                     json.dumps({
>                         "assembly": path.name,
>                         "owner": full_type(td),
>                         "method": method_name,
>                         "rva": hex(rva),
>                         "instructions": instructions,
>                     }, ensure_ascii=False) + "\\n"
>                 )
>
 >                 method_rows.append({
 "co>                     "owner": full_type(td),
>                     "method": method_name,
>                     "rva": hex(rva),
>                     "instruction_count": len(instructions),
>                     "references_target_field": contains_target,
>                 })
>
>     return {
>         "assembly": path.name,                                                          >         "sha256": sha256(path),                                                         >         "type_refs": type_refs,                                                         >         "member_refs": member_refs,
>         "target_type_found": bool(target_defs),
>         "fields": field_rows,
 >         "constants": constants,
>         "methods": method_rows,
>         "evidence": evidence,
>         "parse_errors": parse_errors,                                                   >     }
">
>
> def write_csv(path, rows, columns):
>     with path.open("w", newline="", encoding="utf-8") as f:
>         writer = csv.DictWriter(f, fieldnames=columns)
>         writer.writeheader()

>         writer.writerows(rows)
>
>
> def write_parts(lines, outdir):
>     parts = []
>     buf = []
>     size = 0
>     index = 1
>
>     def flush():
>         nonlocal buf, size, index                                                       >
>         if not buf:
>             return
>
>         filename = (
>             f"FILEPATH_INITIALIZATION_PART_{index:03d}.jsonl"
D>         )
>
>         data = "".join(buf).encode("utf-8")
>
>         if len(data) > MAX_PART:
>             raise RuntimeError("Part exceeded 15 MB")
>
>         (outdir / filename).write_bytes(data)
>
 >         parts.append({
>             "file": filename,
>             "bytes": len(data),
>         })
>
:>         buf = []
>         size = 0
>         index += 1
 >
r>     for line in lines:
  >         n = len(line.encode("utf-8"))
:", len(errors))
 >
>         if size + n > MAX_PART:
>             flush()
>
 >         buf.append(line)
()>         size += n
>
>     flush()
>     return parts
>
>
> def main():
>     if not REPORT.is_dir() or not DLL.is_file():
>         raise FileNotFoundError(f"Missing input: {DLL}")
>
>     stamp = datetime.now().strftime("%Y%m%d_%H%M%S")                                    >     out = REPORT / f"MISSION3_DEFAULT_MAP_PATH_TRACE_{stamp}"
>     out.mkdir(parents=True, exist_ok=False)
>
>     primary = inspect_assembly(DLL)
>
>     # Resolve only the exact AssemblyRef named by FilePath.TypeRef.
>     candidates = sorted({
>         item["assembly_name"]
>         for item in primary["type_refs"]
>         if item["assembly_name"]
>     })
>
>     inspected = [primary]
>
>     for assembly_name in candidates:
>         candidate = DLL.parent / f"{assembly_name}.dll"
>
>         if not candidate.is_file():
>             continue
>
>         if candidate.resolve() == DLL.resolve():
>             continue
>
>         if not candidate.resolve().is_relative_to(ROOT):
>             raise RuntimeError("Dependency path escaped Map Check")
>
>         inspected.append(inspect_assembly(candidate))
>
>     resolution = {
>         "target": TARGET_TYPE,
>         "member_refs": primary["member_refs"],
>         "type_refs": primary["type_refs"],
>         "candidate_assemblies": candidates,
>         "inspected_assemblies": [
>             {
>                 "name": x["assembly"],
>                 "sha256": x["sha256"],
>                 "type_found": x["target_type_found"],
>             }
>             for x in inspected
>         ],
>         "note": (
>             "Only Assembly-CSharp.dll and exact referenced "
>             "FilePath assemblies were inspected."
>         ),
>     }
>
>     (out / "FILEPATH_RESOLUTION.json").write_text(
>         json.dumps(resolution, indent=2, ensure_ascii=False),
>         encoding="utf-8",
>     )
>
>     fields = []
>     methods = []
>     evidence = []
>     errors = []
>
>     for assembly in inspected:
>         fields.extend(assembly["fields"])
>         methods.extend(assembly["methods"])
>         evidence.extend(assembly["evidence"])
>         errors.extend(assembly["parse_errors"])
>
>         for constant in assembly["constants"]:
>             fields.append({
>                 "owner": assembly["assembly"],
>                 "field": constant["field"],
>                 "token": constant["token"],
>                 "signature_hex": "",
>                 "flags": "",
>                 "constant_value": constant["decoded_value"],
>             })
>
>     write_csv(
>         out / "FILEPATH_FIELDS.csv",
>         fields,
>         [
>             "owner", "field", "token",
>             "signature_hex", "flags", "constant_value",
>         ],
>     )
>                                                                                         >     write_csv(
>         out / "FILEPATH_METHOD_INDEX.csv",
>         methods,
>         [
>             "owner", "method", "rva",
>             "instruction_count", "references_target_field",
>         ],
>     )
>
>     parts = write_parts(evidence, out)
>
>     manifest = {
>         "project": "CASTAWAY-DEPTHS",
>         "audit": "MISSION3_DEFAULT_MAP_PATH_TRACE",
>         "timestamp": stamp,
>         "scope": (
>             "Assembly-CSharp.dll plus exact FilePath "
>             "AssemblyRef only; read-only"
>         ),
>         "primary_sha256": sha256(DLL),
>         "candidate_assemblies": candidates,
>         "inspected_assemblies": [
>             x["assembly"] for x in inspected
>         ],
>         "FilePath_type_found": any(
>             x["target_type_found"] for x in inspected
>         ),
>         "FilePath_field_definitions": len(fields),
>         "parse_errors": errors,
>         "evidence_parts": parts,
>     }
>
>     (out / "MANIFEST.json").write_text(
>         json.dumps(manifest, indent=2, ensure_ascii=False),
>         encoding="utf-8",
>     )                                                                                   >
>     print("\\n" + "=" * 65)
>     print("DEFAULT MAP PATH TRACE COMPLETE")
>     print("=" * 65)
>     print("Output:", out)
>     print("Primary DLL SHA256:", manifest["primary_sha256"])
>     print("Referenced assemblies:", candidates)
>     print("Inspected assemblies:", manifest["inspected_assemblies"])
>     print("FilePath type found:", manifest["FilePath_type_found"])
>     print("Field rows:", len(fields))
>     print("Parse errors:", len(errors))
>     print("Evidence parts:", len(parts))
>
>
> if __name__ == "__main__":
>     main()
> PY
root@localhost:/storage/emulated/0/Map Check# /root/.venv_mission3/bin/python \
>   report/mission3_v232_default_map_path_trace.py
Inspecting: Assembly-CSharp.dll
Inspecting: Assembly-CSharp-firstpass.dll
\n=================================================================
DEFAULT MAP PATH TRACE COMPLETE
=================================================================
Output: /storage/emulated/0/Map Check/report/MISSION3_DEFAULT_MAP_PATH_TRACE_20261120_071256
Primary DLL SHA256: d5501ae7a2c09499cdf569c5b52578401f039c2f4aa5e7e9240c22faeef214bc
Referenced assemblies: ['Assembly-CSharp-firstpass']
Inspected assemblies: ['Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll']
FilePath type found: True                                                                 Field rows: 35
Parse errors: 0                                                                           Evidence parts: 1
root@localhost:/storage/emulated/0/Map Check#
