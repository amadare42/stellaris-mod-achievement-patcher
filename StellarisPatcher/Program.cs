using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YYProject.BytesSearch;

namespace StellarisPatcher
{
    internal class Program
    {
        private static long GhidraMemoryStart = 0x140001000 - 1024;
        
        [STAThread]
        public static void Main(string[] args)
        {
            // parse CLI
            if (args.Length > 0 && args.Contains("--help"))
            {
                Console.WriteLine("This application will patch Stellaris so all file hashsum checks will always pass so achievements would work for modded game.");
                Console.WriteLine($"USAGE: {System.Diagnostics.Process.GetCurrentProcess().ProcessName}.exe [path to stellaris.exe] [path to emitted stellaris]");
                Console.WriteLine($"       if arg0 isn't provided, UI to select file would be shown.");
                Console.WriteLine($"       if arg1 isn't provided, defaults to arg0 value");
                return;
            }

            var path = args.FirstOrDefault() ?? ConsoleUtil.SelectFile("Select Stellaris.exe", "Exe|*.exe");
            var resultPath = args.Skip(1).FirstOrDefault() ?? path;
            
            var bytes = File.ReadAllBytes(path);
            var pattern = string.Join(" ",
                "48 8B 12", // mov rdx [rdx]
                "48 8D 0D ?? ?? ?? ??", // lea rcx (some relative location, dependent on platform/binary - this relative location is the location of the actual hashsum, which is also stored in the binary)
                "E8" // call dword (some absolute location, dependent on platform/binary - this location will contain the assembly of the C function strcmp)
            );
            
            Console.WriteLine($"Searching for pattern: {pattern}");
            var finder = new BytesFinder(pattern);
            var dict = new Dictionary<string, List<int>>();
            
            foreach (var loc in EnumerateLocations(bytes, finder))
            {
                var str = TryReadStrResource(bytes, loc);
                if (str == "INVALID") continue;
                if (!dict.TryGetValue(str, out var lst))
                {
                    lst = new List<int>();
                    dict[str] = lst;
                }
                lst.Add(loc);
                
                #if DEBUG
                var loc1 = loc + GhidraMemoryStart;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{loc1:X} ::");
                Console.WriteLine(" > " + string.Join(" ", bytes.Skip(loc).Take(32).Select(x => x.ToString("X"))));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Str: {TryReadStrResource(bytes, loc)}");
                Console.WriteLine();
                #endif
            }
            Console.WriteLine($"Found {dict.Count} potential hash-sum check calls");
            Console.WriteLine();
            var (hash, calls) = SelectCalls(dict)!;

            Console.Write($"Please verify that hashsum is ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(hash.GetLast(4));
            Console.ResetColor();
            Console.WriteLine(". If it is OK, press ENTER to continue, otherwise patch may cause game to crash.");
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" > {calls.Count} calls: {calls.Select(c => c.ToString("X")).StrJoin()}");
            Console.ForegroundColor = ConsoleColor.Gray;
            
            Console.ReadLine();
            var replacement = new byte[]
            {
                0x48, 0x89 , 0xd1, // MOV RCX,RDX
                0x90,              // NOP
                0x90,              // NOP
                0x90,              // NOP
                0x90,              // NOP
            };
            
            foreach (var call in calls)
            {
                Console.WriteLine($"Replacing @{call:X}::");
                Console.WriteLine(bytes.MakeBytesStr(call, 12));
                for (var i = 0; i < replacement.Length; i++)
                {
                    bytes[i + call + 3] = replacement[i];
                }
                Console.WriteLine(bytes.MakeBytesStr(call, 12));
            }
            
            File.WriteAllBytes(resultPath, bytes);
            Console.WriteLine("Done! Press ENTER to close window.");
            Console.ReadLine();
        }

        
        static (string hash, List<int> calls) SelectCalls(Dictionary<string, List<int>> dict)
        {
            // filter keys to try to find best match
            var keyValueArray = dict.Where(kv =>
            {
                if (kv.Value.Count == 1)
                {
                    return false;
                }

                return kv.Key.All(char.IsLetterOrDigit);
            }).ToArray();
            
            // if no keys found, throw
            if (!keyValueArray.Any())
            {
                Console.WriteLine("ERROR: Cannot find compare function call");
                Environment.Exit(1);
                throw null;
            }

            var idx = 0;
            
            // if multiple candidates, ask user to select correct one
            if (keyValueArray.Length > 1)
            {
                var options = keyValueArray.Select((kp) => kp.Key.GetLast(4))
                    .Append("Exit")
                    .ToArray();
                
                ConsoleUtil.SelectOption("Cannot determine correct candidate automatically. Please select correct hashsum manually", options,
                    (selectedIdx, value) =>
                    {
                        if (value == "Exit")
                        {
                            Environment.Exit(1);
                            throw null;
                        }

                        idx = selectedIdx;
                    });
            }

            return (keyValueArray[idx].Key, keyValueArray[idx].Value);
        }

        private static string TryReadStrResource(byte[] bytes, int offset)
        {
            var pointerOffset = BitConverter.ToInt32(bytes, offset + 6);
            if (pointerOffset <= 0 || offset + pointerOffset > bytes.Length)
            {
                return "INVALID";
            }

            var resOffset = (int)(pointerOffset + offset + 10 - 1024);

            #if DEBUG
            Console.WriteLine(string.Join(" ", BitConverter.GetBytes(pointerOffset).Select(x => x.ToString("X"))));
            Console.WriteLine("RES OFFSET: " + (resOffset + GhidraMemoryStart).ToString("X"));
            Console.WriteLine(" > " + string.Join(" ", bytes.Skip(resOffset).Take(32).Select(x => x.ToString("X"))));
            #endif
            return Encoding.ASCII.GetString(bytes, (int)(resOffset), 32);
        }

        public static IEnumerable<int> EnumerateLocations(byte[] bytes, BytesFinder finder)
        {
            var idx = 0;
            do
            {
                var r = finder.FindIndexIn(bytes, idx);
                if (r < idx)
                {
                    yield break;
                }
                
                yield return r;

                idx = r + 1;
                if (idx >= bytes.Length)
                {
                    break;
                }
            } while (idx >= 0);
        }
    }
}