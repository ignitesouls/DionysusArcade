// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Pipeline;
using SoulsFormats;

// SoulsFormats looks for the Oodle DLL in the current working directory,
// so switch to this program's own folder first.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Usage: Dionysus.ParityCheck <resourcesRoot> <seed> <oldRegulationPath>
if (args.Length != 3)
{
    Console.WriteLine("Usage: Dionysus.ParityCheck <resourcesRoot> <seed> <oldRegulationPath>");
    return;
}

string resourcesRoot = args[0];
int seed = int.Parse(args[1]);
string oldRegulationPath = args[2];

// 1. Run the new pipeline with the given seed
BuildResult result = BuildPipeline.Run(resourcesRoot, seed, new Dictionary<string, object?>());
Console.WriteLine($"Built with seed {result.Seed}");

foreach (var (section, lines) in result.Report)
{
    Console.WriteLine($"\n{section}:");
    foreach (string line in lines)
    {
        Console.WriteLine($"  {line}");
    }
}

// 2. Compare every param file in the old and new regulations
string newRegulationPath = Path.Combine(resourcesRoot, "me3-v0.8.0", "basedlc", "regulation.bin");
BND4 oldBnd = SFUtil.DecryptERRegulation(oldRegulationPath);
BND4 newBnd = SFUtil.DecryptERRegulation(newRegulationPath);

Console.WriteLine("\nParams that differ from the old app:");
foreach (BinderFile oldFile in oldBnd.Files)
{
    string name = Path.GetFileName(oldFile.Name);
    BinderFile? newFile = newBnd.Files.FirstOrDefault(f => f.Name == oldFile.Name);

    if (newFile == null)
    {
        Console.WriteLine($"  MISSING  {name}");
    }
    else if (!oldFile.Bytes.Span.SequenceEqual(newFile.Bytes.Span))
    {
        Console.WriteLine($"  DIFF     {name}");
    }
}
Console.WriteLine("\nDone. Any param not listed above is identical.");