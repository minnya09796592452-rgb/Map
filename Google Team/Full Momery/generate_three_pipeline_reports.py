from datetime import datetime

OCEAN_FILE = "OCEAN_QUERY_AND_PIPELINE.txt"
CPU_GPU_FILE = "CPU_GPU_DISPLACEMENT_PIPELINE.txt"
COMBINED_FILE = "COMBINED_OCEAN_CPU_GPU_PIPELINE.txt"

def build_ocean_report(ts: str) -> str:
    return f"""========================================================================
OCEAN QUERY & SAMPLING PIPELINE REPORT (GODOT 4.7.2 REFERENCE)
Generated : {ts}
Status    : VERIFIED (Core Query/Sampling/Scaling) | PARTIAL (Tide.Update)
========================================================================

[1. CALL CHAIN & ENTRY POINTS]
- Ceto.Ocean.QueryWaves (RID 11859 @ 0x06f010 / RID 11860 @ 0x06f09c) [VERIFIED]
  -> Clears query state, sets posX/posZ, calls WaveSpectrum.QueryWaves (0x06003094 / RID 12436 @ 0x07ecd8)
  -> Optionally calls OverlayManager.QueryWaves (0x06002ed4 / RID 11988 @ 0x071d58)
  -> Returns WaveQueryResult.height (0x04003b8e) + Ocean.level (0x040019cc)
  -> RID 11860 also outputs isClipped (0x04003b95)
- WaveSpectrum.QueryWaves (RID 12436 @ 0x07ecd8) [VERIFIED]
  -> Calls get_DisplacementBuffer (0x06003085)
  -> Calls virtual IDisplacementBuffer.QueryWaves (0x06002f8b / RID 12171) passing m_queryScaling (0x04001c21)
- Ceto.QueryDisplacements.QueryWaves (RID 12173 @ 0x0768a8, 951B, 321 insns, gaps=0, overlaps=0) [VERIFIED]
  -> Calls SampleHeightOnly (0x06002f8e / RID 12174 @ 0x076c6c, 345B, 152 insns) or Sample (0x06002f8f / RID 12175 @ 0x076dd4, 581B, 240 insns)

[2. SCALING & COORDINATE TRANSFORM (UpdateQueryScaling RID 12430 @ 0x07e2c8)]
- m_choppyness = m_conditions[0].get_Choppyness() * Clamp(this.choppyness, 0, 1.2)
- m_gridSizes = m_conditions[0].get_GridSizes()
- invGridSizes (0x04001a53): For axis c in {{x, y, z, w}}, invGridSizes.c = 1.0f / (get_GridSizes().c * this.gridScale) [gridScale once]
- scaleY (0x04001a54): this.gridScale
- choppyness (0x04001a55): get_Choppyness() * this.gridScale [gridScale once]
- offset (0x04001a56): m_ocean.get_PositionOffset() (0x06002e35)
- numGrids (0x04001a57): m_conditions[0].get_Key().get_NumGrids() (0x06002ff4 -> 0x0600300f)
- Local query coordinates: loc0 = posX + offset.x ; loc1 = posZ + offset.z

[3. SAMPLE() VS SAMPLEHEIGHTONLY() & ITERATION PROOF]
- Grids 0..3 unrolled, gated by sampleSpectrum[i] != 0 and numGrids > i
- UV transform per grid i: u = x * invGridSizes.<axis>, v = z * invGridSizes.<axis>
- Sample(): computes X, Y, Z; scales X/Z by choppyness.<axis> and Y by scaleY; combines 4 grids (sumX, sumZ)
- SampleHeightOnly(): computes Y only; skips horizontal iterative correction; sets dispX=0, dispZ=0, iter=0, err=0; clamps height to [-40, 40]
- Iteration pass count in QueryDisplacements.QueryWaves (RID 12173):
  * Counter starts at 0 before loop header (IL_0472)
  * Incremented by +1 (IL_0718..IL_0722) per pass BEFORE checking loc12 <= MAX_ITERATIONS (IL_0747..IL_0754)
  * Exits early if errSq <= errSqThr (IL_0745)
  * Maximum passes = MAX_ITERATIONS + 1 (e.g., 1 pass when MAX_ITERATIONS == 0; up to 21+ passes per documented cap)

[4. INTERPOLATION (InterpolatedArray2f.Get / Index)]
- Half-pixel offset: u * SX - 0.5f, periodic modulo wrap ((ix % SX) + SX) % SX (wrap=true), 4-corner bilinear blend across channels 0..2

[5. TIDE & REMAINING GAPS]
- Tide.Update (RVA=0x000146c6, FileOff=0x000128c6) writes Ceto.Ocean.level (0x040019cc) [PARTIALLY VERIFIED - full IL dump pending]
- UNVERIFIED: Unity scene/runtime overrides, independent non-dnfile raw metadata table cross-check
"""

