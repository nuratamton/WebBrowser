using Gtk;
using Utility;
using Network;
using BrowserHistory;
using favourites;
using UI = Gtk.Builder.ObjectAttribute;
using System.Text;

namespace SimpleBrowser
{
    // Represents the main window
    class Browser : Window
    {
        private const string HomePageFilePath = "homePage.txt";
        private static string DefaultHomePage = File.ReadAllText(HomePageFilePath).ToString();

        private NavigationHandler navigationHandler;
        private readonly AccelGroup accelGroup = new();
        private readonly History browserHistory = new();
        private readonly FavouriteManager favouriteManager = new();
        private readonly ListStore historyStore = new(typeof(string));
        private readonly ListStore favouriteStore = new(typeof(string), typeof(string), typeof(bool), typeof(bool));

        // UI components
        [UI] private readonly Button? backButton = null;
        [UI] private readonly Button? nextButton = null;
        [UI] private readonly Entry? addressEntry = null;
        [UI] private readonly Button? refreshButton = null;
        [UI] private readonly Button? homeButton = null;
        [UI] private readonly Button? downloadButton = null;
        [UI] private readonly TextView? contentTextView = null;
        [UI] private readonly Label? responseLabel = null;
        [UI] private readonly TreeView? historyTreeView = null;
        [UI] private readonly TreeView? favouriteTreeView = null;

        // UI Elements
        private Dialog? editHomePageDialog;
        private Dialog? favouriteNameDialog;
        private Dialog? editFavDialog;
        private Entry? urlEntry;
        private Entry? favouriteNameEntry;
        private Entry? editNameEntry;
        private Entry? editUrlEntry;
        private Button? okayButton;
        private Notebook? browserNotebook;
        private FileChooserButton? fileChooserButton;


        private DateTime lastActivated = DateTime.MinValue;
        private DateTime lastDownloadButtonClicked = DateTime.MinValue;

        // Manages home page URL
        private string _homePageURL = DefaultHomePage;
        private string HomePageURL
        {
            get { return _homePageURL; }
            set { _homePageURL = value; }
        }

        // Constructor
        public Browser() : this(new Builder("WebBrowser.browserGUI.glade"))
        {
            // methods called on load
            UpdateNavigationButtonsState();
            browserHistory.LoadHistory();
            _homePageURL = LoadHomePageUrlFromFile();
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

        }

        // Constructor to handle the GTK Builder objects
        private Browser(Builder builder) : base(builder.GetObject("MainWindow").Handle)
        {
            builder.Autoconnect(this);
            this.Title = "Nura's Browser";
            InitializeComponents(builder);
            AddAccelGroup(accelGroup);
            AttachEvents();
            CheckDefaultUrl();
            DeleteEvent += Window_DeleteEvent;
            navigationHandler = new NavigationHandler(
                                                browserNotebook: browserNotebook!,
                                                addressEntry: addressEntry!,
                                                contentTextView: contentTextView!,
                                                LoadUrlFunction: LoadUrl,
                                                promptForFavouriteName: PromptForFavouriteName,
                                                historyInstance: browserHistory,
                                                favouriteManager: favouriteManager,
                                                _homePageURL: _homePageURL,
                                                HomePageURL: HomePageURL
                );
        }

        // Initializes UI components and attaches them to the builder
        private void InitializeComponents(Builder builder)
        {
            editHomePageDialog = (Dialog)builder.GetObject("EditHomePage");
            urlEntry = (Entry)builder.GetObject("urlEntry");
            okayButton = (Button)builder.GetObject("OkayButton");
            browserNotebook = (Notebook)builder.GetObject("notebook");
            favouriteNameDialog = (Dialog)builder.GetObject("FavouriteNameDialog");
            favouriteNameEntry = (Entry)builder.GetObject("nameEntry");
            editFavDialog = (Dialog)builder.GetObject("EditFavDialog");
            editNameEntry = (Entry)builder.GetObject("EditNameEntry");
            editUrlEntry = (Entry)builder.GetObject("EditUrlEntry");
            fileChooserButton = (FileChooserButton)builder.GetObject("fileChooserButton");
            InitializeHistoryTreeView();
            InitializeFavouriteTreeView();

        }

