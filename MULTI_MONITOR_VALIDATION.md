# Multi-monitor and high-DPI capture validation

This document records the implementation and acceptance matrix for GitHub issue #4. It distinguishes deterministic automated geometry coverage from physical display validation; passing geometry tests does not claim that every Windows DPI configuration has been exercised on hardware.

## Implementation changes

- The application manifest now declares Per-Monitor V2 DPI awareness (dpiAwareness=PerMonitorV2) with the legacy true/pm fallback. The project continues to target .NET 8 WPF with ApplicationHighDpiMode=PerMonitorV2.
- Region selection creates one overlay HWND per physical monitor instead of stretching one WPF window across the virtual desktop. Each HWND is positioned with native physical-pixel monitor bounds; WPF renders each overlay at that monitor's own DPI.
- The cursor is read with GetCursorPos, so the drag start/end coordinates remain physical desktop pixels even when the drag crosses monitors with different scale factors.
- Each monitor overlay converts the same global physical rectangle into its own local WPF DIPs for rendering. The selection returned to capture remains in physical coordinates.
- Full-desktop capture derives the virtual bounds from all display rectangles, including negative origins and displays above/below the primary monitor. Region capture rounds outward to cover selected pixels and clips to the virtual desktop before CopyFromScreen.
- Screenshot metadata reports the real virtual-desktop origin or the actual clipped region, rather than assuming (0, 0).
- The recording HUD, screenshot toast, and pinned screenshot windows are moved to the monitor under the cursor. Their native placement uses the destination HWND's effective DPI and the monitor's physical work area.
- Window placement geometry is separated into a pure helper and covered by tests for negative origins, top/right/bottom alignment, offsets, and undersized work areas.

## Before / after evidence

| Area | Before | After |
|---|---|---|
| Region selector | One WPF window used physical virtual-screen width/height as WPF DIPs | One native-positioned WPF overlay per monitor; each is laid out at its own DPI |
| Drag coordinates | Local WPF coordinates plus Left/Top, mixing DIPs and physical pixels | Global cursor coordinates from GetCursorPos; selected rectangle is physical pixels |
| Mixed-DPI drawing | One window could not represent different monitor scales simultaneously | Every monitor draws the same global selection through its own PointFromScreen transform |
| Virtual desktop | Capture origin metadata was always (0, 0) for full-screen images | Actual virtual bounds preserve negative/positive monitor offsets |
| Region dimensions | Math.Round could drop edge pixels and region capture was not clipped | Floor left/top, ceil right/bottom, clip to the virtual desktop, report actual output dimensions |
| Floating UI | Used SystemParameters.WorkArea from the primary display | Uses cursor monitor work area and destination-window DPI |

## Automated verification

Run from the solution directory:

~~~powershell
dotnet test .\ScreenTools.sln -c Release --no-restore -m:1 -nodeReuse:false
dotnet build .\ScreenTools.sln -c Release --no-restore -m:1 -nodeReuse:false
~~~

The geometry suite covers:

- One display and two-display virtual bounds.
- Mixed display resolutions.
- Displays left/right of the primary display (negative X origin).
- Displays above/below the primary display (negative Y origin).
- Fractional selection edges and outward pixel rounding.
- Clipping at virtual-desktop boundaries and rejecting non-intersecting regions.
- Center, top-right, and bottom-right floating-window placement on positive and negative monitor origins.
- Oversized windows clamped to the destination work area.
- Full-screen screenshot dimensions checked against the union of all physical monitor bounds; fractional region edges checked against actual encoded image dimensions.
- Every connected monitor is physically probed at its center and four corners; adjacent monitor seams are captured when the current topology has a contiguous edge.
- PNG, JPG, and BMP full-desktop outputs are decoded and checked against physical desktop dimensions.
- Recording JPEG frames are decoded and checked against the selected quality profile's maximum width and preserved desktop aspect ratio.

The full suite currently contains 97 tests. Physical integration coverage probes the center and four corners of every connected monitor, captures contiguous monitor seams when present, validates PNG/JPG/BMP full-desktop dimensions, and decodes recording JPEG frames for all quality profiles. The tests do not simulate WPF's real WM_DPICHANGED message flow or replace physical mixed-DPI testing.

## Display setup recorded during this change

Collected with tools/Get-DisplayCaptureDiagnostics.ps1 on the development machine:

