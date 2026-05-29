using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Proxyfan.Client.Tests;

/// <summary>
///     Architecture test for the <see cref="App" /> client that scans every .axaml file in
///     <c>src/Clients/Client</c> for interactive controls (<c>DataGrid</c>, <c>ListBox</c>,
///     <c>ComboBox</c>, <c>TextBox</c>) and asserts they declare an
///     <c>AutomationProperties.Name</c> so screen readers can announce them. Provides a
///     forcing function so future additions don't silently regress accessibility.
/// </summary>
public sealed partial class AppAccessibilityArchitectureTests
{
    private static readonly string[] InteractiveControlNames = ["DataGrid", "ListBox", "ComboBox", "TextBox"];
    private static readonly Regex ControlDeclarationRegex = BuildControlDeclarationRegex();
    private static readonly Regex AutomationNameRegex = BuildAutomationNameRegex();
    private static readonly HashSet<string> AllowListedFileNames = BuildAllowList();

    /// <summary>
    ///     Verifies that every interactive control in every .axaml file under
    ///     <c>src/Clients/Client</c> carries an <c>AutomationProperties.Name</c> attribute.
    /// </summary>
    [Test]
    public async Task EveryInteractiveControl_InEveryClientView_HasAutomationName()
    {
        var clientRoot = ResolveClientViewsRoot();
        var offenders = new List<string>();

        foreach (var path in Directory.EnumerateFiles(clientRoot, "*.axaml", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(path);

            if (AllowListedFileNames.Contains(fileName))
            {
                continue;
            }

            var content = File.ReadAllText(path);

            foreach (Match controlMatch in ControlDeclarationRegex.Matches(content))
            {
                if (!HasAutomationNameOnDeclaration(content, controlMatch.Index))
                {
                    offenders.Add($"{fileName}@{controlMatch.Index}: <{controlMatch.Groups[1].Value} ...> missing AutomationProperties.Name");
                }
            }
        }

        await Assert.That(offenders.Count)
            .IsEqualTo(0)
            .Because("Offenders: " + string.Join("; ", offenders));
    }

    [GeneratedRegex("AutomationProperties\\.Name\\s*=")]
    private static partial Regex BuildAutomationNameRegex();

    private static Regex BuildControlDeclarationRegex()
    {
        var pattern = "<(" + string.Join("|", InteractiveControlNames) + ")(\\s|>)";
        var regex = new Regex(pattern);
        return regex;
    }

    private static HashSet<string> BuildAllowList()
    {
        // Files allow-listed below have unlabelled interactive controls that still need a
        // dedicated accessibility pass. They are tracked here so that the test still locks
        // in the progress made on the rest of the views and surfaces future regressions on
        // controls outside this list.
        var allowList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BreakpointView.axaml",
            "CustomColumnsView.axaml",
            "DiffToolView.axaml",
            "InspectorView.axaml",
            "PreferencesView.axaml",
            "RemoteDevicesView.axaml",
            "ReverseProxyView.axaml",
            "TextPromptWindow.axaml",
        };
        return allowList;
    }

    private static bool HasAutomationNameOnDeclaration(string content, int controlStartIndex)
    {
        var endIndex = FindElementDeclarationEnd(content, controlStartIndex);

        if (endIndex < 0)
        {
            return false;
        }

        var declaration = content.Substring(controlStartIndex, endIndex - controlStartIndex);
        return AutomationNameRegex.IsMatch(declaration);
    }

    private static int FindElementDeclarationEnd(string content, int startIndex)
    {
        var depth = 0;

        for (var i = startIndex; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '"')
            {
                depth = depth == 1 ? 0 : 1;
            }
            else if (depth == 0 && c == '>')
            {
                return i + 1;
            }
        }

        return -1;
    }

    private static string ResolveClientViewsRoot()
    {
        var current = AppContext.BaseDirectory;
        var directory = new DirectoryInfo(current);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Clients", "Client");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Clients/Client root from " + current);
    }
}
