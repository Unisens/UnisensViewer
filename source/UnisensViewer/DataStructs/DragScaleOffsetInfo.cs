namespace UnisensViewer
{
	public class DragScaleOffsetInfo
	{
		public DragScaleOffsetInfo(RenderSlice renderslice)
		{
			this.Renderslice = renderslice;
			this.Dragscale = renderslice.Scale;
			this.Dragoffset = renderslice.Offset;
		}

		public RenderSlice Renderslice { get; set; }
		
		public float Dragscale { get; set; }
		
		public float Dragoffset { get; set; }
	}
}

