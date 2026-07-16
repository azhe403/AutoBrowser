using AutoBrowser.Models;
using AutoBrowser.Services;
using AutoBrowser.ViewModels;
using Moq;

namespace AutoBrowser.Tests.ViewModels;

public class SettingsViewModelTests
{
    private readonly Mock<IProtocolService> _mockProtocolService;
    private readonly Mock<IDefaultBrowserService> _mockDefaultBrowserService;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public SettingsViewModelTests()
    {
        _mockProtocolService = new Mock<IProtocolService>();
        _mockDefaultBrowserService = new Mock<IDefaultBrowserService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(new AppSettings());
    }

    [Fact]
    public void Constructor_InitializesSettings()
    {
        // Act
        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);

        // Assert
        Assert.NotNull(vm.AvailableBrowsers);
        Assert.True(vm.MinimizeToTray);
        Assert.True(vm.ShowPushNotifications);
    }


    [Fact]
    public void ShowPushNotifications_SetAndGet_WorksCorrectly()
    {
        var settings = new AppSettings { ShowPushNotifications = true };
        _mockSettingsService.Setup(x => x.LoadSettings()).Returns(settings);

        var vm = new SettingsViewModel(
            _mockProtocolService.Object,
            _mockDefaultBrowserService.Object,
            _mockSettingsService.Object);

        Assert.True(vm.ShowPushNotifications);

        vm.ShowPushNotifications = false;
        Assert.False(vm.ShowPushNotifications);
        _mockSettingsService.Verify(x => x.SaveSettings(It.Is<AppSettings>(s => s.ShowPushNotifications == false)), Times.Once);
    }
}
