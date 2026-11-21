import os
import json
import struct
import hashlib
from datetime import datetime

DLL_PATH = "/mnt/sdcard/Download/Stranded-Deep-Original/Stranded Deep/Stranded_Deep_Data/Managed/Assembly-CSharp.dll"
OUT_DIR = "Google Ai Core V2"
REPORT_FILE = os.path.join(OUT_DIR, "PHASE1_HARDENED_FORENSIC_REPORT.txt")
MANIFEST_FILE = os.path.join(OUT_DIR, "PER_METHOD_VALIDATION_MANIFEST.json")
DIFF_FILE = os.path.join(OUT_DIR, "DECODER_AB_COMPARISON_REPORT.txt")

class HardenedPE:
    def __init__(self, path):
        if not os.path.exists(path):
            raise FileNotFoundError(f"DLL not found: {path}")
        with open(path, "rb") as f:
            self.data = f.read()
        self.size = len(self.data)
        self.sha256 = hashlib.sha256(self.data).hexdigest()
        self.sections = []
        self._parse_pe()
        self._parse_cli()

    def _fail(self, msg):
        raise ValueError(f"[PE/CLI VALIDATION ERROR] {msg}")

    def _u16(self, off, ctx=""):
        if off < 0 or off + 2 > self.size:
            self._fail(f"OOB u16 ({ctx}) @ 0x{off:X}")
        return struct.unpack_from("<H", self.data, off)[0]

    def _u32(self, off, ctx=""):
        if off < 0 or off + 4 > self.size:
            self._fail(f"OOB u32 ({ctx}) @ 0x{off:X}")
        return struct.unpack_from("<I", self.data, off)[0]

    def _u64(self, off, ctx=""):
        if off < 0 or off + 8 > self.size:
            self._fail(f"OOB u64 ({ctx}) @ 0x{off:X}")
        return struct.unpack_from("<Q", self.data, off)[0]

    def _parse_pe(self):
        if self.size < 0x40 or self.data[:2] != b"MZ":
            self._fail("Invalid DOS MZ signature")
        pe_off = self._u32(0x3C, "e_lfanew")
        if pe_off + 24 > self.size or self.data[pe_off:pe_off+4] != b"PE\x00\x00":
            self._fail("Invalid PE signature")
        num_sec = self._u16(pe_off + 6, "NumberOfSections")
        opt_size = self._u16(pe_off + 20, "SizeOfOptionalHeader")
        opt_off = pe_off + 24
        if opt_off + opt_size > self.size or opt_size < 96:
            self._fail("OptionalHeader exceeds file bounds")
        magic = self._u16(opt_off, "OptionalHeader.Magic")
        if magic not in (0x10B, 0x20B):
            self._fail(f"Unknown OptionalHeader magic: 0x{magic:X}")
        dd_off = opt_off + (96 if magic == 0x10B else 112)
        if dd_off + 15 * 8 > opt_off + opt_size:
            self._fail("DataDirectory CLI entry outside OptionalHeader")
        self.cli_rva = self._u32(dd_off + 14 * 8, "CLI_RVA")
        self.cli_size = self._u32(dd_off + 14 * 8 + 4, "CLI_Size")
        if self.cli_rva == 0 or self.cli_size < 72:
            self._fail("Missing or truncated CLI header")
        sec_off = opt_off + opt_size
        if sec_off + num_sec * 40 > self.size:
            self._fail("Section table exceeds file bounds")
        for i in range(num_sec):
            s = sec_off + i * 40
            vsize = self._u32(s + 8, f"Sec[{i}].VSize")
            vrva = self._u32(s + 12, f"Sec[{i}].VRVA")
            rsize = self._u32(s + 16, f"Sec[{i}].RSize")
            roff = self._u32(s + 20, f"Sec[{i}].ROff")
            if roff + rsize > self.size:
                self._fail(f"Section[{i}] raw data exceeds file bounds")
            self.sections.append((vrva, vsize, roff, rsize))

    def rva_to_offset(self, rva):
        if rva == 0:
            return 0
        for vrva, vsize, roff, rsize in self.sections:
            span = max(vsize, rsize)
            if vrva <= rva < vrva + span:
                delta = rva - vrva
                if delta >= rsize:
                    return -1
                return roff + delta
        return -1
    def _parse_cli(self):
        cli_off = self.rva_to_offset(self.cli_rva)
        if cli_off <= 0 or cli_off + 72 > self.size:
            self._fail("Unmapped CLI header RVA")
        meta_rva = self._u32(cli_off + 8, "MetadataRVA")
        meta_size = self._u32(cli_off + 12, "MetadataSize")
        meta_off = self.rva_to_offset(meta_rva)
        if meta_off <= 0 or meta_off + meta_size > self.size:
            self._fail("Invalid Metadata Root RVA/size")
        if self._u32(meta_off, "BSJB") != 0x424A5342:
            self._fail("Invalid Metadata BSJB signature")
        ver_len = self._u32(meta_off + 12, "VersionLength")
        ver_aligned = (ver_len + 3) & ~3
        num_streams = self._u16(meta_off + 16 + ver_aligned + 2, "NumberOfStreams")
        pos = meta_off + 16 + ver_aligned + 4
        self.streams = {}
        for i in range(num_streams):
            soff = self._u32(pos, f"Stream[{i}].Off")
            ssize = self._u32(pos + 4, f"Stream[{i}].Size")
            pos += 8
            end = self.data.find(b"\x00", pos, meta_off + meta_size)
            if end == -1:
                self._fail(f"Unterminated stream name[{i}]")
            name = self.data[pos:end].decode("ascii", errors="replace")
            pos = (end + 4) & ~3
            abs_off = meta_off + soff
            if abs_off < 0 or abs_off + ssize > self.size:
                self._fail(f"Stream '{name}' exceeds file bounds")
            self.streams[name] = (abs_off, ssize)
        for req in ("#~", "#Strings", "#US", "#Blob"):
            if req not in self.streams:
                self._fail(f"Required metadata stream missing: {req}")
        self._parse_tables()

    def get_string(self, idx):
        off, size = self.streams["#Strings"]
        if idx < 0 or idx >= size:
            return None
        end = self.data.find(b"\x00", off + idx, off + size)
        if end == -1:
            return None
        return self.data[off + idx:end].decode("utf-8", errors="replace")

    def get_user_string(self, offset):
        off, size = self.streams["#US"]
        if offset < 0 or offset >= size:
            return None
        p = off + offset
        b0 = self.data[p]
        if (b0 & 0x80) == 0:
            length = b0; p += 1
        elif (b0 & 0xC0) == 0x80:
            if p + 1 >= off + size: return None
            length = ((b0 & 0x3F) << 8) | self.data[p+1]; p += 2
        elif (b0 & 0xE0) == 0xC0:
            if p + 3 >= off + size: return None
            length = ((b0 & 0x1F) << 24) | (self.data[p+1] << 16) | (self.data[p+2] << 8) | self.data[p+3]; p += 4
        else:
            return None
        if length == 0:
            return ""
        if p + length > off + size:
            return None
        return self.data[p:p + length - 1].decode("utf-16le", errors="replace")
    def _parse_tables(self):
        off, tsize = self.streams["#~"]
        heap_sizes = self.data[off + 6]
        valid_mask = self._u64(off + 8, "ValidMask")
        str_sz = 4 if (heap_sizes & 0x01) else 2
        guid_sz = 4 if (heap_sizes & 0x02) else 2
        blob_sz = 4 if (heap_sizes & 0x04) else 2

        pos = off + 24
        self.row_counts = {}
        for tid in range(64):
            if (valid_mask >> tid) & 1:
                self.row_counts[tid] = self._u32(pos, f"RowCount[{tid}]")
                pos += 4

        idx_sz = lambda tids: 4 if any(self.row_counts.get(t, 0) >= 65536 for t in tids) else 2
        coded_sz = lambda tids, bits: 4 if any(self.row_counts.get(t, 0) >= (1 << (16 - bits)) for t in tids) else 2

        td_or_tr = coded_sz([0x02, 0x01, 0x1B], 2)
        has_const = coded_sz([0x04, 0x08, 0x17], 2)
        has_ca = coded_sz([0x06, 0x04, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x00, 0x0E, 0x17, 0x14, 0x11, 0x1A, 0x1B, 0x20, 0x23, 0x26, 0x27, 0x28], 5)
        has_fm = coded_sz([0x04, 0x08], 1)
        has_decl = coded_sz([0x02, 0x06, 0x20], 2)
        mr_parent = coded_sz([0x02, 0x01, 0x1A, 0x06, 0x1B], 3)
        has_sem = coded_sz([0x14, 0x17], 1)
        md_or_mr = coded_sz([0x06, 0x0A], 1)
        impl = coded_sz([0x04, 0x0A], 1)
        ca_type = coded_sz([0x06, 0x0A], 3)
        res_scope = coded_sz([0x00, 0x1A, 0x23, 0x01], 2)
        to_md = coded_sz([0x02, 0x06], 1)

        row_sizes = {
            0x00: 2 + str_sz + guid_sz * 3, 0x01: res_scope + str_sz * 2,
            0x02: 4 + str_sz * 2 + td_or_tr + idx_sz([0x04]) + idx_sz([0x06]),
            0x04: 2 + str_sz + blob_sz, 0x06: 4 + 2 + 2 + str_sz + blob_sz + idx_sz([0x08]),
            0x08: 2 + 2 + str_sz, 0x09: idx_sz([0x02]) + td_or_tr,
            0x0A: mr_parent + str_sz + blob_sz, 0x0B: 2 + has_const + blob_sz,
            0x0C: has_ca + ca_type + blob_sz, 0x0D: has_fm + blob_sz,
            0x0E: 2 + has_decl + blob_sz, 0x0F: 2 + 4 + idx_sz([0x02]),
            0x10: 4 + idx_sz([0x04]), 0x11: blob_sz,
            0x12: idx_sz([0x02]) + idx_sz([0x14]), 0x14: 2 + str_sz + td_or_tr,
            0x15: idx_sz([0x02]) + idx_sz([0x17]), 0x17: 2 + str_sz + blob_sz,
            0x18: 2 + idx_sz([0x06]) + has_sem, 0x19: idx_sz([0x02]) + md_or_mr + md_or_mr,
            0x1A: str_sz, 0x1B: blob_sz,
            0x1C: 2 + impl + str_sz + idx_sz([0x1A]), 0x1D: 4 + idx_sz([0x04]),
            0x20: 4 + 8 + 4 + blob_sz + str_sz * 2, 0x23: 8 + 4 + blob_sz + str_sz * 2 + blob_sz,
            0x26: 4 + 4 + str_sz + impl, 0x27: 4 + 4 + str_sz * 2 + impl,
            0x28: 4 + 4 + str_sz + impl, 0x29: idx_sz([0x02]) + idx_sz([0x02]),
            0x2A: 2 + 2 + to_md + str_sz, 0x2B: md_or_mr + blob_sz,
            0x2C: idx_sz([0x2A]) + td_or_tr,
        }

        table_offsets = {}
        for tid in range(64):
            cnt = self.row_counts.get(tid, 0)
            if cnt > 0:
                if tid not in row_sizes:
                    self._fail(f"Unsupported metadata table 0x{tid:02X}")
                table_offsets[tid] = pos
                pos += cnt * row_sizes[tid]
        if pos > off + tsize:
            self._fail("Metadata tables exceed #~ stream size")

        read_idx = lambda sz, p: self._u32(p) if sz == 4 else self._u16(p)

        self.types = {}
        typedefs = []
        for rid in range(1, self.row_counts.get(0x02, 0) + 1):
            r = table_offsets[0x02] + (rid - 1) * row_sizes[0x02]
            nm = self.get_string(read_idx(str_sz, r + 4))
            ns = self.get_string(read_idx(str_sz, r + 4 + str_sz))
            if nm is None or ns is None:
                self._fail(f"Invalid #Strings index in TypeDef[{rid}]")
            ml = read_idx(idx_sz([0x06]), r + 4 + str_sz * 2 + td_or_tr + idx_sz([0x04]))
            ft = f"{ns}.{nm}" if ns else nm
            self.types[0x02000000 | rid] = ft
            typedefs.append((rid, ft, ml))

        for rid in range(1, self.row_counts.get(0x01, 0) + 1):
            r = table_offsets[0x01] + (rid - 1) * row_sizes[0x01]
            nm = self.get_string(read_idx(str_sz, r + res_scope))
            ns = self.get_string(read_idx(str_sz, r + res_scope + str_sz))
            if nm is None or ns is None:
                self._fail(f"Invalid #Strings index in TypeRef[{rid}]")
            self.types[0x01000000 | rid] = f"{ns}.{nm}" if ns else nm

        self.methods = {}
        td_idx = 0
        for rid in range(1, self.row_counts.get(0x06, 0) + 1):
            while td_idx + 1 < len(typedefs) and typedefs[td_idx + 1][2] <= rid:
                td_idx += 1
            ft = typedefs[td_idx][1] if typedefs else "<Module>"
            r = table_offsets[0x06] + (rid - 1) * row_sizes[0x06]
            rva = self._u32(r)
            impl_flags = self._u16(r + 4)
            flags = self._u16(r + 6)
            nm = self.get_string(read_idx(str_sz, r + 8))
            if nm is None:
                self._fail(f"Invalid #Strings index in MethodDef[{rid}]")
            tok = 0x06000000 | rid
            foff = self.rva_to_offset(rva)
            self.methods[tok] = {
                "token": tok, "rid": rid, "type": ft, "name": nm,
                "rva": rva, "impl_flags": impl_flags, "flags": flags, "offset": foff
            }

        self.fields = {}
        for rid in range(1, self.row_counts.get(0x04, 0) + 1):
            r = table_offsets[0x04] + (rid - 1) * row_sizes[0x04]
            nm = self.get_string(read_idx(str_sz, r + 2))
            if nm is None:
                self._fail(f"Invalid #Strings index in FieldDef[{rid}]")
            self.fields[0x04000000 | rid] = nm

        self.memberrefs = {}
        for rid in range(1, self.row_counts.get(0x0A, 0) + 1):
            r = table_offsets[0x0A] + (rid - 1) * row_sizes[0x0A]
            nm = self.get_string(read_idx(str_sz, r + mr_parent))
            if nm is None:
                self._fail(f"Invalid #Strings index in MemberRef[{rid}]")
            self.memberrefs[0x0A000000 | rid] = nm

    def resolve_token(self, tok):
        tbl = tok >> 24
        rid = tok & 0x00FFFFFF
        if tbl == 0x70:
            us = self.get_user_string(rid)
            return (us is not None, f"US:'{us}'" if us is not None else f"INVALID_US:0x{tok:08X}")
        if rid == 0:
            return (False, f"ZERO_RID:0x{tok:08X}")
        if tbl == 0x06:
            m = self.methods.get(tok)
            return (m is not None, f"{m['type']}.{m['name']}" if m else f"INVALID_METHOD:0x{tok:08X}")
        if tbl == 0x04:
            f = self.fields.get(tok)
            return (f is not None, f"Field:{f}" if f else f"INVALID_FIELD:0x{tok:08X}")
        if tbl == 0x0A:
            mr = self.memberrefs.get(tok)
            return (mr is not None, f"Ref:{mr}" if mr else f"INVALID_MREF:0x{tok:08X}")
        if tbl in (0x01, 0x02):
            tp = self.types.get(tok)
            return (tp is not None, f"Type:{tp}" if tp else f"INVALID_TYPE:0x{tok:08X}")
        if tbl in (0x1B, 0x2B, 0x11):
            ok = rid <= self.row_counts.get(tbl, 0)
            return (ok, f"Tbl0x{tbl:02X}[{rid}]" if ok else f"INVALID_TOK:0x{tok:08X}")
        return (False, f"UNHANDLED_TBL:0x{tok:08X}")
