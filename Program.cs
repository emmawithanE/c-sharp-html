using System;
using System.IO;
using System.Collections.Generic;

/* Language description time
    
Headings: ###(n) [Text] -> <h(n)> [Text] </h(n)>

In this house we use semantic tags ig
Italics: _[Text]_   -> <em>[Text]</em>
Bold:    **[Text]** -> <strong>[Text]</strong>
Strikethrough: ~~[Text]~~ -> <del>[Text]</del>
Super and subscript just get put in as html tags
Divider line: ----- -> <hr>

Dot Points: * [Text] -> <ul> 
                            <li> [Text] </li>
                        </ul>
Ordered list: (num). [Text] -> <ol>
                                   <li> [Text] </li>
                               </ol>

Lists indent - lines with 4 x n spaces at the start are a new list object within a <li> block of the previous level
Handling lists is annoying so I won't for now

All non-header, non-list lines are in <p>[Text]</p>

TODO: Is there some way to distinguish italics that shouldn't be <em>? May need own markdown generator
/see if C# can directly handle formatted text somehow

-----

Links added as stretch goal because that's going to be slightly more complex

*/

public enum TagType { html, head, title, body, par, heading, em, strong, del, un_list, or_list, list_item, link }

public class Tag
{
    TagType tag_type;
    string bonus_text; // Used for header level and links and such

    string TagText()
    {

        switch (tag_type)
        {
            case TagType.html:
                return "html";
            case TagType.head:
                return "head";
            case TagType.title:
                return "title";
            case TagType.body:
                return "body";
            case TagType.par:
                return "p";
            case TagType.heading:
                return "h" + bonus_text;
            case TagType.em:
                return "em";
            case TagType.strong:
                return "strong";
            case TagType.del:
                return "del";
            case TagType.un_list:
                return "ul";
            case TagType.or_list:
                return "ol";
            case TagType.list_item:
                return "li";
            case TagType.link:
                return "a";
            default:
                return "Unrecognised tag type: " + (int)tag_type;
        }
    }

    public Tag(TagType in_type, string extra = null)
    {
        tag_type = in_type;
        bonus_text = extra;
    }

    public TagType GetTagType()
    {
        return tag_type;
    }

    public string Open()
    {
        return "<" + TagText() + ">";
    }

    public string Close()
    {
        return "</" + TagText() + ">";
    }
}

public class TagStack
{
    Stack<Tag> tags = new Stack<Tag>();

    public int StackSize
    {
        get { return tags.Count; }
    }

    public TagType? TopTag()
    {
        try
        {
            return tags.Peek().GetTagType();
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Tried to check top tag but the stack was empty (" + e + ")");
            return null;
        }
    }

    public string OpenTag(TagType type, string text = null)
    {
        Tag tag = new Tag(type, text);
        tags.Push(tag);
        return tag.Open();
    }

    public string CloseTag(TagType? type = null)
    {
        try
        {
            if (type.HasValue && (type != tags.Peek().GetTagType()))
            {
                Console.WriteLine("Tried to close tag " + type.ToString()
                    + " but the top of stack was " + tags.Peek().GetType().ToString());
                    return "";
            }
            return tags.Pop().Close();
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Tried to close a tag but the stack was empty (" + e + ")");
            return "";
        }
    }

    public string ProcessTag(TagType type, string text = null)
    {
        if (tags.Peek().GetTagType() == type)
        {
            return CloseTag();
        }
        else
        {
            return OpenTag(type, text);
        }
    }

    public string FlushTo(TagType type)
    {
        string output = "";

        try
        {
            while (tags.Peek().GetTagType() != type)
            {
                output += CloseTag();
            }
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("Stack Flush invalid operation: flushed without finding tag "
                + type.ToString() + " (" + e + ")");
        }
        return output;
    }
}

public class Parser
{
    TagStack tags = new TagStack();

    string Spacer() 
    {
        return new string(' ', 4 * tags.StackSize);
    }

    public void ProcessFile(string filename)
    {
        using (StreamReader reader = new StreamReader(filename))
        using (StreamWriter writer = new StreamWriter(filename + ".html"))
        {
            writer.WriteLine(tags.OpenTag(TagType.html));
            writer.WriteLine(Spacer() + tags.OpenTag(TagType.head));

            string title_line = Spacer() + tags.OpenTag(TagType.title) + filename + tags.CloseTag(TagType.title);
            writer.WriteLine(title_line);

            writer.WriteLine(Spacer() + tags.CloseTag(TagType.head));
            writer.WriteLine(Spacer() + tags.OpenTag(TagType.body));

            char c = '\n';
            string output = "";

            while (reader.Peek() >= 0)
            {
                if (c == '\n')
                {
                    // At the start of a new line
                    output += Spacer() + tags.ProcessTag(TagType.par); // TODO: Headings should not be in par tags
                } 

                c = (char)reader.Read();
                switch (c)
                {
                    case '#': // Heading: Up to 6 # -> <h(n)>
                        {
                            int count = 1;
                            while (reader.Peek() == '#')
                            {
                                reader.Read();
                                count++;
                            }
                            output += tags.ProcessTag(TagType.heading, count.ToString());
                            if (reader.Peek() == ' ')
                            {
                                reader.Read();
                            }
                            break;
                        }
                    case '_': // Italics -> em tag
                        {
                            output += tags.ProcessTag(TagType.em);
                            break;
                        }
                    case '~': // Chack for ~~ -> del, or just move on
                        {
                            if (reader.Peek() == '~')
                            {
                                output += tags.ProcessTag(TagType.del);
                                reader.Read();
                            }
                            else
                            {
                                output += c;
                            }
                            break;
                        }
                    case '*': // Chack for ** -> strong, or just move on
                        // TODO: Implement unordered list for "* "
                        {
                            if (reader.Peek() == '*')
                            {
                                output += tags.ProcessTag(TagType.strong);
                                reader.Read();
                            }
                            else
                            {
                                output += c;
                            }
                            break;
                        }
                    case '-': // 5 or more -> <hr>, else print as written
                        {
                            int count = 1;
                            while (reader.Peek() == '-')
                            {
                                reader.Read();
                                count++;
                            }
                            if (count >= 5)
                            {
                                output += "<hr>";
                            }
                            else
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    output += "-";
                                }
                            }
                            break;
                        }
                    case '\r': // Fuck Windows
                        {
                            reader.Read();
                            c = '\n';
                            goto case '\n';
                        }
                    case '\n': // End paragraph, start a new one
                        {
                            output += tags.FlushTo(TagType.par) + tags.CloseTag(TagType.par);
                            output += "\n\n";
                            break;
                        }
                    default:
                        output += c;
                        break;
                }
            }

            output += tags.FlushTo(TagType.body);
            writer.WriteLine(output);
            writer.WriteLine(Spacer() + tags.CloseTag(TagType.body));
            writer.WriteLine(tags.CloseTag(TagType.html));

            Console.WriteLine("Output to " + filename + ".html");
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

        Parser parser = new Parser();

		foreach (var filename in args)
        {
			try
            {
                parser.ProcessFile(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("File read failed: " + e);
            }
        }
	}
}
