using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace StellarisPatcher;

public static class ConsoleUtil
{
    public static void SelectOption(string title, IList<string> options, Action<int, string> cb)
    {
        var selector = new ConsoleOptionSelector(title, options, cb);
        selector.Show();
    }
    
    public static string SelectFile(string title, string filter)
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = false,
            Title = title,
            Filter = filter
        };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            Environment.Exit(0);
            throw null;
        }

        return dialog.FileName;
    }
}

public class ConsoleOptionSelector
{
    private readonly string title;
    private readonly IList<string> options;
    private readonly Action<int, string> cb;

    private int selected = 0;
    private int PosX = 0;
    private int PosY = 0;

    public ConsoleOptionSelector(string title, IList<string> options, Action<int, string> cb)
    {
        this.title = title;
        this.options = options;
        this.cb = cb;
        PosX = Console.CursorLeft;
        PosY = Console.CursorTop;
        
    }

    public void Show()
    {
        while (true)
        {
            Render();
            var readKey = Console.ReadKey();
            switch (readKey.Key)
            {
                case ConsoleKey.DownArrow:
                    this.selected++;
                    if (this.selected >= this.options.Count)
                    {
                        this.selected = 0;
                    }

                    break;
                
                case ConsoleKey.UpArrow:
                    this.selected--;
                    if (this.selected < 0)
                    {
                        this.selected = this.options.Count - 1;
                    }

                    break;
                
                case ConsoleKey.Enter:
                    this.cb(this.selected, this.options[this.selected]);
                    return;
            }

            if (char.IsNumber(readKey.KeyChar) && int.TryParse(readKey.KeyChar.ToString(), out var idx) && idx < this.options.Count)
            {
                this.selected = idx;
            }
        }
    }

    public void Render()
    {
        Console.SetCursorPosition(this.PosX, this.PosY);
        Console.WriteLine(this.title);
        for (var i = 0; i < this.options.Count; i++)
        {
            if (this.selected == i)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine($"> [{i}] {this.options[i]}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"  [{i}] {this.options[i]}");
            }
        }
    }
}