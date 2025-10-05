# ETL Professional Features Documentation

## Overview
The Beep.Skia ETL framework now includes professional-grade features that bring it to parity with enterprise ETL tools like SSIS, Informatica, and Talend. This document details the new components and their usage patterns.

## New Components

### 1. ETLLookup - Reference Data Enrichment

**Purpose**: Enrich incoming data by joining with reference tables or lookup sources.

**Visual Design**: Double-cylinder shape (main data + reference data)

**Ports**:
- Input 0 (Left, Top): Main data stream
- Input 1 (Left, Bottom): Reference data stream
- Output (Right): Enriched data

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| CacheMode | Enum | `None` (no cache), `Partial` (cache on demand), `Full` (preload all) |
| MatchColumns | CSV String | Columns to match between main and reference (e.g., "CustomerID,ProductID") |
| ReturnColumns | CSV String | Columns to return from reference data (e.g., "CustomerName,ProductName,Price") |
| FailOnNoMatch | Boolean | If true, rows without matches are sent to error output |
| OutputSchema | JSON | Schema of enriched output (ColumnDefinition array) |

**Use Cases**:
- Add customer names to transaction records
- Enrich orders with product details
- Lookup geographic data for addresses
- Currency conversion lookups

**Example Configuration**:
```json
{
  "CacheMode": "Full",
  "MatchColumns": "CustomerID",
  "ReturnColumns": "CustomerName,CustomerSegment,Country",
  "FailOnNoMatch": false
}
```

---

### 2. ETLConditionalSplit - Multi-Path Routing

**Purpose**: Route rows to different outputs based on conditional expressions.

**Visual Design**: Diamond shape with multiple output branches

**Ports**:
- Input (Top): Single input stream
- Outputs (Right/Bottom): Dynamic count based on conditions + optional default output

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| Conditions | JSON Array | Array of `SplitCondition` objects (Name, Expression, Order) |
| HasDefaultOutput | Boolean | If true, adds an output port for unmatched rows |

**SplitCondition Model**:
```csharp
public class SplitCondition
{
    public string Name { get; set; }           // "High Value Orders"
    public string Expression { get; set; }     // "Amount > 10000"
    public int Order { get; set; }             // Evaluation priority (lower = first)
    public string Description { get; set; }    // Optional documentation
}
```

**Use Cases**:
- Separate high-value vs. low-value transactions
- Route records by region or category
- Split good records from suspect records
- Create train/test datasets (e.g., 80%/20% split)

**Example Configuration**:
```json
{
  "Conditions": [
    {
      "Name": "High Value",
      "Expression": "Amount > 10000",
      "Order": 1
    },
    {
      "Name": "Medium Value",
      "Expression": "Amount >= 1000 AND Amount <= 10000",
      "Order": 2
    },
    {
      "Name": "Low Value",
      "Expression": "Amount < 1000",
      "Order": 3
    }
  ],
  "HasDefaultOutput": false
}
```

**Evaluation Behavior**:
- Conditions are evaluated in `Order` (ascending)
- First matching condition wins (row is routed to that output)
- If no condition matches and `HasDefaultOutput` is true, row goes to default output
- If no condition matches and `HasDefaultOutput` is false, row is discarded

---

### 3. ETLDerivedColumn - Expression-Based Calculations

**Purpose**: Add new calculated columns using expressions without writing code.

**Visual Design**: Rounded rectangle with "f(x)" symbol

**Ports**:
- Input (Left): Single input stream
- Output (Right): Stream with original + derived columns

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| DerivedColumns | JSON Array | Array of `DerivedColumnDefinition` objects |
| OutputSchema | JSON | Schema including derived columns (ColumnDefinition array) |

**DerivedColumnDefinition Model**:
```csharp
public class DerivedColumnDefinition
{
    public string Name { get; set; }         // "FullName"
    public string Expression { get; set; }   // "FirstName + ' ' + LastName"
    public string DataType { get; set; }     // "String", "Int32", "Decimal", etc.
    public string Description { get; set; }  // Optional documentation
}
```

