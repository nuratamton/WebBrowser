using System.Text.Json;
using System.Linq;

namespace favourites
{
    class Favourite
    {
        public string? Name { get; set; }
        public string? URL { get; set; }

        public Favourite()
        { }
    }

    class FavouriteStorage
    {
        private const string favoriteFile = "favourites.json";

        public void SaveFavorites(List<Favourite> favouriteList)
        {
            string json = JsonSerializer.Serialize(favouriteList);
            File.WriteAllText(favoriteFile, json);
        }

        public List<Favourite> LoadFavorites()
        {
            if (File.Exists(favoriteFile))
            {
                string json = File.ReadAllText(favoriteFile);
                return JsonSerializer.Deserialize<List<Favourite>>(json) ?? new List<Favourite>();
            }
            return new List<Favourite>();
            // DisplayFavourites();
        }
    }

    class FavouriteManager
    {
        private List<Favourite> favouriteList = new List<Favourite>();
        private readonly FavouriteStorage storage;
        public FavouriteManager()
        {
            storage = new FavouriteStorage();
            favouriteList = storage.LoadFavorites();
        }

        public List<Favourite> DisplayFavourites()
        {
            return favouriteList;
        }
        
        public void AddFavourite(Favourite item)
        {
            if (item != null)
            {
                if (!favouriteList.Any(fav => fav.URL == item.URL))
                {
                    favouriteList.Add(item);
                    storage.SaveFavorites(favouriteList);
                }
                else{
                    Console.WriteLine("URL already present");
                }
            }
        }

        public void RemoveFavourite(Favourite item)
        {
            favouriteList.RemoveAll(fav => fav.Name == item.Name && fav.URL == item.URL);
            storage.SaveFavorites(favouriteList);
        }
        public void ModifyFavourite(string name, string url, Favourite item)
        {
            var favItemIndex = favouriteList.FindIndex(fav => fav.Name == item.Name && fav.URL == item.URL);
            if (favItemIndex != -1)
            {
                favouriteList[favItemIndex].Name = name;
                favouriteList[favItemIndex].URL = url;
                storage.SaveFavorites(favouriteList);
            }
        }
    }
}