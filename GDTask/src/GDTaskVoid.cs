#pragma warning disable CS1591
#pragma warning disable CS0436
#pragma warning disable CA1822

using System.Runtime.CompilerServices;
using GodotTask.CompilerServices;

namespace GodotTask
{
    [AsyncMethodBuilder(typeof(AsyncGDTaskVoidMethodBuilder))]
    public readonly struct GDTaskVoid
    {
        public void Forget()
        {
        }
    }
}

