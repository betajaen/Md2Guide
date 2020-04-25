using Markdig;
using Md2Guide.AmigaGuide;
using System;
using System.IO;

namespace Md2Guide
{
  partial class Program
  {
    /// <summary>
    /// Converts all markdown files (.md) to an AmigaGuide
    /// </summary>
    /// 
    /// <param name="input">The path to the directory of markdown files that is to be converted.</param>
    /// <param name="output">The name of the output from the conversion.</param>
    /// <param name="main">The name of the main page</param>
    static void Main(DirectoryInfo input, FileInfo output, string main = "MAIN")
    {
      if (input == null)
      {
        input = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
      }

      if (output == null)
      {
        output = new FileInfo(System.IO.Path.Combine( System.IO.Directory.GetCurrentDirectory(), "output.guide"));
      }

      GuideWriter writer = new AmigaGuide.GuideWriter();

      Console.WriteLine(input);

      foreach (var fileInfo in input.EnumerateFiles("*.md", SearchOption.AllDirectories))
      {
        string name = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name).ToUpper();

        if (main.Equals(name, StringComparison.InvariantCultureIgnoreCase))
        {
          name = "MAIN";
        }

        Node node = writer.GetNode(name);

        string source;

        using (StreamReader reader = fileInfo.OpenText())
        {
          source = reader.ReadToEnd();
        }

        Markdown.Convert(source, new AgRenderer(node));


        Console.WriteLine($"--> {fileInfo.Name} is {node.Name} \"{node.Title}\"");

      }

      writer.Save(output);

      Console.WriteLine("Saved.");

    }
  }
}
