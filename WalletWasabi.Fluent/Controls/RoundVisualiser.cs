using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace WalletWasabi.Fluent.Controls;

public class RoundVisualiser : Control
{
	private decimal[] _inputs;
	private decimal[] _outputs;
	private double _lastStrokeThickness;
	private double _lastOutputsStrokeThickness;
	private IPen _linePen;
	private IPen _outputsPen;

	private IBrush _inputsBrush = SolidColorBrush.Parse("#7FF7C01F");
	private IBrush _outPutsBrush = SolidColorBrush.Parse("#7F26C01F");

	public static readonly DirectProperty<RoundVisualiser, decimal[]> InputsProperty =
		AvaloniaProperty.RegisterDirect<RoundVisualiser, decimal[]>(
			nameof(Inputs),
			o => o.Inputs,
			(o, v) => o.Inputs = v);

	public static readonly DirectProperty<RoundVisualiser, decimal[]> OutputsProperty =
		AvaloniaProperty.RegisterDirect<RoundVisualiser, decimal[]>(
			nameof(Outputs),
			o => o.Outputs,
			(o, v) => o.Outputs = v);

	static RoundVisualiser()
	{
		AffectsRender<RoundVisualiser>(InputsProperty);
	}

	public decimal[] Inputs
	{
		get => _inputs;
		set => SetAndRaise(InputsProperty, ref _inputs, value);
	}

	public decimal[] Outputs
	{
		get => _outputs;
		set => SetAndRaise(OutputsProperty, ref _outputs, value);
	}

	public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (Inputs != null)
            {
	            var length = Inputs.Length;

	            var gaps = length + 1;

	            var gapSize = 1.0;

	            var binStroke = (Bounds.Width - gaps * gapSize) / (length);
	            binStroke = Math.Floor(binStroke);

	            if (_lastStrokeThickness != binStroke)
	            {
		            _lastStrokeThickness = binStroke;
		            _linePen = new Pen(_inputsBrush, _lastStrokeThickness);
	            }

	            var x = binStroke / 2 + gapSize;

	            for (var i = 0; i < length; i++)
	            {
		            context.DrawLine(_linePen, new Point(x, Bounds.Height),
			            new Point(x,
				            Bounds.Height * (1 - (double)Inputs[i])));
		            x += binStroke + gapSize;
	            }
            }

            if (Outputs != null)
            {
	            var length = Outputs.Length;

	            var gaps = length + 1;

	            var gapSize = 1.0;

	            if ((gaps * gapSize) > Bounds.Width)
	            {
		            gapSize = 1;
	            }

	            var binStroke = (Bounds.Width - gaps * gapSize) / (length);
	            binStroke = Math.Floor(binStroke);

	            if (_lastOutputsStrokeThickness != binStroke)
	            {
		            _lastOutputsStrokeThickness = binStroke;
		            _outputsPen = new Pen(_outPutsBrush, _lastOutputsStrokeThickness);
	            }

	            var x = binStroke / 2 + gapSize;

	            for (var i = 0; i < length; i++)
	            {
		            context.DrawLine(_outputsPen, new Point(x, Bounds.Height),
			            new Point(x,
				            Bounds.Height * (1 - (double)Outputs[length - 1 - i])));
		            x += binStroke + gapSize;
	            }
            }
        }
}