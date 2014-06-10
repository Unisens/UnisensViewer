namespace UnisensViewer
{
	// darf keine struct sein wegen List<>
	public class AccumulatedFactor	
	{
		public AccumulatedFactor(double factor)
		{
			this.Factor = factor;
			this.Exponent = 1;
		}
	
		public double Factor { get; set; }

		public int Exponent { get; set; }
	}
}
