using Markdig;
using Md2Guide.AmigaGuide;

namespace Md2Guide
{
  partial class Program
  {

    static void Main(string[] args)
    {

      string path = @"C:\Users\robin\Documents\Amiga\dev\AmiSCUMM\Source\.documentation\md";

      string dstPath = @"C:\Users\robin\Documents\Amiga\dev\AmiSCUMM\Source\Api.guide";

      string testPath = @"C:\Users\robin\Documents\Amiga\Systems\A1200-3141-AGA-4MBFast-Shared\Api.Guide";


      GuideWriter writer = new AmigaGuide.GuideWriter();

      foreach (var fileName in System.IO.Directory.EnumerateFiles(path, "*.md"))
      {
        string name = System.IO.Path.GetFileNameWithoutExtension(fileName);
        Node node = writer.GetNode(name);
        string source = System.IO.File.ReadAllText(fileName);
        Markdown.Convert(source, new AgRenderer(node));
      }

      writer.Save(dstPath, testPath);
    }
  }
}
