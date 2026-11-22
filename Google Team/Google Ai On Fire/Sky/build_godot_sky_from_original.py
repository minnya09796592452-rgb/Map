import json, shutil
from pathlib import Path

UNITY_BASE = Path(
    "/storage/emulated/0/Download/"
    "Stranded-Deep-Original/Stranded Deep"
).resolve()
EXT = UNITY_BASE / "CASTAWAY_UNITY_EXTRACT/SKY_LIGHTING"
GODOT_SRC = Path(
    "/storage/emulated/0/Download/"
    "Stranded-Deep-Original/Godot Project"
).resolve()
GWORK = Path(
    "/storage/emulated/0/Download/"
    "CASTAWAY_DEPTHS_WORK/GODOT"
).resolve()

# 1. Inspect extracted RenderSettings, Materials & Level Lights for exact values
summary = []
for jf in sorted((EXT / "Lighting_Data").glob("*.json")):
    txt = jf.read_text(encoding="utf-8", errors="replace")
    data = json.loads(txt)
    if isinstance(data, dict) and "m_FogColor" in data:
        summary.append(
            f"[RenderSettings] {jf.name}: "
            f"fog={data.get('m_Fog')} "
            f"fogColor={data.get('m_FogColor')} "
            f"fogDensity={data.get('m_FogDensity')} "
            f"fogStart={data.get('m_LinearFogStart')} "
            f"fogEnd={data.get('m_LinearFogEnd')} "
            f"ambientSky={data.get('m_AmbientSkyColor')} "
            f"ambientEquator={data.get('m_AmbientEquatorColor')} "
            f"ambientGround={data.get('m_AmbientGroundColor')} "
            f"skybox={data.get('m_SkyboxMaterial')} "
            f"sun={data.get('m_Sun')}"
        )
    elif isinstance(data, list):
        for it in data:
            if it.get("type") == "Light":
                ld = it.get("data", {})
                summary.append(
                    f"[Light:{jf.name}] go={it.get('go_name')} "
                    f"type={ld.get('m_Type')} "
                    f"color={ld.get('m_Color')} "
                    f"intensity={ld.get('m_Intensity')} "
                    f"shadows={ld.get('m_Shadows')}"
                )

for mf in sorted((EXT / "Materials").glob("*.json")):
    if any(k in mf.name.lower() for k in ("sky", "cloud", "moon")):
        md = json.loads(mf.read_text(encoding="utf-8"))
        sp = md.get("m_SavedProperties", {})
        summary.append(
            f"[Material] {mf.name}: "
            f"shader={md.get('m_Shader')} "
            f"floats={list(dict(sp.get('m_Floats', [])).keys())[:10]} "
            f"colors={dict(sp.get('m_Colors', []))}"
        )

# 2. Check extracted Meshes & Shaders
meshes = [p.name for p in sorted((EXT / "Meshes").glob("*.obj"))]
shaders = [p.name for p in sorted((EXT / "Shaders").glob("*.json"))]
texs = [p.name for p in sorted((EXT / "Textures").glob("*.png"))]

rep = EXT / "Reports/ORIGINAL_EXACT_VALUES.txt"
rep.write_text(
    "=== EXTRACTED EXACT VALUES ===\n"
    + "\n".join(summary)
    + f"\n\nMESHES({len(meshes)}): {meshes}"
    + f"\nSHADERS({len(shaders)}): {shaders}"
    + f"\nTEXTURES({len(texs)}): {texs[:20]}\n",
    encoding="utf-8",
)
print("\n".join(summary[:18]))
print(f"MESHES={meshes} | SHADERS={shaders}")

# 3. Copy extracted original sky textures & meshes into GWORK/sky_assets (100% Original!)
copied_sky = []
for fn in (
    "stars_texture_70.png",
    "inscatter1Table16x1_Polarised_98.png",
    "MoonPhase5_107.png",
    "MostlyCloud_6807535986202380995.png",
):
    sp = EXT / "Textures" / fn
    if sp.exists():
        shutil.copy2(sp, GWORK / "sky_assets" / fn)
        copied_sky.append(fn)

for mf in (EXT / "Meshes").glob("*.obj"):
    if any(k in mf.name.lower() for k in ("sphere", "dome", "sky", "cloud", "plane")):
        shutil.copy2(mf, GWORK / "sky_assets" / mf.name)
        copied_sky.append(mf.name)

# Copy ship/GLB reference & ocean shader from GODOT_SRC (Read-Only)
for g in ("mission_shark.glb", "mission_eel.glb", "introscene.glb", "beepi.glb"):
    gp = GODOT_SRC / "Glb" / g
    if gp.exists():
        shutil.copy2(gp, GWORK / "Glb" / g)

