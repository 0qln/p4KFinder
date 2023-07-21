using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using p4KFinder;


class Program
{
    static FindObjective objective;

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

        int combinedWidth = Math.Abs(furthestLeft - furthestRight);
        int combinedHeight = Math.Abs(furthestTop - furthestBottom);

        Console.WriteLine($"Combined height: {combinedHeight}");
        Console.WriteLine($"Combined width: {combinedWidth}");

        Console.WriteLine($"    Combined width: {combinedWidth}");
        Console.WriteLine($"    Combined hight: {combinedHeight}");

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
            "   ( [1.1]: All images that are the size of or bigger than your first monitor)\n" +
            "   ( [1.2]: All images that are the exactly size of your first monitor)\n" +
            "   ( [2.1]: All images that are the size of or bigger than your second monitor)\n" +
            "   ( [2.2]: All images that are the exactly size of your second monitor)\n" +
            "   ( [3]: All images)\n" +
           $"   ( [4]: All images that have a resolution high enough to stretch over all your monitors ({combinedWidth}x{combinedHeight}))\n" + // TODO
           $"   ( [4.1]: All images that have the exact resolution, like all your monitors combined, when arranged in your setup({combinedWidth}x{combinedHeight}))\n" + // TODO
            "   ( [5[index]]: All images that are the size of or bigger than your [index] monitor)\n" + //TODO
            "   ( [5.1[index]]: All images that are the exactly size of your [index] monitor)" //TODO
        );

        string? input = Console.ReadLine();
        switch (input)
        {
            case "0":
                objective = FindObjective.Pseudo4k;
                break;
            case "1.1":
                objective = FindObjective.Monitor1_ExactAndBigger;
                break;
            case "1.2":
                objective = FindObjective.Monitor1_Exact;
                break;
            case"2.1":
                objective = FindObjective.Monitor2_ExactAndBigger;
                break;
            case "2.2":
                objective = FindObjective.Monitor2_Exact;
                break;
            case "3":
                objective = FindObjective.All;
                break;

            default:
                Console.WriteLine("Not an option.");
                return;
        }

        int dupFileNames = 0;

        List<string> list = GetAllPseudo4kImages(parentDir).ToList();

        string newDir = GetUniqePathName($"pseudo4k_{Enum.GetName(objective)}", parentDir);
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

    static IEnumerable<string> GetAllPseudo4kImages(string dir)
    {
        var result = new List<string>();

        foreach (string path in Directory.GetFiles(dir))
        {
            if (OptAddPath(ref result, in path))
                Console.WriteLine($"Image found: {path}");
        }

        foreach (string path in Directory.GetDirectories(dir))
        {
            result.AddRange(GetAllPseudo4kImages(path));
        }

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

            using (var imageStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var data = Image.FromStream(imageStream, false, false))
            switch (objective)
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

            }
            return true;
        }
        catch 
        {
            return false;    
        }
    }
}