def build_cpu_gpu_report(ts: str) -> str:
    return f"""========================================================================
CPU & GPU DISPLACEMENT PIPELINE REPORT (GODOT 4.7.2 REFERENCE)
Generated : {ts}
Status    : VERIFIED (Buffer Architecture, FFT/Blit, Readback, Grid 3)
            UNVERIFIED (Compiled HLSL/ComputeShader bytecode, Scene values)
========================================================================

[1. BUFFER SELECTION & DISPATCH]
- WaveSpectrum.get_DisplacementBuffer (0x06003085 | RID 12421) reads m_displacementBuffer (0x04001c1c)
- GetFourierSize (0x0600309D) checks shaderLevel < 30 / !supportsRT / disableFourierGPU for GPU/CPU path capability

[2. CPU DISPLACEMENT PIPELINE (VERIFIED)]
- Allocation: DisplacementBufferCPU..ctor (0x06002F74 | RVA 0x783C4) allocates m_displacements (0x04001B58) as List<InterpolatedArray2f[]> of length 2:
  * Slot [0]: Write Buffer
  * Slot [1]: Read Buffer -> returned by GetReadDisplacements() (0x06002F77 | RID 12151)
  * Each slot holds 4 InterpolatedArray2f(size, size, channels=3, wrap=true) grids
- Storage layout: Flat 1D float[] m_data (size * size * 3), interleaved index (x + y * SX) * 3 + ch
  * ch0 = Displacement X, ch1 = Height Y, ch2 = Displacement Z
- Execution & Swap Order (DisplacementBufferCPU.Run | 0x06002F78 | RID 12152):
  1. SwapDisplacements() (0x06002F7B | RID 12155) FIRST: swaps m_displacements[0] <-> m_displacements[1]
  2. Base WaveSpectrumBufferCPU.Run() (0x06002FBD | RID 12221):
     - DisplacementBufferCPU.Initilize() (0x06002F75 | RID 12149) clears Write Buffer m_displacements[0]
     - InitSpectrumDisplacementsTask.Run() (0x06003060) -> InitilizeGrids1..4() (0x06003061..64)
     - FourierTask.Run() (0x06003048) -> PerformSingleFourier() (0x0600304A) -> FourierCPU.PeformFFT_SinglePacked / DoublePacked (0x0600302E / 0x0600302F)
     - FourierTask.ProcessData() (0x0600304B) writes spatial results into m_displacements[0]:
       * m_index == 0 -> Height (Y) into Channel 1 of Grids 0..3
       * m_index == 1 -> Horizontal (X, Z) into Channels 0 & 2 of Grids 0..1
       * m_index == 2 -> Horizontal (X, Z) into Channels 0 & 2 of Grids 2..3
- Query & Copy:
  * DisplacementBufferCPU.QueryWaves (0x06002F7D | RID 12157) reads GetReadDisplacements() (Slot [1])
  * CopyAndCreateDisplacements() (0x06002F79) & CopyDisplacements() (0x06002F7A) support async query cloning

[3. GPU DISPLACEMENT PIPELINE & GRID 3 AUDIT (VERIFIED)]
- Allocation: WaveSpectrum.CreateBuffers (0x06003095 | RID 12437) instantiates DisplacementBufferGPU (0x06002F7E | RID 12158) + m_readBuffer = ComputeBuffer(size * size, stride=12)
- Spectrum & FFT: WaveSpectrumBufferGPU.Run (0x06002FCC | RID 12236) -> Initilize (0x06002FC5 | RID 12229) binds Ceto_Spectrum01/23, Ceto_WTable, Ceto_InverseGridSizes, Ceto_GridSizes, Ceto_Offset, Ceto_Time -> FourierGPU.PeformFFT (0x06003037..3A)
- RenderTargets: WaveSpectrum.GenerateDisplacement (0x06003090 | RVA 0x7FA88) blits 4 ARGBHalf RenderTextures (m_displacementMaps[0..3]) and sets global textures Ceto_DisplacementMap0..3
- Readback: WaveSpectrum.ReadFromGPU(numGrids) (0x06003091 | RVA 0x7FD94):
  * Grids 0, 1, 2: CBUtility.ReadFromRenderTexture (0x060030F7) dispatches 'readNames2D' -> ComputeBuffer.GetData(Data)
  * Grid 3: Omitted from CPU readback (bandwidth optimization); remains active for GPU surface shaders
- Channel mapping: R -> ch0 (X), G -> ch1 (Height Y), B -> ch2 (Z)

[4. UNVERIFIED GPU/SHADER ITEMS]
- Compiled Shader & ComputeShader bytecode (initDisplacementSdr, fourierSdr, displacementCopySdr, readSdr)
- PhillipsSpectrum / UnifiedSpectrum internal equations
- Unity scene serialized inspector overrides (fourierSize, gridScale, windSpeed, disableReadBack)
"""

def main() -> None:
    ts = datetime.now().isoformat(timespec="seconds")
    ocean_txt = build_ocean_report(ts)
    cpu_gpu_txt = build_cpu_gpu_report(ts)
    combined_txt = (
        ocean_txt.rstrip()
        + "\n\n"
        + "#" * 72
        + "\n\n"
        + cpu_gpu_txt.lstrip()
    )

    with open(OCEAN_FILE, "w", encoding="utf-8") as f:
        f.write(ocean_txt)
    with open(CPU_GPU_FILE, "w", encoding="utf-8") as f:
        f.write(cpu_gpu_txt)
    with open(COMBINED_FILE, "w", encoding="utf-8") as f:
        f.write(combined_txt)

    print("Generated:")
    print(" -", OCEAN_FILE)
    print(" -", CPU_GPU_FILE)
    print(" -", COMBINED_FILE)

if __name__ == "__main__":
    main()
