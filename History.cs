using System.Runtime.InteropServices;

namespace BrowserHistory
{
    // Represents every URL visited, works as a doubly linked list
    public class Node
    {
        // stores the URL of the webpage
        public string? Url { get; set; }
        // points to previous visited node
        public Node? Previous { get; set; }
        // points to next visited node
        public Node? Next { get; set; }
    }

    // class to manage and keep track of history
    public class History
    {
        // Current points to the current URL being visited
        private Node? Current;
        private readonly List<string> historyList = new();
        private string? historyFilePath;

        public History()
        {
            SetHistoryFilePath();
            LoadHistory();
        }

        public string CurrentUrl => Current?.Url ?? string.Empty;
        public bool CanMoveBack => Current?.Previous != null;
        public bool CanMoveForward => Current?.Next != null;

        public void Visit(string url)
        {

            Console.WriteLine("Visit called with" + url);
            // if current url is the same just return
            if (Current != null && Current.Url == url)
            {
                return;
            }
            // remove if url already there
            if (historyList.Contains(url))
            {
                historyList.Remove(url);
            }

            // Add url to the beginning
            historyList.Insert(0, url);

            // creating a new node for the url we are visiting
            var newNode = new Node
            {
                Url = url,
                Previous = Current
            };
            if (Current != null)
            {
                // next of the node we are on becomes the new node
                Current.Next = newNode;
            }
            // current becomes the new node
            Current = newNode;
            SaveHistory();
        }
        public bool MoveBack()
        {
            // if previous node exists
            if (!CanMoveBack)
            {
                return false;
            }
            // update the current node to the previous node
            Current = Current?.Previous;
            return true;
        }

        public bool MoveForward()
        {
            // if next node exists
            if (!CanMoveForward)
            {
                return false;
            }
            // update the next node as the current node
            Current = Current?.Next;
            return true;
        }

        // gets all URLs from browser history
        public List<string> GetAllHistoryUrls()
        {
            var currentSessionUrls = new List<string>();
            var temp = Current;

            // gets all URLs from the current session
            while (temp != null)
            {
                if (temp.Url != null && !currentSessionUrls.Contains(temp.Url))
                {
                    currentSessionUrls.Add(temp.Url); ;
                }
                temp = temp.Previous;
            }

            // get urls in file and combine it with current 
            var allHistory = new HashSet<string>(historyList);
            currentSessionUrls.AddRange(historyList);

            // var orderedHistory = allHistory.ToList();
            return allHistory.ToList();
        }
        private void SetHistoryFilePath()
        {
            string appFolder;
            string appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                appFolder = Path.Combine(appDataFolder, appName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string libraryFolder = Path.Combine(homeFolder, "Library");
                appFolder = Path.Combine(libraryFolder, "Application Support", appName);
            }
            else
            {
                // For Linux or other OS
                string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                appFolder = Path.Combine(homeFolder, "." + appName);
            }

            Directory.CreateDirectory(appFolder);
            historyFilePath = Path.Combine(appFolder, "history.txt");
        }

        private void SaveHistory()
        {
            try
            {
                // Retrieves all Urls into a list
                List<string> historyList = GetAllHistoryUrls();
                
                using StreamWriter sw = new(historyFilePath);
                // each URL in list is added to the file
                foreach (var item in historyList)
                {
                    sw.WriteLine(item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save history. Error:" + ex.Message);
            }
        }

        public void LoadHistory()
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    using StreamReader sr = new(historyFilePath);
                    string? line;
                    // for each URL in the file, add to a list
                    while ((line = sr.ReadLine()) != null)
                    {
                        historyList.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load history. Error:" + ex.Message);
            }
        }
    }
}
