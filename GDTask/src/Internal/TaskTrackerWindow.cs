using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace GodotTask;

internal partial class TaskTrackerWindow : Window
{
    private static TaskTrackerWindow _instance;
    private static StringBuilder _stringBuilder;
    
#if !NET7_0_OR_GREATER
    private static readonly Regex _removeHrefLabel = new("<a href.+>(.+)</a>", RegexOptions.Compiled);
#endif
    
#if NET7_0_OR_GREATER
    [GeneratedRegex("<a href.+>(.+)</a>")]
    private static partial Regex GetRemoveHrefLabelRegex();
#endif
   
    private static Regex RemoveHrefLabelRegex()
    {
#if NET7_0_OR_GREATER
        return GetRemoveHrefLabelRegex();
#else
        return _removeHrefLabel;
#endif
    }
    
    internal static void Launch()
    {
        if (_instance != null)
        {
            _instance.Visible = true;
            _instance.ProcessMode = ProcessModeEnum.Always;
            return;
        }

        _instance = CreateInstance();

        foreach (var trackingData in TaskTracker.GetAllExistingTrackingData())
        {
            TryAddItem(trackingData);
        }
        
        var window = ((SceneTree)Engine.GetMainLoop()).Root;
        window.CallDeferred(MethodName.AddChild, _instance);
        window.CallDeferred(MethodName.MoveChild, _instance, 0);
    }

    internal static void TryAddItem(TaskTracker.TrackingData trackingData)
    {
        if(_instance is null) return;
        lock (_instance)
        {
            var trackingDataMap = _instance._trackingDataMap;
            var treeItemMap = _instance._treeItemMap;
            var tree = _instance._tree;
            TreeItem treeItem;
            if (!trackingDataMap.TryGetValue(trackingData, out treeItem))
            {
                treeItem = tree.CreateItem(_instance._rootTreeItem, trackingData.TrackingId);
                trackingDataMap.Add(trackingData, treeItem);
                treeItemMap.Add(treeItem, trackingData);
            }
            UpdateTreeItem(trackingData, treeItem);
        }
    }

    internal static void TryRemoveItem(TaskTracker.TrackingData trackingData)
    {
        if(_instance is null) return;
        lock (_instance)
        {
            var trackingDataMap = _instance._trackingDataMap;
            var treeItemMap = _instance._treeItemMap;
            
            if (!trackingDataMap.Remove(trackingData, out var treeItem)) return;
            treeItemMap.Remove(treeItem);
            
            treeItem.Free();
        }
    }

    internal static void UpdateTreeItem(TaskTracker.TrackingData trackingData, TreeItem treeItem)
    {
        treeItem.SetText(0, trackingData.FormattedType);
        treeItem.SetText(3, GetFirstLine(trackingData.StackTrace));
        return;

        static string GetFirstLine(string str)
        {
            _stringBuilder ??= new();
            var span = str.AsSpan();
            foreach (var spanChar in span)
            {
                if (spanChar is '\r' or '\n') break;
                _stringBuilder.Append(spanChar);
            }

            var constructed = _stringBuilder.ToString();
            _stringBuilder.Clear();
            
            return RemoveHrefLabelRegex().Replace(constructed, "$1");
        }
    }
    
