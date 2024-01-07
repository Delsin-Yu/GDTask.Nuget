using System;
using System.Threading;
using Fractural.Tasks.Triggers;
using Godot;

namespace Fractural.Tasks
{
    public static class GDTaskCancellationExtensions
    {
        /// <summary>This CancellationToken is canceled when the Node will be destroyed.</summary>
        public static CancellationToken GetCancellationTokenOnDestroy(this Node node)
        {
            return node.GetAsyncPredeleteTrigger().CancellationToken;
        }
    }
}

namespace Fractural.Tasks.Triggers
{
    public static partial class AsyncTriggerExtensions
    {
		internal static T GetImmediateChild<T>(this Node node, bool includeRoot = true)
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

		internal static T AddImmediateChild<T>(this Node node) where T : Node, new()
		{
			T child = new T();
			node.AddChild(child);
			return child;
		}

		internal static T GetOrAddImmediateChild<T>(this Node node) where T : Node, new()
		{
			T child = GetImmediateChild<T>(node);
			if (child == null)
				child = AddImmediateChild<T>(node);
			return child;
		}

		/// <summary>
		/// Creates a task that will complete when the <see cref="Node"/> is receiving <see cref="Node.NotificationPredelete"/>
		/// </summary>
		public static GDTask OnDestroyAsync(this Node node)
        {
            return node.GetAsyncPredeleteTrigger().OnPredeleteAsync();
        }

		/// <summary>
		/// Creates a task that will complete when the <see cref="Node._Ready"/> is called
		/// </summary>
        public static GDTask OnReadyAsync(this Node node)
        {
            return node.GetAsyncStartTrigger().OnReadyAsync();
        }

        /// <summary>
        /// Creates a task that will complete when the <see cref="Node._EnterTree"/> is called
        /// </summary>
        public static GDTask OnEnterTreeAsync(this Node node)
        {
            return node.GetAsyncEnterTreeTrigger().OnEnterTreeAsync();
        }
    }
}

