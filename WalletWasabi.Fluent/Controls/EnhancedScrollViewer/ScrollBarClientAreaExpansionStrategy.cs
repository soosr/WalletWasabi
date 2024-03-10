using System.Reactive.Disposables;
using Avalonia.Controls.Primitives;

namespace WalletWasabi.Fluent.Controls.EnhancedScrollViewer;


public class ScrollBarClientAreaExpansionStrategy : ScrollViewerExpansionStrategy
{
    private readonly CompositeDisposable _disposables = new();

    public ScrollBarClientAreaExpansionStrategy(ScrollBar scrollBar)
    {
        ExpandOnHover(scrollBar, scrollBar).DisposeWith(_disposables);
    }

    public override void Dispose()
    {
        _disposables.Dispose();
    }
}