**Use Cases**:
- Concatenate fields (full name from first + last)
- Mathematical calculations (tax, totals, percentages)
- String manipulation (uppercase, substring, trim)
- Date/time calculations (age from birthdate, fiscal quarter)
- Type conversions (string to int, int to decimal)

**Example Configuration**:
```json
{
  "DerivedColumns": [
    {
      "Name": "FullName",
      "Expression": "FirstName + ' ' + LastName",
      "DataType": "String"
    },
    {
      "Name": "TotalWithTax",
      "Expression": "SubTotal * 1.13",
      "DataType": "Decimal",
      "Description": "SubTotal + 13% HST"
    },
    {
      "Name": "FiscalQuarter",
      "Expression": "CEILING((MONTH(OrderDate) + 3) / 3.0)",
      "DataType": "Int32",
      "Description": "Q1-Q4 with Apr-Mar fiscal year"
    }
  ]
}
```

**Expression Language** (Future):
- Arithmetic: `+`, `-`, `*`, `/`, `%`, `^`
- String: `+` (concatenation), `UPPER()`, `LOWER()`, `SUBSTRING()`, `LENGTH()`, `TRIM()`
- Date/Time: `YEAR()`, `MONTH()`, `DAY()`, `DATEADD()`, `DATEDIFF()`
- Logical: `AND`, `OR`, `NOT`, `==`, `!=`, `<`, `>`, `<=`, `>=`
- Math: `CEILING()`, `FLOOR()`, `ROUND()`, `ABS()`, `SQRT()`
- Null handling: `ISNULL()`, `COALESCE()`

---

### 4. ETLScd - Slowly Changing Dimensions

**Purpose**: Manage dimension tables that change slowly over time with full history tracking.

**Visual Design**: Cylinder with clock icon overlay (indicating historical tracking)

**Ports**:
- Input (Left): Stream of dimension updates
- Output (Right): Processed dimension changes (INSERT/UPDATE operations)

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| ScdType | Enum | `Type1` (overwrite), `Type2` (history), `Type3` (limited history) |
| BusinessKeys | CSV String | Natural key columns (e.g., "CustomerID,ProductSKU") |
| ChangeTrackingColumns | CSV String | Columns to track for changes (Type 2/3 only) |
| EffectiveFromColumn | String | Start date column name (Type 2 only, default: "EffectiveFrom") |
| EffectiveToColumn | String | End date column name (Type 2 only, default: "EffectiveTo") |
| CurrentFlagColumn | String | Current indicator column (Type 2 only, default: "IsCurrent") |
| VersionColumn | String | Version number column (Type 3 only, default: "Version") |
| TableName | String | Target dimension table name |

**SCD Types**:

#### Type 1 - Overwrite (No History)
- Simplest approach
- Updates existing row in place
- No history of previous values
- Best for: corrections, insignificant changes

**Example**: Customer address change
```
Before: CustomerID=123, Name="John", City="Toronto"
Update: City="Vancouver"
After:  CustomerID=123, Name="John", City="Vancouver"
```

#### Type 2 - Full History (Row Versioning)
- Creates new row for each change
- Maintains complete change history
- Uses effective dates and current flag
- Best for: auditing, trend analysis, regulatory compliance

**Example**: Customer address change
```
Before:
CustomerID | Name  | City    | EffectiveFrom | EffectiveTo | IsCurrent
123        | John  | Toronto | 2020-01-01    | 9999-12-31  | 1

After:
CustomerID | Name  | City      | EffectiveFrom | EffectiveTo | IsCurrent
123        | John  | Toronto   | 2020-01-01    | 2024-03-15  | 0
123        | John  | Vancouver | 2024-03-16    | 9999-12-31  | 1
```

#### Type 3 - Limited History (Column Versioning)
- Adds columns for previous values
- Stores only one previous version
- More compact than Type 2
- Best for: recent changes, limited attributes

