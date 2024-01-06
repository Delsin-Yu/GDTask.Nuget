using System.Reflection;
using Chickensoft.GoDotTest;
using Godot;

namespace Fractural.Tasks.Tests;

public partial class TestRunner : Node2D
{
    public override void _Ready()
        => _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this);
}