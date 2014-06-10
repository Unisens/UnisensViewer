#include "StdAfx.h"
#include "SampleReader.cli.h"

using namespace UnisensViewerClrCppLibrary;




SampleReader::SampleReader(void* mapbase, void* mapend, int channels, int samplesize)
{
	this->mapbase		= mapbase;
	this->mapend		= mapend;
	this->channels		= channels;
	samplestructsize	= channels * samplesize;

	data = gcnew array<SampleD>(channels);
}




SampleReader^ SampleReaderFactory::GetSampleReader(void* mapbase, void* mapend, StreamDataType sdt, int channels)
{
	switch (sdt)
	{
		case StreamDataType::Intel_Int8:		return gcnew SampleReader_T<signed char>(mapbase, mapend, channels);
		case StreamDataType::Intel_UInt8:		return gcnew SampleReader_T<unsigned char>(mapbase, mapend, channels);
		case StreamDataType::Intel_Int16:		return gcnew SampleReader_T<signed short>(mapbase, mapend, channels);
		case StreamDataType::Intel_UInt16:		return gcnew SampleReader_T<unsigned short>(mapbase, mapend, channels);
		case StreamDataType::Intel_Int32:		return gcnew SampleReader_T<signed int>(mapbase, mapend, channels);
		case StreamDataType::Intel_UInt32:		return gcnew SampleReader_T<unsigned int>(mapbase, mapend, channels);
		case StreamDataType::Intel_Int64:		return gcnew SampleReader_T<signed __int64>(mapbase, mapend, channels);
		case StreamDataType::Intel_UInt64:		return gcnew SampleReader_T<unsigned __int64>(mapbase, mapend, channels);
		case StreamDataType::Motorola_Int16:	return gcnew SampleReader_Motorola<short, unsigned short>(mapbase, mapend, channels);
		case StreamDataType::Motorola_UInt16:	return gcnew SampleReader_Motorola<unsigned short, unsigned short>(mapbase, mapend, channels);
		case StreamDataType::Motorola_Int32:	return gcnew SampleReader_Motorola<int, unsigned>(mapbase, mapend, channels);
		case StreamDataType::Motorola_UInt32:	return gcnew SampleReader_Motorola<unsigned, unsigned>(mapbase, mapend, channels);
		case StreamDataType::Ieee754_Single:	return gcnew SampleReader_T<float>(mapbase, mapend, channels);
		case StreamDataType::Ieee754_Double:	return gcnew SampleReader_T<double>(mapbase, mapend, channels);

		case StreamDataType::Ieee754_Quad:
		case StreamDataType::Ieee754r_Half:
		case StreamDataType::Invalid:
		default:
			throw gcnew Exception("kein SampleReader für den Datentyp implementiert");
	}

}




////////////////////////////////////////////////////////////////////////////////////////////////////




template <class T>
SampleReader_T<T>::SampleReader_T(void* mapbase, void* mapend, int channels)
	: SampleReader(mapbase, mapend, channels, sizeof(T))
{
}





template <class T>
void SampleReader_T<T>::Read(void* mapaddr)
{
	for (int a = 0; a < channels; ++a)
		data[a].value = (float)((T*)mapaddr)[a];
}





template <class T>
void SampleReader_T<T>::Peak(void* addr, void* addrend)
{
	for (int a = 0; a < channels; ++a)
	{
		data[a].min = System::Single::MaxValue;
		data[a].max = System::Single::MinValue;
	}


	while (addr < addrend && addr < mapend)	// wenn samplesperpixel < 1 ist, dann ist m == mend
	{
		// hier könnte man mit MMX/SSE die min/max parallel machen (MAXPS)

		for (int a = 0; a < channels; ++a)
		{
			data[a].value = (float)((T*)addr)[a];

			if (data[a].value < data[a].min)
				data[a].min = data[a].value;

			if (data[a].value > data[a].max)
				data[a].max = data[a].value;
		}

		//addr += samplestructsize;
		addr = (char*)addr + samplestructsize;
	}

}





// hier sollte man mal die sinc funktion mit einer tabelle approximieren (taylor), soo genau muss das ja ned sein...
// wär bestimmt um einiges schneller als dauernd die FPU-operation zu benutzen
// ...naja gut gibt noch ne menge anderes das man vielleicht zuerst optimieren könnte, z.b. nen komplett neuen nativen
// renderer bauen. d.h. aber auch die rasterrenderslices ins native c++ runterschieben, die kann man dann mit asm optimieren.
// dann kann man auch endlich templates für die renderfunktionen nutzen und kann dann den function-call-overhead
// für die samplereader in der inner loop komplett einsparen
template <class T>
void SampleReader_T<T>::Sinc(void* addr, double sampleoffs)
{
	char* m = (char*)addr;


	for (int a = 0; a < channels; ++a)
		data[a].value = 0.0f;


	double d = Math::Truncate(sampleoffs) - sampleoffs;		// abstand vom sample


	// kurz den grenzfall bei d == 0.0 checken,  sin(0)/0 = 1
	if (d >= -Double::Epsilon * 2.0)		// wir brauchen hier kein Abs wegen truncate oben, d <= 0
	{
		for (int a = 0; a < channels; ++a)
			data[a].value += (float)((T*)m)[a];

		d -= 1.0;
		m -= samplestructsize;		// erstmal nach links...
	}



	// nach links laufen, max 10 samples
	while (d >= -SINC_USESAMPLES && m >= mapbase)
	{
		double x = Math::PI * d;
		float weight = (float)(Math::Sin(x) / x);

		for (int a = 0; a < channels; ++a)
			data[a].value += (float)((T*)m)[a] * weight;

		d -= 1.0;
		m -= samplestructsize;
	}


	// nach rechts laufen

	m = (char*)addr + samplestructsize;
	d = Math::Truncate(sampleoffs) + 1.0 - sampleoffs;

	while (d <= SINC_USESAMPLES && m < mapend)
	{
		double x = Math::PI * d;
		float weight = (float)(Math::Sin(x) / x);

		for (int a = 0; a < channels; ++a)
			data[a].value += (float)((T*)m)[a] * weight;

		d += 1.0;
		m += samplestructsize;
	}

}




