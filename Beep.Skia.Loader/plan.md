# Beep.Skia.Loader Skia Controls Plan

## Overview
This plan outlines the creation of Skia-based UI controls for the Beep.Skia.Loader project, which focuses on data loading and import functionality. The controls will leverage Beep.Skia as the core rendering framework and Beep.Skia.Model for interfaces and data contracts.

## Project Purpose
Beep.Skia.Loader provides data loading and import tools, requiring controls for file selection, data preview, and loading progress visualization.

## Required Skia Controls

### 1. File Loading Controls
- **FileSelector**: For file selection and browsing
- **FilePreview**: For data preview before loading
- **LoaderProgress**: For loading progress visualization
- **FormatDetector**: For automatic format detection

### 2. Data Import Controls
- **ImportWizard**: Step-by-step import process
- **ColumnMapper**: For field mapping
- **DataValidator**: For import validation
- **ImportMonitor**: For import status tracking

### 3. Loading Management Controls
- **LoaderManager**: Main loading interface
- **BatchLoader**: For batch processing
- **ScheduleLoader**: For scheduled loading
- **ErrorReporter**: For loading error reporting

## Implementation Plan

### Phase 1: Core Infrastructure (1-2 weeks)
1. Set up project dependencies on Beep.Skia and Beep.Skia.Model
2. Create base LoaderControl class inheriting from SkiaComponent
3. Implement loading interfaces using IConnectionPoint
4. Set up event handling for loading updates

### Phase 2: Basic File Controls (2-3 weeks)
1. Implement FileSelector with file browsing
2. Add FilePreview with data sampling
3. Create LoaderProgress with progress bars
4. Develop FormatDetector with format icons

### Phase 3: Advanced Import Controls (2-3 weeks)
1. Build ImportWizard with step navigation
2. Implement ColumnMapper with drag-and-drop
3. Create DataValidator with validation rules
4. Develop ImportMonitor with real-time status

### Phase 4: Management Tools (1-2 weeks)
1. Implement LoaderManager control
2. Create BatchLoader for multiple files
3. Build ScheduleLoader with calendar integration
4. Add ErrorReporter with detailed error logs

### Phase 5: Integration and Testing (1 week)
1. Integrate controls with DrawingManager
2. Implement connection points for data flow
3. Add unit tests for each control
4. Performance optimization

## Dependencies
- Beep.Skia: Core rendering and component framework
- Beep.Skia.Model: Interfaces for connections and data contracts
- SkiaSharp: Graphics rendering
- System.IO: For file operations

## Key Patterns
- Use absolute coordinates for all rendering (X, Y, X+Width, Y+Height)
- Implement Material Design 3.0 styling
- Support drag-and-drop for control positioning
- Enable connection tracking for data flow
- Follow PascalCase naming conventions

## Success Criteria
- All controls render correctly with absolute coordinates
- Smooth file loading and import experience
- Proper integration with DrawingManager
- Comprehensive test coverage
- Clean, maintainable code following project conventions