| Item | Observed value |
|---|---|
| OS | Windows 11 Home, build 26300, x64 |
| Displays | 2 |
| Display 1 | 1920 × 1080, origin (0, 0), primary, 96 DPI / 100% |
| Display 2 | 1920 × 1080, origin (1920, 0), secondary, 96 DPI / 100% |
| Virtual desktop | 3840 × 1080, origin (0, 0) |
| Layout | Side-by-side, same resolution, same 100% scaling |

This is a discovered setup report, not a claim that a manual end-to-end capture run was performed for every row. The development machine does not currently provide 125% or 150% scaling, vertical monitor arrangements, or mixed-DPI displays for physical acceptance.

Collect the setup on any test machine:

~~~powershell
.\tools\Get-DisplayCaptureDiagnostics.ps1 -OutputPath .\BenchmarkResults\display-diagnostics.json
~~~

The JSON report records Windows version/build, process architecture, monitor count, each monitor's physical bounds and work area, effective DPI/scale, and virtual desktop bounds.

## Manual acceptance matrix

Run these scenarios on actual Windows displays. Record the diagnostic JSON, test date, application build, screenshot/recording output dimensions, and a screenshot of the selector/HUD where possible.

| ID | Display topology | DPI configuration | Scenario | Pass criteria |
|---|---|---|---|---|
| D01 | One 1920×1080 display | 100% | Full screenshot and region screenshot | Output dimensions match physical desktop/selected pixel rectangle |
| D02 | Two displays side-by-side | 100% / 100% | Drag selection wholly on each display and across the seam | Overlay covers both displays; no offset or scale jump; selected output matches physical rectangle |
| D03 | Two displays, different resolutions | 100% / 100% | Full desktop and cross-display region | Virtual bounds include both monitor edges; output dimensions match virtual bounds |
| D04 | Secondary display left of primary | 100% | Region capture on negative-X display | Negative origin is preserved; selected pixels match the visible selection |
| D05 | Secondary display above primary | 100% | Region capture on negative-Y display | Negative Y origin is preserved; no clipping or coordinate shift |
| D06 | Secondary display below primary | 100% | Region capture on lower display | Selection and magnifier coordinates match the physical cursor |
| D07 | One display | 125% | Region selection, full screenshot, HUD | No 1.25× coordinate drift; HUD remains inside work area |
| D08 | One display | 150% | Region selection, full screenshot, HUD | No 1.5× coordinate drift; output dimensions remain physical pixels |
| D09 | Two displays, same resolution | 100% / 150% | Drag from 100% display to 150% display | Selection rectangle remains continuous and output bounds are exact |
| D10 | Two displays, mixed resolutions | 125% / 150% | Full screenshot, cross-monitor region, HUD and toast | No clipping, misplaced UI, or incorrect output dimensions |
| D11 | Any multi-monitor layout | Mixed | Recording and exported dimensions | Full-desktop recording matches the virtual desktop after the configured quality-profile resize |
| D12 | Any multi-monitor layout | Mixed | Disconnect/reconnect or rearrange a display, then capture again | Fresh monitor bounds are used; no stale monitor coordinates remain |

### Recording-region scope

The current application does not expose a selectable recording-region feature. Recording and instant replay capture the full virtual desktop; the selected screenshot region is a screenshot-only option. D11 therefore validates full-desktop recording dimensions. A user-selectable video recording region is a separate feature and must not be reported as tested or implemented by this issue.

## Acceptance status

- [x] Per-Monitor V2 process awareness declared in the manifest and project configuration.
- [x] Per-monitor selection overlays use native physical monitor bounds.
- [x] Cross-monitor drag coordinates use physical desktop coordinates.
- [x] Full virtual-desktop bounds support negative monitor origins.
- [x] Region output uses outward pixel rounding and virtual-desktop clipping.
- [x] Full-screen and region screenshot metadata report actual pixel bounds.
- [x] HUD and screenshot floating windows use the cursor monitor's work area and DPI.
- [x] Automated geometry and placement tests pass.
- [x] Release solution build passes.
- [ ] Physical 125% and 150% single-monitor runs.
- [ ] Physical mixed-DPI / mixed-resolution multi-monitor runs.
- [ ] Physical above/below and negative-origin selector runs.
- [ ] Manual end-to-end screenshots of selector, HUD, and capture outputs on those configurations.
- [ ] Windows 10 physical display validation.

Do not mark the physical rows complete until the diagnostic report and actual output files have been inspected on the corresponding hardware.
