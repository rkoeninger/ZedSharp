# zed-sharp

A library for multi-paradigm programming in C#/.Net

### Basic Data Types

  * `Maybe<A>` - may or may not have a value and maps functions over a hypothetical value
  * `Unit` - has only one meaningully unique value
  * `Void` - no value can be constructed; is always null
  * `ISequence<A>` - lazy, one-time iteration of values

### Function Composition

  * `Match` - extensible, immutable control-flow structure that branches on arbitrary predicates
  * `MultiMethod` - mutable set of method overloads that are dispatched on the types of multiple arguments
  * `GenericMethod` - mutable set of method overloads that are dispatched on arbitrary predicates

### Pure Functional Programming Facilities

  * `IO` - a type for modeling and composing I/O and side effects
  * `Lens` - composable get and set pair for immutable types

### Testing and Validation

  * `Expect` - testing for exceptions and invalid syntax; companion to Assert
  * `Check` - property based testing
  * `Verify` - structural examination of data
  * `Validation` - appies a series of validation predicates to a value, collecting exceptions
  * `Rand` - produces random test data
  * `Sample` - provides common and edge-case test values
  * `All` - enumerates all values of certain types

### Relational Data Structures

  * `Table` - a list/set of Rows
  * `Row` - a typed series of named values in a Table
  * `Column` - a typed, named list/set of values in a Table
  * `Projection` - a conversion from one Table to another

### Improved Date and Time Facilities

  * `DateTimeRange` - a span of DateTimes that describes an entire day instead of midight the morning of that day

### Dependency Injection

  * `Needs` - a basic IoC container that uses the following attributes
  * `DefaultImplementationAttribute` - placed on an interface to indicate the default implementation so an IoC container doesn't have to explicitly configured with it
  * `DefaultImplementationOfAttribute` - placed on a class to indicate that it is the default implementation of the specified interface
  * Mockable interfaces to common components: IConsole, IClock, IFileSystem
  * Standard "live" implementations of those interfaces that pass-through to Console, DateTime, File, Directory, etc.
  * Useful mock implementations of those interfaces like ScriptedConsole, StoppedClock and VirtualFileSystem

### Crazy XML Building Facility Based on Operator Overloading

  * This was clearly a mistake
  * I am so sorry
  * Uses overloaded `<`, `>`, `<=` and `>=` operators to build XML
  * Lets you do this:

```csharp
Xml.Doc < "catalog"
    < "book" >= "id" <= "bk101"
        < "author" <= "Gambardella, Matthew"
        < "title" <= "XML Developer's Guide"
        < "price" <= "44.95" > Xml.End
    < "book" >= "id" <= "bk102"
        < "author" <= "Ralls, Kim"
        < "title" <= "Midnight Rain"
        < "price" <= "5.95" > Xml.EndDoc
```