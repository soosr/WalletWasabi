using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.ReactiveUI;

namespace WalletWasabi.Fluent.Controls.EnhancedScrollViewer;

public abstract class ScrollViewerExpansionStrategy : IDisposable
{
	public abstract void Dispose();

	protected static IDisposable ExpandOnHover(ScrollBar scrollBar, AvaloniaObject mouseTarget)
    {
        var isExpandedProperty = typeof(ScrollBar).GetProperty("IsExpanded");

        if (isExpandedProperty is null)
        {
            return Disposable.Empty;
        }

        var hideAfter = scrollBar.GetValue(Expansion.HideAfterProperty);
        var pointerEnter = Observable.FromEventPattern(mouseTarget, "PointerEntered").Select(_ => Observable.Return(true));
        var pointerExit = Observable.FromEventPattern(mouseTarget, "PointerExited").Select(_ => Observable.Return(false).Delay(hideAfter, AvaloniaScheduler.Instance));

        var isExpanded = pointerEnter.Merge(pointerExit).Switch();

        return isExpanded
            .Do(b => isExpandedProperty.SetValue(scrollBar, b))
            .Subscribe();
    }
}
