using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;

    public MainViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());


    }

    [Fact]
    public void AppVersion_And_WindowTitle_ReturnsCorrectValues()
    {
        // Act
        var vm = new MainViewModel(_mockSettingsService.Object);

        // Assert
        Assert.NotNull(vm.AppVersion);
        Assert.Contains(vm.AppVersion, vm.WindowTitle);
        Assert.StartsWith("AutoBrowser v", vm.WindowTitle);
    }
}
