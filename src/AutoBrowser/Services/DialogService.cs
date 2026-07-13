using AutoBrowser.Models;
using AutoBrowser.Views;
using Serilog;

namespace AutoBrowser.Services;

public class DialogService : IDialogService
{
    public RoutingRule? ShowAddRuleDialog()
    {
        try
        {
            Log.Information("ShowAddRuleDialog: Opening dialog");
            var dialog = new RuleEditorView();
            var result = dialog.ShowDialog();
            Log.Information("ShowAddRuleDialog: Dialog result={Result}", result);
            return result == true ? dialog.Rule : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ShowAddRuleDialog: Failed to show dialog");
            return null;
        }
    }

    public RoutingRule? ShowEditRuleDialog(RoutingRule existingRule)
    {
        try
        {
            Log.Information("ShowEditRuleDialog: Opening dialog for rule {RuleName}", existingRule.Name);
            var dialog = new RuleEditorView(existingRule);
            var result = dialog.ShowDialog();
            Log.Information("ShowEditRuleDialog: Dialog result={Result}", result);
            return result == true ? dialog.Rule : null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ShowEditRuleDialog: Failed to show dialog for rule {RuleName}", existingRule.Name);
            return null;
        }
    }
}