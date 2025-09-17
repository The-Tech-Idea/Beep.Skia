# Beep.Skia.UML Skia Controls Plan

## Overview
This plan outlines the creation of Skia-based UI controls for the Beep.Skia.UML project, which focuses on Unified Modeling Language (UML) diagram creation and editing. The controls will leverage Beep.Skia as the core rendering framework and Beep.Skia.Model for interfaces and data contracts.

## Project Purpose
Beep.Skia.UML provides UML diagramming tools, requiring controls for class diagrams, sequence diagrams, and other UML diagram types.

## Required Skia Controls

### 1. Class Diagram Controls
- **UMLClass**: For class representation
- **UMLInterface**: For interface representation
- **UMLAssociation**: For class relationships
- **UMLInheritance**: For inheritance relationships

### 2. Sequence Diagram Controls
- **UMLActor**: For sequence actors
- **UMLLifeline**: For object lifelines
- **UMLMessage**: For message arrows
- **UMLActivation**: For activation bars

### 3. Diagram Management Controls
- **UMLDiagram**: Main diagram canvas
- **UMLEditor**: Diagram editing interface
- **UMLPalette**: Tool palette for UML elements
- **UMLPropertyPanel**: Property editing panel

## Implementation Plan

### Phase 1: Core Infrastructure (1-2 weeks)
1. Set up project dependencies on Beep.Skia and Beep.Skia.Model
2. Create base UMLControl class inheriting from SkiaComponent
3. Implement UML interfaces using IConnectionPoint
4. Set up event handling for diagram updates

### Phase 2: Class Diagram Controls (2-3 weeks)
1. Implement UMLClass with compartments
2. Add UMLInterface with interface notation
3. Create UMLAssociation with multiplicity
4. Develop UMLInheritance with arrow styling

### Phase 3: Sequence Diagram Controls (2-3 weeks)
1. Build UMLActor with stick figure representation
2. Implement UMLLifeline with dashed lines
3. Create UMLMessage with different arrow types
4. Develop UMLActivation with timing bars

### Phase 4: Diagram Management (1-2 weeks)
1. Implement UMLDiagram with grid and rulers
2. Create UMLEditor with tool selection
3. Build UMLPalette with drag-and-drop
4. Add UMLPropertyPanel with property editing

### Phase 5: Integration and Testing (1 week)
1. Integrate controls with DrawingManager
2. Implement connection points for UML relationships
3. Add unit tests for each control
4. Performance optimization

## Dependencies
- Beep.Skia: Core rendering and component framework
- Beep.Skia.Model: Interfaces for connections and data contracts
- SkiaSharp: Graphics rendering
- System.Xml: For UML serialization

## Key Patterns
- Use absolute coordinates for all rendering (X, Y, X+Width, Y+Height)
- Implement Material Design 3.0 styling
- Support drag-and-drop for control positioning
- Enable connection tracking for UML relationships
- Follow PascalCase naming conventions

## Success Criteria
- All controls render correctly with absolute coordinates
- Standard UML notation compliance
- Proper integration with DrawingManager
- Comprehensive test coverage
- Clean, maintainable code following project conventions