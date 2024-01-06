using Godot;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.GoDotLog;
using Chickensoft.GoDotTest;
using Shouldly;

namespace Fractural.Tasks.Tests
{
	public class Test : TestClass
	{
		public Test(Node testScene) : base(testScene) { }

		private readonly ILog _log = new GDLog(nameof(Test));

		[Test]
		public async Task Delay3Seconds()
		{
			_log.Print("Pre delay");
			var timer = new Stopwatch();
			timer.Start();

			await GDTask.Delay(TimeSpan.FromSeconds(3));

			var elapsed = timer.Elapsed.TotalSeconds;

			elapsed.ShouldBeInRange(2, 4);

			_log.Print($"Post delay after ~3 seconds ({elapsed:N2})");
		}

		[Test]
		public async Task RunWithResult()
		{
			const string resultString = "Hello";
			_log.Print("Pre RunWithResult");
			string result = await Impl();
			result.ShouldBe(resultString);
			_log.Print($"Post got result: {result}");
			return;

			static async GDTask<string> Impl()
			{
				await GDTask.Delay(TimeSpan.FromSeconds(2));
				return resultString;
			}
		}

		[Test]
		public async Task LongTaskWithCancellation()
		{
			_log.Print("LongTask started");
			var cts = new CancellationTokenSource();
			Impl(cts.Token).Forget();
			await GDTask.Delay(TimeSpan.FromSeconds(3));
			cts.Cancel();
			_log.Print("LongTask cancelled");
			return;

			async GDTaskVoid Impl(CancellationToken cancellationToken)
			{
				int seconds = 10;
				_log.Print($"Starting long task ({seconds} seconds long).");
				for (int i = 0; i < seconds; i++)
				{
					_log.Print($"Working on long task for {i} seconds...");
					await GDTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken).SuppressCancellationThrow();
				}

				_log.Print("Finished long task.");
			}
		}

		[Test]
		public async Task WaitForEndOfFrame()
		{
			_log.Print("Pre WaitForEndOfFrame");
			await GDTask.WaitForEndOfFrame();
			_log.Print("Post WaitForEndOfFrame");
		}

		[Test]
		public async Task WaitForPhysicsProcess()
		{
			_log.Print("Pre WaitForPhysicsProcess");
			await GDTask.WaitForPhysicsProcess();
			_log.Print("Post WaitForPhysicsProcess");
		}

		[Test]
		public async Task NextFrame()
		{
			_log.Print("Pre NextFrame");
			await GDTask.NextFrame();
			_log.Print("Post NextFrame");
		}
	}
}
