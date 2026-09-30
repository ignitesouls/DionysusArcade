// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using EldenRingParamsEditor;
using SoulsFormats;

namespace Dionysus.Core.Workspace;

// Opens game files for editing. Each file is looked up in the sources in order
// (packs first, then the bundled vanilla files), opened once, and shared by every module.
public sealed class GameFiles
{
    public const string RegulationPath = "regulation.bin";
    public const string MenuTextPath = "msg/engus/menu_dlc02.msgbnd.dcx";

    private readonly IReadOnlyList<string> _sources;
    private readonly IReadOnlyList<string> _paramPatches;
    private readonly IReadOnlyList<string> _textPatches;
    private readonly string _defsFolder;
    private readonly Dictionary<string, OpenFile> _open = new(StringComparer.OrdinalIgnoreCase);

    private sealed record OpenFile(object Editor, Action<string> Write);

    public GameFiles(IReadOnlyList<string> sources, IReadOnlyList<string> paramPatches,
        IReadOnlyList<string> textPatches, string defsFolder)
    {
        _sources = sources;
        _paramPatches = paramPatches;
        _textPatches = textPatches;
        _defsFolder = defsFolder;
    }

    // ---------- File types ----------
    // Each supported file type is one property: its game path, how to open it, and how to save it.

    // The regulation is patched first, then handed to ParamsEditor, so every module builds on the patched params
    public ParamsEditor Params => Open<ParamsEditor>(RegulationPath,
        path =>
        {
            BND4 regulation = SFUtil.DecryptERRegulation(path);
            ParamPatcher.Apply(regulation, _paramPatches, _defsFolder);
            return ParamsEditor.FromRegulationBnd(regulation);
        },
        (editor, path) => editor.WriteToRegulationPath(path));

    // The menu text is patched first, then handed to the menu editor
    public MenuBndEditorService MenuText => Open<MenuBndEditorService>(MenuTextPath,
        path =>
        {
            BND4 menu = BND4.Read(File.ReadAllBytes(path));
            TextPatcher.Apply(menu, _textPatches);
            return MenuBndEditorService.FromBnd(menu);
        },
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