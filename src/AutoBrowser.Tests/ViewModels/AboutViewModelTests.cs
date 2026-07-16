using AutoBrowser.ViewModels;

namespace AutoBrowser.Tests.ViewModels;

public class AboutViewModelTests
{
    [Fact]
    public void AppVersion_ReturnsCorrectValue()
    {
        // Act
        var vm = new AboutViewModel();

        // Assert
        Assert.NotNull(vm.AppVersion);
        Assert.NotNull(vm.OpenUrlCommand);
    }
}
