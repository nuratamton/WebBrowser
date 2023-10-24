using Gtk;
using Utility;
using Network;
using BrowserHistory;
using favourites;
using UI = Gtk.Builder.ObjectAttribute;

namespace SimpleBrowser
{

    class Browser : Window
    {
        private const string DefaultHomePage = "https://www.hw.ac.uk/dubai/";
        private const string HomePageFilePath = "homePage.txt";

        private History browserHistory = new();
        public FavouriteStorage favouriteStorage = new();
        public FavouriteManager favouriteManager = new();
        private readonly ListStore historyStore = new(typeof(string));
        private readonly ListStore favouriteStore = new(typeof(string), typeof(string), typeof(string), typeof(string));

        [UI] private readonly Button? backButton = null;
        [UI] private readonly Button? nextButton = null;
        [UI] private readonly Entry? addressEntry = null;
        [UI] private readonly Button? refreshButton = null;
        [UI] private readonly Button? homeButton = null;
        [UI] private readonly TextView? contentTextView = null;
        [UI] private readonly Label? responseLabel = null;
        [UI] private readonly TreeView? historyTreeView = null;
        [UI] private readonly TreeView? favouriteTreeView = null;

        private Dialog? editHomePageDialog;
        private Dialog? favouriteNameDialog;
        private Dialog? editFavDialog;
        private Entry? urlEntry;
        private Entry? favouriteNameEntry;
        private Entry? editNameEntry;
        private Entry? editUrlEntry;
        private Button? okayButton;
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
            // historyScrolledWindow = (ScrolledWindow)builder.GetObject("historyScrolledWindow");
            // contentScrolledWindow = (ScrolledWindow)builder.GetObject("scrolledWindow");
            browserNotebook = (Notebook)builder.GetObject("notebook");
            favouriteNameDialog = (Dialog)builder.GetObject("FavouriteNameDialog");
            favouriteNameEntry = (Entry)builder.GetObject("nameEntry");
            editFavDialog = (Dialog)builder.GetObject("EditFavDialog");
            editNameEntry = (Entry)builder.GetObject("EditNameEntry");
            editUrlEntry = (Entry)builder.GetObject("EditUrlEntry");
            InitializeHistoryTreeView();
            InitializeFavouriteTreeView();
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
            if (favouriteTreeView != null)
            {
                favouriteTreeView.RowActivated += FavouriteTreeView_RowActivated;
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

        private void InitializeFavouriteTreeView()
        {
            if (favouriteTreeView != null)
            {
                favouriteTreeView.Model = favouriteStore;
                TreeViewColumn nameColumn = new() { Title = "Name" };
                CellRendererText nameCell = new();
                nameColumn.PackStart(nameCell, true);
                nameColumn.AddAttribute(nameCell, "text", 0);

                TreeViewColumn urlColumn = new() { Title = "URL" };
                CellRendererText urlCell = new();
                urlColumn.PackStart(urlCell, true);
                urlColumn.AddAttribute(urlCell, "text", 1);

                TreeViewColumn editColumn = new() { Title = "Edit" };
                CellRendererToggle editButton = new();
                editButton.Activatable = true; //clickable
                editButton.Toggled += OnEditClicked;
                editColumn.PackStart(editButton, false);
                editColumn.AddAttribute(editButton, "active", 2);
                // editColumn.Visible = true;

                TreeViewColumn deleteColumn = new() { Title = "Delete" };
                CellRendererToggle deleteButton = new();
                deleteButton.Activatable = true; //clickable
                deleteButton.Toggled += OnDeleteClicked;
                deleteColumn.AddAttribute(deleteButton, "active", 3);
                deleteColumn.PackStart(deleteButton, false);
                deleteColumn.FixedWidth = 50;

                // To adjust UI spacing
                TreeViewColumn paddingColumn = new TreeViewColumn();
                paddingColumn.FixedWidth = 20;

                favouriteTreeView.AppendColumn(nameColumn);
                favouriteTreeView.AppendColumn(urlColumn);
                favouriteTreeView.AppendColumn(editColumn);
                favouriteTreeView.AppendColumn(deleteColumn);
                favouriteTreeView.AppendColumn(paddingColumn);
            }
        }
        private void FavouriteTreeView_RowActivated(object sender, RowActivatedArgs args)
        {
            TreeIter iter;
            if (favouriteStore.GetIter(out iter, args.Path))
            {
                string url = (string)favouriteStore.GetValue(iter, 1);
                if (addressEntry != null)
                {
                    addressEntry.Text = url;
                    LoadUrl(url);
                }
            }
        }

        private void InitializeHistoryTreeView()
        {
            if (historyTreeView != null)
            {
                historyTreeView.Model = historyStore;

                // Create a column for the URL
                TreeViewColumn urlColumn = new TreeViewColumn { Title = "History" };

                // Create a renderer for the column data
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
            historyStore.Clear();
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
        private void DisplayFavouriteList(object sender, EventArgs e)
        {
            FavouriteManager favouriteManager = new();
            List<Favourite> loadedFavorites = favouriteManager.DisplayFavourites();
            favouriteStore.Clear();
            foreach (var fav in loadedFavorites)
            {
                Console.WriteLine("From favorites page" + fav.URL);
                favouriteStore.AppendValues(fav.Name, fav.URL, false, false);
            }

            if (browserNotebook != null)
            {
                int currentPage = browserNotebook.CurrentPage;
                browserNotebook.CurrentPage = currentPage == 0 ? 2 : 0;
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

        private string PromptForFavouriteName()
        {

            // Reset the entry each time it's shown
            if (favouriteNameEntry != null)
            {
                favouriteNameEntry.Text = "";
            }

            string? favouriteName = null;

            // Show the dialog and check the response
            if (favouriteNameDialog != null && favouriteNameEntry != null)
            {
                if (favouriteNameDialog.Run() == (int)ResponseType.Ok && !string.IsNullOrEmpty(favouriteNameEntry.Text))
                {
                    favouriteName = favouriteNameEntry.Text;
                }

                // Hide the dialog after use
                favouriteNameDialog.Hide();
            }
            if (favouriteName != null)
            {
                return favouriteName;
            }
            return "";

        }

        private void on_favouriteButton_clicked(object sender, EventArgs e)
        {
            Console.WriteLine("Fav button clicked");

            // Check if the addressEntry has a value, if not, just return
            if (addressEntry == null || string.IsNullOrEmpty(addressEntry.Text))
            {
                Console.WriteLine("No URL available to add to favourites.");
                return;
            }
            string favouriteName = PromptForFavouriteName();

            // Create a new favourite item
            var favouriteItem = new Favourite()
            {
                Name = favouriteName,
                URL = addressEntry.Text
            };

            // Add to favourites and save
            favouriteManager.AddFavourite(favouriteItem);

            Console.WriteLine($"Added to favourites: {addressEntry.Text}");
            Console.WriteLine("Fav button clicked");
            List<Favourite> favList = favouriteManager.DisplayFavourites();
            favouriteStorage.SaveFavorites(favList);
        }

        private (string, string) ShowEditFavDialog(string currentName, string currentURL)
        {
            if (editNameEntry == null || editUrlEntry == null || editFavDialog == null)
            {
                // Log or notify the user about the missing controls.
                Console.WriteLine("One or more controls are not initialized.");
                return (string.Empty, string.Empty);
            }
            editNameEntry.Text = currentName;
            editUrlEntry.Text = currentURL;

            editFavDialog.ShowAll();
            var response = editFavDialog.Run();

            string newName = editNameEntry.Text.Trim();
            string newURL = editUrlEntry.Text.Trim();

            editFavDialog.Destroy();
            if (response == (int)ResponseType.Ok)
            {
                return (newName, newURL);
            }
            return (string.Empty, string.Empty);

        }
        private void OnEditClicked(object sender, ToggledArgs args)
        {
            // determine which item is treeview is selected
            TreeIter iter;
            var selectedRows = favouriteTreeView.Selection.GetSelectedRows();
            if (selectedRows.Length > 0 && favouriteStore.GetIter(out iter, selectedRows[0]))
            {
                // getting name and URL of that item
                var favName = (string)favouriteStore.GetValue(iter, 0);
                var favURL = (string)favouriteStore.GetValue(iter, 1);
                // show dialog with those values as default
                var (newName, newURL) = ShowEditFavDialog(favName, favURL);

                if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newURL))
                {
                    // updating the existing item in file
                    Favourite existingFav = new Favourite { Name = favName, URL = favURL };
                    favouriteManager.ModifyFavourite(newName, newURL, existingFav);
                    // update in treeview
                    favouriteStore.SetValue(iter, 0, newName);
                    favouriteStore.SetValue(iter, 1, newURL);
                }
            }
        }

        private void OnDeleteClicked(object sender, ToggledArgs args)
        {
            TreeIter iter;
            if (favouriteStore.GetIter(out iter, new TreePath(args.Path)))
            {
                string name = (string)favouriteStore.GetValue(iter, 0);
                string url = (string)favouriteStore.GetValue(iter, 1);
                Favourite favToDelete = new Favourite { Name = name, URL = url };

                // Remove it from the favourite manager and update JSON
                favouriteManager.RemoveFavourite(favToDelete);

                // Remove the entry from the TreeView
                favouriteStore.Remove(ref iter);
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
                    }

                    browserHistory.Visit(url);
                    UpdateNavigationButtonsState();
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