**Example**: Customer address change
```
Before: CustomerID=123, Name="John", CurrentCity="Toronto", PreviousCity=NULL
After:  CustomerID=123, Name="John", CurrentCity="Vancouver", PreviousCity="Toronto"
```

**Configuration Example (Type 2)**:
```json
{
  "ScdType": "Type2",
  "BusinessKeys": "CustomerID",
  "ChangeTrackingColumns": "Name,Address,City,Province,PostalCode",
  "EffectiveFromColumn": "EffectiveFrom",
  "EffectiveToColumn": "EffectiveTo",
  "CurrentFlagColumn": "IsCurrent",
  "TableName": "DimCustomer"
}
```

**Processing Logic**:
1. Read incoming dimension update
2. Lookup existing row by business key
3. Compare change tracking columns
4. If changed:
   - **Type 1**: Update existing row
   - **Type 2**: Close current row (set EffectiveTo, IsCurrent=0), insert new row
   - **Type 3**: Move current values to "Previous" columns, update current
5. If not found: Insert new row

---

### 5. Enhanced ETL Base Components

#### ETLControl - Error Output Support

**New Property**: `HasErrorOutput` (Boolean)

**Purpose**: Enable error output ports on all ETL components for rejected/failed rows.

**Behavior**:
- When `HasErrorOutput = true`, component adds an error output port
- Rows that fail validation or processing are routed to error output
- Error rows include original data + error details
- Allows error handling without failing entire job

**Use Cases**:
- Capture data quality violations
- Log failed lookups
- Separate good records from bad records
- Retry logic for transient failures

#### ETLTarget - Data Quality Validation

**New Property**: `DataQualityRules` (JSON Array)

**Purpose**: Validate incoming data against rules before writing to destination.

**DataQualityRule Model**:
```csharp
public class DataQualityRule
{
    public string Name { get; set; }           // "Email Format"
    public string ColumnName { get; set; }     // "EmailAddress"
    public RuleType Type { get; set; }         // NotNull, Unique, Range, Regex, Length, Custom
    public string Expression { get; set; }     // Type-specific expression
    public string ErrorMessage { get; set; }   // "Invalid email format"
    public bool StopOnError { get; set; }      // Halt on failure vs. log and continue
}

public enum RuleType
{
    NotNull,      // Column must have a value
    Unique,       // Column must be unique within batch
    Range,        // Value must be within range (Expression: "0,100")
    Regex,        // Value must match pattern (Expression: regex pattern)
    Length,       // String length constraints (Expression: "min,max")
    Custom        // Custom expression (Expression: boolean expression)
}
```

**Example Configuration**:
```json
{
  "DataQualityRules": [
    {
      "Name": "Email Required",
      "ColumnName": "Email",
      "Type": "NotNull",
      "ErrorMessage": "Email address is required",
      "StopOnError": false
    },
    {
      "Name": "Email Format",
      "ColumnName": "Email",
      "Type": "Regex",
      "Expression": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$",
      "ErrorMessage": "Invalid email format",
      "StopOnError": false
    },
    {
      "Name": "Age Range",
      "ColumnName": "Age",
      "Type": "Range",
      "Expression": "0,120",
      "ErrorMessage": "Age must be between 0 and 120",
      "StopOnError": false
    },
    {
      "Name": "Amount Positive",
      "ColumnName": "Amount",
      "Type": "Custom",
      "Expression": "Amount > 0",
      "ErrorMessage": "Amount must be positive",
      "StopOnError": true
    }
  ]
}
```

**Validation Workflow**:
1. Row arrives at ETLTarget
2. For each rule:
   - Extract column value
   - Apply rule logic
   - If fails: collect error, optionally halt
3. If validation passes: write to destination
4. If validation fails: route to error output (if enabled)

---

## Property Editor Integration

All new properties are automatically editable in the component property editor overlay:

### Grid Editors
The property editor now includes specialized grid editors for complex properties:

#### DerivedColumns Grid
- Columns: Name | Expression | DataType | Description
- Add/edit/delete derived column definitions inline
- Expression syntax hints (future enhancement)

