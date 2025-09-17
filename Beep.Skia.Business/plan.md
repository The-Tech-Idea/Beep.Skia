# Beep.Skia.Business Skia Controls Plan

## Overview
This plan outlines the creation of Skia-based UI controls for the Beep.Skia.Business project, which focuses on business process modeling and workflow management. The controls will leverage Beep.Skia as the core rendering framework and Beep.Skia.Model for interfaces and data contracts.

## Project Purpose
Beep.Skia.Business provides business process modeling tools, requiring controls for workflow diagrams, process flows, and business rule visualization.

## Required Skia Controls

### 1. Process Flow Controls
- **ProcessNode**: For business process steps
- **DecisionNode**: For conditional logic
- **Connector**: For flow connections
- **StartEndNode**: For process boundaries

### 2. Business Rule Controls
- **RuleEngine**: For business rule visualization
- **ConditionBuilder**: For rule condition editing
- **ActionNode**: For rule actions
- **RuleFlow**: For rule execution flow

### 3. Workflow Controls
- **WorkflowDesigner**: Main design surface
- **TaskNode**: For workflow tasks
- **GatewayNode**: For parallel processing
- **EventNode**: For workflow events

## Implementation Plan

### Phase 1: Core Infrastructure (1-2 weeks)
1. Set up project dependencies on Beep.Skia and Beep.Skia.Model
2. Create base BusinessControl class inheriting from SkiaComponent
3. Implement workflow interfaces using IConnectionPoint
4. Set up event handling for process updates

### Phase 2: Basic Process Controls (2-3 weeks)
1. Implement ProcessNode with absolute coordinate rendering
2. Add DecisionNode with conditional styling
3. Create Connector with curved lines
4. Develop StartEndNode with distinct visual cues

### Phase 3: Advanced Workflow Controls (2-3 weeks)
1. Build WorkflowDesigner with drag-and-drop
2. Implement TaskNode with status indicators
3. Create GatewayNode for parallel flows
4. Develop EventNode with event types

### Phase 4: Business Rule Tools (1-2 weeks)
1. Implement RuleEngine control
2. Create ConditionBuilder with drag-and-drop
3. Build ActionNode for rule actions
4. Add RuleFlow visualization

### Phase 5: Integration and Testing (1 week)
1. Integrate controls with DrawingManager
2. Implement connection points for workflow flow
3. Add unit tests for each control
4. Performance optimization

## Dependencies
- Beep.Skia: Core rendering and component framework
- Beep.Skia.Model: Interfaces for connections and data contracts
- SkiaSharp: Graphics rendering
- System.ComponentModel: For data binding

## Key Patterns
- Use absolute coordinates for all rendering (X, Y, X+Width, Y+Height)
- Implement Material Design 3.0 styling
- Support drag-and-drop for control positioning
- Enable connection tracking for process flow
- Follow PascalCase naming conventions

## Success Criteria
- All controls render correctly with absolute coordinates
- Smooth workflow editing experience
- Proper integration with DrawingManager
- Comprehensive test coverage
- Clean, maintainable code following project conventions