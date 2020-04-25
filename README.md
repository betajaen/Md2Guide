Md2Guide
========

Version 0.1

Written by Robin Southern https://github.com/betajaen

About
-----

This is a simple program that converts a directory of markdown files (.md) to an AmigaGuide file.


Usage
-----

Amiga2Markdown requires .NET Core 3.1 runtimes, so runs on Windows, Linux and MacOS.

It is launched through the console

```
    Md2Guide.exe --input inputpath --output Documentation.guide
```

Supported Features
------------------

* Headings
* Paragraphs
* AmigaGuide links - Links are provided via `[Title](guide://name)`
* Bold and Italics
* Lists
* Code Blocks (with limited syntax highlighting)
* A table of contents node is generated automatically