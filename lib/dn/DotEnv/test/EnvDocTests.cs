using System;
using System.Linq;

namespace NeoBeard.DotEnv.Tests;

public static class EnvDocTests
{
    [Fact]
    public static void Constructor_Creates_Empty_Document()
    {
        var doc = new EnvDoc();
        Assert.Equal(0, doc.Count);
    }

    [Fact]
    public static void Set_Adds_Variable()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");

        Assert.Equal(1, doc.Count);
        Assert.Equal("value", doc.Get("KEY"));
    }

    [Fact]
    public static void Set_Updates_Existing_Variable()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value1");
        doc.Set("KEY", "value2");

        Assert.Equal(1, doc.Count);
        Assert.Equal("value2", doc.Get("KEY"));
    }

    [Fact]
    public static void Set_With_Quote_Style()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value", QuoteStyle.Double);

        var variable = doc.OfType<EnvVariable>().First();
        Assert.Equal(QuoteStyle.Double, variable.Quote);
    }

    [Fact]
    public static void Set_Auto_Quotes_Special_Chars()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value\nwith\nnewlines");

        var variable = doc.OfType<EnvVariable>().First();
        Assert.Equal(QuoteStyle.Double, variable.Quote);
    }

    [Fact]
    public static void Get_Returns_Null_For_Missing_Key()
    {
        var doc = new EnvDoc();
        Assert.Null(doc.Get("MISSING"));
    }

    [Fact]
    public static void TryGetValue_Returns_True_For_Existing_Key()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");

        var found = doc.TryGetValue("KEY", out var value);

        Assert.True(found);
        Assert.Equal("value", value);
    }

    [Fact]
    public static void TryGetValue_Returns_False_For_Missing_Key()
    {
        var doc = new EnvDoc();

        var found = doc.TryGetValue("MISSING", out var value);

        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public static void Indexer_Get_Set()
    {
        var doc = new EnvDoc();
        doc["KEY"] = "value";

        Assert.Equal("value", doc["KEY"]);

        doc["KEY"] = "updated";
        Assert.Equal("updated", doc["KEY"]);

        doc["KEY"] = null;
        Assert.Null(doc["KEY"]);
    }

    [Fact]
    public static void Remove_Deletes_Variable()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");

        var removed = doc.Remove("KEY");

        Assert.True(removed);
        Assert.Equal(0, doc.Count);
    }

    [Fact]
    public static void Remove_Returns_False_For_Missing_Key()
    {
        var doc = new EnvDoc();
        var removed = doc.Remove("MISSING");
        Assert.False(removed);
    }

    [Fact]
    public static void Clear_Removes_All()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value2");
        doc.Clear();

        Assert.Equal(0, doc.Count);
    }

    [Fact]
    public static void AddNewline_Adds_Newline_Element()
    {
        var doc = new EnvDoc();
        doc.AddNewline();

        Assert.Equal(1, doc.Count);
        Assert.IsType<EnvNewline>(doc.First());
    }

    [Fact]
    public static void AddComment_Adds_Comment_Element()
    {
        var doc = new EnvDoc();
        doc.AddComment("This is a comment");

        Assert.Equal(1, doc.Count);
        var comment = Assert.IsType<EnvComment>(doc.First());
        Assert.Equal("This is a comment", comment.Text);
        Assert.False(comment.IsInline);
    }

    [Fact]
    public static void Keys_Returns_All_Keys()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value2");
        doc.AddComment("comment");
        doc.Set("KEY3", "value3");

        var keys = doc.Keys;

        Assert.Equal(3, keys.Count);
        Assert.Contains("KEY1", keys);
        Assert.Contains("KEY2", keys);
        Assert.Contains("KEY3", keys);
    }

    [Fact]
    public static void Comments_Returns_All_Comments()
    {
        var doc = new EnvDoc();
        doc.AddComment("comment1");
        doc.Set("KEY", "value");
        doc.AddComment("comment2");

        var comments = doc.Comments;

        Assert.Equal(2, comments.Count);
        Assert.Contains("comment1", comments);
        Assert.Contains("comment2", comments);
    }

    [Fact]
    public static void ToDictionary_Converts_Variables()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value2");
        doc.AddComment("comment");

        var dict = doc.ToDictionary();

        Assert.Equal(2, dict.Count);
        Assert.Equal("value1", dict["KEY1"]);
        Assert.Equal("value2", dict["KEY2"]);
    }

    [Fact]
    public static void Merge_Combines_Documents()
    {
        var doc1 = new EnvDoc();
        doc1.Set("KEY1", "value1");
        doc1.Set("KEY2", "value2");

        var doc2 = new EnvDoc();
        doc2.Set("KEY2", "override");
        doc2.Set("KEY3", "value3");

        doc1.Merge(doc2);

        Assert.Equal("value1", doc1.Get("KEY1"));
        Assert.Equal("override", doc1.Get("KEY2"));
        Assert.Equal("value3", doc1.Get("KEY3"));
    }

    [Fact]
    public static void Merge_Static_Combines_Documents()
    {
        var doc1 = new EnvDoc();
        doc1.Set("KEY1", "value1");

        var doc2 = new EnvDoc();
        doc2.Set("KEY2", "value2");

        var merged = EnvDoc.Merge(doc1, doc2);

        Assert.Equal("value1", merged.Get("KEY1"));
        Assert.Equal("value2", merged.Get("KEY2"));
    }

    [Fact]
    public static void Expand_Mutates_Values()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "${HOME}");
        doc.Set("KEY2", "prefix_${KEY1}_suffix");

        var returned = doc.Expand(value => value.Replace("${HOME}", "/home/user").Replace("${KEY1}", "value1"));

        Assert.Same(doc, returned);
        Assert.Equal("/home/user", doc.Get("KEY1"));
        Assert.Equal("prefix_value1_suffix", doc.Get("KEY2"));
    }

    [Fact]
    public static void ExpandClone_Returns_New_Document()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "${HOME}");

        var clone = doc.ExpandClone(value => value.Replace("${HOME}", "/home/user"));

        Assert.NotSame(doc, clone);
        Assert.Equal("${HOME}", doc.Get("KEY"));
        Assert.Equal("/home/user", clone.Get("KEY"));
    }

    [Fact]
    public static void ExpandClone_Preserves_Comments_And_Newlines()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");
        doc.AddNewline();
        doc.AddComment("comment");

        var clone = doc.ExpandClone(v => v);

        Assert.Equal(3, clone.Count);
    }

    [Fact]
    public static void ToString_Serializes_Document()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value with spaces", QuoteStyle.Double);

        var output = doc.ToString();

        Assert.Contains("KEY1=value1", output);
        Assert.Contains("KEY2=\"value with spaces\"", output);
    }

    [Fact]
    public static void ToString_Preserves_Comments()
    {
        var doc = new EnvDoc();
        doc.AddComment("This is a comment");
        doc.Set("KEY", "value");

        var output = doc.ToString();

        Assert.Contains("# This is a comment", output);
    }

    [Fact]
    public static void ToString_Preserves_Newlines()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.AddNewline();
        doc.Set("KEY2", "value2");

        var output = doc.ToString();

        Assert.Contains("KEY1=value1", output);
        Assert.Contains("KEY2=value2", output);
    }

    [Fact]
    public static void GetEnumerator_Enumerates_Elements()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");
        doc.AddComment("comment");

        var count = 0;
        foreach (var element in doc)
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public static void EnvVariable_Properties()
    {
        var v = new EnvVariable("KEY", "value", QuoteStyle.Double);

        Assert.Equal("KEY", v.Key);
        Assert.Equal("value", v.Value);
        Assert.Equal(QuoteStyle.Double, v.Quote);
        Assert.True(v.IsQuoted);
        Assert.Equal(EnvElementKind.Variable, v.Kind);

        v.Key = "NEW_KEY";
        v.Value = "new_value";
        v.Quote = QuoteStyle.Single;

        Assert.Equal("NEW_KEY", v.Key);
        Assert.Equal("new_value", v.Value);
        Assert.Equal(QuoteStyle.Single, v.Quote);
    }

    [Fact]
    public static void EnvComment_Properties()
    {
        var c = new EnvComment("text", isInline: true);

        Assert.Equal("text", c.Text);
        Assert.True(c.IsInline);
        Assert.Equal(EnvElementKind.Comment, c.Kind);

        c.Text = "new text";
        c.IsInline = false;

        Assert.Equal("new text", c.Text);
        Assert.False(c.IsInline);
    }

    [Fact]
    public static void EnvNewline_Kind()
    {
        var n = new EnvNewline();
        Assert.Equal(EnvElementKind.Newline, n.Kind);
    }

    [Fact]
    public static void Merge_Dictionary_Overwrites_Keys()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");

        var dict = new Dictionary<string, string>
        {
            ["KEY2"] = "value2",
            ["KEY1"] = "override",
        };

        doc.Merge(dict);

        Assert.Equal("override", doc.Get("KEY1"));
        Assert.Equal("value2", doc.Get("KEY2"));
    }

    [Fact]
    public static void Merge_Dictionary_Null_Throws()
    {
        var doc = new EnvDoc();
        Assert.Throws<ArgumentNullException>(() => doc.Merge((IDictionary<string, string>)null!));
    }

    [Fact]
    public static void ToDictionary_Generic_Transforms_Values()
    {
        var doc = new EnvDoc();
        doc.Set("PORT", "8080");
        doc.Set("TIMEOUT", "30");

        var dict = doc.ToDictionary<string, int>(k => k, v => int.Parse(v));

        Assert.Equal(8080, dict["PORT"]);
        Assert.Equal(30, dict["TIMEOUT"]);
    }

    [Fact]
    public static void ToDictionary_Generic_Null_Selectors_Throw()
    {
        var doc = new EnvDoc();
        doc.Set("KEY", "value");

        Assert.Throws<ArgumentNullException>(() => doc.ToDictionary<string, string>(null!, v => v));
        Assert.Throws<ArgumentNullException>(() => doc.ToDictionary<string, string>(k => k, null!));
    }

    [Fact]
    public static void ToOrderedDictionary_Preserves_Order()
    {
        var doc = new EnvDoc();
        doc.Set("KEY3", "value3");
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value2");

        var dict = doc.ToOrderedDictionary();

        var keys = dict.Keys.Cast<string>().ToList();
        Assert.Equal(new[] { "KEY3", "KEY1", "KEY2" }, keys);
    }

    [Fact]
    public static void ToOrderedDictionary_Contains_All_Variables()
    {
        var doc = new EnvDoc();
        doc.Set("KEY1", "value1");
        doc.Set("KEY2", "value2");
        doc.AddComment("comment");
        doc.Set("KEY3", "value3");

        var dict = doc.ToOrderedDictionary();

        Assert.Equal(3, dict.Count);
        Assert.Equal("value1", dict["KEY1"]);
        Assert.Equal("value2", dict["KEY2"]);
        Assert.Equal("value3", dict["KEY3"]);
    }
}