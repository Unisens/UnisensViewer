using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnisensViewer
{
	// zwischen zwei einheiten MUSS mindestens eine zahl stehen!
	//  D 24 H 60 M 60 s 1000 ms 10
	//  D 24 H 60 M 60 s 10 10 10 ms
	//  10 H 60 M 60 s 100 h 10 m 10
	//  10 J 1.602E19 eV 10
	////conversionlist = new object[] { "H", 60.0, "M", 60.0, "s", 10.0, 100.0, 10.0 };

	public class Scale
	{
		private object[] scale;
		private int unitindex;

		private double range;
		private string unit;
		private double normalizedrange;
		private string normalizedfactors;
		private string normalizedunit;
		private double upticks;
		private double downticks;
		private double prettyfactor;
		private string prettyunit;
		
		/// <summary>
		/// Erstellt ein neues Scale-Objekt mit allen Informationen zur Skalierung.
		/// </summary>
		/// <param name="scale">Skalierungs-Parameter</param>
		/// <param name="unit">Einheit</param>
		public Scale(object[] scale, string unit)
		{
			this.scale = scale;
			this.unit = unit;

			// Position der Einheit suchen
			this.unitindex = -1;

			for (int a = 0, b = scale.Length; a < b; ++a)
			{
				// unit-definition in settings jetzt mit mehreren synonymen einheiten
				if (this.scale[a] is string)
				{
					// nicht ((string)scale[a]).Contains(unit) verwenden!
					// suche nach s liefert auch ms!
					string[] units = ((string)this.scale[a]).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

					if (units.Contains(this.unit))
					{
						this.unitindex = a;
					}
				}
			}

			if (this.unitindex == -1)
			{
				throw new Exception("Einheit " + this.unit + " ungültig.");
			}

			this.range = 0.0;
			this.normalizedrange = 0.0;
			this.normalizedfactors = null;
			this.normalizedunit = this.unit;
			this.upticks = 0.0;
			this.downticks = 0.0;

			this.prettyfactor = 0.0;
			this.prettyunit = null;
		}

		public double Range
		{
			get
			{
				return this.range;
			}
			
			set
			{
				this.range = value;

				if (this.range != 0.0)
				{
					if (Math.Abs(this.range) <= 1.0)
					{
						this.NormalizeDown();
					}
					else
					{
						this.NormalizeUp();
					}
				}
			}
		}

		public string Unit
		{
			get { return this.unit; }
		}

		public double NormalizedRange
		{
			get { return this.normalizedrange; }
		}

		public string NormalizedFactors
		{
			get { return this.normalizedfactors; }
		}

		public string NormalizedUnit
		{
			get { return this.normalizedunit; }
		}

		public double UpTicks
		{
			get { return this.upticks; }
		}

		public double DownTicks
		{
			get { return this.downticks; }
		}

		public double PrettyFactor
		{
			get { return this.prettyfactor; }
		}

		public string PrettyUnit
		{
			get { return this.prettyunit; }
		}

		private string GetFirstSynonymousUnit(string unitsynonymstring)
		{
			int a = unitsynonymstring.IndexOf(' ');

			return a < 0 ? unitsynonymstring : unitsynonymstring.Substring(0, a);
		}
		
		private void NormalizeUp()
		{
			FactorAccumulator	faccu = new FactorAccumulator();
			int					a, b, u;
			double				v, factor, lastfactor;

			v = this.range;
			b = this.scale.Length - 1;
			a = this.unitindex;
			u = this.unitindex;

			lastfactor = this.unitindex < b ? (double)this.scale[this.unitindex + 1] : 0.0;

			if (--a < 0)	
			{
				// stop, wenn links von der einheit kein faktor mehr
				this.normalizedrange = v;
				this.normalizedfactors = string.Empty;
				this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[this.unitindex]);	
				this.upticks = 0.0;
				this.downticks = lastfactor;

				this.prettyfactor = 0.0;
				this.prettyunit = null;
				return;
			}

			factor = (double)this.scale[a];

			while (Math.Abs(v) >= factor && a > 0)
			{
				v /= factor;
				faccu.Accumulate(factor);

				--a;

				if (this.scale[a] is string)
				{
					u = a;
					faccu.Reset();

					if (--a < 0)	
					{
						// stop, wenn links von der einheit kein faktor mehr
						this.normalizedrange = v;
						this.normalizedfactors = string.Empty;
						this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
						this.upticks = 0.0;
						this.downticks = factor;

						this.prettyfactor = 0.0;
						this.prettyunit = null;
						return;
					}
				}

				lastfactor = factor;
				factor = (double)this.scale[a];
			}

			this.prettyfactor = 0.0;
			this.prettyunit = null;

			if (Math.Abs(v) >= factor) 
			{
                // Umgeht Exponentenschreibweise bei kleiner 1000
                if (Math.Abs(v) <= 10000)
                {
					this.prettyfactor = 1.0;
					this.prettyunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
                }

				// den letzten faktor ganz links durchteilen solange es geht
				while (Math.Abs(v) >= factor)
				{
					v /= factor;
					faccu.Accumulate(factor);
					this.prettyfactor *= factor;
				}

				lastfactor = factor;
			}

			this.normalizedrange = v;
			this.normalizedfactors = faccu.FormatPositive();
			this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
			this.upticks = factor;
			this.downticks = lastfactor;
		}

		private void NormalizeDown()
		{
			FactorAccumulator	faccu = new FactorAccumulator();
			int					a, b, u;
			double				v, factor, lastfactor;

			v = this.range;
			b = this.scale.Length - 1;
			a = this.unitindex;
			u = this.unitindex;

			lastfactor = this.unitindex > 0 ? (double)this.scale[this.unitindex - 1] : 0.0;

			if (++a > b)	
			{
				// stop, wenn rechts von der einheit kein faktor mehr
				this.normalizedrange = v;
				this.normalizedfactors = string.Empty;
				this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[this.unitindex]);
				this.upticks = lastfactor;
				this.downticks = 0.0;

				this.prettyfactor = 0.0;
				this.prettyunit = null;
				return;
			}

			factor = (double)this.scale[a];

			while (Math.Abs(v) < 1.0 && a < b)
			{
				v *= factor;
				faccu.Accumulate(factor);

				++a;

				if (this.scale[a] is string)
				{
					u = a;
					faccu.Reset();

					if (++a > b)	
					{
						// stop, wenn rechts von der einheit kein faktor mehr
						this.normalizedrange = v;
						this.normalizedfactors = string.Empty;
						this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
						this.upticks = factor;
						this.downticks = 0.0;

						this.prettyfactor = 0.0;
						this.prettyunit = null;
						return;
					}
				}

				lastfactor = factor;
				factor = (double)this.scale[a];
			}

			this.prettyfactor = 0.0;
			this.prettyunit = null;
            
			if (Math.Abs(v) < 1.0) 
			{
                // Umgeht Exponentenschreibweise bei größer 0.001
                if (Math.Abs(v) > 0.001)
                {
					this.prettyfactor = 1.0;
					this.prettyunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
                }

				// den letzten faktor ganz rechts durchteilen solange es geht
				while (Math.Abs(v) < 1.0)
				{
					v *= factor;
					faccu.Accumulate(factor);
					this.prettyfactor /= factor;
				}

				lastfactor = factor;
			}
			else
			{
				// schleife oben abgebrochen, weil fertig formatiert, d.h. value >= 1.0

				// PRETTY PRINT
				// ----------------------------------------------------------------
				// man könnte schaun, ob es noch eine weitere kleinere einheit gibt
				// und die zahl mit dieser einheit ausgeben.
				// für solche conv-strings: x 10 10 10 z
				// 0.242 X  =>  2.42 * 10^-1 X  =>  242 Z
				double pf = 1.0;
				double f = factor;

				while (a < b)
				{
					pf *= f;
					++a;

					if (this.scale[a] is string)	
					{
						// gab noch eine kleinere einheit, also damit ausgeben
						this.prettyunit = this.GetFirstSynonymousUnit((string)this.scale[a]);	
						this.prettyfactor = pf;
						break;
					}

					f = (double)this.scale[a];
				}
			}

			this.normalizedrange = v;
			this.normalizedfactors = faccu.FormatNegative();
			this.normalizedunit = this.GetFirstSynonymousUnit((string)this.scale[u]);
			this.upticks = lastfactor;
			this.downticks = factor;
		}
	}
}
