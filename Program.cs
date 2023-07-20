using System.Drawing;
using System.Drawing.Imaging;
using p4KFinder;


Console.WriteLine("Enter your directory: ");

string parentDir = Console.ReadLine() ?? "";
if (!Directory.Exists(parentDir)) return;

int dupFileNames = 0;

string newDir = parentDir + $"\\pseudo4K_";

List<string> list = GetAllPseudo4kImages(parentDir).ToList();

Directory.CreateDirectory(newDir);


foreach (var img in list)
{
    string linkName = newDir + "\\" + Path.GetFileName(img) + "_LINK";
    int ctr = 0;
    while (File.Exists(NewName(linkName, ctr)))
    {
        ctr++;
        dupFileNames++;
    }
    File.CreateSymbolicLink(NewName(linkName, ctr), img);

    Console.WriteLine($"Link created: {linkName}");
}

Console.WriteLine($"Finished. Created {list.Count()} links. Duplicate file names: {dupFileNames}");

string NewName(string name, int ctr)
{
    return name + $" ({ctr})";
}



List<string> GetAllFiles(string directory)
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

IEnumerable<string> GetAllPseudo4kImages(string dir)
{
    var result = new List<string>();

    foreach (string path in Directory.GetFiles(dir))
    {
        try
        {
            using (var imageStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var data = Image.FromStream(imageStream, false, false))
            {
                if (data.Width > 1920 && data.Height > 1920)
                {
                    result.Add(path);
                    Console.WriteLine($"Image found: {path}");
                }
            }
        }
        catch { }
    }

    foreach (string path in Directory.GetDirectories(dir))
    {
        result.AddRange(GetAllPseudo4kImages(path));
    }

    return result;
}