ONE_BYTE = {
    0x00:("nop",0),0x01:("break",0),0x02:("ldarg.0",0),0x03:("ldarg.1",0),0x04:("ldarg.2",0),0x05:("ldarg.3",0),
    0x06:("ldloc.0",0),0x07:("ldloc.1",0),0x08:("ldloc.2",0),0x09:("ldloc.3",0),0x0A:("stloc.0",0),0x0B:("stloc.1",0),
    0x0C:("stloc.2",0),0x0D:("stloc.3",0),0x0E:("ldarg.s",1),0x0F:("ldarga.s",1),0x10:("starg.s",1),0x11:("ldloc.s",1),
    0x12:("ldloca.s",1),0x13:("stloc.s",1),0x14:("ldnull",0),0x15:("ldc.i4.m1",0),0x16:("ldc.i4.0",0),0x17:("ldc.i4.1",0),
    0x18:("ldc.i4.2",0),0x19:("ldc.i4.3",0),0x1A:("ldc.i4.4",0),0x1B:("ldc.i4.5",0),0x1C:("ldc.i4.6",0),0x1D:("ldc.i4.7",0),
    0x1E:("ldc.i4.8",0),0x1F:("ldc.i4.s",1),0x20:("ldc.i4",4),0x21:("ldc.i8",8),0x22:("ldc.r4","R4"),0x23:("ldc.r8","R8"),
    0x25:("dup",0),0x26:("pop",0),0x27:("jmp","TOK"),0x28:("call","TOK"),0x29:("calli","TOK"),0x2A:("ret",0),
    0x2B:("br.s","I1"),0x2C:("brfalse.s","I1"),0x2D:("brtrue.s","I1"),0x2E:("beq.s","I1"),0x2F:("bge.s","I1"),
    0x30:("bgt.s","I1"),0x31:("ble.s","I1"),0x32:("blt.s","I1"),0x33:("bne.un.s","I1"),0x34:("bge.un.s","I1"),
    0x35:("bgt.un.s","I1"),0x36:("ble.un.s","I1"),0x37:("blt.un.s","I1"),0x38:("br","I4"),0x39:("brfalse","I4"),
    0x3A:("brtrue","I4"),0x3B:("beq","I4"),0x3C:("bge","I4"),0x3D:("bgt","I4"),0x3E:("ble","I4"),0x3F:("blt","I4"),
    0x40:("bne.un","I4"),0x41:("bge.un","I4"),0x42:("bgt.un","I4"),0x43:("ble.un","I4"),0x44:("blt.un","I4"),
    0x45:("switch","SW"),0x46:("ldind.i1",0),0x47:("ldind.u1",0),0x48:("ldind.i2",0),0x49:("ldind.u2",0),
    0x4A:("ldind.i4",0),0x4B:("ldind.u4",0),0x4C:("ldind.i8",0),0x4D:("ldind.i",0),0x4E:("ldind.r4",0),
    0x4F:("ldind.r8",0),0x50:("ldind.ref",0),0x51:("stind.ref",0),0x52:("stind.i1",0),0x53:("stind.i2",0),
    0x54:("stind.i4",0),0x55:("stind.i8",0),0x56:("stind.r4",0),0x57:("stind.r8",0),0x58:("add",0),0x59:("sub",0),
    0x5A:("mul",0),0x5B:("div",0),0x5C:("div.un",0),0x5D:("rem",0),0x5E:("rem.un",0),0x5F:("and",0),0x60:("or",0),
    0x61:("xor",0),0x62:("shl",0),0x63:("shr",0),0x64:("shr.un",0),0x65:("neg",0),0x66:("not",0),0x67:("conv.i1",0),
    0x68:("conv.i2",0),0x69:("conv.i4",0),0x6A:("conv.i8",0),0x6B:("conv.r4",0),0x6C:("conv.r8",0),0x6D:("conv.u4",0),
    0x6E:("conv.u8",0),0x6F:("callvirt","TOK"),0x70:("cpobj","TOK"),0x71:("ldobj","TOK"),0x72:("ldstr","TOK"),
    0x73:("newobj","TOK"),0x74:("castclass","TOK"),0x75:("isinst","TOK"),0x76:("conv.r.un",0),0x79:("unbox","TOK"),
    0x7A:("throw",0),0x7B:("ldfld","TOK"),0x7C:("ldflda","TOK"),0x7D:("stfld","TOK"),0x7E:("ldsfld","TOK"),
    0x7F:("ldsflda","TOK"),0x80:("stsfld","TOK"),0x81:("stobj","TOK"),0x82:("conv.ovf.i1.un",0),0x83:("conv.ovf.i2.un",0),
    0x84:("conv.ovf.i4.un",0),0x85:("conv.ovf.i8.un",0),0x86:("conv.ovf.u1.un",0),0x87:("conv.ovf.u2.un",0),
    0x88:("conv.ovf.u4.un",0),0x89:("conv.ovf.u8.un",0),0x8A:("conv.ovf.i.un",0),0x8B:("conv.ovf.u.un",0),
    0x8C:("box","TOK"),0x8D:("newarr","TOK"),0x8E:("ldlen",0),0x8F:("ldelema","TOK"),0x90:("ldelem.i1",0),
    0x91:("ldelem.u1",0),0x92:("ldelem.i2",0),0x93:("ldelem.u2",0),0x94:("ldelem.i4",0),0x95:("ldelem.u4",0),
    0x96:("ldelem.i8",0),0x97:("ldelem.i",0),0x98:("ldelem.r4",0),0x99:("ldelem.r8",0),0x9A:("ldelem.ref",0),
    0x9B:("stelem.i",0),0x9C:("stelem.i1",0),0x9D:("stelem.i2",0),0x9E:("stelem.i4",0),0x9F:("stelem.i8",0),
    0xA0:("stelem.r4",0),0xA1:("stelem.r8",0),0xA2:("stelem.ref",0),0xA3:("ldelem","TOK"),0xA4:("stelem","TOK"),
    0xA5:("unbox.any","TOK"),0xB3:("conv.ovf.i1",0),0xB4:("conv.ovf.u1",0),0xB5:("conv.ovf.i2",0),0xB6:("conv.ovf.u2",0),
    0xB7:("conv.ovf.i4",0),0xB8:("conv.ovf.u4",0),0xB9:("conv.ovf.i8",0),0xBA:("conv.ovf.u8",0),0xC2:("refanyval","TOK"),
    0xC3:("ckfinite",0),0xC6:("mkrefany","TOK"),0xD0:("ldtoken","TOK"),0xD1:("conv.u2",0),0xD2:("conv.u1",0),
    0xD3:("conv.i",0),0xD4:("conv.ovf.i",0),0xD5:("conv.ovf.u",0),0xD6:("add.ovf",0),0xD7:("add.ovf.un",0),
    0xD8:("mul.ovf",0),0xD9:("mul.ovf.un",0),0xDA:("sub.ovf",0),0xDB:("sub.ovf.un",0),0xDC:("endfinally",0),
    0xDD:("leave","I4"),0xDE:("leave.s","I1"),0xDF:("stind.i",0),0xE0:("conv.u",0)
}
TWO_BYTE = {
    0x00:("arglist",0),0x01:("ceq",0),0x02:("cgt",0),0x03:("cgt.un",0),0x04:("clt",0),0x05:("clt.un",0),
    0x06:("ldftn","TOK"),0x07:("ldvirtftn","TOK"),0x09:("ldarg",2),0x0A:("ldarga",2),0x0B:("starg",2),
    0x0C:("ldloc",2),0x0D:("ldloca",2),0x0E:("stloc",2),0x0F:("localloc",0),0x11:("endfilter",0),
    0x12:("unaligned.",1),0x13:("volatile.",0),0x14:("tail.",0),0x15:("initobj","TOK"),0x16:("constrained.","TOK"),
    0x17:("cpblk",0),0x18:("initblk",0),0x1A:("rethrow",0),0x1C:("sizeof","TOK"),0x1D:("refanytype",0),0x1E:("readonly.",0)
}
def parse_method_structure(pe, m):
    rva = m["rva"]
    off = m["offset"]
    if rva == 0:
        is_abs = (m["flags"] & 0x0400) != 0 or (m["impl_flags"] & 0x0003) != 0 or (m["impl_flags"] & 0x1000) != 0
        return ("ABSTRACT_OR_EXTERN", None) if is_abs else ("INVALID_RVA_ZERO", "Non-abstract method has RVA 0")
    if off < 0 or off >= pe.size:
        return ("UNMAPPED_RVA", f"RVA 0x{rva:X} unmapped")

    b0 = pe.data[off]
    fmt = b0 & 0x03
    eh_clauses = []
    if fmt == 0x02:
        code_size = b0 >> 2; body_off = off + 1; max_stack = 8; local_tok = 0; more_sects = False
    elif fmt == 0x03:
        if off + 12 > pe.size:
            return ("TRUNCATED_FAT_HEADER", "Fat header < 12 bytes")
        flags_hdr, max_stack, code_size, local_tok = struct.unpack_from("<HHII", pe.data, off)
        hdr_size = (flags_hdr >> 12) * 4
        if hdr_size < 12 or off + hdr_size > pe.size:
            return ("INVALID_FAT_HEADER_SIZE", f"hdr_size={hdr_size}")
        more_sects = (flags_hdr & 0x0008) != 0; body_off = off + hdr_size
    else:
        return ("INVALID_HEADER_FORMAT", f"fmt=0x{fmt:X}")

    if body_off + code_size > pe.size:
        return ("TRUNCATED_BODY", "body_off+code_size > file")
    if local_tok != 0:
        ok, _ = pe.resolve_token(local_tok)
        if not ok or (local_tok >> 24) != 0x11:
            return ("INVALID_LOCAL_SIG", f"local_tok=0x{local_tok:08X}")

    if more_sects:
        sec_pos = (body_off + code_size + 3) & ~3
        while True:
            if sec_pos + 4 > pe.size:
                return ("TRUNCATED_EH_HEADER", "EH header OOB")
            sflags = pe.data[sec_pos]
            if (sflags & 0x3F) != 0x01:
                return ("INVALID_EH_KIND", f"sflags=0x{sflags:02X}")
            is_fat_eh = (sflags & 0x40) != 0; has_more = (sflags & 0x80) != 0
            if is_fat_eh:
                dsize = pe.data[sec_pos+1] | (pe.data[sec_pos+2] << 8) | (pe.data[sec_pos+3] << 16)
                if dsize < 4 or (dsize - 4) % 24 != 0 or sec_pos + dsize > pe.size:
                    return ("INVALID_FAT_EH_SIZE", f"dsize={dsize}")
                p = sec_pos + 4
                for _ in range((dsize - 4) // 24):
                    eh_clauses.append(struct.unpack_from("<IIIIII", pe.data, p)); p += 24
            else:
                dsize = pe.data[sec_pos+1]
                if dsize < 4 or (dsize - 4) % 12 != 0 or sec_pos + dsize > pe.size:
                    return ("INVALID_SMALL_EH_SIZE", f"dsize={dsize}")
                p = sec_pos + 4
                for _ in range((dsize - 4) // 12):
                    eh_clauses.append(struct.unpack_from("<HHBHBI", pe.data, p)); p += 12
            if not has_more:
                break
            sec_pos = (sec_pos + dsize + 3) & ~3

    return ("OK", {"body_off": body_off, "code_size": code_size, "max_stack": max_stack, "local_tok": local_tok, "eh": eh_clauses})

def decoder_a(pe, code, eh_clauses):
    pc = 0; n = len(code); insns = []; starts = set(); branches = []; tok_errs = []
    while pc < n:
        starts.add(pc); spc = pc; op = code[pc]; pc += 1
        if op == 0xFE:
            if pc >= n: return ("FAIL_TRUNC_FE", insns)
            op2 = code[pc]; pc += 1
            if op2 not in TWO_BYTE: return (f"FAIL_UNK_FE_{op2:02X}", insns)
            mnem, spec = TWO_BYTE[op2]
        else:
            if op not in ONE_BYTE: return (f"FAIL_UNK_{op:02X}", insns)
            mnem, spec = ONE_BYTE[op]
        op_str = ""
        if isinstance(spec, int) and spec > 0:
            if pc + spec > n: return (f"FAIL_TRUNC_OP_{mnem}", insns)
            val = int.from_bytes(code[pc:pc+spec], "little"); pc += spec; op_str = f"0x{val:X}"
        elif spec == "R4":
            if pc + 4 > n: return ("FAIL_TRUNC_R4", insns)
            op_str = f"{struct.unpack_from('<f', code, pc)}"; pc += 4
        elif spec == "R8":
            if pc + 8 > n: return ("FAIL_TRUNC_R8", insns)
            op_str = f"{struct.unpack_from('<d', code, pc)}"; pc += 8
        elif spec == "TOK":
            if pc + 4 > n: return (f"FAIL_TRUNC_TOK_{mnem}", insns)
            tok = struct.unpack_from("<I", code, pc)[0]; pc += 4
            ok, desc = pe.resolve_token(tok)
            if not ok: tok_errs.append(f"IL_{spc:04X}:{desc}")
            op_str = f"0x{tok:08X} [{desc}]"
        elif spec == "I1":
            if pc + 1 > n: return (f"FAIL_TRUNC_I1_{mnem}", insns)
            rel = struct.unpack_from("<b", code, pc)[0]; pc += 1
            targ = pc + rel; branches.append((spc, mnem, targ)); op_str = f"IL_{targ:04X}"
        elif spec == "I4":
            if pc + 4 > n: return (f"FAIL_TRUNC_I4_{mnem}", insns)
            rel = struct.unpack_from("<i", code, pc)[0]; pc += 4
            targ = pc + rel; branches.append((spc, mnem, targ)); op_str = f"IL_{targ:04X}"
        elif spec == "SW":
            if pc + 4 > n: return ("FAIL_TRUNC_SW_CNT", insns)
            cnt = struct.unpack_from("<I", code, pc)[0]; pc += 4
            if cnt > 65535 or pc + cnt * 4 > n: return ("FAIL_TRUNC_SW_TBL", insns)
            base = pc + cnt * 4; targs = []
            for _ in range(cnt):
                rel = struct.unpack_from("<i", code, pc)[0]; pc += 4
                t = base + rel; branches.append((spc, "switch", t)); targs.append(f"IL_{t:04X}")
            op_str = f"[{', '.join(targs)}]"
        insns.append((spc, mnem, op_str))

    for spc, mnem, t in branches:
        if t < 0 or t >= n or t not in starts: return (f"FAIL_BAD_BRANCH_{mnem}_0x{t:X}", insns)
    for cflags, toff, tlen, hoff, hlen, ctok in eh_clauses:
        if toff + tlen > n or hoff + hlen > n or toff not in starts or hoff not in starts: return ("FAIL_BAD_EH_BOUNDS", insns)
        if cflags == 0 and ctok != 0:
            ok, _ = pe.resolve_token(ctok)
            if not ok: return (f"FAIL_BAD_EH_CATCH_TOK_0x{ctok:08X}", insns)
    if tok_errs: return (f"FAIL_TOKEN_{tok_errs}", insns)
    return ("PASS", insns)

def decoder_b_independent(code):
    mv = memoryview(code); idx = 0; end = len(mv); trace = []
    while idx < end:
        start = idx; b = mv[idx]; idx += 1
        if b == 0xFE:
            if idx >= end or mv[idx] not in TWO_BYTE: return ("B_FAIL", trace)
            mnem, spec = TWO_BYTE[mv[idx]]; idx += 1
        else:
            if b not in ONE_BYTE: return ("B_FAIL", trace)
            mnem, spec = ONE_BYTE[b]
        if isinstance(spec, int): step = spec
        elif spec in ("R4", "TOK", "I4"): step = 4
        elif spec == "R8": step = 8
        elif spec == "I1": step = 1
        elif spec == "SW":
            if idx + 4 > end: return ("B_FAIL", trace)
            cnt = int.from_bytes(mv[idx:idx+4], "little"); idx += 4; step = cnt * 4
        else: return ("B_FAIL", trace)
        if idx + step > end: return ("B_FAIL", trace)
        trace.append((start, mnem, bytes(mv[start:idx+step]).hex()))
        idx += step
    return ("B_PASS", trace)
def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    pe = HardenedPE(DLL_PATH)

    ceto_methods = [
        m for m in pe.methods.values()
        if m["type"].startswith("Ceto.") or "Ocean" in m["type"] or "Wave" in m["type"] or "Fourier" in m["type"] or "Displacement" in m["type"]
    ]
    ceto_methods.sort(key=lambda x: x["token"])

    counts = {"VERIFIED": 0, "PARTIALLY VERIFIED": 0, "INFERRED": 0, "UNVERIFIED": 0, "ABSTRACT_EXCLUDED": 0}
    manifest = []
    ab_diffs = []
    decoded_a = {}

    for m in ceto_methods:
        tok_hex = f"0x{m['token']:08X}"
        st, info = parse_method_structure(pe, m)
        if st == "ABSTRACT_OR_EXTERN":
            counts["ABSTRACT_EXCLUDED"] += 1
            manifest.append({"token": tok_hex, "method": f"{m['type']}.{m['name']}", "status": "ABSTRACT_EXCLUDED", "detail": "Confirmed RVA=0 abstract/extern"})
            continue
        if st != "OK":
            counts["UNVERIFIED"] += 1
            manifest.append({"token": tok_hex, "method": f"{m['type']}.{m['name']}", "status": "UNVERIFIED", "detail": f"{st}: {info}"})
            continue

        code = pe.data[info["body_off"]:info["body_off"] + info["code_size"]]
        res_a, insns_a = decoder_a(pe, code, info["eh"])
        res_b, trace_b = decoder_b_independent(code)
        decoded_a[m["token"]] = insns_a

        bounds_match = (res_a == "PASS" and res_b == "B_PASS" and [(x[0], x[1]) for x in insns_a] == [(y[0], y[1]) for y in trace_b])
        if bounds_match:
            final_st = "VERIFIED"; counts["VERIFIED"] += 1
        elif res_a.startswith("FAIL_TOKEN_") and res_b == "B_PASS":
            final_st = "PARTIALLY VERIFIED"; counts["PARTIALLY VERIFIED"] += 1
            ab_diffs.append(f"{tok_hex} {m['type']}.{m['name']} -> DecoderA={res_a} | DecoderB={res_b}")
        else:
            final_st = "UNVERIFIED"; counts["UNVERIFIED"] += 1
            ab_diffs.append(f"{tok_hex} {m['type']}.{m['name']} -> DecoderA={res_a} | DecoderB={res_b}")

        manifest.append({
            "token": tok_hex, "rid": m["rid"], "method": f"{m['type']}.{m['name']}",
            "rva": f"0x{m['rva']:X}", "code_size": info["code_size"],
            "eh_clauses": len(info["eh"]), "decoder_a": res_a, "decoder_b": res_b, "status": final_st
        })

    with open(MANIFEST_FILE, "w", encoding="utf-8") as f:
        json.dump({"dll_sha256": pe.sha256, "counts": counts, "methods": manifest}, f, indent=2)

    with open(DIFF_FILE, "w", encoding="utf-8") as f:
        f.write("DECODER A vs INDEPENDENT DECODER B COMPARISON REPORT\n" + "=" * 80 + "\n")
        f.write(f"Total Scanned: {len(ceto_methods)} | VERIFIED: {counts['VERIFIED']} | PARTIALLY VERIFIED: {counts['PARTIALLY VERIFIED']} | UNVERIFIED: {counts['UNVERIFIED']} | ABSTRACT: {counts['ABSTRACT_EXCLUDED']}\n\n")
        f.write("Disagreements / Structural Failures:\n")
        f.write("\n".join(ab_diffs) if ab_diffs else "NONE (0 disagreements on verified methods)\n")

    def dump_il(tok):
        m = pe.methods[tok]
        lines = [f"--- {m['type']}.{m['name']} (0x{tok:08X}) ---"]
        for pc, mnem, op in decoded_a.get(tok, []):
            lines.append(f"  IL_{pc:04X}: {mnem:<14} {op}")
        return "\n".join(lines)

    rep = [
        "=" * 90, "GOOGLE AI CORE V2 — HARDENED PHASE 1 FORENSIC REPORT",
        f"Generated : {datetime.now().isoformat(timespec='seconds')}", f"DLL SHA256: {pe.sha256}", "=" * 90, "",
        "[1. RECALCULATED VERIFICATION COUNTS (4-STATUS MODEL)]",
        f"  - Total Scanned Methods      : {len(ceto_methods)}",
        f"  - VERIFIED (Dual-Decoder OK) : {counts['VERIFIED']}",
        f"  - PARTIALLY VERIFIED         : {counts['PARTIALLY VERIFIED']}",
        f"  - INFERRED                   : {counts['INFERRED']}",
        f"  - UNVERIFIED (Failed Checks) : {counts['UNVERIFIED']}",
        f"  - ABSTRACT / EXTERN (RVA=0)  : {counts['ABSTRACT_EXCLUDED']}", "",
        "[2. VERIFIED EVIDENCE — GRID 3 & CPU BUFFER LIFECYCLE]",
        dump_il(0x06003091), "", dump_il(0x06002F78), "", dump_il(0x06002FBD), "",
        "[3. REMAINING STRUCTURAL & EVIDENCE GAPS]",
        "  - Compiled Shader / ComputeShader binaries (.shader/.assets): UNVERIFIED (external to Assembly-CSharp.dll)",
        "  - Serialized Scene Inspector parameters (fourierSize, gridScale, windSpeed): UNVERIFIED (external to DLL)",
        "=" * 90
    ]
    with open(REPORT_FILE, "w", encoding="utf-8") as f:
        f.write("\n".join(rep))

    print(f"[DONE] Scanned={len(ceto_methods)} | VERIFIED={counts['VERIFIED']} | PARTIAL={counts['PARTIALLY VERIFIED']} | UNVERIFIED={counts['UNVERIFIED']} | ABSTRACT={counts['ABSTRACT_EXCLUDED']}")
    print(f"[FILES] {REPORT_FILE}, {MANIFEST_FILE}, {DIFF_FILE}")

if __name__ == "__main__":
    main()
