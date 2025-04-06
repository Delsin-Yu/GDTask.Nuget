using System.Threading;
using GdUnit4;

namespace GodotTask.Tests;

public class GDTaskTest_CancellationTokenEqualityComparer
{
    [TestCase]
    public static void CancellationTokenEqualityComparer_None()
    {
        var result = CancellationTokenEqualityComparer.Default.Equals(CancellationToken.None, CancellationToken.None);
        Assertions.AssertThat(result).IsTrue();
    }
    
    [TestCase]
    public static void CancellationTokenEqualityComparer_Valid()
    {
        using var source = new CancellationTokenSource();
        var result = CancellationTokenEqualityComparer.Default.Equals(source.Token, source.Token);
        Assertions.AssertThat(result).IsTrue();
        source.Cancel();
        result = CancellationTokenEqualityComparer.Default.Equals(source.Token, source.Token);
        Assertions.AssertThat(result).IsTrue();
    }
}