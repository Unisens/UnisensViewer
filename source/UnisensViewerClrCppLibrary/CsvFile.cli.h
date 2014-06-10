#include <windows.h>
#include <vcclr.h>



namespace UnisensViewerClrCppLibrary
{

	public ref class CsvFile
	{
	public:
		float*	data;
		int		samples;

		CsvFile(System::String^ filepath, int channels, wchar_t delimiter, wchar_t decimalSeparator);
		~CsvFile();
	};

}