////////////////////////////////////////////////////////////////////////////////////////////////////




template <class T, class R>
SampleReader_Motorola<T, R>::SampleReader_Motorola(void* mapbase, void* mapend, int channels)
	: SampleReader(mapbase, mapend, channels, sizeof(T))
{
}




// hier kommt ne fiese trickserei hehe..
// aber ne polymorphe motorola-converter-klasse ist so nicht hinzukriegen, wegen unterschiedlichen parameter-typen.
// siehe Motorola.cs für die ursprünglich verwendeten funktionen die ich vor der studiarbeit (also während der diplarbeit) verwendet hab.

template <>
short SampleReader_Motorola<short, unsigned short>::MotoConv(unsigned short rawint16)
{
	return (short)((rawint16 << 8) | (rawint16 >> 8));
}

template <>
unsigned short SampleReader_Motorola<unsigned short, unsigned short>::MotoConv(unsigned short rawuint16)
{
	return (unsigned short)((rawuint16 << 8) | (rawuint16 >> 8));
}

template <>
int SampleReader_Motorola<int, unsigned>::MotoConv(unsigned rawint32)
{
	return (int)((rawint32 << 24) | (rawint32 >> 24) | (rawint32 << 8 & 0x00ff0000) | (rawint32 >> 8 & 0x0000ff00));
}

template <>
unsigned SampleReader_Motorola<unsigned, unsigned>::MotoConv(unsigned rawuint32)
{
	return (unsigned)((rawuint32 << 24) | (rawuint32 >> 24) | (rawuint32 << 8 & 0x00ff0000) | (rawuint32 >> 8 & 0x0000ff00));
}






template <class T, class R>
void SampleReader_Motorola<T, R>::Read(void* mapaddr)
{
	for (int a = 0; a < channels; ++a)
		data[a].value = (float)MotoConv(((R*)mapaddr)[a]);
}





template <class T, class R>
void SampleReader_Motorola<T, R>::Peak(void* addr, void* addrend)
{
	for (int a = 0; a < channels; ++a)
	{
		data[a].min = System::Single::MaxValue;
		data[a].max = System::Single::MinValue;
	}


	while (addr < addrend && addr < mapend)	// wenn samplesperpixel < 1 ist, dann ist m == mend
	{
		// hier könnte man mit MMX/SSE die min/max parallel machen (MAXPS)

		for (int a = 0; a < channels; ++a)
		{
			data[a].value = (float)MotoConv(((R*)addr)[a]);

			if (data[a].value < data[a].min)
				data[a].min = data[a].value;

			if (data[a].value > data[a].max)
				data[a].max = data[a].value;
		}

		//addr += samplestructsize;
		addr = (char*)addr + samplestructsize;
	}

}





template <class T, class R>
void SampleReader_Motorola<T, R>::Sinc(void* addr, double sampleoffs)
{
	char* m = (char*)addr;


	for (int a = 0; a < channels; ++a)
		data[a].value = 0.0f;


	double d = Math::Truncate(sampleoffs) - sampleoffs;		// abstand vom sample


	// kurz den grenzfall bei d == 0.0 checken,  sin(0)/0 = 1
	if (d >= -Double::Epsilon * 2.0)		// wir brauchen hier kein Abs wegen truncate oben, d <= 0
	{
		for (int a = 0; a < channels; ++a)
			data[a].value += (float)MotoConv(((R*)m)[a]);

		d -= 1.0;
		m -= samplestructsize;		// erstmal nach links...
	}



	// nach links laufen, max 10 samples
	while (d >= -SINC_USESAMPLES && m >= mapbase)
	{
		double x = Math::PI * d;
		float weight = (float)(Math::Sin(x) / x);

		for (int a = 0; a < channels; ++a)
			data[a].value += (float)MotoConv(((R*)m)[a]) * weight;

		d -= 1.0;
		m -= samplestructsize;
	}


	// nach rechts laufen

	m = (char*)addr + samplestructsize;
	d = Math::Truncate(sampleoffs) + 1.0 - sampleoffs;

	while (d <= SINC_USESAMPLES && m < mapend)
	{
		double x = Math::PI * d;
		float weight = (float)(Math::Sin(x) / x);

		for (int a = 0; a < channels; ++a)
			data[a].value += (float)MotoConv(((R*)m)[a]) * weight;

		d += 1.0;
		m += samplestructsize;
	}

}