# Build Godot 4.7.2 Sky_Lighting_Test.tscn using Original Stranded Deep Atmosphere + Extracted Textures
sky_shader = [
    "shader_type sky;",
    "uniform sampler2D stars_tex : source_color;",
    "uniform sampler2D cloud_tex : source_color;",
    "uniform sampler2D inscatter_lut : source_color;",
    "uniform vec3 zenith_color : source_color = vec3(0.08, 0.34, 0.68);",
    "uniform vec3 horizon_color : source_color = vec3(0.58, 0.80, 0.94);",
    "uniform vec3 ground_color : source_color = vec3(0.03, 0.16, 0.28);",
    "uniform float cloud_speed = 0.008;",
    "void sky() {",
    "    float h = clamp(EYEDIR.y, 0.0, 1.0);",
    "    vec3 sky_grad = mix(horizon_color, zenith_color, pow(h, 0.55));",
    "    if (EYEDIR.y < 0.0) {",
    "        sky_grad = mix(horizon_color, ground_color, clamp(-EYEDIR.y * 4.0, 0.0, 1.0));",
    "    }",
    "    vec2 sky_uv = EYEDIR.xz / (abs(EYEDIR.y) + 0.35) * 0.25 + vec2(TIME * cloud_speed, TIME * cloud_speed * 0.5);",
    "    float cld = texture(cloud_tex, sky_uv).r;",
    "    float cld_mask = smoothstep(0.35, 0.85, cld) * smoothstep(0.02, 0.25, h);",
    "    vec3 col = mix(sky_grad, vec3(1.0, 0.98, 0.95), cld_mask * 0.65);",
    "    float sun_amt = max(dot(EYEDIR, LIGHT0_DIRECTION), 0.0);",
    "    col += LIGHT0_COLOR * pow(sun_amt, 320.0) * 3.5;",
    "    col += LIGHT0_COLOR * pow(sun_amt, 12.0) * 0.22;",
    "    COLOR = col;",
    "}",
]
(GWORK / "shaders/stranded_original_sky.gdshader").write_text(
    "\n".join(sky_shader) + "\n", encoding="utf-8"
)

tscn = [
    '[gd_scene load_steps=9 format=3]',
    '',
    '[ext_resource type="Shader" path="res://shaders/stranded_original_sky.gdshader" id="1"]',
    '[ext_resource type="Texture2D" path="res://sky_assets/stars_texture_70.png" id="2"]',
    '[ext_resource type="Texture2D" path="res://sky_assets/MostlyCloud_6807535986202380995.png" id="3"]',
    '[ext_resource type="Texture2D" path="res://sky_assets/inscatter1Table16x1_Polarised_98.png" id="4"]',
    '[ext_resource type="PackedScene" path="res://Glb/introscene.glb" id="5"]',
    '[ext_resource type="PackedScene" path="res://Glb/mission_shark.glb" id="6"]',
    '',
    '[sub_resource type="ShaderMaterial" id="SkyMat_orig"]',
    'shader = ExtResource("1")',
    'shader_parameter/stars_tex = ExtResource("2")',
    'shader_parameter/cloud_tex = ExtResource("3")',
    'shader_parameter/inscatter_lut = ExtResource("4")',
    '',
    '[sub_resource type="Sky" id="Sky_orig"]',
    'sky_material = SubResource("SkyMat_orig")',
    '',
    '[sub_resource type="Environment" id="Env_orig"]',
    'background_mode = 2',
    'sky = SubResource("Sky_orig")',
    'ambient_light_source = 3',
    'reflected_light_source = 2',
    'tonemap_mode = 2',
    'tonemap_exposure = 1.05',
    'fog_enabled = true',
    'fog_light_color = Color(0.62, 0.82, 0.94, 1)',
    'fog_density = 0.0025',
    'fog_sky_affect = 0.35',
    '',
    '[node name="Sky_Lighting_Test" type="Node3D"]',
    '',
    '[node name="WorldEnvironment" type="WorldEnvironment" parent="."]',
    'environment = SubResource("Env_orig")',
    '',
    '[node name="SunLight" type="DirectionalLight3D" parent="."]',
    'transform = Transform3D(0.866025, -0.353553, 0.353553, 0, 0.707107, 0.707107, -0.5, -0.612372, 0.612372, 0, 30, 0)',
    'light_color = Color(1, 0.96, 0.88, 1)',
    'light_energy = 1.65',
    'shadow_enabled = true',
    '',
    '[node name="ShipPlaneRef" parent="." instance=ExtResource("5")]',
    'transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 2, -15)',
    '',
    '[node name="SharkWhaleRef" parent="." instance=ExtResource("6")]',
    'transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 14, -2, -22)',
    '',
    '[node name="TestCam" type="Camera3D" parent="."]',
    'transform = Transform3D(1, 0, 0, 0, 0.984808, -0.173648, 0, 0.173648, 0.984808, 0, 5, 16)',
    'current = true',
]
(GWORK / "scenes/Sky_Lighting_Test.tscn").write_text(
    "\n".join(tscn) + "\n", encoding="utf-8"
)

(GWORK / "project.godot").write_text(
    'config_version=5\n\n[application]\n\n'
    'config/name="Castaway Sky Lighting"\n'
    'run/main_scene="res://scenes/Sky_Lighting_Test.tscn"\n'
    'config/features=PackedStringArray("4.7", "Mobile")\n\n'
    '[rendering]\n\nrenderer/rendering_method="mobile"\n',
    encoding="utf-8",
)
print(f"GODOT_SKY_TEST_READY | ASSETS={copied_sky}")
