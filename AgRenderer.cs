using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Md2Guide.AmigaGuide;

namespace Md2Guide
{
  partial class Program
  {
    class AgRenderer : RendererBase
    {
      public Node Node { get; private set;}

      public int QuoteIndent { get; private set; }

      public List<int> ListIndent { get; private set; }

      public AgRenderer(Node node)
      {
        Node = node;
        QuoteIndent = 0;
        ListIndent = new List<int>();

        ObjectRenderers.Add(new HeadingRenderer());
        ObjectRenderers.Add(new CodeBlockRenderer());
        ObjectRenderers.Add(new ListRenderer());
        ObjectRenderers.Add(new ParagraphRenderer());
        ObjectRenderers.Add(new QuoteRenderer());
      }

      public void StartQuote()
      {
        QuoteIndent++;
      }

      public void StopQuote()
      {
        QuoteIndent--;
      }

      public void StartNumberList(int startAt)
      {
        ListIndent.Add(startAt);
      }

      public void StopList()
      {
        ListIndent.RemoveAt(ListIndent.Count - 1);
      }

      public void StartUnorderedList()
      {
        ListIndent.Add(Int32.MinValue);
      }

      public string GetListPrefix()
      {
        StringBuilder sb = new StringBuilder();

        for (int ii = 0; ii < ListIndent.Count; ii++)
        {
          int index = ListIndent[ii];

          if (index == Int32.MinValue)
          {
            sb.Append("*  ");
          }
          else
          {
            sb.Append($"{index}. ");
            index++;
            ListIndent[ii] = index;
          }
        }

        return sb.ToString();
      }

      public string GetQuotePrefix()
      {
        StringBuilder sb = new StringBuilder();
        for(int ii=0;ii < QuoteIndent;ii++)
        {
          sb.Append("> ");
        }

        return sb.ToString();
      }

      public override object Render(MarkdownObject markdownObject)
      {
        Write(markdownObject);
        return Node;
      }

      protected void WriteInline(Para p, Inline inline, Colour forceColour = Colour.None, bool forceItalic = false, bool forceBold = false)
      {
        while (inline != null)
        {
          switch (inline)
          {
            case LinkInline li:
            {
                if (li.Url.StartsWith("guide://"))
                {
                  string nodeName = li.Url.Substring(8).Trim().ToLower();

                  StringBuilder sb = new StringBuilder();

                  foreach (var ch in li)
                  {
                    switch(ch)
                    {
                      case LiteralInline lin:
                      {
                          sb.Append(lin.ToString());
                      }
                      break;
                    }
                  }

                  Node other = null;

                  if (Node.Nodes.TryGetValue(nodeName, out other) == false)
                  {
                    other = new Node(Node.Nodes, nodeName);
                  }

                  Link link = new Link(other, sb.ToString());
                  p.Runs.Add(link);
                }

              }
            break;
            case CodeInline ci:
            {
                Span span = new Span(ci.Content.ToString());
                span.Foreground = Colour.Shine;
                p.Runs.Add(span);
            }
            break;
            case EmphasisInline ei:
            {
                WriteInline(p, ei.FirstChild, Colour.None, ei.DelimiterCount == 1, ei.DelimiterCount > 1);
            }
            break;
            case ContainerInline ci:
            {
                WriteInline(p, ci.FirstChild);
            }
            break;
            case LiteralInline li:
            {
                Span span = new Span(li.Content.ToString());
                if (forceItalic)
                {
                  span.Italic = true;
                }
                if (forceBold)
                {
                  span.Bold = true;
                }
                if (forceColour != Colour.None)
                {
                  span.Foreground = forceColour;
                }
                p.Runs.Add(span);
            }
            break;
          }

          inline = inline.NextSibling;
        }
      }

      protected void WriteLeafInline(Para p, LeafBlock block)
      {
        Inline inline = block.Inline;
        if (inline != null)
        {
          while (inline != null)
          {
            WriteInline(p, inline);
            inline = inline.NextSibling;
          }
        }
      }

      private abstract class AgObjectRenderer<TObject> : MarkdownObjectRenderer<AgRenderer, TObject> where TObject : MarkdownObject
      {
      }


      private class ParagraphRenderer : AgObjectRenderer<ParagraphBlock>
      {
        protected override void Write(AgRenderer renderer, ParagraphBlock paragraphBlock)
        {
          var n = renderer.Node;
          var p = n.Paragraph();

          p.Span(renderer.GetListPrefix());
          p.Span(renderer.GetQuotePrefix());

          renderer.WriteLeafInline(p, paragraphBlock);
        }
      }

