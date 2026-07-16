using Xunit;

namespace AutoBrowser.Tests.UI;

[CollectionDefinition("UiTests", DisableParallelization = true)]
public class UiTestsCollection : ICollectionFixture<AppLauncher> { }