    private static TaskTrackerWindow CreateInstance()
    {
        var tree = new Tree
        {
            SizeFlagsHorizontal = Control.SizeFlags.Fill,
            SizeFlagsVertical = Control.SizeFlags.Fill,
            SelectMode = Tree.SelectModeEnum.Row,
            Columns = 4,
            ColumnTitlesVisible = true,
            HideRoot = true
        };
        var textEdit = new TextEdit { Editable = false };
  
        var instance = new TaskTrackerWindow(tree, textEdit)
        {
            AlwaysOnTop = true,
            InitialPosition = WindowInitialPosition.CenterScreenWithMouseFocus,
            Mode = ModeEnum.Windowed,
            WrapControls = true,
            Visible = true,
            ContentScaleMode = ContentScaleModeEnum.Viewport,
            Title = "GDTask Tracker",
            ProcessMode = ProcessModeEnum.Always,
            Size = new(600, 600)
        };
        {
            var panelContainer = new PanelContainer()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            {
                var marginContainer = new MarginContainer();
                const int margin = 10;
                marginContainer.AddThemeConstantOverride("margin_top", margin);
                marginContainer.AddThemeConstantOverride("margin_left", margin);
                marginContainer.AddThemeConstantOverride("margin_bottom", margin);
                marginContainer.AddThemeConstantOverride("margin_right", margin);
                {
                    var vBox = new VBoxContainer
                    {
                        Alignment = BoxContainer.AlignmentMode.Begin
                    };
                    {
                        var toolbar = new HBoxContainer();
                        {
                            var enableTrackingToggle = new CheckButton
                            {
                                Text = "Enable Tracking"
                            };
                            var enableStackTraceToggle = new CheckButton
                            {
                                Text = "Enable StackTrace"
                            };

                            enableTrackingToggle.Toggled += pressed => TaskTracker.EnableTracking = pressed;
                            enableStackTraceToggle.Toggled += pressed => TaskTracker.EnableStackTrace = pressed;

                            TaskTracker._enableTracking.Subscribe(new CheckButtonObserver(enableTrackingToggle));
                            TaskTracker._enableStackTrace.Subscribe(new CheckButtonObserver(enableStackTraceToggle));

                            var spacer = new Control
                            {
                                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                            };

                            var gcButton = new Button()
                            {
                                Text = "GC Collect"
                            };

                            gcButton.Pressed += GC.Collect;
                            
                            toolbar.AddChild(enableTrackingToggle);
                            toolbar.AddChild(enableStackTraceToggle);
                            toolbar.AddChild(spacer);
                            toolbar.AddChild(gcButton);
                        }
                        vBox.AddChild(toolbar);
                    }
                    {
                        var hSeparator = new HSeparator()
                        {
                            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                        };
                        vBox.AddChild(hSeparator);
                    }
                    {
                        var vSplitContainer = new VSplitContainer
                        {
                            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                            SplitOffset = 350
                        };
                        {
                            tree.SetColumnTitle(0, "Task Type");
                            tree.SetColumnTitle(1, "Elapsed");
                            tree.SetColumnTitle(2, "Status");
                            tree.SetColumnTitle(3, "Position");
                            
                            tree.SetColumnTitleAlignment(0, HorizontalAlignment.Left);
                            tree.SetColumnTitleAlignment(1, HorizontalAlignment.Left);
                            tree.SetColumnTitleAlignment(2, HorizontalAlignment.Left);
                            tree.SetColumnTitleAlignment(3, HorizontalAlignment.Left);
                            
                            tree.SetColumnClipContent(3, true);
                            
                            tree.ItemSelected += () =>
                            {
                                var item = tree.GetSelected();
                                if (!instance._treeItemMap.TryGetValue(item, out var trackingData)) return;
                                instance._activeTreeItem = item;
                                textEdit.Text = trackingData.StackTrace;
                            };

                            vSplitContainer.AddChild(tree);
                            vSplitContainer.AddChild(textEdit);
                        }
                        
                        vBox.AddChild(vSplitContainer);
                    }
                    marginContainer.AddChild(vBox);
                }
                panelContainer.AddChild(marginContainer);
            }

            instance.AddChild(panelContainer);
            panelContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        }
        return instance;
    }

    private readonly Tree _tree;
    private readonly TreeItem _rootTreeItem;
    private readonly TextEdit _textEdit;
    private readonly Dictionary<TaskTracker.TrackingData, TreeItem> _trackingDataMap = new();
    private readonly Dictionary<TreeItem, TaskTracker.TrackingData> _treeItemMap = new();

    private TreeItem _activeTreeItem;
    
    public TaskTrackerWindow(Tree tree, TextEdit textEdit)
    {
        _tree = tree;
        _textEdit = textEdit;
        _rootTreeItem = _tree.CreateItem();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Visible = false;
            ProcessMode = ProcessModeEnum.Disabled;
        }
        base._Notification(what);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        foreach (var (trackingData, treeItem) in _trackingDataMap)
        {
            if(!IsInstanceValid(treeItem)) continue;
            treeItem.SetText(1, (DateTime.UtcNow - trackingData.AddTime).TotalSeconds.ToString("N2"));
            treeItem.SetText(2, trackingData.StatusProvider().ToString());
        }
    }
}