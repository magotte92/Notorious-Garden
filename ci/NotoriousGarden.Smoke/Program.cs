using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

internal static class Program
{
    private static readonly string[] RequiredTypes =
    {
        "Player",
        "Enemy",
        "Plant",
        "UIManager",
        "Guardian",
        "Point",
        "Super_point",
        "Disease_point",
        "TalentTree",
        "Magotte.TooltipUI.Skill",
        "Magotte.TooltipUI.InsectSkills",
        "Magotte.TooltipUI.Rarity",
        "Magotte.TooltipUI.SkillButton",
        "Magotte.TooltipUI.TooltipPopup",
        "Magotte.TooltipUI.UltimateSkill",
    };

    public static int Main()
    {
        var assembly = typeof(Player).Assembly;
        var missing = RequiredTypes.Where(name => assembly.GetType(name) == null).ToArray();
        if (missing.Length > 0)
        {
            Console.Error.WriteLine("Missing gameplay types: " + string.Join(", ", missing));
            return 1;
        }

        if (typeof(Player).BaseType == null || typeof(Player).BaseType.Name != "MonoBehaviour")
        {
            Console.Error.WriteLine("Player does not derive from MonoBehaviour.");
            return 1;
        }

        var editorReference = assembly.GetReferencedAssemblies()
            .FirstOrDefault(reference => string.Equals(reference.Name, "UnityEditor", StringComparison.Ordinal));
        if (editorReference != null)
        {
            Console.Error.WriteLine("Runtime assembly references UnityEditor.");
            return 1;
        }

        var root = FindRepositoryRoot();
        var usingEditor = new Regex(@"^\s*using\s+UnityEditor\s*;", RegexOptions.CultureInvariant);
        var scriptPaths = Directory.GetFiles(Path.Combine(root, "Assets"), "*.cs", SearchOption.AllDirectories);
        foreach (var scriptPath in scriptPaths)
        {
            foreach (var line in File.ReadLines(scriptPath))
            {
                if (!usingEditor.IsMatch(line))
                {
                    continue;
                }

                Console.Error.WriteLine("Runtime script imports UnityEditor, which fails player builds: " + scriptPath);
                return 1;
            }
        }

        Console.WriteLine("Smoke check passed (" + RequiredTypes.Length + " gameplay types).");
        return 0;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var assets = Path.Combine(directory.FullName, "Assets");
            var ci = Path.Combine(directory.FullName, "ci");
            if (Directory.Exists(assets) && Directory.Exists(ci))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
    }
}
