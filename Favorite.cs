using System;

using System.Text.Json;
using Gtk;

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
            favouriteList.Add(item);
            storage.SaveFavorites(favouriteList);
        }
        public void RemoveFavourite(Favourite item)
        {
            favouriteList.Remove(item);
            storage.SaveFavorites(favouriteList);
        }
        public void ModifyFavourite(string name, string url, Favourite item)
        {
            if (favouriteList.Contains(item))
            {
                item.Name = name;
                item.URL = url;
                storage.SaveFavorites(favouriteList);
            }
        }
       

    }




}