      private class QuoteRenderer : AgObjectRenderer<QuoteBlock>
      {
        protected override void Write(AgRenderer renderer, QuoteBlock quoteBlock)
        {
          var n = renderer.Node;
          
          renderer.QuoteIndent++;
          foreach(var c in quoteBlock)
          {
            renderer.Write(c);
          }
          renderer.QuoteIndent--;
        }
      }

      private class HeadingRenderer : AgObjectRenderer<HeadingBlock>
      {
        protected override void Write(AgRenderer renderer, HeadingBlock headerBlock)
        {
          int level = headerBlock.Level;

          Para p = renderer.Node.Paragraph();

          renderer.WriteLeafInline(p, headerBlock);
          p.BreakAfter = true;

          if (level == 1)
          {

            StringBuilder title = new StringBuilder();
            foreach(var s in p.OfType<Span>())
            {
              s.Bold = true;
              s.Underline = true;
              title.Append(s.Text);
            }

            renderer.Node.Title = title.ToString();

          }
          else if (level < 3)
          {
            foreach (var s in p.OfType<Span>())
            {
              s.Bold = true;
            }
            p.BreakBefore = true;
          }
          
        }
      }
      private class CodeBlockRenderer : AgObjectRenderer<CodeBlock>
      {
        protected override void Write(AgRenderer renderer, CodeBlock obj)
        {
          var n = renderer.Node;

          var fence = obj as FencedCodeBlock;
          if (fence != null)
          {
            Para begin = n.Paragraph();
            begin.BreakBefore = true;

            foreach(var line in fence.Lines)
            {
              Para p = renderer.Node.Paragraph();
              p.Span("  ");
              

              ColourizeLine(p, line.ToString(), fence.Info.Trim().ToLower());
            }

            Para after = n.Paragraph();
            after.BreakAfter = true;
          }
          else
          {
            foreach (var line in obj.Lines)
            {
              Para p = renderer.Node.Paragraph();
              p.Span("  ");


              ColourizeLine(p, line.ToString(), string.Empty);
            }
          }
        }

        static Regex Number = new Regex(@"(?<!\w)(0x[\da-f]+|\d+)(?!\w)", RegexOptions.IgnoreCase);
        static Regex Keywords = new Regex(@"(?<!\w|\$|\%|\@|>)(var|and|or|xor|for|do|while|foreach|as|return|die|exit|if|then|else|elseif|new|delete|try|throw|catch|finally|class|function|string|array|object|resource|var|bool|boolean|int|integer|float|double|real|string|array|global|const|static|public|private|protected|published|extends|switch|true|false|null|void|this|self|struct|char|signed|unsigned|short|long)(?!\w|="")", RegexOptions.IgnoreCase);
        static Regex Punc = new Regex(@"([{}\(\)\[\],\.])", RegexOptions.IgnoreCase);

        static void ColourizeLine(Para p, string line, string lang)
        {
          if (string.IsNullOrWhiteSpace(lang))
          {
            p.Span(line);
            return;
          }

          StringBuilder sb = new StringBuilder();

          foreach (var ch in line)
          {
            if (ch == '@')
              sb.Append("\\@");
            else if (ch == '\\')
              sb.Append("\\\\");
            else if (char.IsControl(ch))
            { }
            else if (ch > 127)
            { }
            else
              sb.Append(ch);

          }
          string line2 = sb.ToString();

          line2 = Number.Replace(line2, "<i>$1</i>");
          line2 = Keywords.Replace(line2, "<f>$1</f>");
          line2 = Punc.Replace(line2, "<b>$1</b>");

          sb.Length = 0;
          sb.Append(line2);
          sb.Replace("<i>", "@{i}");
          sb.Replace("</i>", "@{ui}");
          sb.Replace("<f>", "@{FG Fill}");
          sb.Replace("</f>", "@{FG Text}");
          sb.Replace("<b>", "@{b}");
          sb.Replace("</b>", "@{ub}");


          p.Emit(sb.ToString());

        }

      }
      private class ListRenderer : AgObjectRenderer<ListBlock>
      {
        protected override void Write(AgRenderer renderer, ListBlock listBlock)
        {
          var n = renderer.Node;

          if (listBlock.IsOrdered)
          {
            int index = 0;
            if (listBlock.OrderedStart != null)
            {
              switch (listBlock.BulletType)
              {
                case '1':
                  int.TryParse(listBlock.OrderedStart, out index);
                  break;
              }
            }

            renderer.StartNumberList(index);
              renderer.WriteChildren(listBlock);
            renderer.StopList();
          }
          else
          {
            renderer.StartUnorderedList();
              renderer.WriteChildren(listBlock);
            renderer.StopList();
          }
        }
        
      }
    }
  }
}
