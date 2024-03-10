using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;

namespace WalletWasabi.Fluent.Controls.EnhancedScrollViewer;

public class ScrollViewerClientAreaExpansionStrategy : ScrollViewerExpansionStrategy
{
    private readonly CompositeDisposable disposables = new();

    public ScrollViewerClientAreaExpansionStrategy(ScrollBar scrollBar)
    {
        if (scrollBar.TemplatedParent is not null)
        {
            ExpandOnHover(scrollBar, scrollBar.TemplatedParent).DisposeWith(disposables);
        }
    }

    public override void Dispose()
    {
        disposables.Dispose();
    }
}