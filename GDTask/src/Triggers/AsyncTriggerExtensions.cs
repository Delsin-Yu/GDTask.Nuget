using System;
using Godot;
using GodotTask.Internal;

namespace GodotTask.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
		internal static T GetChild<T>(this Node node, bool includeRoot = true)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (includeRoot && node is T castedRoot)
				return castedRoot;
			else
			{
				foreach (Node child in node.GetChildren())
					if (child is T castedChild) return castedChild;
			}
			return default(T);
		}

		internal static T CreateChild<T>(this Node node) where T : Node, new()
		{
			T child = new T { Name = typeof(T).Name };
			node.AddChild(child);
			return child;
		}

		internal static T GetOrCreateChild<T>(this Node node) where T : Node, new()
		{
			RuntimeChecker.ThrowIfEditor();
			T child = GetChild<T>(node);
			if (child == null)
				child = CreateChild<T>(node);
			return child;
		}
    }
}

