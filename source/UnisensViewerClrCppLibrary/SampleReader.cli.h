#pragma once

#include "MapFile.cli.h"
#include "StreamDataType.cli.h"
using namespace System;



namespace UnisensViewerClrCppLibrary
{

	const double SINC_USESAMPLES = 10.0;




	public value struct SampleD // 242 ftw! ;)
	{
		float value;	// bei peakdaten das letzte sample
		float min;
		float max;
	};




	public ref class SampleReader abstract
	{
	protected:
		int					channels;
		void*				mapbase;
		void*				mapend;
		int					samplestructsize;

	public:
		array<SampleD>^		data;

		

		SampleReader(void* mapbase, void* mapend, int channels, int samplesize);
		virtual void Read(void* addr) = 0;
		virtual void Peak(void* addr, void* addrend) = 0;
		virtual void Sinc(void* addr, double sampleoffs) = 0;
	};





	template <class T>
	public ref class SampleReader_T : SampleReader
	{
	public:

		SampleReader_T(void* mapbase, void* mapend, int channels);
		virtual void Read(void* addr) override;
		virtual void Peak(void* addr, void* addrend) override;
		virtual void Sinc(void* addr, double sampleoffs) override;
	};




	template <class T, class R>
	public ref class SampleReader_Motorola : SampleReader
	{
	public:

		SampleReader_Motorola(void* mapbase, void* mapend, int channels);
		virtual void Read(void* addr) override;
		virtual void Peak(void* addr, void* addrend) override;
		virtual void Sinc(void* addr, double sampleoffs) override;

		T MotoConv(R rawvalue);
	};





	public ref class SampleReaderFactory
	{
	public:

		static SampleReader^ GetSampleReader(void* mapbase, void* mapend, StreamDataType sdt, int channels);
	};


}

