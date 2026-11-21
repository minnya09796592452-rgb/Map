# CASTAWAY-DEPTHS — Mission 3 Reverse Engineering

ဒီ repository သည် **Stranded Deep ၏ Unity map/terrain data ကို source evidence အခြေခံပြီး စစ်ဆေးကာ Godot သို့ မှန်ကန်စွာ ပြောင်းရွှေ့ရန်** စုဆောင်းထားသော extraction, binary, serialized-object, C# source နှင့် IL evidence များဖြစ်သည်။

> **အရေးကြီး:** `level3` ထဲရှိ Terrain 49 ခုကို Mission 3 ဟု object count တစ်ခုတည်းဖြင့် မသတ်မှတ်ထားပါ။ Game source က Mission 3 ကို `Zone.Id == "INTERNAL_MISSION_3"` ဖြင့်ရှာသည်။ လက်ရှိ serialized scene audit မှာ runtime `World.MapList` index ကို မသိရသေးပါ။

## အရင်ဖတ်ရန်

1. [Mission 3 Master Reverse-Engineering Record](MISSION3_REVERSE_ENGINEERING_RECORD.txt) — အတည်ပြုထားသည့် findings, status, limitations, Linux/Python workflow။
2. [V26.2 master summary](extracted/V26.2/00_V26_2_MASTER_SUMMARY.txt) — နောက်ဆုံး Terrain/object correlation audit။
3. [V26.2 source method evidence](extracted/V26.2/10_V26_2_SOURCE_METHOD_EVIDENCE.txt) — Map Load/Save, World bundle, zone assignment နှင့် mission identity source။
4. [V26.2 Terrain component index](extracted/V26.2/03_V26_2_TERRAIN_COMPONENT_INDEX.csv) နှင့် [Terrain/TerrainCollider data links](extracted/V26.2/04_V26_2_TERRAIN_DATA_LINKS.csv)။
5. [V26.2 Transform hierarchy](extracted/V26.2/05_V26_2_TRANSFORM_HIERARCHY.csv), [Terrain world-transform audit](extracted/V26.2/07_V26_2_TERRAIN_WORLD_TRANSFORMS.csv)။
6. [Heightmap profile](extracted/V26.2/11A_V26_2_HEIGHTMAP_PROFILE.csv), [raw sample checkpoints](extracted/V26.2/11_V26_2_HEIGHTMAP_SAMPLES.csv), [TerrainData correlation](extracted/V26.2/12_V26_2_HEIGHTMAP_CORRELATION.csv)။
7. [Runtime mission mapping audit](extracted/V26.2/13_V26_2_RUNTIME_MISSION_MAPPING.csv) နှင့် [external-reference audit](extracted/V26.2/09_V26_2_EXTERNAL_REFERENCE_AUDIT.csv)။

## လက်ရှိအတည်ပြုထားသော အချက်များ

- Repository audit: `main` branch တွင် ယခင် audit အချိန်က tracked file **734 ခု**၊ top-level evidence group များမှာ `extracted/` 684, `V232/` 39, `Scrpic/` 5 နှင့် root files များဖြစ်သည်။ အသေးစိတ် path/size/blob SHA များကို အောက်ပါ directory tree မှ တိုက်ရိုက်ကြည့်နိုင်သည်။
- V23.2 input field CSV: **99,111 rows**, **17 containers**။ အသုံးပြုထားသည့် `03_V23_2_TERRAIN_TRANSFORM_FIELDS.csv` သည် V26.2 input manifest ထဲရှိ SHA-256 နှင့် တူညီသည်: `5f25d105378e20b3eca2988c96369071d0ed3668303ba67794b3a5d3cd1cf974`။
- V26.2 serialized object index: **21,100 objects** — GameObject 11,373; Transform 9,574; Terrain 51; TerrainCollider 51; TerrainData 51။
- `level3` တွင် Terrain 49 ခုနှင့် TerrainCollider 49 ခုရှိသည်။ ထို့အပြင် `level4` နှင့် `sharedassets3.assets` တွင် တစ်ခုစီရှိသောကြောင့် global total သည် 51 ဖြစ်သည်။
- Mission 3 binary height file သည် 132,098 bytes ဖြစ်ပြီး 257 × 257 × 2 နှင့် ကိုက်ညီသည်။ File သည် little-endian UInt16 ဖြစ်သည်။ သို့သော် Game loader က raw ushort ကို `ToCompressStable()` ဖြင့် stabilize လုပ်ပြီးမှ `/65535f` ပြုလုပ်သောကြောင့် raw division sample ကို runtime-final height နှင့် တန်းတူမယူရ။
- Map source code မှ map တစ်ခုသည် EDITOR, OBJECT, HEIGHT stream သုံးခုဖြစ်ကြောင်း အတည်ပြုထားသည်။ EDITOR ကို BinaryFormatter, OBJECT ကို JObject JSON text, HEIGHT ကို 257×257 UInt16 loop ဖြင့် ဖတ်/ရေးသည်။
- `World.Bundle` သည် GZip stream အတွင်း 49 Maps, map stream length/data blocks, 49 zone-position Vector2 များနှင့် generated-zone-point list ကို အစဉ်လိုက် သိမ်းသည်။
- `ApplyWorldToZones()` သည် `Zones[i]` ကို `World.MapList[i]` နှင့်တွဲပြီး `map.HeightmapData` ကို TerrainData.SetHeights သို့ပေးသည်။ `FindEndGameZone()` သည် `Zone.Id == "INTERNAL_MISSION_3"` ကိုရှာသည်။ Runtime index တန်ဖိုးကိုတော့ မသိရသေးပါ။
- TerrainData 51 ခုလုံး၏ `m_Heightmap` preview သည် truncate ဖြစ်နေသည်။ 257×257 complete array မရသေးသဖြင့် TerrainData-to-binary exact numeric comparison မလုပ်ရသေးပါ။
- Static Terrain world transforms ကို အပြည့်မတွက်နိုင်သေးပါ။ 50 ခုတွင် local rotation မရှိ/မဖြေရှင်းနိုင်၊ တစ်ခုတွင် parent target မရှိသောကြောင့် world XYZ ကို မထုတ်ပြန်ထားပါ။

