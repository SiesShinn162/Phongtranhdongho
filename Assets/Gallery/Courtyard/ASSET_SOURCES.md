# Courtyard palette — CC0 provenance (Task 1)

All external textures below were approved by the user at 2K. They are licensed CC0 by their respective publishers: https://ambientcg.com/license and https://polyhaven.com/license. Downloaded on 2026-09-28. Files are not applied to the scene.

| Use | Source / download URL | Archive size and SHA-256 / individual verification | Maps kept |
| --- | --- | --- | --- |
| Grass | https://ambientcg.com/view?id=Grass005 ; https://ambientCG.com/get?file=Grass005_2K-JPG.zip | 39,523,019 bytes; `89183d8dceabc3c23a26978f426e1662143dd2f84f1dc3586a1592d101e5234b` | 2048×2048 color, NormalGL, roughness |
| Pond-edge strip/decal | https://ambientcg.com/view?id=PavingEdge002 ; https://ambientCG.com/get?file=PavingEdge002_2K-JPG.zip | 2,126,454 bytes; `e4fc744a1117ae33af42cd9b5b367ae6c9317db871ef774067b62b28f2663fde` | 2048×64 color, NormalGL, roughness, opacity; derived RGBA color/opacity |
| Gazebo roof tiles only | https://ambientcg.com/view?id=RoofingTiles012A ; https://ambientCG.com/get?file=RoofingTiles012A_2K-JPG.zip | 13,552,792 bytes; `71cf14afa940d20ea88cdc254072fe7c33152a6f2902fd16ff93117a6a1ca66e` | 2048×2048 color, NormalGL, roughness, opacity |
| Weathered gazebo wood | https://polyhaven.com/a/wood_planks_dirt ; https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/wood_planks_dirt/wood_planks_dirt_diff_2k.jpg ; https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/wood_planks_dirt/wood_planks_dirt_nor_gl_2k.jpg ; https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/wood_planks_dirt/wood_planks_dirt_rough_2k.jpg | 3,744,786 / 2,210,797 / 2,180,731 bytes; publisher MD5 `d462a7bb2ad387b9632203137dc172d7` / `f89b1f0b3e24b3864f1e892fcdf5968e` / `5ba21ed80a87a482516f590901f9308d` | 2048×2048 diffuse, normal OpenGL, roughness |
| Pond walls / stepping stones | https://ambientcg.com/view?id=Rock022 ; https://ambientCG.com/get?file=Rock022_2K-JPG.zip | 26,249,830 bytes; archive SHA-256 `b14568e5ccb4f6aae646df2e3afcc35768e818a78c246e93e0374230147bf2a5`; ZIP integrity checked | 2048×2048 Color JPG (4,356,526 bytes), NormalGL JPG (8,182,680 bytes), Roughness JPG (2,314,027 bytes); derived metallic/smoothness RGBA PNG |

Derived `*MetallicSmoothness.png` files use RGB=0 (nonmetallic), alpha=255−roughness, for URP Lit. Base colors use sRGB; normal maps are imported as NormalMap with sRGB disabled; packed masks are imported linear. Original roughness and opacity are retained for traceability. Materials use neutral tint and default UV scale pending actual geometry dimensions. `PavingEdge002` is a narrow decal strip, not a seamless stone facing for all four pond walls.

**Roof UV gate:** Measured in live Unity on `PhongTrienLam_Gallery/MaiNgoi/Roof_Face_Front`: 2,283,849 vertices; UV0 count 2,283,849; both minimum and maximum `(0,0)`. Ordinary BaseMap/Normal cannot tile this mesh. The user approved trying world-space triplanar projection. `Shaders/RoofTriplanar.shader` and `Materials/MainRoof_Triplanar_Preview.mat` form a **separate trial** with earth-red tint, slope-friendly world-space axes, and projected normal. Rendered isolated sloped test cube: `C:\Users\sies\AppData\Local\Temp\opencode\task1_roof_triplanar_isolated.png`. This test does not prove the appearance on all four original roof faces/trims; `Assets/Gallery/Materials/Roof_Brick.mat` and the main roof geometry remain unchanged.

