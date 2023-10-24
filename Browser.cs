using Gtk;
using Utility;
using Network;
using BrowserHistory;
using UI = Gtk.Builder.ObjectAttribute;

namespace SimpleBrowser
{

    class Browser : Window
    {
        private History browserHistory = new();
        private ListStore historyStore = new(typeof(string));
        private const string DefaultHomePage = "https://www.hw.ac.uk/dubai/";
        private const string HomePageFilePath = "homePage.txt";

        [UI] private readonly Button? backButton = null;
        [UI] private readonly Button? nextButton = null;
        [UI] private readonly Entry? addressEntry = null;
        [UI] private readonly Button? refreshButton = null;
        [UI] private readonly Button? homeButton = null;
        [UI] private readonly TextView? contentTextView = null;
        [UI] private readonly Label? responseLabel = null;
        [UI] private readonly TreeView? historyTreeView = null;
        // [UI] private readonly Notebook? browserNotebook = null;

        private Dialog? editHomePageDialog;
        private Entry? urlEntry;
        private Button? okayButton;
        private ScrolledWindow historyScrolledWindow;
        private ScrolledWindow contentScrolledWindow;
        private Notebook? browserNotebook;

        private DateTime lastBackButtonClicked = DateTime.MinValue;
        private DateTime lastNextButtonClicked = DateTime.MinValue;
        private DateTime lastActivated = DateTime.MinValue;
        private string _homePageURL = DefaultHomePage;
        private string HomePageURL
        {
            get { return _homePageURL; }
            set { _homePageURL = value; }
        }

        public Browser() : this(new Builder("WebBrowser.browserGUI.glade"))
        {
            
            UpdateNavigationButtonsState();
            browserHistory.LoadHistory();
            _homePageURL = LoadHomePageUrlFromFile();
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }
        }

        private Browser(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);