#### Conditions Grid
- Columns: Name | Expression | Order | Description
- Drag to reorder (future enhancement)
- Expression validation (future enhancement)

#### DataQualityRules Grid
- Columns: Name | Column | Type | Expression | Error Message
- Type dropdown with enum values
- Expression field adapts to rule type

### Simple Properties
- CacheMode: Dropdown (None, Partial, Full)
- ScdType: Dropdown (Type1, Type2, Type3)
- MatchColumns, ReturnColumns, BusinessKeys, ChangeTrackingColumns: Text fields (CSV format)
- HasErrorOutput, FailOnNoMatch, PreCreateTable: Checkboxes

---

## Common Patterns & Best Practices

### Pattern 1: Lookup with Error Handling
```
ETLSource → ETLLookup (FailOnNoMatch=false, HasErrorOutput=true) → ETLTarget
                ↓ (error port)
              ETLTarget (ErrorLog table)
```

### Pattern 2: Data Quality Pipeline
```
ETLSource → ETLDerivedColumn (calculate fields)
         → ETLTarget (with DataQualityRules, HasErrorOutput=true)
         → ETLTarget (main table)
            ↓ (error port)
            ETLTarget (reject table for manual review)
```

### Pattern 3: Multi-Path Processing
```
ETLSource → ETLConditionalSplit (3 conditions)
         → Output 0: ETLTarget (HighValueOrders)
         → Output 1: ETLTarget (MediumValueOrders)
         → Output 2: ETLTarget (LowValueOrders)
```

### Pattern 4: Dimension Update (SCD Type 2)
```
ETLSource (staging) → ETLLookup (enrich with reference data)
                   → ETLScd (ScdType=Type2, BusinessKeys="CustomerID")
                   → ETLTarget (DimCustomer table)
```

### Pattern 5: Complex Transformation
```
ETLSource → ETLDerivedColumn (add calculated fields)
         → ETLLookup (enrich with reference)
         → ETLConditionalSplit (route by region)
         → [Region-specific processing branches]
         → ETLTarget (with validation rules)
```

---

## Expression Language (Future Enhancement)

A full expression language is planned for Conditions, DerivedColumns, and Custom rules:

**Features**:
- Type-safe evaluation with schema awareness
- IntelliSense/autocomplete in property editor
- Syntax validation and error highlighting
- Function library (string, math, date, null handling)
- Custom function extensibility
- Performance optimization (compiled expressions)

**Example Advanced Expressions**:
```csharp
// String manipulation
UPPER(TRIM(FirstName)) + ' ' + SUBSTRING(LastName, 0, 1) + '.'

// Date arithmetic
DATEDIFF(day, OrderDate, GETDATE()) <= 30 ? "Recent" : "Old"

// Null handling
ISNULL(PhoneNumber, "N/A")

// Conditional logic
CASE 
  WHEN Amount > 10000 THEN "Platinum"
  WHEN Amount > 5000 THEN "Gold"
  WHEN Amount > 1000 THEN "Silver"
  ELSE "Bronze"
END
```

---

## Remaining Gap Analysis

Based on the original gap analysis, we've now implemented:
- ✅ Error Handling & Error Output
- ✅ Lookup Transformation (with cache modes)
- ✅ Slowly Changing Dimensions (Type 1/2)
- ✅ Conditional Split (multi-output routing)
- ✅ Derived Column (expression-based calculations)
- ✅ Data Quality Validation (in ETLTarget)

