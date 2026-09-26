// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Pipeline;
using SoulsFormats;
using System.Globalization;
using System.Text;
using Andre.Formats;

// SoulsFormats looks for the Oodle DLL in the current working directory,
// so switch to this program's own folder first.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Compare mode: writes every difference between two regulations (and optionally two menu files) to CSV files.
// Usage: Dionysus.ParityCheck compare <vanillaRegulation> <modifiedRegulation> <paramDefsFolder> <outputFolder> [vanillaMenu] [modifiedMenu]
if (args.Length >= 5 && args[0] == "compare")
{
    string outputFolder = args[4];
    Directory.CreateDirectory(outputFolder);

    // Load every param definition in the folder, indexed by the param type it describes
    var defs = new Dictionary<string, PARAMDEF>();
    foreach (string defPath in Directory.GetFiles(args[3], "*.xml"))
    {
        try
        {
            PARAMDEF def = PARAMDEF.XmlDeserialize(defPath);
            defs[def.ParamType] = def;
        }
        catch (Exception)
        {
            // Not a param definition file; skip it
        }
    }

    BND4 vanillaReg = SFUtil.DecryptERRegulation(args[1]);
    BND4 modifiedReg = SFUtil.DecryptERRegulation(args[2]);

    var paramLines = new List<string> { "Param,RowId,RowName,Field,Vanilla,Value" };

    foreach (BinderFile modFile in modifiedReg.Files)
    {
        string paramName = Path.GetFileNameWithoutExtension(modFile.Name);
        BinderFile? vanFile = vanillaReg.Files.FirstOrDefault(f => f.Name == modFile.Name);
        if (vanFile != null && vanFile.Bytes.Span.SequenceEqual(modFile.Bytes.Span))
        {
            continue; // unchanged param
        }

        Param modParam = Param.Read(modFile.Bytes);
        if (modParam.ParamType == null || !defs.TryGetValue(modParam.ParamType, out PARAMDEF? def))
        {
            paramLines.Add(Csv(paramName, "", "", $"(no definition for {modParam.ParamType})", "", ""));
            continue;
        }
        modParam.ApplyParamdef(def);

        // Vanilla rows by ID (stays empty if the whole param is new)
        Param? vanParam = null;
        var vanRows = new Dictionary<int, Param.Row>();
        if (vanFile != null)
        {
            vanParam = Param.Read(vanFile.Bytes);
            vanParam.ApplyParamdef(def);
            foreach (Param.Row row in vanParam.Rows) vanRows.TryAdd(row.ID, row);
        }

        var modIds = new HashSet<int>();
        foreach (Param.Row modRow in modParam.Rows)
        {
            modIds.Add(modRow.ID);
            vanRows.TryGetValue(modRow.ID, out Param.Row? vanRow);
            string id = modRow.ID.ToString(CultureInfo.InvariantCulture);
            string name = modRow.Name ?? "";

            if (vanRow == null)
            {
                paramLines.Add(Csv(paramName, id, name, "(new row)", "", ""));
            }
            else if (vanRow.Name != modRow.Name)
            {
                paramLines.Add(Csv(paramName, id, name, "(row name)", vanRow.Name ?? "", name));
            }

            for (int c = 0; c < modParam.Columns.Count; c++)
            {
                Param.Column modCol = modParam.Columns[c];
                if (modCol.Def.DisplayType == PARAMDEF.DefType.dummy8) continue; // padding bytes

                string value = Format(modCol.GetValue(modRow));
                // Existing rows compare against vanilla; new rows against the field's default value
                string before = vanRow != null
                    ? Format(vanParam!.Columns[c].GetValue(vanRow))
                    : Format(modCol.Def.Default);

                if (value != before)
                {
                    paramLines.Add(Csv(paramName, id, name, modCol.Def.InternalName, vanRow != null ? before : "(default)", value));
                }
            }
        }

        foreach (Param.Row vanRow in vanRows.Values)
        {
            if (!modIds.Contains(vanRow.ID))
            {
                paramLines.Add(Csv(paramName, vanRow.ID.ToString(CultureInfo.InvariantCulture), vanRow.Name ?? "", "(row removed)", "", ""));
            }
        }
    }

    File.WriteAllLines(Path.Combine(outputFolder, "param_changes.csv"), paramLines, new UTF8Encoding(true));
    Console.WriteLine($"param_changes.csv: {paramLines.Count - 1} lines");

    if (args.Length >= 7)
    {
        BND4 vanMenu = BND4.Read(File.ReadAllBytes(args[5]));
        BND4 modMenu = BND4.Read(File.ReadAllBytes(args[6]));
        var textLines = new List<string> { "File,Id,Vanilla,Value" };

        foreach (BinderFile modFile in modMenu.Files)
        {
            BinderFile? vanFile = vanMenu.Files.FirstOrDefault(f => f.Name == modFile.Name);

            var vanText = new Dictionary<int, string?>();
            if (vanFile != null)
            {
                foreach (FMG.Entry entry in FMG.Read(vanFile.Bytes).Entries) vanText[entry.ID] = entry.Text;
            }
            var modText = new Dictionary<int, string?>();
            foreach (FMG.Entry entry in FMG.Read(modFile.Bytes).Entries) modText[entry.ID] = entry.Text;

            foreach (int textId in vanText.Keys.Union(modText.Keys).OrderBy(i => i))
            {
                vanText.TryGetValue(textId, out string? before);
                modText.TryGetValue(textId, out string? after);
                if (before != after)
                {
                    textLines.Add(Csv(Path.GetFileName(modFile.Name), textId.ToString(CultureInfo.InvariantCulture), before ?? "", after ?? ""));
                }
            }
        }

        File.WriteAllLines(Path.Combine(outputFolder, "text_changes.csv"), textLines, new UTF8Encoding(true));
        Console.WriteLine($"text_changes.csv: {textLines.Count - 1} lines");
    }
    return;
}

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

// Writes values consistently, with "." as the decimal separator on every PC
static string Format(object? value) => value switch
{
    null => "",
    float f => f.ToString("R", CultureInfo.InvariantCulture),
    double d => d.ToString("R", CultureInfo.InvariantCulture),
    byte[] bytes => Convert.ToHexString(bytes),
    _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
};

// Joins fields into one CSV line, quoting any field that contains a comma, quote or line break
static string Csv(params string[] fields) => string.Join(",", fields.Select(field =>
    field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
        ? "\"" + field.Replace("\"", "\"\"") + "\""
        : field));