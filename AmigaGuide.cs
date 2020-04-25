using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Md2Guide.AmigaGuide
{
  internal static class Ag
  {
    internal static void AppendDirectiveLine(this StringBuilder sb, string text)
    {
      if (sb.Length > 0)
      {
        if (sb[sb.Length - 1] != '\n')
          sb.Append('\n');
      }

      sb.Append(text);
      sb.Append('\n');
    }

    internal static void AppendAmigaLine(this StringBuilder sb, string text)
    {
      sb.Append(text);
      sb.Append('\n');
    }

    internal static void AppendEscapedText(this StringBuilder sb, string text)
    {
      foreach(var ch in text)
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
    }

  }

  public enum Justify
  {
    Left,
    Center,
    Right
  }

  public enum Colour
  {
    None,
    Text,
    Shine,
    Shadow,
    Fill,
    FillText,
    Background,
    Highlight
  }

  public interface IRun
  {
    void Save(StringBuilder sb);
  }

  public class Emit : IRun
  {
    public string Text { get; set; }
    
    public Emit(string text)
    {
      Text = text;
    }

    public void Save(StringBuilder sb)
    {
      if (string.IsNullOrEmpty(Text) == false)
      {
        sb.Append(Text);
      }
    }
  }

  public class Span : IRun
  {
    public string Text { get; set; }

    public bool Bold { get; set; }

    public bool Underline { get; set; }

    public bool Italic { get; set; }

    Colour foreground = Colour.Text;
    Colour background = Colour.Background;
    bool foregroundChanged, backgroundChanged;

    public Colour Foreground
    {
      get => foreground;
      set {
        foreground = value;
        foregroundChanged =true;
      }
    }

    public Colour Background
    {
      get => foreground;
      set
      {
        background = value;
        backgroundChanged = true;
      }
    }


    public Span(string text)
    {
      Text = text;
    }

    public void Save(StringBuilder sb)
    {
      if (string.IsNullOrEmpty(Text) == false)
      {
        if (foregroundChanged)
        {
          sb.Append($"@{{FG {foreground}}}");
        }

        if (backgroundChanged)
        {
          sb.Append($"@{{BG {background}}}");
        }

        if (Bold)
        {
          sb.Append("@{b}");
        }

        if (Underline)
        {
          sb.Append("@{u}");
        }

        if (Italic)
        {
          sb.Append("@{i}");
        }

        sb.AppendEscapedText(Text);

        if (Underline)
        {
          sb.Append("@{uu}");
        }

        if (Bold)
        {
          sb.Append("@{ub}");
        }

        if (Italic)
        {
          sb.Append("@{ui}");
        }

        if (backgroundChanged)
        {
          sb.Append("@{BG Background}");
        }

        if (foregroundChanged)
        {
          sb.Append("@{FG Text}");
        }
      }
    }
  }

  public class Link : IRun
  {
    public Node Node { get; set; }

    public string Text { get; set; }

    public Link(Node node)
    {
      Node = node;
      Text = string.Empty;
    }

    public Link(Node node, String text)
    {
      Node = node;
      Text = text;
    }

    public void Save(StringBuilder sb)
    {
      if (Node == null)
        return;

      string linkText = Node.Title;

      if (string.IsNullOrWhiteSpace(Text) == false)
      {
        linkText = Text;
      }

      sb.Append($"@{{\"{linkText}\" LINK {Node.Name}}}");
    }

  }

  public class Para
  {
    public List<IRun> Runs { get; private set; }

    public bool BreakAfter { get; set; }
    public bool BreakBefore { get; set; }


    Justify _justify;
    bool _justifyChanged;

    public Justify Justify
    {
      get => _justify;
      set
      {
        _justify = value;
        _justifyChanged = true;
      }
    }

    public Para()
    {
      Runs = new List<IRun>();
      _justify = Justify.Left;
      _justifyChanged = false;
    }

    public Para Emit(string text)
    {
      Runs.Add(new Emit(text));
      return this;
    }

    public Para Span(string text, Colour foreground, Colour background, bool isItalic, bool isBold, bool isUnderline)
    {
      Span sp = new Span(text);
      sp.Foreground = foreground;
      sp.Background = background;
      sp.Bold = isBold;
      sp.Italic = isItalic;
      sp.Underline = isUnderline;
      Runs.Add(sp);
      return this;
    }

    public Para Span(string text)
    {
      Span sp = new Span(text);
      Runs.Add(sp);
      return this;
    }

    public Para Link(Node node)
    {
      Link link = new Link(node);
      Runs.Add(link);
      return this;
    }

    public Para Link(string text, Node node)
    {
      Link link = new Link(node, text);
      Runs.Add(link);
      return this;
    }

    public void Save(StringBuilder sb)
    {
      if (BreakBefore)
      {
        sb.AppendAmigaLine(string.Empty);
      }

      if (_justifyChanged)
      {
        switch (_justify)
        {
          case Justify.Left:
            sb.Append("@{JLEFT}");
            break;
          case Justify.Center:
            sb.Append("@{JCENTER}");
            break;
          case Justify.Right:
            sb.Append("@{JRIGHT}");
            break;
        }
      }

      foreach (IRun r in Runs)
      {
        r.Save(sb);
      }

      sb.AppendAmigaLine(string.Empty);

      if (_justifyChanged)
      {
        sb.Append("@{JLEFT}");
      }

      if (BreakAfter)
      {
        sb.AppendAmigaLine(string.Empty);
      }

    }

    public IEnumerable<TResult> OfType<TResult>() where TResult : IRun
    {
      foreach (object obj in Runs)
      {
        if (obj is TResult) yield return (TResult)obj;
      }
    }
  }

  public enum NodeType
  {
    Main,
    Toc,
    Text
  }

  public class Node
  {
    public Dictionary<string, Node> Nodes { get; private set; }

    public List<Para> Paragraphs { get; private set; }

    public string Name
    {
      get => _name;
      set
      {
        string newName = value.ToUpper();

        Node n;
        if (Nodes != null && Nodes.TryGetValue(newName, out n))
        {
          if (n != this)
          {
            throw new Exception("Node already exists with this name! " + value);
          }
          Nodes.Remove(_name);
        }

        if (Nodes != null)
        {
          Nodes.Add(newName, this);
        }

        _name = newName;
        
        if (string.IsNullOrWhiteSpace(Title))
        {
          if (Type == NodeType.Text)
          {
            Title = _name;
          }
          else if (Type == NodeType.Main)
          {
            Title = "Main";
          }
          else if (Type == NodeType.Toc)
          {
            Title = "Table of Contents";
          }
        }
      }
    }

    string _name;

    public string Title
    {
      get; set;
    }

    public NodeType Type
    {
      get
      {
        if (_name == "MAIN")
          return NodeType.Main;
        else if (_name == "TOC")
          return NodeType.Toc;
        else 
          return NodeType.Text;
      }
    }

    public Node(Dictionary<string, Node> nodes, string name)
    {
      Nodes = nodes;
      Title = string.Empty;
      Name = name;
      Paragraphs = new List<Para>();
    }

    public Para Paragraph(params IRun[] runs)
    {
      Para para = new Para();
      Paragraphs.Add(para);
      foreach (var run in runs)
      {
        para.Runs.Add(run);
      }
      return para;
    }

    public void BuildToc()
    {
      char lastLetter = "A"[0];

      if (Paragraphs.Count == 0)
      {
        Para p = Paragraph();
        Span heading = new Span("Table of Contents");
        heading.Underline = true;
        heading.Bold = true;
        p.BreakAfter = true;
        p.Runs.Add(heading);
      }

      foreach(var node in Nodes.Values.OrderBy(x => x.Title))
      {
        if (node.Type != NodeType.Toc)
        {
          char firstLetter = char.ToUpper(node.Title[0]);

          if (firstLetter != lastLetter)
          {
            lastLetter = firstLetter;
            Para hp = Paragraph();
            Span hs = new Span(firstLetter.ToString());
            hs.Bold = true;
            hp.BreakAfter = true;
            hp.Runs.Add(hs);
          }

          Para para = Paragraph();
          para.Span("  ");
          para.Link(node.Title, node);
        }
      }
    }

    public void Save(StringBuilder sb)
    {

      sb.AppendAmigaLine(string.Empty);
      sb.AppendDirectiveLine($"@NODE {Name} \"{Title}\"");

      foreach (Para p in Paragraphs)
      {
        p.Save(sb);
      }

      sb.AppendDirectiveLine("@ENDNODE");
    }
  }

  public class GuideWriter
  {
    public Dictionary<string, Node> Nodes { get; private set; }

    public GuideWriter()
    {
      Nodes = new Dictionary<string, Node>();
    }

    public Node GetNode(string name)
    {
      Node node = _SearchNode(name);

      if (node != null)
        return node;

      node = new Node(Nodes, name);
      return node;
    }

    Node _SearchNode(string name)
    {
      name = name.ToUpper();

      Node node = null;
      Nodes.TryGetValue(name, out node);
      return node;
    }

    public void Save(params string[] path)
    {
      StringBuilder sb = new StringBuilder();

      sb.AppendDirectiveLine("@DATABASE");
      sb.AppendDirectiveLine("@TOC TOC");


      Node main = GetNode("MAIN");
      main.Save(sb);

      Node toc = GetNode("TOC");
      toc.BuildToc();
      toc.Save(sb);
      

      foreach (var node in Nodes.Values.OrderBy(x => x.Name))
      {
        if (node.Type == NodeType.Text)
        {
          node.Save(sb);
        }
      }



      byte[] ascii = System.Text.Encoding.Convert(
        System.Text.Encoding.Default,
        System.Text.Encoding.ASCII,
        System.Text.Encoding.Default.GetBytes(sb.ToString()));

      foreach(var p in path)
      {
        System.IO.File.WriteAllBytes(p, ascii);
      }
    }

  }
}