**Still to implement** (future roadmap):
- Pivot/Unpivot transformations
- Sort and distinct operations
- Change Data Capture (CDC) connectors
- Fuzzy matching and lookup
- Surrogate key generation
- Parameterization and variables
- Script component (Python/C# inline)
- Union, merge, merge join transformations
- Row sampling and percentage split
- Performance monitoring and lineage tracking

**Current Coverage**: ~50% of professional ETL features (up from 30%)

---

## Performance Considerations

### ETLLookup Caching
- **Full Cache**: Best for small reference tables (< 100K rows), preloads all data
- **Partial Cache**: For medium tables, caches on demand with LRU eviction
- **No Cache**: For large tables or frequently changing data, query per lookup

### ETLScd Type 2 Performance
- Index business keys for fast lookups
- Index effective dates for point-in-time queries
- Consider partitioning by effective date for large dimensions
- Batch updates when possible (reduce roundtrips)

### ETLConditionalSplit Optimization
- Order conditions by probability (most common first)
- Keep expressions simple (complex logic slows evaluation)
- Consider splitting into multiple stages if many conditions

### Data Quality Rules
- Validate early (closer to source = faster failure detection)
- Use StopOnError judiciously (balance speed vs. completeness)
- Batch validation when possible (fewer passes over data)

---

## Migration from Basic ETL

### Step 1: Add Error Outputs
```diff
- ETLTarget (write all data, fail on error)
+ ETLTarget (write valid data) + HasErrorOutput=true
+ ETLTarget (write errors to reject table)
```

### Step 2: Replace Custom Logic with Components
```diff
- ETLTransform (custom code for lookups)
+ ETLLookup (declarative configuration)
```

### Step 3: Add Data Quality
```diff
- ETLTarget (no validation)
+ ETLTarget (with DataQualityRules array)
```

### Step 4: Implement SCD for Dimensions
```diff
- ETLTarget (truncate/load dimension)
+ ETLScd (Type2) → ETLTarget (maintain history)
```

---

## Troubleshooting

### Issue: Lookup returns no matches
- Check `MatchColumns` CSV format (no spaces, correct column names)
- Verify reference data is loaded (check Input 1 connection)
- Enable `HasErrorOutput` to capture unmatched rows for inspection

### Issue: Conditional Split routes all rows to default
- Check condition expressions for syntax errors
- Verify column names in expressions match input schema
- Check condition `Order` values (lower = evaluated first)

### Issue: Derived Column expressions fail
- Verify column names exist in input schema
- Check data type compatibility (e.g., can't add string + int)
- Use `ISNULL()` to handle null values

### Issue: SCD not tracking changes
- Verify `ChangeTrackingColumns` includes all relevant columns
- Check `BusinessKeys` uniquely identify dimension records
- Ensure effective date columns are correct data type (DateTime)

### Issue: Data Quality rules always fail
- Check `ColumnName` exactly matches schema (case-sensitive)
- Verify `Expression` format for rule type (e.g., "min,max" for Range)
- Enable error output and inspect error messages

---

## Version History

**v1.0 (2024-09)**: Professional ETL features release
- New components: ETLLookup, ETLConditionalSplit, ETLDerivedColumn, ETLScd
- Enhanced: ETLControl (error outputs), ETLTarget (data quality rules)
- Property editor grid support for complex properties
- Gap analysis: 30% → 50% professional feature coverage

---

## Next Steps

1. **Expression Engine**: Implement full expression parser/evaluator
2. **Property Editor Enhancements**: Expression IntelliSense, syntax highlighting, validation
3. **Performance Metrics**: Add throughput, row counts, timing to components
4. **Data Lineage**: Track data flow from source to target with transformations
5. **Unit Testing**: Test harness for validating ETL logic without connections
6. **Template Library**: Pre-built patterns for common scenarios (SCD, CDC, data quality)
7. **Monitoring Dashboard**: Real-time view of running ETL jobs with error tracking
8. **Additional Components**: Pivot, Unpivot, CDC, Fuzzy Lookup, Script, Union, Merge

---

## Support & Resources

- **Gap Analysis**: See `ETL_GAP_ANALYSIS.md` for complete feature comparison
- **Code Examples**: See `Beep.Skia.ETL/*.cs` for component implementations
- **Model Definitions**: See `Beep.Skia.Model/DataQualityRule.cs` for data contracts
- **Property Editor**: See `Beep.Skia/Components/ComponentPropertyEditor.cs` for UI integration