        // Attaches events to the UI components
        private void AttachEvents()
        {
            if (backButton != null)
            {
                // Gdk.ModifierType.ControlMask -> CTRL KEY; Gdk.ModifierType.Mod1Mask-> ALT; Gdk.ModifierType.MetaMask-> CMD
                backButton.AddAccelerator("clicked", accelGroup, (uint)Gdk.Key.Left, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                backButton.Clicked += (s, e) => BackButtonClicked(backButton, e);
            }
            if (nextButton != null)
            {
                nextButton.AddAccelerator("clicked", accelGroup, (uint)Gdk.Key.Right, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                nextButton.Clicked += (s, e) => NextButtonClicked(nextButton, e);
            }
            if (refreshButton != null)
            {
                refreshButton.AddAccelerator("clicked", accelGroup, (uint)Gdk.Key.R, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                refreshButton.Clicked += (s, e) => RefreshButtonClicked(refreshButton, e);
            }
            if (homeButton != null)
            {
                homeButton.Clicked += (s, e) => HomeButtonClicked(homeButton, e);
            }
            if (downloadButton != null)
            {
                downloadButton.AddAccelerator("clicked", accelGroup, (uint)Gdk.Key.D, Gdk.ModifierType.ControlMask, Gtk.AccelFlags.Visible);
                downloadButton.Clicked += (s, e) => DownloadButtonClicked(downloadButton, e);
            }
            if (addressEntry != null)
            {
                addressEntry.Activated += (s, e) => AddressBarActivated(addressEntry, e);
            }
            if (fileChooserButton != null)
            {
                fileChooserButton.FileActivated += (s, e) => FileButtonFileSet(fileChooserButton, e);

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

        // Event handler for when the main window is closes
        private void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }

        // Sets the default page on startup
        private void CheckDefaultUrl()
        {
            if (File.Exists(HomePageFilePath))
            {
                string defaultUrl = File.ReadAllText(HomePageFilePath).Trim();
                if (addressEntry != null)
                {
                    addressEntry.Text = defaultUrl;
                    _ = LoadUrl(addressEntry.Text, false);
                }
            }
            if (string.IsNullOrEmpty(addressEntry?.Text))
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = DefaultHomePage;
                    _ = LoadUrl(addressEntry.Text, false);
                }
            }
        }

        // Updates  navigation state of back and next buttons
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

        // BUTTONS

        // Event handler for the back button
        private void BackButtonClicked(object sender, EventArgs e)
        {
            // Calls the method to handle the naviagtion
            navigationHandler.BackButton();
            // Update the state of the button
            UpdateNavigationButtonsState();
        }

        // Event handler for the next button
        private void NextButtonClicked(object sender, EventArgs e)
        {
            // Calls the method to handle the naviagtion
            navigationHandler.NextButton();
            // Update the state of the button
            UpdateNavigationButtonsState();
        }

        // Event handler for the refresh button
        private void RefreshButtonClicked(object sender, EventArgs e)
        {
            navigationHandler.RefreshButton();
        }

        // Event handler for the favourite button
        private void FavouriteButtonClicked(object sender, EventArgs e)
        {
            navigationHandler.FavouriteButton();
        }

        // Event handler for the download button
        private async void DownloadButtonClicked(object? sender, EventArgs e)
        {
            // Check if the file chooser button is initialized
            if (fileChooserButton == null)
            {
                Console.WriteLine("FileChooserButton is not initialized.");
                return;
            }

            // Check if a file is selected
            if (string.IsNullOrEmpty(fileChooserButton.Filename))
            {
                // if no predefined file then method is called which will have a default file
                await BulkDownload();
                return;
            }

            string filename = fileChooserButton.Filename;

            // prevents double click
            if ((DateTime.Now - lastDownloadButtonClicked).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double next button click detected. Ignoring.");
                return;
            }

            lastDownloadButtonClicked = DateTime.Now;

            // Bulkdownload with the chosen file
            await BulkDownload(filename);
        }

        // Event handler for home button
        private void HomeButtonClicked(object sender, EventArgs e)
        {
            // Calls the method to navigate to the home page.
            navigationHandler.HomeButton();
        }


        private void AddressBarActivated(object sender, EventArgs e)
        {
            // set current page to 0
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            // prevents double click
            if ((DateTime.Now - lastActivated).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double activation detected. Ignoring.");
                return;
            }

            lastActivated = DateTime.Now;

            if (addressEntry != null)
            {
                // loads the URL entered
                _ = LoadUrl(addressEntry.Text, false);
            }
        }

        // Event handler for selecting a file using the file chooser.
        private void FileButtonFileSet(object sender, EventArgs e)
        {

            if (fileChooserButton != null)
            {
                string filename = fileChooserButton.Filename;
                Console.WriteLine("Selected file: " + filename);
            }

        }

        // Handler for okay button in edit home page dialog
        private void OnOkayButtonClicked(object? sender, EventArgs e)
        {
            if (urlEntry != null)
            {
                // store the url into a variable
                string enteredurl = urlEntry.Text;
                if (HtmlUtility.IsValidUrl(enteredurl))
                {
                    // set it as home page 
                    _homePageURL = enteredurl;
                    // save the url to the file
                    SaveHomePageUrlToFile(enteredurl);
                    if (editHomePageDialog != null)
                    {
                        // hide the dialog
                        editHomePageDialog.Hide();
                    }
                }
                else
                {
                    Console.WriteLine("Invalid URL entered");
                }
            }
            // reinitialized handler with updated settings
            navigationHandler = new NavigationHandler(
                                                browserNotebook: browserNotebook!,
                                                addressEntry: addressEntry!,
                                                contentTextView: contentTextView!,
                                                LoadUrlFunction: LoadUrl,
                                                promptForFavouriteName: PromptForFavouriteName,
                                                historyInstance: browserHistory,
                                                favouriteManager: favouriteManager,
                                                _homePageURL: _homePageURL,
                                                HomePageURL: HomePageURL
                );
        }

        // DIALOGS and their related methods

        // Method to show a dialog for editing the home page URL.
        public void ShowEditHomePageDialog(object sender, EventArgs e)
        {
            if (editHomePageDialog != null)
            {
                // Display the dialog and wait for user action
                _ = editHomePageDialog.Run();
                // Hide the dialog once the user is done
                editHomePageDialog.Hide();
            }
        }
        private static void SaveHomePageUrlToFile(string url)
        {
            // write the URL to the file
            File.WriteAllText(HomePageFilePath, url);
        }

        // load the home page url from file
        private static string LoadHomePageUrlFromFile()
        {
            if (File.Exists(HomePageFilePath))
            {
                // Read and return the URL from the file
                return File.ReadAllText(HomePageFilePath).Trim();
            }
            return DefaultHomePage;
        }

        // Method to show a dialog for editing a favorite item.
        private (string, string) ShowEditFavDialog(string currentName, string currentURL)
        {
            if (editNameEntry == null || editUrlEntry == null || editFavDialog == null)
            {
                return (string.Empty, string.Empty);
            }

            // set the current values in the dialog fields
            editNameEntry.Text = currentName;
            editUrlEntry.Text = currentURL;

            // show the dialog
            editFavDialog.ShowAll();
            var response = editFavDialog.Run();

            // get the new values
            string newName = editNameEntry.Text.Trim();
            string newURL = editUrlEntry.Text.Trim();

            // hide the dialog
            editFavDialog.Hide();
            // if the user clicked ok
            if (response == (int)ResponseType.Ok)
            {
                // update
                return (newName, newURL);
            }
            // return empty values if cancelled
            return (string.Empty, string.Empty);
        }

        private string PromptForFavouriteName()
        {

            // clear each time
            if (favouriteNameEntry != null)
            {
                favouriteNameEntry.Text = "";
            }

            string? favouriteName = null;

            // show the dialog and check the response
            if (favouriteNameDialog != null && favouriteNameEntry != null)
            {
                if (favouriteNameDialog.Run() == (int)ResponseType.Ok && !string.IsNullOrEmpty(favouriteNameEntry.Text))
                {
                    favouriteName = favouriteNameEntry.Text;
                }

                // hide the dialog
                favouriteNameDialog.Hide();
            }

            // Return the favorite name or empty string if none was entered
            return favouriteName ?? "";
        }

        // DISPLAYING FROM MENU ITEMS

        // History 

        // method to display the history
        private void DisplayHistoryList(object sender, EventArgs e)
        {
            // retrieve the history
            List<string> loadedHistory = browserHistory.GetAllHistoryUrls();
            // clear the existing history
            historyStore.Clear();

            // populating the history historyStore
            foreach (var url in loadedHistory)
            {
                _ = historyStore.AppendValues(url);
            }

            // to toggle the history menu item
            if (browserNotebook != null)
            {
                int currentPage = browserNotebook.CurrentPage;
                browserNotebook.CurrentPage = currentPage == 0 ? 1 : 0;
            }
        }

        private void InitializeHistoryTreeView()
        {
            if (historyTreeView != null)
            {
                // set the model
                historyTreeView.Model = historyStore;

                // Create a column for the URL
                TreeViewColumn urlColumn = new() { Title = "History" };

                // Create a renderer for the column data
                CellRendererText urlCell = new();

                // Add the cell renderer to the column and bind the data
                urlColumn.PackStart(urlCell, true);
                urlColumn.AddAttribute(urlCell, "text", 0);

                // Add the column to the TreeView
                historyTreeView.AppendColumn(urlColumn);
            }
        }

        // Event handler for selecting a row in the history TreeView.
        private void HistoryTreeView_RowActivated(object sender, RowActivatedArgs args)
        {
            // get the selected item
            if (historyStore.GetIter(out TreeIter iter, args.Path))
            {
                string url = (string)historyStore.GetValue(iter, 0);
                if (addressEntry != null)
                {
                    // update the address bar and load selected url
                    addressEntry.Text = url;
                    _ = LoadUrl(url, false);
                }
            }
        }

        // Favourites
        // displays list when triggered
        private void DisplayFavouriteList(object sender, EventArgs e)
        {
            FavouriteManager favouriteManager = new();
            List<Favourite> loadedFavorites = favouriteManager.DisplayFavourites();
            favouriteStore.Clear();

            // add each favourite value
            foreach (var fav in loadedFavorites)
            {
                Console.WriteLine("From favorites page" + fav.URL);
                _ = favouriteStore.AppendValues(fav.Name, fav.URL, false, false);
            }

            // toggle between the notebooks on click of menu item
            if (browserNotebook != null)
            {
                int currentPage = browserNotebook.CurrentPage;
                browserNotebook.CurrentPage = currentPage == 0 ? 2 : 0;
            }
        }

        private void InitializeFavouriteTreeView()
        {
            if (favouriteTreeView != null)
            {
                // setting up the columns
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
                editButton.Activatable = true;
                editButton.Toggled += OnEditClicked;
                editColumn.PackStart(editButton, false);
                editColumn.AddAttribute(editButton, "active", 2);

                TreeViewColumn deleteColumn = new() { Title = "Delete" };
                CellRendererToggle deleteButton = new();
                deleteButton.Activatable = true;
                deleteButton.Toggled += OnDeleteClicked;
                deleteColumn.PackStart(deleteButton, false);
                deleteColumn.AddAttribute(deleteButton, "active", 3);
                deleteColumn.FixedWidth = 50;

                // To adjust UI spacing
                TreeViewColumn paddingColumn = new TreeViewColumn();
                paddingColumn.FixedWidth = 20;

                // adding the columns to the tree view
                favouriteTreeView.AppendColumn(nameColumn);
                favouriteTreeView.AppendColumn(urlColumn);
                favouriteTreeView.AppendColumn(editColumn);
                favouriteTreeView.AppendColumn(deleteColumn);
                favouriteTreeView.AppendColumn(paddingColumn);
            }
        }
        private void FavouriteTreeView_RowActivated(object sender, RowActivatedArgs args)
        {
            // getting the row clicked
            if (favouriteStore.GetIter(out TreeIter iter, args.Path))
            {
                // get the url
                string url = (string)favouriteStore.GetValue(iter, 1);
                if (addressEntry != null)
                {
                    // update address entry and load url
                    addressEntry.Text = url;
                    _ = LoadUrl(url, false);
                }
            }
        }

        private void OnEditClicked(object sender, ToggledArgs args)
        {
            // determine which item is treeview is selected
            if (favouriteTreeView != null)
            {
                var selectedRows = favouriteTreeView.Selection.GetSelectedRows();

                if (selectedRows.Length > 0 && favouriteStore.GetIter(out TreeIter iter, selectedRows[0]))
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
        }

        private void OnDeleteClicked(object sender, ToggledArgs args)
        {
            if (favouriteStore.GetIter(out TreeIter iter, new TreePath(args.Path)))
            {
                string name = (string)favouriteStore.GetValue(iter, 0);
                string url = (string)favouriteStore.GetValue(iter, 1);
                Favourite favToDelete = new() { Name = name, URL = url };

                // Remove it from the favourite manager and update JSON
                favouriteManager.RemoveFavourite(favToDelete);

                // Remove the entry from the TreeView
                _ = favouriteStore.Remove(ref iter);
            }
        }

        // To load url
        public async Task<string> LoadUrl(string url, bool isBulkMode)
        {
            // switch to notebook page 0 for display
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            // checks if the entered URL is valid
            if (!HtmlUtility.IsValidUrl(url) && responseLabel != null)
            {
                responseLabel.Text = "Invalid URL";
            }

            // async load of the url
            var result = await NetworkManager.LoadUrl(url);

            // clears messages
            if (responseLabel != null)
            {
                responseLabel.Text = "";
            }

            // if there is an error message, display it in the response Label
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                if (responseLabel != null)
                {
                    responseLabel.Text = result.ErrorMessage;
                }
            }

            // if not in download mode
            if (!isBulkMode)
            {
                // set the text view to display the html response(the body)
                if (contentTextView != null && contentTextView.Buffer != null)
                {
                    contentTextView.Buffer.Text = result.Body;
                }
                // get the title of the page
                string title = HtmlUtility.ExtractTitle(result.Body);

                // display status code, reason phrase and title to the label
                if (responseLabel != null)
                {
                    responseLabel.Text = (int)result.StatusCode + " | " + result.ReasonPhrase + "\n" + title;
                }
                // record visited url
                browserHistory.Visit(url);
                // update button state
                UpdateNavigationButtonsState();
            }
            // when in bulk mode
            else
            {
                // return text that should be displayed
                string displayText = $"{result.StatusCode} {result.ByteCount} {result.Url}";
                Console.WriteLine(displayText);
                return displayText;
            }
            // return empty if not in bulk mode
            return string.Empty;
        }

        public async Task BulkDownload(string filename = "bulk.txt")
        {
            try
            {
                // read the urls from the file
                var urls = GetUrlsFromFile(filename);
                StringBuilder allResults = new();

                foreach (var url in urls)
                {
                    // Load each URL asynchronously in bulk mode and get the response.
                    string displayText = await LoadUrl(url, true);
                    if (contentTextView != null && contentTextView.Buffer != null)
                    {
                        // append it to result
                        _ = allResults.AppendLine(displayText);
                    }

                    // wait before processing next url
                    await Task.Delay(50);
                }

                // update contentTextView.Buffer.Text once all urls are processed
                if (contentTextView != null && contentTextView.Buffer != null)
                {
                    contentTextView.Buffer.Text = allResults.ToString();
                }
            }
            catch (Exception ex)
            {
                if (responseLabel != null)
                {
                    responseLabel.Text = ex.Message;
                }
            }
        }

        private static List<string> GetUrlsFromFile(string filename)
        {
            if (File.Exists(filename))
            {
                // read all urls and return as a list
                return File.ReadAllLines(filename).ToList();
            }
            else
            {
                throw new FileNotFoundException("File not found");
            }
        }

    }

    // Delegate to prompt for a favorite name. Returns a string representing the name.
    public delegate string PromptForFavouriteNameDelegate();
    class NavigationHandler
    {
        // handle button clicks
        private DateTime lastBackButtonClicked;
        private DateTime lastNextButtonClicked;

        // to manage history and favourites
        private readonly History browserHistory;
        private readonly FavouriteManager favouriteManager;

        private readonly Func<string, bool, Task<string>> LoadUrlFunction;
        private readonly Notebook browserNotebook;
        private readonly TextView contentTextView;
        private readonly Entry addressEntry;
        private readonly PromptForFavouriteNameDelegate promptForFavouriteName;

        // home page management
        private readonly string _homePageURL;
        private readonly string HomePageURL;

        // constructor
        public NavigationHandler(Notebook browserNotebook,
                                Entry addressEntry,
                                TextView contentTextView,
                                string _homePageURL,
                                string HomePageURL,
                                Func<string, bool,
                                Task<string>> LoadUrlFunction,
                                PromptForFavouriteNameDelegate promptForFavouriteName,
                                History? historyInstance = null,
                                FavouriteManager? favouriteManager = null
                                )
        {
            this.browserNotebook = browserNotebook;
            this.addressEntry = addressEntry;
            this.contentTextView = contentTextView;
            this.LoadUrlFunction = LoadUrlFunction;
            this.browserHistory = historyInstance ?? new History();
            this.favouriteManager = favouriteManager ?? new FavouriteManager();
            this.promptForFavouriteName = promptForFavouriteName;
            this._homePageURL = _homePageURL;
            this.HomePageURL = HomePageURL;

        }

        // to get and set the time stamps
        public DateTime LastBackButtonClicked
        {
            get => lastBackButtonClicked;
            set => lastBackButtonClicked = value;
        }

        public DateTime LastNextButtonClicked
        {
            get => lastNextButtonClicked;
            set => lastNextButtonClicked = value;
        }

        // handles back button logic
        public void BackButton()
        {
            // set page to 0
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            // ignore double clicks
            if ((DateTime.Now - lastBackButtonClicked).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double back button click detected. Ignoring.");
                return;
            }

            lastBackButtonClicked = DateTime.Now;

            // if its possible to move back load prev url
            if (browserHistory.MoveBack())
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = browserHistory.CurrentUrl;
                    LoadUrlFunction(addressEntry.Text, false);
                }
            }
        }

