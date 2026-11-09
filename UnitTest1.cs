using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace NageenaSnipIt.Automation;

public class Tests
{
    private const string DemoUrl = "https://demo.snipeitapp.com";
    private const string DemoUsername = "admin";
    private const string DemoPassword = "password";
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    private IPage Page => _page ?? throw new InvalidOperationException("The browser page has not been initialized.");

    [SetUp]
    public async Task SetUp()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "chrome",
            Headless = false
        });
        _page = await _browser.NewPageAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    [Test]
    public async Task CreateMacbookProAsset()
    {
        var username = Environment.GetEnvironmentVariable("SNIPEIT_USERNAME") ?? DemoUsername;
        var password = Environment.GetEnvironmentVariable("SNIPEIT_PASSWORD") ?? DemoPassword;

        await Page.GotoAsync($"{DemoUrl}/login");
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Username" }).FillAsync(username);
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Password" }).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(DemoUrl)}/(?!login)"));

        await Page.GotoAsync($"{DemoUrl}/hardware/create");

        var assetTag = await Page.GetByRole(AriaRole.Textbox, new()
        {
            Name = "Asset Tag",
            Exact = true
        }).InputValueAsync();
        Assert.That(assetTag, Is.Not.Empty, "The asset form must provide an asset tag.");

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Select a Model" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new()
        {
            NameRegex = new Regex("Macbook Pro 13", RegexOptions.IgnoreCase)
        }).ClickAsync();

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Select Status" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Ready to Deploy", Exact = true }).First.ClickAsync();

        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Select a User" }).ClickAsync();
        var userSearch = Page.Locator(".select2-search__field").Last;
        await userSearch.FillAsync("admin");
        await Page.GetByRole(AriaRole.Option, new()
        {
            NameRegex = new Regex("admin", RegexOptions.IgnoreCase)
        }).First.ClickAsync();

        await Page.Locator("select[name='redirect_option']").SelectOptionAsync("index");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).First.ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(DemoUrl)}/hardware(?:[/?#].*)?$"));

        await Page.GetByRole(AriaRole.Button, new() { Name = "Refresh" }).ClickAsync();
        var assetSearch = Page.GetByRole(AriaRole.Searchbox, new() { Name = "Search" });
        await assetSearch.FillAsync(assetTag);
        await assetSearch.PressAsync("Enter");

        var createdAssetRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasTextString = assetTag });
        await Assertions.Expect(createdAssetRow).ToBeVisibleAsync();
        await Assertions.Expect(createdAssetRow).ToContainTextAsync("Macbook Pro 13");
        await Assertions.Expect(createdAssetRow).ToContainTextAsync("Ready to Deploy");

    
    }
}
