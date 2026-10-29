using System.IO;
using Xunit;
namespace GeminiCapyPet.Tests;
public sealed class SoftwareTests
{
    private sealed class Fake : IManagedApp
    {
        public bool Visible { get; set; }
        public bool Fails;
        public int Shows, Hides;
        public TaskCompletionSource? Block;
        public async Task ShowAsync() { Shows++; if (Block != null) await Block.Task; if (Fails) throw new IOException("launch"); Visible = true; }
        public void Hide() { Hides++; Visible = false; }
    }
    [Fact] public async Task ToggleAndActivateAreDifferent()
    {
        var fake = new Fake(); var c = new AppCoordinator(_ => fake); var a = new SoftwareEntry("a","A","a.exe");
        await c.ToggleAsync(a); Assert.True(fake.Visible);
        await c.ToggleAsync(a,true); Assert.True(fake.Visible);
        await c.ToggleAsync(a); Assert.False(fake.Visible);
    }
    [Fact] public async Task FailedDestinationKeepsPreviousVisible()
    {
        var a = new Fake(); var b = new Fake { Fails = true };
        var c = new AppCoordinator(e => e.Id == "a" ? a : b);
        await c.ToggleAsync(new("a","A"));
        await Assert.ThrowsAsync<IOException>(() => c.ToggleAsync(new("b","B")));
        Assert.True(a.Visible); Assert.False(c.Busy);
    }
    [Fact] public async Task RapidTogglesAreQueuedWithoutDuplicateLaunch()
    {
        var f = new Fake { Block = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var c = new AppCoordinator(_ => f); var app = new SoftwareEntry("a","A");
        var first = c.ToggleAsync(app); var second = c.ToggleAsync(app);
        f.Block.SetResult(); await Task.WhenAll(first,second);
        Assert.False(f.Visible); Assert.Equal(1,f.Shows); Assert.Equal(1,f.Hides);
    }
    [Fact] public async Task SwitchingHidesPreviousAfterShowingDestination()
    {
        var a = new Fake(); var b = new Fake(); var c = new AppCoordinator(e => e.Id == "a" ? a : b);
        await c.ToggleAsync(new("a","A")); await c.ToggleAsync(new("b","B"));
        Assert.False(a.Visible); Assert.True(b.Visible);
    }
    [Fact] public void LibraryRoundTripAndRemovalPreserveExecutable()
    {
        var dir = Path.Combine(Path.GetTempPath(),"capies-tests-"+Guid.NewGuid()); Directory.CreateDirectory(dir);
        try {
            var exe=Path.Combine(dir,"Demo App.exe"); File.WriteAllText(exe,"fixture");
            var file=Path.Combine(dir,"software.json"); var c=new SoftwareCatalog(file,"ChatGpt");
            Assert.Equal("chatgpt",c.SelectedId); c.Add(exe);
            var id=c.SelectedId; var loaded=new SoftwareCatalog(file,"Gemini");
            Assert.Equal(id,loaded.SelectedId); Assert.Equal(3,loaded.Apps.Count);
            Assert.Throws<ArgumentException>(()=>loaded.Add(exe));
            loaded.Remove(id); Assert.True(File.Exists(exe)); Assert.Equal(2,loaded.Apps.Count);
            Assert.True(File.Exists(file+".bak"));
        } finally {Directory.Delete(dir,true);}
    }
    [Fact] public void UnsupportedLibraryIsPreserved()
    {
        var file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json");
        const string original="{\"Version\":99,\"SelectedId\":\"gemini\",\"Apps\":[]}";
        File.WriteAllText(file,original);
        try {var c=new SoftwareCatalog(file,"Gemini"); Assert.NotNull(c.Error);
            Assert.Throws<IOException>(()=>c.Select("chatgpt")); Assert.Equal(original,File.ReadAllText(file));
        } finally {File.Delete(file);}
    }
}
