// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using Andre.Formats;
using CsvHelper;
using CsvHelper.Configuration;
using SoulsFormats;

namespace Dionysus.Core.FormatEditors;

// Applies patch CSV files to a decrypted regulation, before anything else edits it.
//
// Columns (any others, such as Vanilla or Feature, are notes and are ignored):
//   Param   - the param's file name without extension, e.g. ShopLineupParam
//   RowId   - a row ID, a range "100-199", a list "1;5;9", or "*" for every row
//   RowName - only used by (new row)
//   Field   - a field name as shown in Smithbox, or one of the actions below
//   Value   - the new value
//
// Actions in the Field column:
//   (new row)     creates the row(s) with default values, named RowName
//   (row removed) deletes the row(s)
//   (row name)    sets the row name to Value
//   (copy from)   copies every field from row Value into the row(s)
public static class ParamPatcher
{
    // One line of a patch file. The property names match the CSV column names.
    public sealed class PatchLine
    {
        public string Param { get; set; } = "";
        public string RowId { get; set; } = "";
        public string RowName { get; set; } = "";
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public static void Apply(BND4 regulation, IReadOnlyList<string> patchFiles, string defsFolder)
    {
        if (patchFiles.Count == 0)
        {
            return;
        }

        List<PatchLine> lines = patchFiles.SelectMany(path => ReadPatchFile(path)).ToList();
        Dictionary<string, PARAMDEF> defs = LoadDefs(defsFolder);

        // Each param is read once, patched line by line in file order, then written back
        foreach (IGrouping<string, PatchLine> paramLines in lines.GroupBy(line => line.Param))
        {
            string paramName = paramLines.Key;
            BinderFile file = regulation.Files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.Name) == paramName)
                ?? throw new InvalidDataException($"Param patch refers to unknown param '{paramName}'.");

            Param param = Param.Read(file.Bytes);
            if (param.ParamType == null || !defs.TryGetValue(param.ParamType, out PARAMDEF? def))
            {
                throw new InvalidDataException($"No definition for '{paramName}' ({param.ParamType}) in {defsFolder}.");
            }
            param.ApplyParamdef(def);

            foreach (PatchLine line in paramLines)
            {
                try
                {
                    ApplyLine(param, line);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"{paramName}, row {line.RowId}, {line.Field}: {ex.Message}", ex);
                }
            }

            file.Bytes = param.Write();
        }
    }

    private static void ApplyLine(Param param, PatchLine line)
    {
        switch (line.Field)
        {
            case "(new row)":
                foreach (int id in ParseRows(param, line.RowId))
                {
                    AddRow(param, id, line.RowName);
                }
                return;

            case "(row removed)":
                foreach (int id in ParseRows(param, line.RowId))
                {
                    param.RemoveRow(GetRow(param, id));
                }
                return;

            case "(row name)":
                foreach (int id in ParseRows(param, line.RowId))
                {
                    GetRow(param, id).Name = line.Value;
                }
                return;

            case "(copy from)":
                Param.Row source = GetRow(param, int.Parse(line.Value, CultureInfo.InvariantCulture));
                foreach (int id in ParseRows(param, line.RowId))
                {
                    Param.Row target = GetRow(param, id);
                    foreach (Param.Column column in param.Columns)
                    {
                        column.SetValue(target, column.GetValue(source));
                    }
                }
                return;
        }

        Param.Column field = param[line.Field]
            ?? throw new InvalidDataException($"There is no field called '{line.Field}'.");
        object value = ParseValue(field, line.Value);
        foreach (int id in ParseRows(param, line.RowId))
        {
            field.SetValue(GetRow(param, id), value);
        }
    }

    // Turns "5", "100-199", "1;5;9" or "*" into row IDs. Ranges and "*" only match rows that exist.
    private static List<int> ParseRows(Param param, string spec)
    {
        spec = spec.Trim();
        if (spec == "*")
        {
            return param.Rows.Select(row => row.ID).ToList();
        }

        var ids = new List<int>();
        foreach (string part in spec.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int dash = part.IndexOf('-', 1); // from index 1, so a leading minus sign isn't read as a range
            if (dash > 0)
            {
                int from = int.Parse(part[..dash], CultureInfo.InvariantCulture);
                int to = int.Parse(part[(dash + 1)..], CultureInfo.InvariantCulture);
                ids.AddRange(param.Rows.Select(row => row.ID).Where(id => id >= from && id <= to));
            }
            else
            {
                ids.Add(int.Parse(part, CultureInfo.InvariantCulture));
            }
        }
        return ids;
    }

    private static Param.Row GetRow(Param param, int id) =>
        param[id] ?? throw new InvalidDataException($"Row {id} does not exist.");

    // Creates a row with every field at its default value, keeping the rows sorted by ID
    private static void AddRow(Param param, int id, string name)
    {
        if (param[id] != null)
        {
            throw new InvalidDataException($"Row {id} already exists.");
        }

        var row = new Param.Row(id, string.IsNullOrEmpty(name) ? null : name, param);
        foreach (Param.Column column in param.Columns)
        {
            if (column.Def.DisplayType == PARAMDEF.DefType.dummy8 || column.Def.ArrayLength > 1)
            {
                continue; // padding, byte arrays and text stay empty
            }

            string defaultText = Convert.ToString(column.Def.Default, CultureInfo.InvariantCulture) ?? "";
            if (defaultText.Length > 0)
            {
                column.SetValue(row, ParseValue(column, defaultText));
            }
        }

        int index = 0;
        while (index < param.Rows.Count && param.Rows[index].ID <= id)
        {
            index++;
        }
        param.InsertRow(index, row);
    }

    // Converts text to the exact type the field stores. Each result is cast to object on its own,
    // so the value keeps its precise type (a byte stays a byte, not an int).
    private static object ParseValue(Param.Column column, string text)
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        text = text.Trim();
        return column.Def.DisplayType switch
        {
            PARAMDEF.DefType.s8 => (object)sbyte.Parse(text, c),
            PARAMDEF.DefType.s16 => (object)short.Parse(text, c),
            PARAMDEF.DefType.s32 or PARAMDEF.DefType.b32 => (object)int.Parse(text, c),
            PARAMDEF.DefType.f32 or PARAMDEF.DefType.angle32 => (object)float.Parse(text, c),
            PARAMDEF.DefType.f64 => (object)double.Parse(text, c),
            PARAMDEF.DefType.u8 or PARAMDEF.DefType.dummy8 when column.Def.ArrayLength > 1 => (object)Convert.FromHexString(text),
            PARAMDEF.DefType.u8 or PARAMDEF.DefType.dummy8 => (object)byte.Parse(text, c),
            PARAMDEF.DefType.u16 => (object)ushort.Parse(text, c),
            PARAMDEF.DefType.u32 => (object)uint.Parse(text, c),
            PARAMDEF.DefType.fixstr or PARAMDEF.DefType.fixstrW => (object)text,
            _ => throw new NotSupportedException($"Unsupported field type {column.Def.DisplayType}.")
        };
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

    private static Dictionary<string, PARAMDEF> LoadDefs(string folder)
    {
        var defs = new Dictionary<string, PARAMDEF>();
        foreach (string path in Directory.GetFiles(folder, "*.xml"))
        {
            try
            {
                PARAMDEF def = PARAMDEF.XmlDeserialize(path);
                defs[def.ParamType] = def;
            }
            catch (Exception)
            {
                // Not a param definition file; skip it
            }
        }
        return defs;
    }
}