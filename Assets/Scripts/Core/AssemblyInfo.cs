using System.Runtime.CompilerServices;

// Exposes `internal` members of ProjectAstra.Core to the test assembly.
// Lives in its own file because it's an assembly-level attribute — placing it
// on any specific class file (e.g. GridCursor.cs) implies a per-class scope
// that doesn't actually exist.
[assembly: InternalsVisibleTo("ProjectAstra.Core.Tests")]