**Water approval:** The user accepted the visual direction and requested more realism. `Textures/PondWater_Normal_Seamless.png` is a NEW 512×512 tangent-space normal, not the previous color mock-up: sums of integer-frequency periodic height waves with analytic x/y derivatives and normalized z; repeats at tile borders. Imported as NormalMap, linear (sRGB off), Repeat. `Shaders/PondWater.shader` uses two slowly scrolling normal samples, Fresnel, weak sun glint and opaque green color under alpha blending. `Materials/Pond_Water.mat` is ready for later placement; none is placed now. Isolated preview: `C:\Users\sies\AppData\Local\Temp\opencode\task1_pond_shader_isolated.png`. It uses a temporary cube only and does not validate actual depth, XR device stereo, water body geometry or pond gameplay.

Both new shader assets reported `isSupported=True`, zero shader messages in the current Editor and rendered to temporary RenderTextures successfully. They use URP shader-library matrices/lighting, instancing and Unity stereo input/output setup for XR single-pass compatibility; an actual headset stereo pass has not yet been measured. PavingEdge002 remains a narrow edging strip; use the approved Rock022 rough-stone material for bulk stone facing rather than stretching the edge decal.

## Task 3.5: Concourse & Pond Surrounding Wall PBR Textures (2026-09-28)

All external textures below were downloaded at 2K resolution from Poly Haven under the Creative Commons CC0 license: https://polyhaven.com/license.

| Use | Source URL | Files | Maps kept |
| --- | --- | --- | --- |
| Concourse & Pond Walkways | https://polyhaven.com/a/floor_bricks_02 | `floor_bricks_02_diff_2k.jpg`, `nor_gl_2k.jpg`, `rough_2k.jpg` | 2048×2048 Diffuse, Normal OpenGL, Roughness; derived `MetallicSmoothness.png` |
| Pond Surrounding Wall & Pillars | https://polyhaven.com/a/plaster_stone_wall_01 | `plaster_stone_wall_01_diff_2k.jpg`, `nor_gl_2k.jpg`, `rough_2k.jpg` | 2048×2048 Diffuse, Normal OpenGL, Roughness; derived `MetallicSmoothness.png` |

- `Materials/Courtyard_ConcourseBrick_PBR.mat`: URP Lit material using FloorBricks02 textures, Tiling calibrated to (5.0, 72.0) matching real brick paver scale (~0.25m x 0.12m) along the 36m walkway.
- `Materials/Pond_SurroundingWall_PBR.mat`: URP Lit material using PlasterStoneWall01 textures, Tiling calibrated to (24.0, 1.0) along the 36m wall run.
- `Materials/Pond_PillarStone_PBR.mat`: Tiling (1.0, 2.0) for vertical pillar posts.
- `Materials/Pond_ShortWall_PBR.mat`: Tiling (2.5, 1.0) for North/South wall segments.

## Task 4.5: Upgrade & Diversify PBR Textures (2026-09-28)

All external textures below were approved by the user at 2K resolution under the Creative Commons CC0 license (Poly Haven & ambientCG):

| Use / Component | Source | Asset ID / URL | Maps kept |
| --- | --- | --- | --- |
| **Roof Tiles (Main & Porch)** | ambientCG | `RoofingTiles011A` (https://ambientcg.com/view?id=RoofingTiles011A) | 2048×2048 Color JPG, NormalGL JPG, Roughness JPG; derived `RoofingTiles011A_2K-JPG_MetallicSmoothness.png` |
| **Main Gallery Exterior Walls** | Poly Haven | `worn_mossy_plasterwall` (https://polyhaven.com/a/worn_mossy_plasterwall) | 2048×2048 Diffuse JPG, Normal OpenGL JPG, Roughness JPG; derived `worn_mossy_plasterwall_MetallicSmoothness.png` |
| **Porch Wood Ceiling** | ambientCG | `Wood026` (https://ambientcg.com/view?id=Wood026) | 2048×2048 Color JPG, NormalGL JPG, Roughness JPG; derived `Wood026_2K-JPG_MetallicSmoothness.png` |
| **Concourse & Walkways** | Poly Haven | `cobblestone_large_01` (https://polyhaven.com/a/cobblestone_large_01) | 2048×2048 Diffuse JPG, Normal OpenGL JPG, Roughness JPG; derived `cobblestone_large_01_MetallicSmoothness.png` |
| **Pond Surrounding Walls** | ambientCG | `Marble012` (https://ambientcg.com/view?id=Marble012) | 2048×2048 Color JPG, NormalGL JPG, Roughness JPG; derived `Marble012_2K-JPG_MetallicSmoothness.png` |