            InitializeComponents(builder);
            AttachEvents();
            CheckDefaultUrl();
            DeleteEvent += Window_DeleteEvent;
        }

        private void InitializeComponents(Builder builder)
        {
            editHomePageDialog = (Dialog)builder.GetObject("EditHomePage");
            urlEntry = (Entry)builder.GetObject("urlEntry");
            okayButton = (Button)builder.GetObject("OkayButton");
            historyScrolledWindow = (ScrolledWindow)builder.GetObject("historyScrolledWindow");
            contentScrolledWindow = (ScrolledWindow)builder.GetObject("scrolledWindow");
            browserNotebook = (Notebook)builder.GetObject("notebook");
            historyScrolledWindow.Add(historyTreeView);
            InitializeHistoryTreeView();
            // historyScrolledWindow.Hide();
        }
        private void AttachEvents()
        {
            if (backButton != null)
            {
                backButton.Clicked += (s, e) => BackButton_clicked(backButton, e);
            }
            if (nextButton != null)
            {
                nextButton.Clicked += (s, e) => NextButton_clicked(nextButton, e);
            }
            if (refreshButton != null)
            {
                refreshButton.Clicked += (s, e) => RefreshButton_clicked(refreshButton, e);
            }
            if (homeButton != null)
            {
                homeButton.Clicked += (s, e) => HomeButton_clicked(homeButton, e);
            }
            if (addressEntry != null)
            {
                addressEntry.Activated += (s, e) => AddressBar_activated(addressEntry, e);
            }
            if (okayButton != null)
            {
                okayButton.Clicked += OnOkayButtonClicked;
            }
            if (historyTreeView != null)
            {
                historyTreeView.RowActivated += HistoryTreeView_RowActivated;
            }
        }
        private void CheckDefaultUrl()
        {
            if (File.Exists(HomePageFilePath))
            {
                string defaultUrl = File.ReadAllText(HomePageFilePath).Trim();
                if (addressEntry != null)
                {
                    addressEntry.Text = defaultUrl;
                    LoadUrl(addressEntry.Text);
                }
            }
            if (string.IsNullOrEmpty(addressEntry?.Text))
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = DefaultHomePage;
                    LoadUrl(addressEntry.Text);
                }
            }
        }
        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        private void UpdateNavigationButtonsState()
        {
            if (backButton != null)
            {
                backButton.Sensitive = browserHistory.CanMoveBack;
            }
            if (nextButton != null)
            {
                nextButton.Sensitive = browserHistory.CanMoveForward;
            }
        }

        private void InitializeHistoryTreeView()
        {
            if (historyTreeView != null)
            {
                historyTreeView.Model = historyStore;

                // Create a column for the URL
                TreeViewColumn urlColumn = new TreeViewColumn { Title = "History" };

                // Create a renderer for the column data (text in this case)
                CellRendererText urlCell = new CellRendererText();

                // Add the cell renderer to the column and bind the data
                urlColumn.PackStart(urlCell, true);
                urlColumn.AddAttribute(urlCell, "text", 0);

                // Add the column to the TreeView
                historyTreeView.AppendColumn(urlColumn);
            }
        }

        private void DisplayHistoryList(object sender, EventArgs e)
        {
            List<string> loadedHistory = browserHistory.GetAllHistoryUrls();
            historyScrolledWindow.Opacity = 1.0;
            historyStore.Clear();  // Clear previous entries
            foreach (var url in loadedHistory)
            {
                Console.WriteLine("From history page" + url);
                historyStore.AppendValues(url);
            }
            if (browserNotebook != null)
            {
                int currentPage = browserNotebook.CurrentPage;
                browserNotebook.CurrentPage = currentPage == 0 ? 1 : 0;
            }
        }
        private void HistoryTreeView_RowActivated(object sender, RowActivatedArgs args)
        {
            TreeIter iter;
            if (historyStore.GetIter(out iter, args.Path))
            {
                string url = (string)historyStore.GetValue(iter, 0);
                if (addressEntry != null)
                {
                    addressEntry.Text = url;
                    LoadUrl(url);
                }
            }
        }

        private void BackButton_clicked(object sender, EventArgs e)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            if ((DateTime.Now - lastBackButtonClicked).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double back button click detected. Ignoring.");
                return;
            }
            lastBackButtonClicked = DateTime.Now;

            if (browserHistory.MoveBack())
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = browserHistory.CurrentUrl;
                    Console.WriteLine($"Loading URL from BackButton_clicked: {addressEntry.Text}");
                    LoadUrl(addressEntry.Text);
                }

                UpdateNavigationButtonsState();
            }
        }

        private void NextButton_clicked(object sender, EventArgs e)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            if ((DateTime.Now - lastNextButtonClicked).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double next button click detected. Ignoring.");
                return;
            }
            lastNextButtonClicked = DateTime.Now;
            if (browserHistory.MoveForward())
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = browserHistory.CurrentUrl;
                    LoadUrl(addressEntry.Text);
                }

                UpdateNavigationButtonsState();
            }
        }
        private void RefreshButton_clicked(object sender, EventArgs e)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }
            if (contentTextView != null)
            {
                contentTextView.Buffer.Text = "";
            }

            string currentUrl = browserHistory.CurrentUrl;
            if (addressEntry != null)
            {
                addressEntry.Text = currentUrl;
                LoadUrl(addressEntry.Text);
            }
            else if (addressEntry != null && string.IsNullOrEmpty(addressEntry.Text))
            {
                addressEntry.Text = HomePageURL;
                LoadUrl(addressEntry.Text);
            }

        }

        private void HomeButton_clicked(object sender, EventArgs e)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }
            if (addressEntry != null)
            {
                addressEntry.Text = _homePageURL;
                LoadUrl(addressEntry.Text);
            }

        }

        private void AddressBar_activated(object sender, EventArgs e)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }
            if ((DateTime.Now - lastActivated).TotalMilliseconds < 500) // 500ms threshold, adjust as needed
            {
                Console.WriteLine("Double activation detected. Ignoring.");
                return;
            }
            lastActivated = DateTime.Now;

            Console.WriteLine("AddressBar_activated called.");
            if (addressEntry != null)
            {

                LoadUrl(addressEntry.Text);


            }
        }
        private async void LoadUrl(string url)
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }
            if (HtmlUtility.IsValidUrl(url))
            {
                var result = await NetworkManager.LoadUrl(url);
                if (responseLabel != null)
                {
                    responseLabel.Text = "";
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    if (responseLabel != null)
                    {
                        responseLabel.Text = result.ErrorMessage;
                    }
                }
                else
                {
                    if (contentTextView != null && contentTextView.Buffer != null)
                    {
                        contentTextView.Buffer.Text = result.Body;
                    }
                    string title = HtmlUtility.ExtractTitle(result.Body);

                    if (responseLabel != null)
                    {
                        responseLabel.Text = (int)result.StatusCode + " | " + result.ReasonPhrase + "\n" + title;
                        // responseLabel.Text = (int)result.StatusCode switch
                        // {
                        //     200 => "200 | OK - " + title,
                        //     400 => "400 | Bad Request",
                        //     403 => "403 | Forbidden",
                        //     404 => "404 | Not Found",
                        //     _ => (int)result.StatusCode + "|" + result.ReasonPhrase,
                        // };
                    }

                    browserHistory.Visit(url);
                    UpdateNavigationButtonsState();  // Update navigation buttons state after a new URL is visited
                }
            }
            else
            {
                if (responseLabel != null)
                {
                    responseLabel.Text = "Invalid URL";
                }

            }

        }

        public void ShowEditHomePageDialog(object sender, EventArgs e)
        {
            if (editHomePageDialog != null)
            {
                editHomePageDialog.Run();
                editHomePageDialog.Hide();
            }

        }
        private void SaveHomePageUrlToFile(string url)
        {
            File.WriteAllText(HomePageFilePath, url);
        }

        private string LoadHomePageUrlFromFile()
        {
            if (File.Exists(HomePageFilePath))
            {
                return File.ReadAllText(HomePageFilePath).Trim();
            }
            return DefaultHomePage;
        }

        private void OnOkayButtonClicked(object? sender, EventArgs e)
        {
            if (urlEntry != null)
            {
                string enteredurl = urlEntry.Text;
                if (HtmlUtility.IsValidUrl(enteredurl))
                {
                    _homePageURL = enteredurl;
                    SaveHomePageUrlToFile(enteredurl);
                    if (editHomePageDialog != null)
                    {
                        editHomePageDialog.Hide();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid URL entered");
                }
            }
        }
    }
}
