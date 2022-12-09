using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using WalletWasabi.Fluent.Controls;

namespace WalletWasabi.Fluent.Behaviors;

public class NavBarSelectedIndicatorChildBehavior : AttachedToVisualTreeBehavior<Control>
{
	public static readonly AttachedProperty<NavBarItem> NavBarItemParentProperty =
		AvaloniaProperty.RegisterAttached<NavBarSelectedIndicatorChildBehavior, Control, NavBarItem>("NavBarItemParent");

	public static NavBarItem GetNavBarItemParent(Control element)
	{
		return element.GetValue(NavBarItemParentProperty);
	}

	public static void SetNavBarItemParent(Control element, NavBarItem value)
	{
		element.SetValue(NavBarItemParentProperty, value);
	}

	protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
	{
		if (AssociatedObject is null)
		{
			return;
		}

		var sharedState = NavBarSelectedIndicatorParentBehavior.GetParentState(AssociatedObject);
		if (sharedState is null)
		{
			Detach();
			return;
		}

		var navBarItem = GetNavBarItemParent(AssociatedObject);

		navBarItem
			.WhenAnyValue(x => x.IsSelected)
			.Skip(1)
			.DistinctUntilChanged()
			.Where(x => x)
			.ObserveOn(AvaloniaScheduler.Instance)
			.Subscribe(_ => sharedState.AnimateIndicatorAsync(AssociatedObject))
			.DisposeWith(disposable);

		if (navBarItem.IsSelected)
		{
			sharedState.SetActive(AssociatedObject);
		}
	}
}
