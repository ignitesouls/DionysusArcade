# UniversalReplacementRandomizer

**UniversalReplacementRandomizer** is a standalone .NET library for performing flexible, seed-deterministic randomization of integer-to-integer mappings, with optional validation constraints. It's designed to support complex modding scenarios—such as replacing item drops in ELDEN RING—while ensuring reproducibility and extensibility.

---

## ✨ Features

- Uniform shuffling via Fisher-Yates (with lazy generators for short-circuiting)
- Deterministic RNG with seed + key-based namespacing
- Constraint-based validation (e.g., disallowing certain combinations)
- Retry-based fallbacks for randomized failures
- Built-in interface for custom validators

---

## 📦 Installation

Clone this repository and reference the `.csproj` in your solution:

```sh
git clone https://github.com/ignitesouls/UniversalReplacementRandomizer.git
```

Then, in your project:
- **Visual Studio:** Add project reference
- **CLI:**
```sh
dotnet add reference ../UniversalReplacementRandomizer/UniversalReplacementRandomizer.csproj
```

---

## 🧠 Core Concepts

### `RandomizationGroup`

Encapsulates a set of `Targets` and possible `Replacements`. It supports:

```csharp
var group = new RandomizationGroup(targets, replacements, optionalValidator);
var result = group.Randomize(rng); // Dictionary<int, int>
```

If a validator is provided, use:

```csharp
var result = group.ValidatedRandomize(rng);
```

Or enable automatic retries on failure:

```csharp
var result = group.RetryingValidatedRandomize(rng, maxAttempts: 100);
```

---

### `ReplacementRandomizer`

Orchestrates multiple named `RandomizationGroup` instances with a shared base seed:

```csharp
var urr = new ReplacementRandomizer(prefix: "myPrefix", baseSeed: 1234);
urr.AddGroup("dlcWeapons", group);
var mapping = urr.RandomizeGroup("dlcWeapons");
```

You can also randomize all groups in bulk:

```csharp
var results = urr.RandomizeAllGroups(); // Dictionary<string, Dictionary<int, int>>
```

---

### `IReplacementValidator`

To enforce constraints between targets and replacements, implement:

```csharp
public interface IReplacementValidator
{
    bool Validate(int target, int replacement);
}
```

#### Example:
```csharp

public class EncodedBitmapValidator : IReplacementValidator
{
    private readonly Dictionary<int, int> TargetEncodedBitmaps;
    private readonly Dictionary<int, int> ReplacementEncodedBitmaps;

    public Dictionary<string, Func<int, int, bool>>? ValidatorsByKey { get; set; }

    public EncodedBitmapValidator(Dictionary<int, int> targetEncodedBitmaps, Dictionary<int, int> replacementEncodedBitmaps)
    {
        TargetEncodedBitmaps = targetEncodedBitmaps;
        ReplacementEncodedBitmaps = replacementEncodedBitmaps;
    }

    public bool Validate(int target, int replacement)
    {
        if (!TargetEncodedBitmaps.TryGetValue(target, out int targetEncodedBitmap))
            return true;                                              // the target doesn't have any conflicts
        if (!ReplacementEncodedBitmaps.TryGetValue(replacement, out int replacementEncodedBitmap))
            return true;                                              // the replacement doesn't have any conflicts
        return (targetEncodedBitmap & replacementEncodedBitmap) == 0; // we have to check for conflicts between the two
    }
}
```
This validator class keeps the constraints encoded as bitmaps.

---

### `SeedManager`

Provides cryptographically secure and namespaced deterministic randomness:

```csharp
var sm = new SeedManager(prefix: "myPrefix", baseSeed: 12345);
var rng = sm.GetRandomByKey("itemlots");
```

Each `key` generates a consistent `Random` object tied to the base seed.

---

### `ListRandomizationExtensions`

Includes classic and lazy Fisher-Yates shuffle implementations:

```csharp
list.FisherYatesShuffle(rng);
foreach (var (i, val) in list.LazyReverseFisherYatesShuffle(rng)) { ... }
```

---

## 🔐 License

This project is licensed under **GPL-3.0-only**. See [LICENSE](./LICENSE) for more.

SPDX-License-Identifier: GPL-3.0-only

---

## 👤 Authors

Built by psiphicode and collaborators, for use across multiple ELDEN RING modding projects including weapon, item, shop, enemy and boss randomization.

---

## 🗓️ Last updated

2025-06-13
