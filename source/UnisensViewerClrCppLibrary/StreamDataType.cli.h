#pragma once



namespace UnisensViewerClrCppLibrary
{


	public enum class StreamDataType
	{
		Invalid,

		Intel_Int8,
		Intel_UInt8,
		Intel_Int16,
		Intel_UInt16,
		Intel_Int32,
		Intel_UInt32,
		Intel_Int64,
		Intel_UInt64,

		Motorola_Int16,
		Motorola_UInt16,
		Motorola_Int32,
		Motorola_UInt32,

		Ieee754r_Half,
		Ieee754_Single,
		Ieee754_Double,
		Ieee754_Quad
	};


}

