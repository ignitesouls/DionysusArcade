// SPDX-License-Identifier: GPL-3.0-only
// Originally part of the Athena Randomizer (https://github.com/ignitesouls/AthenaRandomizer),
// copyright the Athena Randomizer contributors. Modified for Dionysus Arcade.
using SoulsFormats;

namespace Dionysus.Core.FormatEditors;

public class MenuBndEditorService
{
    private readonly BND4 menuBnd;
    private readonly FMG lineHelp;
    public static readonly int[] LineHelpClassDescriptionIDs = { 297130, 297131, 297132, 297133, 297134, 297135, 297138, 297136, 297137, 297139, 297140, 297141 };

    private MenuBndEditorService(BND4 bnd)
    {
        menuBnd = bnd;
        BinderFile file = menuBnd.Files.FirstOrDefault(f => Path.GetFileName(f.Name) == "GR_LineHelp.fmg")
            ?? throw new Exception("Failed to read FMG file necessary for rewriting starting class descriptions.");
        lineHelp = FMG.Read(file.Bytes);
    }

    public void SetClassDescription(int i, string classDescription)
    {
        int lineHelpFmgIndex = LineHelpClassDescriptionIDs[i];
        lineHelp[lineHelpFmgIndex] = classDescription;
    }

    public static MenuBndEditorService ReadFromMenuBndFilePath(string menuBndFilePathIn) =>
        new(BND4.Read(File.ReadAllBytes(menuBndFilePathIn)));

    // Opens an archive that has already been read (and possibly patched)
    public static MenuBndEditorService FromBnd(BND4 bnd) => new(bnd);

    public void WriteToMenuBndFilePath(string menuBndFilePathOut)
    {
        foreach (BinderFile file in menuBnd.Files)
        {
            if (Path.GetFileName(file.Name) == "GR_LineHelp.fmg")
            {
                file.Bytes = lineHelp.Write();
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(menuBndFilePathOut)!);
        File.WriteAllBytes(menuBndFilePathOut, menuBnd.Write());
    }
}