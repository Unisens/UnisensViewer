using System.Collections.Generic;
using System.Text;

namespace UnisensViewer
{
	public class FactorAccumulator
	{
		private List<AccumulatedFactor>	factors;

		public FactorAccumulator()
		{
			this.factors = new List<AccumulatedFactor>();
		}

		public void Reset()
		{
			this.factors.Clear();
		}

		public void Accumulate(double factor)
		{
			// mit basis wird nicht gerechnet, vergleiche können also ohne ABS durchgeführt werden
			AccumulatedFactor f = this.factors.Find(l => l.Factor == factor);

			if (f != null)
			{
				++f.Exponent;
			}
			else
			{
				this.factors.Add(new AccumulatedFactor(factor));
			}
		}

		public string FormatPositive()
		{
			StringBuilder sb = new StringBuilder();

			foreach (AccumulatedFactor f in this.factors)
			{
				sb.Append(f.Factor);
				sb.Append("^");
				sb.Append(f.Exponent);
				sb.Append("\u00b7");
			}

			if (sb.Length > 0)
			{
				--sb.Length;
			}

			return sb.ToString();
		}

		public string FormatNegative()
		{
			StringBuilder sb = new StringBuilder();

			foreach (AccumulatedFactor f in this.factors)
			{
				sb.Append(f.Factor);
				sb.Append("^-");
				sb.Append(f.Exponent);
				sb.Append("\u00b7");
			}

			if (sb.Length > 0)
			{
				--sb.Length;
			}

			return sb.ToString();
		}
	}
}

