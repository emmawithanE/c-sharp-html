/*
 * New take, doing this as a tree of tags
 * So, the end product is a html tag, which contains things, which contain things, and so on
 * format is [tagname]: [tagdata] - tag contents
 * 
 * html:             [lang = "en"] - head, body
 *   head:           meta, title, style
 *     meta:         [charset="UTF-8"] -      (unterminated)
 *     title:        text_obj+
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

public enum TagType { html, head, body, meta, title, style, heading, par, hl, plain_str, em, strong, del, link, ul, ol, li };

public abstract class ITag
{
	// Tag interface
	protected TagType type;
	List<string> data;
	public abstract string Print();
	public virtual string PrintStart()
    {
		if (data.Count == 0)
        {
			return $"<{type.ToString()}>";
        }
		else
        {
			var output = $"<{type.ToString()}";
			foreach (var datastring in data)
            {
				output += " " + datastring;
            }
			output += '>';
			return output;
		}
    }
}

public abstract class ISingleTag : ITag
{
	// Tags that don't have an end, like hl
	public override string Print()
    {
		return PrintStart();
    }
}

public abstract class IMultiTag : ITag
{
	List<ITag> tags;

	string PrintEnd()
    {
		return $"</{type.ToString()}>";
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

public class Program
{
	public static void Main()
	{
    }
}

