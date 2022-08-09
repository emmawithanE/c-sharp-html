/*
 * New take, doing this as a tree of tags
 * So, the end product is a html tag, which contains things, which contain things, and so on
 * format is [tagname]: [tagdata] - tag contents
 * 
 * html:             [lang = "en"] - head, body
 *   head:           meta, title, style
 *     meta:         [charset="UTF-8"] -      (unterminated)
 *     title:        string
 *     style:        [rel="stylesheet" href="styles.css"] -      (unterminated)
 *   body:           text_block*
 *     text_block:   heading | par | hl
 *     heading:      [level] - text_obj+
 *     par:          text_obj+
 *     hl:           -----      (unterminated)
 *     text_obj:     string | tag_obj
 *     tag_obj:      em | str | del | link | ul | ol
 *     em, str, del: text_obj+
 *     link:         dest - text_obj+
 *     ul, ol:       li+
 *     li:           text_obj+
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public enum TagType { html, head, body, meta, title, style, heading, par, hl, plain_str, em, strong, del, link, ul, ol, li };

public abstract class ITag
{
	// Tag interface
	protected TagType type;
	protected virtual string TagName()
    {
		return type.ToString(); 
    }
    protected bool StartTagNewline = false;

    List<string> data = new();
	public abstract string Print();

	public virtual string PrintStart()
    {
		if (data.Count == 0)
        {
			return $"<{TagName()}>" + (StartTagNewline ? '\n' : null);
        }
		else
        {
			var output = $"<{TagName()}";
			foreach (var datastring in data)
            {
				output += " " + datastring;
            }
			output += ">" + (StartTagNewline ? '\n' : null);
			return output;
		}
    }

    public void AddData(string new_data)
    {
        data.Add(new_data);
    }
}

public class SingleTag : ITag
{
	// Tags that don't have an end, like hl
	public override string Print()
    {
		return PrintStart();
    }

    public SingleTag(TagType input_type)
    {
        StartTagNewline = true;
        type = input_type;
    }
}

public abstract class IMultiTag : ITag
{
	protected List<ITag> tags = new();
    protected bool EndTagNewline = false;

	string PrintEnd()
    {
		return $"</{TagName()}>" + (EndTagNewline ? "\n" : null);
	}

	public override string Print()
    {
		string output = PrintStart();

		foreach (var tag in tags)
        {
			output += tag.Print();
        }
		output += PrintEnd();

		return output;
    }
}



// Single Tag Overrides

public class PlainString : SingleTag
{
    string content;

    public override string Print()
    {
        return content;
    }

    public PlainString(string input_content) : base(TagType.plain_str)
    {
        content = input_content;
    }
}



// Multi Tag Overrides

public class HtmlTag : IMultiTag
{
    public HtmlTag(StreamReader reader)
    {
        // [lang = "en"] - head, body
        type = TagType.html;
        StartTagNewline = true;
        EndTagNewline = true;

        AddData($"lang=\"en\"");

        tags.Add(new HeadTag(reader));
    }
}

public class HeadTag : IMultiTag
{
    public HeadTag(StreamReader reader)
    {
        // meta, title, style
        type = TagType.head;
        StartTagNewline = true;
        EndTagNewline = true;

        tags.Add(new SingleTag(TagType.meta));
        tags.Last().AddData($"charset=\"UTF - 8\"");

        tags.Add(new TitleTag(reader));

        tags.Add(new SingleTag(TagType.style));
        tags.Last().AddData($"rel=\"stylesheet\" href=\"styles.css\"");
    }
}

public class TitleTag : IMultiTag
{
    public TitleTag(StreamReader reader)
    {
        type = TagType.title;
        EndTagNewline = true;

        tags.Add(new PlainString(reader.ReadLine()));
    }
}



public class Generator
{
    void ProcessFile(string filename)
    {
        string output_name = Path.ChangeExtension(filename, "html");
        using (StreamReader reader = new StreamReader(filename))
        using (StreamWriter writer = new StreamWriter(output_name))
        {
            HtmlTag root_tag = new HtmlTag(reader);
            writer.WriteLine(root_tag.Print());
        }

    }

    void ProcessDirectory(string target)
    {
        // Process the list of files found in the directory
        string[] files = Directory.GetFiles(target, "*.txt");
        foreach (string file in files)
            ProcessFile(file);

        // Recurse into subdirectories of this directory
        string[] subdirs = Directory.GetDirectories(target);
        foreach (string subdir in subdirs)
            ProcessDirectory(subdir);
    }

    public void Process(string[] targets)
    {
        foreach (var target in targets)
        {
            if (File.Exists(target))
            {
                try
                {
                    ProcessFile(target);
                }
                catch (Exception e)
                {
                    Console.WriteLine("File processing failed: " + e);
                }
            }
            else if (Directory.Exists(target))
            {
                ProcessDirectory(target);
            }
            else
            {
                Console.WriteLine("Could not find target file or directory: " + target);
            }
        }
    }
}

public class Program
{
	public static void Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("No files given");
			return;
		}

        Generator generator = new();
        generator.Process(args);

        return;
	}
}

