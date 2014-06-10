#include "StdAfx.h"
#include "CsvFile.cli.h"

using namespace System;
using namespace System::IO;
using namespace System::Globalization;
using namespace UnisensViewerClrCppLibrary;


// Diese Datei ist nur zum Parsen von CSV-SignalEntries!!!
// TODO: CSV-File wird nur als Float eingelesen. Diese Datei und StreamRenderer auf Double umstellen

 //csv datei in memory array konvertieren
CsvFile::CsvFile(String^ filepath, int channels, wchar_t delimiter, wchar_t decimalSeparator)
{
	int				a, b, c, d;
	array<wchar_t>^	delim = gcnew array<wchar_t>(1);

	CultureInfo^ MyCI = gcnew CultureInfo("en-US", false);
	NumberFormatInfo^ nfi = MyCI->NumberFormat;
	nfi->NumberDecimalSeparator = ".";
	nfi->NumberGroupSeparator = "";

	delim[0] = delimiter;

	array<String^>^ lines = File::ReadAllLines(filepath);


	b = lines->Length;
	data = (float*) malloc(b * channels * sizeof(float));


	d = 0;
	samples = 0;

	try
	{
		for (a = 0; a < b; ++a)
		{
			array<String^>^ split = lines[a]->Split(delim);

			if (split->Length < channels)
				break;

			for (c = 0; c < channels; ++c)
			{
				data[d] = Single::Parse(split[c]->Replace(decimalSeparator.ToString(), "."), nfi);
				++d;
			}
			++samples;
		}
	}
	catch(Exception^ ex)
	{

	}
}





CsvFile::~CsvFile()
{
	free(data);
}