        // to handle next logic
        public void NextButton()
        {
            // notebook page 0
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            // ignore double clicks
            if ((DateTime.Now - lastNextButtonClicked).TotalMilliseconds < 500)
            {
                Console.WriteLine("Double next button click detected. Ignoring.");
                return;
            }

            lastNextButtonClicked = DateTime.Now;

            // if can move forward load next url
            if (browserHistory.MoveForward())
            {
                if (addressEntry != null)
                {
                    addressEntry.Text = browserHistory.CurrentUrl;
                    LoadUrlFunction(addressEntry.Text, false);
                }
            }
        }
        public void RefreshButton()
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            // clears text view to give refresh feel
            if (contentTextView != null)
            {
                contentTextView.Buffer.Text = "";
            }

            // calls current url if available
            string currentUrl = browserHistory.CurrentUrl;
            if (addressEntry != null)
            {
                addressEntry.Text = currentUrl;
                LoadUrlFunction(addressEntry.Text, false);
            }

            else if (addressEntry != null && string.IsNullOrEmpty(addressEntry.Text))
            {
                addressEntry.Text = HomePageURL;
                LoadUrlFunction(addressEntry.Text, false);
            }
        }

        // handles home button logiv
        public void HomeButton()
        {
            if (browserNotebook != null)
            {
                browserNotebook.CurrentPage = 0;
            }

            if (addressEntry != null)
            {
                // load the stored url
                addressEntry.Text = _homePageURL;
                LoadUrlFunction(addressEntry.Text, false);
            }
        }
        public void FavouriteButton()
        {
            // Check if the addressEntry has a value, if not, just return
            if (addressEntry == null || string.IsNullOrEmpty(addressEntry.Text))
            {
                Console.WriteLine("No URL available to add to favourites.");
                return;
            }
            string favouriteName = promptForFavouriteName();

            // Create a new favourite item
            var favouriteItem = new Favourite()
            {
                Name = favouriteName,
                URL = addressEntry.Text
            };

            // Add to favourites and save
            favouriteManager.AddFavourite(favouriteItem);

            List<Favourite> favList = favouriteManager.DisplayFavourites();
            FavouriteStorage.SaveFavorites(favList);
        }
    }

}