## Repository layout

| Path | ရည်ရွယ်ချက် |
|---|---|
| `extracted/Default Maps/` | Default map triplets (EDITOR/OBJECT/HEIGHT), binary/profile reports, Mission 3 deep reports |
| `extracted/V23.2/` | Unity serialized object index, field CSV, pointer refs, external refs, source evidence |
| `extracted/V23.2 Last New/`, `V23.2 Latest/`, `V23.2 Latest 1/` | Terrain/transform extraction passes နှင့် interim identity/preview audits |
| `extracted/V24/` | Decompiled C# source နှင့် source-reader index/parts |
| `extracted/V25/` | 99,111-row input ပေါ်မှ Terrain, Collider, TerrainData, Transform correlation |
| `extracted/V26.2/` | လက်ရှိအဓိက correlation output, external-reference audit, heightmap profile, runtime-index audit |
| `V232/` | Assembly-CSharp IL/token, map constants, default map path, transform trace evidence |
| `Scrpic/` | Zone ID/IL evidence batch, method index, manifest |
| `Default Maps.zip` | Default Maps source archive |
| `MISSION_3_TERRAIN_CLASS_MAP.txt` | Terrain-related class evidence index |
| `MISSION_3_SOURCE_EXTRACTION_V15.txt` | Historical V15 extraction status; later V24/V26.2 source outputs supersede its old “decompiler not found” note |

## Mission 3 work status

| Workstream | Status |
|---|---|
| Single HEIGHT.map decoding | **Raw format confirmed**: LE UInt16, 257×257; runtime stable-value remap must be applied to reproduce Unity exactly |
| Map file structure | **Source layout confirmed**: EDITOR binary + OBJECT JObject text + HEIGHT UInt16; exact full EDITOR BinaryFormatter schema not fully documented |
| Transform serialization | **Save/Load confirmed**: localPosition Vector3, localRotation Quaternion, localScale Vector3; Truncate only applies to scale |
| 49 Terrain/Collider links | **Partial**: 49 level3 Terrain + 49 level3 Collider identified; TerrainData links are file-scoped and mostly unresolved external targets |
| 51 TerrainData full sample arrays | **Pending**: previews truncated; full source asset arrays not recovered |
| 49-zone world position | **Assignment rule confirmed**: position = (GenerationZonePositons[i].x, 0, .y); actual 49 coordinate values and complete static parent transforms not resolved |
| Mission 3 runtime MapList index | **Pending**: source assignment path known; actual loaded World.MapList order not recovered |
| Godot integration | **Not implemented/verified**: requires exact stable-height decode, source array order, dimensions, world placement and coordinate convention validation |

## Data integrity notes

- V26.2 reports `ERRORS: NONE` and `WARNINGS: NONE`; that does **not** mean every requested output is complete. The report itself marks runtime index UNKNOWN, TerrainData arrays incomplete, and world transforms unresolved.
- V26.2 heightmap profile's recorded actual SHA-256 is `4694e3cd0d762fcd6fb282d524520465c1a37ffbbd70868786e613cb729e46a5`; its `expected_sha256` is different (`4694e3cd0d762fcd6fb282df88e0e3873fb1c730c8b8e4a8e35a129eaa7463bcf`). Keep this mismatch visible; do not silently call the expected hash verified.
- V25 explicitly did not claim that `level3` is Mission 3. That caution remains valid.
- The large CSV previews are indexes/previews, not substitutes for resolving referenced external serialized files.

## Scope

This repository is a read-only reverse-engineering/evidence archive for map and terrain migration. No unrelated gameplay systems, save/load feature work, or new runtime game code are implied by these documents.
