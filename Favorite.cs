using System.Text.Json;

namespace favourites
{
    // Class representing each favourite item
    class Favourite
    {
        public string? Name { get; set; }
        public string? URL { get; set; }
    }

    // class for storing and saving the list of favourites
    class FavouriteStorage
    {
        static string appName = System.IO.Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
        private static string favoriteFile;
        // private const string favoriteFile = "favourites.json";

        static FavouriteStorage()
        {
            favoriteFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, "favourites.json");
        }

        // static method that takes a list of favourite objects
        public static void SaveFavorites(List<Favourite> favouriteList)
        {
            if (!Directory.Exists(Path.GetDirectoryName(favoriteFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(favoriteFile));
            }
            try
            {
                // serialize the list of favourites to a json string
                string json = JsonSerializer.Serialize(favouriteList);
                // write the string to the file
                File.WriteAllText(favoriteFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save favourite. Error:" + ex.Message);
            }

        }

        // a method that returns a list of favourite objects
        public static List<Favourite> LoadFavorites()
        {
            try
            {
                if (File.Exists(favoriteFile))
                {
                    // reads all the text from the file
                    string json = File.ReadAllText(favoriteFile);
                    // deserializes the list 
                    return JsonSerializer.Deserialize<List<Favourite>>(json) ?? new List<Favourite>();
                }
                // if the file does not exist, return a new list
                return new List<Favourite>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load favourites. Error: " + ex.Message);
            }
            return new List<Favourite>();

        }
    }

    // a class to manage the favourite items
    class FavouriteManager
    {
        // to store the list of favourites
        private readonly List<Favourite> favouriteList = new();
        // declares a readonly field storage of type FavouriteStorage
        private readonly FavouriteStorage storage;
        public FavouriteManager()
        {
            // initializes storage
            storage = new FavouriteStorage();
            // load favourites from storage into list
            favouriteList = FavouriteStorage.LoadFavorites();
        }

        // Simply returns the list
        public List<Favourite> DisplayFavourites()
        {
            return favouriteList;
        }

        // a method to add a new favourite
        public void AddFavourite(Favourite item)
        {
            if (item != null)
            {
                // ensures the list already doesnt have that favourite
                if (!favouriteList.Any(fav => fav.URL == item.URL))
                {
                    favouriteList.Add(item);
                    FavouriteStorage.SaveFavorites(favouriteList);
                }
                // if its already present print a message
                else
                {
                    Console.WriteLine("URL already present");
                }
            }
        }

        // a method to remove a favourite
        public void RemoveFavourite(Favourite item)
        {
            // Remove the item that matches the Name and URL
            favouriteList.RemoveAll(fav => fav.Name == item.Name && fav.URL == item.URL);
            // save the updated list
            FavouriteStorage.SaveFavorites(favouriteList);
        }

        // a method to modify an existing item
        public void ModifyFavourite(string name, string url, Favourite item)
        {
            // finds the index of the item to be modify
            var favItemIndex = favouriteList.FindIndex(fav => fav.Name == item.Name && fav.URL == item.URL);
            // if item is found
            if (favItemIndex != -1)
            {
                // update the name and URL and save it to file
                favouriteList[favItemIndex].Name = name;
                favouriteList[favItemIndex].URL = url;
                FavouriteStorage.SaveFavorites(favouriteList);
            }
        }
    }
}