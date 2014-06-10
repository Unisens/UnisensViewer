#pragma once

#include <windows.h>
#include <vcclr.h>

using namespace System;


// IMAGE_FILE_LARGE_ADDRESS_AWARE


namespace UnisensViewerClrCppLibrary
{


	public ref class MapFile
	{
		HANDLE					file;
		HANDLE					mapping;

	public:
		static System::Int64	granularity;
		static System::Int64	granularitymask;	// fileoffset beim mapping geht nur in dwAllocGranularity!
		System::Int64			filesize;
		PVOID					mapbase;




		static MapFile()
		{
			SYSTEM_INFO sysinfo;
			GetSystemInfo(&sysinfo);
			granularity = sysinfo.dwAllocationGranularity;
			granularitymask = ~(granularity - 1);
		}





		MapFile(String^ filepath, bool readonly)
		{
			pin_ptr<const wchar_t>	filepathw = PtrToStringChars(filepath);
			pin_ptr<System::Int64> pfilesize = &filesize;

			file = INVALID_HANDLE_VALUE;
			mapping = 0;
			mapbase = 0;


			if (readonly)
				file = CreateFile((LPCWSTR) filepathw, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, 0);
			else
				file = CreateFile((LPCWSTR) filepathw, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, 0);

			if (file == INVALID_HANDLE_VALUE)
			{
				CleanUp();
				throw gcnew Exception("Konnte Datei " + filepath + " nicht öffnen.");
			}

			if (!GetFileSizeEx(file, (PLARGE_INTEGER) pfilesize))
			{
				CleanUp();
				throw gcnew Exception("GetFileSizeEx(" + filepath + ")");
			}


			if (readonly)
				mapping = CreateFileMapping(file, 0, PAGE_READONLY, 0, 0, 0);
			else
				mapping = CreateFileMapping(file, 0, PAGE_READWRITE, 0, 0, 0);

			if (mapping == 0)
			{
				CleanUp();
				throw gcnew Exception("CreateFileMapping(" + filepath + ")");
			}


			if (readonly)
				mapbase = MapViewOfFile(mapping, FILE_MAP_READ, 0, 0, 0);
			else
				mapbase = MapViewOfFile(mapping, FILE_MAP_ALL_ACCESS, 0, 0, 0);

			if (mapbase == 0)
			{
				DWORD lastError = GetLastError();
				CleanUp();
				throw gcnew Exception("MapViewOfFile(" + filepath + ") Error" + lastError);
			}


			return;
		}




		void CleanUp()
		{
			if (mapbase)
				UnmapViewOfFile(mapbase);
			CloseHandle(mapping);
			CloseHandle(file);
		}




		~MapFile()
		{
			CleanUp();
		}




		void MemMove(int dstoffs, int srcoffs, int bytes)
		{
			char* dst = (char*)mapbase + dstoffs;
			char* src = (char*)mapbase + srcoffs;

			memmove(dst, src, bytes);
		}


	};


}

