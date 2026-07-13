using AutoBrowser.Models;

namespace AutoBrowser.Services;

public interface IDialogService
{
    RoutingRule? ShowAddRuleDialog();
    RoutingRule? ShowEditRuleDialog(RoutingRule existingRule);
}