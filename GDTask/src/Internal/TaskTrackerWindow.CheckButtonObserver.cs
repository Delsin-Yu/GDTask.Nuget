using System;
using Godot;

namespace GodotTask;

internal partial class TaskTrackerWindow
{
    private class CheckButtonObserver : IObserver<bool>
    {
        private readonly CheckButton _checkButton;

        public CheckButtonObserver(CheckButton checkButton) => _checkButton = checkButton;

        public void OnCompleted() { }

        public void OnError(Exception error) => GD.PrintErr(error.ToString());

        public void OnNext(bool value)
        {
            if(_checkButton.ButtonPressed == value) return;
            _checkButton.SetPressedNoSignal(value);
        }
    }
}