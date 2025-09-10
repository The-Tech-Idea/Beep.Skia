# Beep.Skia Optimization Plan

## Overview
This document outlines the plan to fix and optimize the Beep.Skia and Beep.Skia.Model projects based on analysis of the codebase.

## Issues Identified

### 1. Project Configuration Issues
- **Duplicate entries** in .csproj files (ImplicitUsings, LangVersion)
- **Target framework** includes net9.0 (not yet released)
- **Missing package icon** references
- **Outdated package versions** that need updating

### 2. Code Quality Issues
- **Typo**: "IsConnectd" should be "IsConnected" in interfaces and implementations
- **Naming conventions**: Properties in SkiaMenuItem should follow PascalCase
- **Namespace inconsistency**: SkiaMenuItem uses wrong namespace
- **Bug in ConnectionPoint.Disconnect()**: Calls method on potentially null object

### 3. Architecture Issues
- **Tight coupling** between components
- **Inconsistent error handling**
- **Missing null checks** in several places
- **Event handling inconsistencies**

### 4. Performance Issues
- **Inefficient drawing operations** in TableDrawer
- **Memory leaks** potential in animation timers
- **Redundant calculations** in intersection detection

## Optimization Plan

### Phase 1: Critical Fixes
1. Fix .csproj configuration issues
2. Correct typos and naming conventions
3. Fix bugs in ConnectionPoint and TableDrawer
4. Update package dependencies

### Phase 2: Architecture Improvements
1. Implement proper error handling
2. Add null checks and validation
3. Improve event handling consistency
4. Refactor tight coupling issues

### Phase 3: Performance Optimizations
1. Optimize drawing operations
2. Improve memory management
3. Cache frequently used calculations
4. Implement object pooling where appropriate

### Phase 4: Code Quality
1. Add comprehensive documentation
2. Implement unit tests
3. Add code analysis rules
4. Improve code organization

## Timeline
- Phase 1: 2-3 days
- Phase 2: 3-4 days
- Phase 3: 2-3 days
- Phase 4: 2-3 days

## Success Criteria
- All compilation errors resolved
- No runtime exceptions with proper input
- Improved performance benchmarks
- Clean code analysis results
- Comprehensive test coverage

---

## Location Normalization Plan (Absolute Coordinate Migration)

Goal: Ensure every visual component renders using its absolute X,Y (no implicit local origin (0,0) assumptions and no unintended parent canvas translation coupling) so drag/drop positioning is consistent.

### Strategy
1. Audit each component for patterns:
	- new SKRect(0, 0, Width, Height) or variants (0-based partial height)
	- canvas.DrawRect / DrawRoundRect / paths using raw 0 offsets
	- Hit tests using new SKRect(0,0,...)
	- Internal offset drawing (e.g., item loops) lacking X,Y prefix
	- Use of canvas.Translate in component DrawContent (should be removed)
2. Replace with absolute rectangles (X, Y, X+Width, Y+Height) and propagate X/Y into child element positioning.
3. Adjust hit-tests similarly (retain local math only when explicitly converting point to local).
4. Leave framework-level pan/zoom (RenderingHelper) intact; it applies globally once.
5. Avoid changing obsolete text API calls in this pass (defer to separate modernization task to limit regression risk).

### Component Status Table
Legend: DONE (converted), PENDING (needs conversion), N/A (already absolute / not visual), PARTIAL (in progress)

| Component | Status | Notes |
|-----------|--------|-------|
| Checkbox | DONE | Baseline reference pattern |
| Button | DONE | Earlier pass |
| ButtonGroup | DONE | Removed per-button translate |
| Card | DONE | Rect made absolute |
| SvgImage | DONE | Removed Translate / pan zoom path |
| Menu | DONE | Rewritten absolute + Invalidate shim |
| MenuItem | DONE | Uses ParentMenu.Invalidate() shim present |
| MenuButton | PENDING | Audit required |
| MenuBar | PENDING | Likely horizontal item rects at origin |
| CascadingMenu / ContextMenu | PENDING | Derive from Menu patterns |
| ComponentPalette / Palette | DONE | Already absolute (validated) |
| List | DONE | Item bounds absolute |
| Dropdown | DONE | Field + list absolute |
| SegmentedButtons | DONE | Segment rects absolute |
| SplitButton | DONE | Primary & dropdown rects absolute |
| Slider | DONE | Hover rect absolute; track still local (acceptable) |
| Switch | DONE | Hit tests absolute |
| TextBox | DONE | All helper drawings absolute |
| TextArea | DONE | Background rect absolute |
| Search | DONE | Bar & view absolute + hit tests |
| FabMenu | DONE | Overlay absolute; item loops local OK |
| Notification | DONE | Round rect absolute |
| RadioGroup | DONE | Background rect absolute |
| CheckBoxGroup | DONE | Background rect absolute |
| Panel | DONE | Already absolute |
| ProgressBar | PARTIAL | Linear trackRect 0-based; fix to absolute |
| Spinner | PENDING | Verify arcs use X,Y center |
| Tabs | PENDING | Tab strip likely 0-based |
| NavigationBar | PENDING | Verify item rect loops |
| NavigationDrawer | PENDING | Drawer surface + items |
| NavigationItem | PENDING | Sub-item offsets |
| StatusBar / StatusBarItem | PENDING | Check 0-origin draws |
| ToolBar / ToolBarItem | PENDING | Similar to StatusBar |
| ToggleButton | PENDING | Likely button-like; confirm |
| FloatingActionButton | PENDING | Circle path origin |
| ColorPicker | PENDING | Gradient/wheel origin |
| DatePicker / TimePicker | PENDING | Calendar/time grid origins |
| DataGrid / DataGridColumn | PENDING | Cell rect local origins |

### Execution Order
1. Simple primitives (ProgressBar final fix, ToggleButton, FloatingActionButton, Spinner).
2. Navigation set (Tabs, Navigation*, StatusBar, ToolBar).
3. Pickers (DatePicker, TimePicker, ColorPicker).
4. DataGrid family.

### Acceptance Criteria (per component)
- No unintended (0,0)-anchored draw primitives for component bounds.
- Hit tests reflect absolute coordinates or convert point to local intentionally.
- No introduction of new translation side-effects.
- Visual parity maintained aside from correct placement.

### Deferred Work
- Text API modernization (SKFont, new DrawText overloads).
- Color token consolidation.

