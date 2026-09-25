// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using EldenRingParamsEditor;

namespace Dionysus.Core.Workspace;

// Opens game files for editing. Each file is looked up in the sources in order
// (packs first, then the bundled vanilla files), opened once, and shared by every module.
public sealed class GameFiles
{
    public const string RegulationPath = "regulation.bin";
    public const string MenuTextPath = "msg/engus/menu_dlc02.msgbnd.dcx";

    private readonly IReadOnlyList<string> _sources;
    private readonly Dictionary<string, OpenFile> _open = new(StringComparer.OrdinalIgnoreCase);

    private sealed record OpenFile(object Editor, Action<string> Write);

    public GameFiles(IReadOnlyList<string> sources)
    {
        _sources = sources;
    }

    // ---------- File types ----------
    // Each supported file type is one property: its game path, how to open it, and how to save it.

    public ParamsEditor Params => Open<ParamsEditor>(RegulationPath,
        ParamsEditor.ReadFromRegulationPath,
        (editor, path) => editor.WriteToRegulationPath(path));

    public MenuBndEditorService MenuText => Open<MenuBndEditorService>(MenuTextPath,
        MenuBndEditorService.ReadFromMenuBndFilePath,
        (editor, path) => editor.WriteToMenuBndFilePath(path));

    // ---------- Mechanics ----------

    // Finds a game file in the first source that has it
    public string Locate(string gamePath)
    {
        foreach (string source in _sources)
        {
            string candidate = Path.Combine(source, gamePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"'{gamePath}' was not found in any file source.");
    }

    // Writes every opened file into the package, at its game path
    public void WriteAll(string packageDir)
    {
        foreach (var (gamePath, file) in _open)
        {
            string outputPath = Path.Combine(packageDir, gamePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            file.Write(outputPath);
        }
    }

    private T Open<T>(string gamePath, Func<string, T> read, Action<T, string> write) where T : class
    {
        if (_open.TryGetValue(gamePath, out OpenFile? existing))
        {
            return (T)existing.Editor;
        }

        T editor = read(Locate(gamePath));
        _open[gamePath] = new OpenFile(editor, path => write(editor, path));
        return editor;
    }
}