# Building UnisensViewer

## Requirements

- Visual Studio IDE Community >= 2017(needed components: .NET Desktop Development, Desktop Devlopment with C++)
- Java JDK
- ant
- Advanced Installer >= V11.2.1

## Instructions

- In ```builder\build.properties``` set
  - path for Visual Studio and Advanced Installer
- **Version info is taken from git and written into several files during build. Be sure to revert all these files before/after each build.**
- Run build script with ```ant```
