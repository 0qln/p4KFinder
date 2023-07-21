using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using p4KFinder;


class Program
{
    private static (FindObjective Type, int MonitorIndex) _objective;
    private static int _combinedWidth;
    private static int _combinedHeight;

    static void Main(string[] args)
    {

        Helper.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Helper.MonitorEnumCallback, IntPtr.Zero); 
        int furthestLeft = Helper.MonitorRects[0].Left;
        int furthestRight = Helper.MonitorRects[0].Right;
        int furthestTop = Helper.MonitorRects[0].top;
        int furthestBottom = Helper.MonitorRects[0].bottom;
        for (int i = 1; i < Helper.Monitors.Count; i++)
        {
            if (Helper.MonitorRects[i].Left < furthestLeft) furthestLeft = Helper.MonitorRects[i].Left;
            if (Helper.MonitorRects[i].Right > furthestRight) furthestRight = Helper.MonitorRects[i].Right;
            if (Helper.MonitorRects[i].top < furthestTop) furthestTop = Helper.MonitorRects[i].top;
            if (Helper.MonitorRects[i].bottom > furthestBottom) furthestBottom = Helper.MonitorRects[i].bottom;
        }
        _combinedWidth = Math.Abs(furthestLeft - furthestRight);
        _combinedHeight = Math.Abs(furthestTop - furthestBottom);
        Console.WriteLine($"    Combined width: {_combinedWidth}");
        Console.WriteLine($"    Combined hight: {_combinedHeight}");


        Console.WriteLine("Enter your directory: ");
        string parentDir = Console.ReadLine() ?? "";
        if (!Directory.Exists(parentDir))
        {
            Console.WriteLine("\nDirectory does not exist.\n");
            return;
        }

        Console.WriteLine(
            "\n" +
            "What do you want to find? \n" +
            "   ( [0]: Pseudo 4k images) \n" +
            "   ( [1]: All images that are the size of or bigger than your first monitor)\n" +
            "   ( [2]: All images that are the exactly size of your first monitor)\n" +
            "   ( [3]: All images that are the size of or bigger than your second monitor)\n" +
            "   ( [4]: All images that are the exactly size of your second monitor)\n" +
            "   ( [5]: All images)\n" +
           $"   ( [6]: All images that have a resolution high enough to stretch over all your monitors (min.{_combinedWidth}x{_combinedHeight}))\n" +
           $"   ( [7]: All images that have the exact resolution, like all your monitors combined, when arranged in your setup({_combinedWidth}x{_combinedHeight}))\n" +
            "   ( [8_index]: All images that are the size of or bigger than your [index] monitor)\n" +
            "   ( [9_index]: All images that are the exactly size of your [index] monitor)" 
        );

        string[] input = Console.ReadLine()?.Split('_') ?? new string[0];
        try
        {
            _objective.Type = (FindObjective)int.Parse(input[0]);
        }
        catch (Exception e)
        {
            Console.WriteLine("Invalid objective.");
            Console.WriteLine(e.ToString());
            return;
        }

        if (input.Length == 2)
        {
            try
            {
                _objective.MonitorIndex = int.Parse(input[0]);

                if (_objective.MonitorIndex < 0 || _objective.MonitorIndex >= Helper.Monitors.Count)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid monitorIndex.");
                Console.WriteLine(e.ToString());
                return;
            }
        }


        int dupFileNames = 0;

        List<string> list = GetAllPseudo4kImages(parentDir).Result.ToList();

        string newDir = GetUniqePathName($"pseudo4k_{Enum.GetName(_objective.Type)}", parentDir); // clean this up
        Directory.CreateDirectory(newDir);


        foreach (var img in list)
        {
            string linkName = GetUniqePathName(Path.GetFileName(img), newDir, ref dupFileNames);
            File.CreateSymbolicLink(linkName, img);

            Console.WriteLine($"Link created: {linkName}");
        }

        Console.WriteLine($"Finished. Created {list.Count()} links. Duplicate file names: {dupFileNames}");
    }


    static string NewName(string name, int ctr)
    {
        return name + $" ({ctr})";
    }

    static string GetUniqePathName(string name, string parentFolder, ref int counter)
    {
        string newName = parentFolder + "\\" + name;
        int ctr = 0;
        while (Path.Exists(NewName(newName, ctr)))
        {
            ctr++;
            counter++;
        }
        return NewName(newName, ctr);
    }
    static string GetUniqePathName(string name, string parentFolder)
    {
        string newName = parentFolder + "\\" + name;
        int ctr = 0;
        while (Path.Exists(NewName(newName, ctr)) || Directory.Exists(NewName(newName, ctr)))
        {
            ctr++;
        }
        return NewName(newName, ctr);
    }

    static List<string> GetAllFiles(string directory)
    {
        var result = new List<string>();

        foreach (var file in Directory.GetFiles(directory))
            if (!result.Contains(file))
                result.Add(file);

        foreach (var dir in Directory.GetDirectories(directory))
        {
            var data = GetAllFiles(dir);
            foreach (var file in data)
                if (!result.Contains(file))
                    result.Add(file);
        }
        return result;
    }

    static async ValueTask<IEnumerable<string>> GetAllPseudo4kImages(string dir)
    {
        var result = new List<string>();

        foreach (string path in Directory.GetFiles(dir))
        {
            if (OptAddPath(ref result, in path))
                Console.WriteLine($"Image found: {path}");
        }

        var tasks = new List<Task>();
        foreach (string path in Directory.GetDirectories(dir))
        {
            tasks.Add(Task.Run(() => result.AddRange(GetAllPseudo4kImages(path).Result)));
        }
        await Task.WhenAll(tasks);

        return result;
    }

    private static bool OptAddPath(ref List<string> result, in string path)
    {
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            {
                return false; // Skip if it's a symbolic link
            }

            using var imageStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var data = Image.FromStream(imageStream, false, false);
            switch (_objective.Type)
            {
                case FindObjective.Pseudo4k:
                    if (data.Width > 1920 && data.Height > 1920)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.Monitor1_ExactAndBigger:
                    if (data.Width >= Helper.Monitors[0].Width && data.Height >= Helper.Monitors[0].Height)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.Monitor1_Exact:
                    if (data.Width == Helper.Monitors[0].Width && data.Height == Helper.Monitors[0].Height)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.Monitor2_ExactAndBigger:
                    if (data.Width >= Helper.Monitors[1].Width && data.Height >= Helper.Monitors[1].Height)
                        result.Add(path);
                    else
                        return false;
                    break;


                case FindObjective.Monitor2_Exact:
                    if (data.Width == Helper.Monitors[1].Width && data.Height == Helper.Monitors[1].Height)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.All:
                    result.Add(path);
                    return true;

                case FindObjective.AllMonitors_ExactAndBigger:
                    if (data.Width >= _combinedWidth && data.Height >= _combinedHeight)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.AllMonitors_Exact:
                    if (data.Width == _combinedWidth && data.Height == _combinedHeight)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.MonitorIndex_ExactAndBigger:
                    if (data.Width >= Helper.Monitors[0].Width && data.Height >= Helper.Monitors[0].Height)
                        result.Add(path);
                    else
                        return false;
                    break;

                case FindObjective.MonitorIndex_Exact:
                    if (data.Width == Helper.Monitors[_objective.MonitorIndex].Width && data.Height == Helper.Monitors[_objective.MonitorIndex].Height)
                        result.Add(path);
                    else
                        return false;
                    break;

            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}



