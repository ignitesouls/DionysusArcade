// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SoulsFormats;

namespace Dionysus.Core.FormatEditors;

// Applies text patch CSV files to a message archive (msgbnd), before anything else edits it.
//
// Columns (any others, such as Vanilla or Feature, are notes and are ignored):
//   File  - the text file inside the archive, e.g. GR_LineHelp.fmg
//   Id    - the text entry's ID
//   Value - the new text; empty clears the entry
//
// For now, only the menu archive (menu_dlc02) is supported, so every File must be inside it.
public static class TextPatcher
{
    // One line of a patch file. The property names match the CSV column names.
    public sealed class PatchLine
    {
        public string File { get; set; } = "";
        public string Id { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public static void Apply(BND4 archive, IReadOnlyList<string> patchFiles)
    {
        if (patchFiles.Count == 0)
        {
            return;
        }

        List<PatchLine> lines = patchFiles.SelectMany(path => ReadPatchFile(path)).ToList();

        // Each text file is read once, patched, then written back
        foreach (IGrouping<string, PatchLine> fileLines in lines.GroupBy(line => line.File))
        {
            BinderFile file = archive.Files.FirstOrDefault(f => Path.GetFileName(f.Name) == fileLines.Key)
                ?? throw new InvalidDataException($"Text patch refers to '{fileLines.Key}', which isn't in this archive.");

            FMG fmg = FMG.Read(file.Bytes);
            foreach (PatchLine line in fileLines)
            {
                int id = int.Parse(line.Id, CultureInfo.InvariantCulture);
                fmg[id] = line.Value.Length == 0 ? null! : line.Value;
            }
            file.Bytes = fmg.Write();
        }
    }

    private static List<PatchLine> ReadPatchFile(string path)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,    // note columns may be missing or extra
            MissingFieldFound = null
        };
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, config);
        return csv.GetRecords<PatchLine>().ToList();
    }
}