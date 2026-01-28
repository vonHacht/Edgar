using System;
using System.Collections.Generic;

namespace Edgar.Models
{
    /// <summary>
    /// Holds LM-style dictionaries loaded into memory.
    /// All tokens should be stored in UPPERCASE.
    /// </summary>
    public class LmDictionaries
    {
        public HashSet<string> Risk { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Negative { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Uncertainty { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
